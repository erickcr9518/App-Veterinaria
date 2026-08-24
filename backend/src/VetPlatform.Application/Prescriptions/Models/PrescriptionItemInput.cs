namespace VetPlatform.Application.Prescriptions.Models;

public record PrescriptionItemInput(
    string ProductName,
    string? Concentration,
    string? Presentation,
    string Quantity,
    string Route,
    string Frequency,
    string Duration,
    string? Instructions);
