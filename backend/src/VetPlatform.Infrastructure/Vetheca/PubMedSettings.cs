namespace VetPlatform.Infrastructure.Vetheca;

public class PubMedSettings
{
    public const string SectionName = "PubMed";

    public string BaseUrl { get; set; } = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/";
    public string ApiKey { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
}
