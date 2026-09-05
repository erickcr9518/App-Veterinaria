using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Infrastructure.Vetheca;

// First slice of Vetheca (see docs/VETIA_CLINIC_ANALYSIS.md, section J):
// a plain keyword search against PubMed's public E-utilities, no LLM
// involved. esearch finds matching PMIDs, efetch returns the structured
// article XML (title, authors, journal, year, abstract) for those PMIDs.
public class PubMedClient : IPubMedClient
{
    private readonly HttpClient _httpClient;
    private readonly PubMedSettings _settings;
    private readonly ILogger<PubMedClient> _logger;

    public PubMedClient(HttpClient httpClient, IOptions<PubMedSettings> settings, ILogger<PubMedClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PubMedArticleDto>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken)
    {
        var pmids = await SearchPmidsAsync(query, maxResults, cancellationToken);
        if (pmids.Count == 0)
        {
            return Array.Empty<PubMedArticleDto>();
        }

        return await FetchArticlesAsync(pmids, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> SearchPmidsAsync(string query, int maxResults, CancellationToken cancellationToken)
    {
        var url = BuildUrl("esearch.fcgi", new Dictionary<string, string>
        {
            ["db"] = "pubmed",
            ["term"] = query,
            ["retmax"] = maxResults.ToString(),
            ["retmode"] = "json",
            ["sort"] = "relevance",
        });

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("esearchresult", out var result) ||
            !result.TryGetProperty("idlist", out var idList))
        {
            return Array.Empty<string>();
        }

        return idList.EnumerateArray().Select(id => id.GetString() ?? string.Empty)
            .Where(id => id.Length > 0)
            .ToArray();
    }

    private async Task<IReadOnlyList<PubMedArticleDto>> FetchArticlesAsync(IReadOnlyList<string> pmids, CancellationToken cancellationToken)
    {
        var url = BuildUrl("efetch.fcgi", new Dictionary<string, string>
        {
            ["db"] = "pubmed",
            ["id"] = string.Join(",", pmids),
            ["rettype"] = "abstract",
            ["retmode"] = "xml",
        });

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            var document = XDocument.Parse(xml);
            return document.Descendants("PubmedArticle")
                .Select(ParseArticle)
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo interpretar la respuesta XML de PubMed para los PMIDs {Pmids}", string.Join(",", pmids));
            return Array.Empty<PubMedArticleDto>();
        }
    }

    private static PubMedArticleDto ParseArticle(XElement pubmedArticle)
    {
        var medlineCitation = pubmedArticle.Element("MedlineCitation");
        var article = medlineCitation?.Element("Article");
        var journal = article?.Element("Journal");
        var pubDate = journal?.Element("JournalIssue")?.Element("PubDate");

        var pmid = medlineCitation?.Element("PMID")?.Value ?? string.Empty;
        var title = article?.Element("ArticleTitle")?.Value ?? "(sin título)";
        var journalTitle = journal?.Element("Title")?.Value ?? journal?.Element("ISOAbbreviation")?.Value;
        var year = pubDate?.Element("Year")?.Value ?? pubDate?.Element("MedlineDate")?.Value;

        var authors = article?.Element("AuthorList")?.Elements("Author")
            .Select(author =>
            {
                var lastName = author.Element("LastName")?.Value;
                var initials = author.Element("Initials")?.Value;
                if (!string.IsNullOrWhiteSpace(lastName))
                {
                    return string.IsNullOrWhiteSpace(initials) ? lastName : $"{lastName} {initials}";
                }

                return author.Element("CollectiveName")?.Value;
            })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            ?? Enumerable.Empty<string>();

        var abstractText = article?.Element("Abstract")?.Elements("AbstractText")
            .Select(section =>
            {
                var label = section.Attribute("Label")?.Value;
                return string.IsNullOrWhiteSpace(label) ? section.Value : $"{label}: {section.Value}";
            });

        return new PubMedArticleDto
        {
            Pmid = pmid,
            Title = title,
            Authors = string.Join(", ", authors),
            Journal = journalTitle,
            Year = year,
            AbstractText = abstractText is null ? null : string.Join(" ", abstractText),
            Url = $"https://pubmed.ncbi.nlm.nih.gov/{pmid}/",
        };
    }

    private string BuildUrl(string endpoint, Dictionary<string, string> queryParams)
    {
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            queryParams["api_key"] = _settings.ApiKey;
        }

        if (!string.IsNullOrWhiteSpace(_settings.ContactEmail))
        {
            queryParams["email"] = _settings.ContactEmail;
            queryParams["tool"] = "VetPlatform-Vetheca";
        }

        var query = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        return $"{endpoint}?{query}";
    }
}
