namespace VetPlatform.Application.Common.Interfaces;

public interface IPdfTextExtractor
{
    // One entry per page, in order, 1-indexed by position (index 0 = page 1).
    // Throws PdfExtractionException if the bytes aren't a readable PDF.
    IReadOnlyList<string> ExtractPageTexts(byte[] fileBytes);
}

public class PdfExtractionException : Exception
{
    public PdfExtractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
