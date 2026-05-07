using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DocumentService.Services;

public record CitizenData(
    string FullName,
    string NationalId,
    DateOnly DateOfBirth,
    string Address,
    string City,
    string Gender,
    string Email,
    string PhoneNumber
);

public interface IPdfGeneratorService
{
    byte[] GenerateDocument(string documentType, CitizenData citizen, string referenceNumber, DateTime? expiresAt, bool isDraft = false);
}

public class PdfGeneratorService : IPdfGeneratorService
{
    private static readonly Dictionary<string, string> DocumentTitles = new()
    {
        ["BirthCertificate"] = "BIRTH CERTIFICATE",
        ["NationalId"] = "NATIONAL IDENTITY CARD",
        ["MarriageCertificate"] = "MARRIAGE CERTIFICATE",
        ["DeathCertificate"] = "DEATH CERTIFICATE",
        ["DrivingLicense"] = "DRIVING LICENSE",
    };

    public byte[] GenerateDocument(string documentType, CitizenData citizen, string referenceNumber, DateTime? expiresAt, bool isDraft = false)
    {
        var title = DocumentTitles.GetValueOrDefault(documentType, "OFFICIAL DOCUMENT");
        var fields = GetFieldsForType(documentType, citizen);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(50);
                page.MarginVertical(40);

                page.Header().Element(h => ComposeHeader(h, title));
                page.Content().Element(c => ComposeContent(c, fields, isDraft));
                page.Footer().Element(f => ComposeFooter(f, referenceNumber, expiresAt));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, string title)
    {
        container.Column(col =>
        {
            col.Spacing(5);

            col.Item().AlignCenter().Text("REPUBLIC OF DEMOCRIA")
                .FontSize(14).Bold().FontColor(Colors.Grey.Darken3);

            col.Item().AlignCenter().Text("Ministry of Civil Affairs")
                .FontSize(10).FontColor(Colors.Grey.Darken1);

            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            col.Item().AlignCenter().Text(title)
                .FontSize(22).Bold().FontColor(Colors.Blue.Darken3);

            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            col.Item().Height(10);
        });
    }

    private static void ComposeContent(IContainer container, List<(string Label, string Value)> fields, bool isDraft)
    {
        container.Column(col =>
        {
            col.Spacing(8);

            if (isDraft)
            {
                col.Item().AlignCenter().PaddingBottom(10)
                    .Text("— DRAFT PREVIEW —")
                    .FontSize(16).Bold().FontColor(Colors.Red.Medium);
            }

            foreach (var (label, value) in fields)
            {
                if (string.IsNullOrEmpty(label))
                {
                    col.Item().Height(10);
                    continue;
                }

                col.Item().Row(row =>
                {
                    row.RelativeItem(1).Text(label + ":")
                        .FontSize(11).Bold().FontColor(Colors.Grey.Darken2);
                    row.RelativeItem(2).Text(value)
                        .FontSize(11).FontColor(Colors.Black);
                });
            }

            col.Item().Height(20);
            col.Item().PaddingVertical(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            col.Item().Height(5);

            col.Item().Text("This document is issued by the Republic of Democria and is valid for all legal purposes within the jurisdiction.")
                .FontSize(9).FontColor(Colors.Grey.Darken1).Italic();

            if (isDraft)
            {
                col.Item().PaddingTop(10).AlignCenter()
                    .Text("This is a preview only. The final document will be generated upon approval.")
                    .FontSize(9).Bold().FontColor(Colors.Red.Medium);
            }
        });
    }

    private static void ComposeFooter(IContainer container, string referenceNumber, DateTime? expiresAt)
    {
        container.Column(col =>
        {
            col.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

            col.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Reference: ").FontSize(8).Bold();
                    text.Span(referenceNumber).FontSize(8);
                });

                row.RelativeItem().AlignCenter().Text(text =>
                {
                    text.Span("Issued: ").FontSize(8).Bold();
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd")).FontSize(8);
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Expires: ").FontSize(8).Bold();
                    text.Span(expiresAt?.ToString("yyyy-MM-dd") ?? "No expiry").FontSize(8);
                });
            });

            col.Item().PaddingTop(5).AlignCenter()
                .Text("Civil Registry Office — Republic of Democria")
                .FontSize(7).FontColor(Colors.Grey.Darken1);
        });
    }

    private static List<(string Label, string Value)> GetFieldsForType(string documentType, CitizenData c)
    {
        return documentType switch
        {
            "BirthCertificate" => new()
            {
                ("Full Name", c.FullName),
                ("Date of Birth", c.DateOfBirth.ToString("MMMM d, yyyy")),
                ("Place of Birth", c.City + ", Democria"),
                ("Gender", c.Gender),
                ("National ID", c.NationalId),
                ("", ""),
                ("Father", "[On Record]"),
                ("Mother", "[On Record]"),
            },
            "NationalId" => new()
            {
                ("Full Name", c.FullName),
                ("Date of Birth", c.DateOfBirth.ToString("MMMM d, yyyy")),
                ("Gender", c.Gender),
                ("National ID Number", c.NationalId),
                ("", ""),
                ("Address", c.Address),
                ("City", c.City),
                ("Phone", c.PhoneNumber),
                ("Nationality", "Democrian"),
            },
            "MarriageCertificate" => new()
            {
                ("Spouse 1", c.FullName),
                ("Spouse 2", "[Partner Name — On Record]"),
                ("Date of Marriage", "[On Record]"),
                ("Place of Marriage", c.City + ", Democria"),
                ("", ""),
                ("National ID (Spouse 1)", c.NationalId),
                ("Officiant", "[Registered Officiant]"),
            },
            "DeathCertificate" => new()
            {
                ("Full Name of Deceased", c.FullName),
                ("Date of Birth", c.DateOfBirth.ToString("MMMM d, yyyy")),
                ("Date of Death", "[On Record]"),
                ("Place of Death", c.City + ", Democria"),
                ("", ""),
                ("National ID", c.NationalId),
                ("Cause of Death", "[On Record]"),
            },
            "DrivingLicense" => new()
            {
                ("Full Name", c.FullName),
                ("Date of Birth", c.DateOfBirth.ToString("MMMM d, yyyy")),
                ("National ID", c.NationalId),
                ("", ""),
                ("Address", c.Address),
                ("City", c.City),
                ("License Class", "B — Passenger Vehicles"),
            },
            _ => new()
            {
                ("Full Name", c.FullName),
                ("National ID", c.NationalId),
            }
        };
    }
}
