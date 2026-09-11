using UglyToad.PdfPig;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Infrastructure.Vetheca;

// Uses page.Text (PdfPig's simple reading-order-agnostic extraction) rather
// than a layout-analysis extension - good enough for the mostly single/
// double-column text of veterinary manuals and textbooks, and keeps this
// slice dependency-light. Revisit with DocumentLayoutAnalysis if real
// uploaded documents come back with garbled ordering.
public class PdfPigTextExtractor : IPdfTextExtractor
{
    public IReadOnlyList<string> ExtractPageTexts(byte[] fileBytes)
    {
        try
        {
            using var document = PdfDocument.Open(fileBytes);
            return document.GetPages().Select(page => page.Text).ToArray();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new PdfExtractionException("No se pudo leer el archivo como un PDF válido.", ex);
        }
    }
}
