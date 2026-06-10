using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DocumentService.Services.Templates;

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
        if (documentType == "NationalId")
            return NationalIdDocumentTemplate.Generate(citizen, referenceNumber, expiresAt, isDraft);

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


    // ── Generic header (used by all other document types) ──
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
                ("", ""),
                ("National ID", c.NationalId),
                ("Father's Name", "[On Record]"),
                ("Mother's Name", "[On Record]"),
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
    private static byte[] GenerateCipsDocument(CitizenData citizen, string referenceNumber, DateTime? expiresAt, bool isDraft)
{
    var nameParts = citizen.FullName.Trim().Split(' ', 2);
    var firstName = nameParts[0].ToUpperInvariant();
    var lastName = nameParts.Length > 1 ? nameParts[1].ToUpperInvariant() : "";

    var document = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(50);
            page.MarginVertical(40);

            page.Content().Column(col =>
            {
                col.Spacing(0);

                col.Item().AlignRight().Text("PBA-3/ПБА-3").FontSize(8).FontColor(Colors.Grey.Darken1);
                col.Item().Height(10);

                col.Item().Column(inner =>
                {
                    inner.Item().Text("Ministry of Civil Affairs, Republic of Democria").FontSize(10).Bold();
                    inner.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                    inner.Item().Text("/Naziv organa — Назив органа/").FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                });

                col.Item().Height(12);

                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text(text =>
                        {
                            text.Span("Broj/Број: ").FontSize(9);
                            text.Span(referenceNumber).FontSize(9).Bold();
                        });
                        inner.Item().Text(text =>
                        {
                            text.Span("Datum/Датум: ").FontSize(9);
                            text.Span(DateTime.UtcNow.ToString("dd.MM.yyyy") + " god/год.").FontSize(9);
                        });
                    });
                });

                col.Item().Height(16);

                col.Item().Text(
                    "Na osnovu/temelju člana/članka 26. Zakona o jedinstvenom matičnom broju (\"Sl. Glasnik BiH\" br. 32/01) " +
                    "i člana 12. Zakona o centralnoj evidenciji i razmjeni podataka (\"Sl. Glasnik BiH\" br. 32/01 i 16/02), na zahtjev / " +
                    "На основу члана 26. Закона о јединственом матичном броју (\"Сл. Гласник БиХ\" бр. 32/01) и члана 12. Закона о " +
                    "централној евиденцији и размјени података (\"Сл. Гласник БиХ\" бр. 32/01 и 16/02), на захтјев"
                ).FontSize(8).FontColor(Colors.Grey.Darken2);

                col.Item().Height(12);

                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Ime:/ Име: ").FontSize(10);
                        text.Span(firstName).FontSize(10).Bold();
                    });
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Prezime: /Презиме: ").FontSize(10);
                        text.Span(lastName).FontSize(10).Bold();
                    });
                });

                col.Item().Height(6);
                col.Item().Text("izdaje se/издаје се").FontSize(9).Italic();
                col.Item().Height(10);

                col.Item().AlignCenter().Text("OBAVJEŠTENJE / OBAVIJEST / ОБАВЈЕШТЕЊЕ").FontSize(16).Bold();
                col.Item().Height(8);
                col.Item().AlignCenter().Text(
                    "da je uveden u evidenciju prebivališta-boravišta sa ličnim/osobnim podacima / да је\n" +
                    "уведен у евиденцију пребивалишта-боравишта са личним подацима"
                ).FontSize(9).Italic();

                col.Item().Height(16);

                col.Item().Row(row =>
                {
                    // Left column
                    row.RelativeItem().Column(left =>
                    {
                        left.Spacing(6);

                        left.Item().Text(text => { text.Span("JMB/JMБ: ").FontSize(9); text.Span(citizen.NationalId).FontSize(9).Bold(); });
                        left.Item().Text(text => { text.Span("Ime/Име: ").FontSize(9); text.Span(firstName).FontSize(9).Bold(); });
                        left.Item().Text(text => { text.Span("Prezime/Презиме: ").FontSize(9); text.Span(lastName).FontSize(9).Bold(); });
                        left.Item().Text(text =>
                        {
                            text.Span("Spol/Пол: ").FontSize(9);
                            text.Span(citizen.Gender.Length > 0 ? citizen.Gender[0].ToString().ToUpperInvariant() : "—").FontSize(9).Bold();
                        });
                        left.Item().Column(inner =>
                        {
                            inner.Item().Text("Datum rođenja/").FontSize(9);
                            inner.Item().Text(text => { text.Span("Датум рођења: ").FontSize(9); text.Span(citizen.DateOfBirth.ToString("dd.MM.yyyy")).FontSize(9).Bold(); });
                        });
                        left.Item().Column(inner =>
                        {
                            inner.Item().Text("Općina rođenja/").FontSize(9);
                            inner.Item().Text(text => { text.Span("Општина рођења: ").FontSize(9); text.Span(citizen.City.ToUpperInvariant()).FontSize(9).Bold(); });
                        });
                        left.Item().Column(inner =>
                        {
                            inner.Item().Text("Mjesto rođenja/").FontSize(9);
                            inner.Item().Text(text => { text.Span("Мјесто рођења: ").FontSize(9); text.Span(citizen.City.ToUpperInvariant()).FontSize(9).Bold(); });
                        });
                        left.Item().Column(inner =>
                        {
                            inner.Item().Text("Mjesto prebivališta/").FontSize(9);
                            inner.Item().Text(text => { text.Span("Мјесто пребивалишта: ").FontSize(9); text.Span(citizen.City.ToUpperInvariant()).FontSize(9).Bold(); });
                        });
                        left.Item().Column(inner =>
                        {
                            inner.Item().Text("Poštanski broj/").FontSize(9);
                            inner.Item().Text("Поштански број:").FontSize(9);
                        });
                    });

                    row.ConstantItem(20);

                    // Right column
                    row.RelativeItem().Column(right =>
                    {
                        right.Spacing(6);

                        right.Item().Column(inner =>
                        {
                            inner.Item().Text("Općina prebivališta/").FontSize(9);
                            inner.Item().Text(text => { text.Span("Општина пребивалишта: ").FontSize(9); text.Span(citizen.City.ToUpperInvariant()).FontSize(9).Bold(); });
                        });
                        right.Item().Column(inner =>
                        {
                            inner.Item().Text("Adresa prebivališta/ Адреса").FontSize(9);
                            inner.Item().Text(text => { text.Span("пребивалишта: ").FontSize(9); text.Span(citizen.Address.ToUpperInvariant()).FontSize(9).Bold(); });
                        });
                        right.Item().Text(text => { text.Span("Entitet/Ентитет: ").FontSize(9); text.Span("FEDERACIJA BOSNE I HERCEGOVINE").FontSize(9).Bold(); });
                        right.Item().Text(text => { text.Span("Kanton/Кантон: ").FontSize(9); text.Span("Not provided").FontSize(9).Bold(); });
                        right.Item().Column(inner =>
                        {
                            inner.Item().Text("Državljanstvo/").FontSize(9);
                            inner.Item().Text(text => { text.Span("Држављанство: ").FontSize(9); text.Span("BOSNA I HERCEGOVINA").FontSize(9).Bold(); });
                        });
                        right.Item().Column(inner =>
                        {
                            inner.Item().Text("Promjena imena/").FontSize(9);
                            inner.Item().Text("Промјена имена:").FontSize(9);
                        });
                        right.Item().Column(inner =>
                        {
                            inner.Item().Text("Vrsta prebivališta/ Врста").FontSize(9);
                            inner.Item().Text(text => { text.Span("пребивалишта: ").FontSize(9); text.Span("STALNO").FontSize(9).Bold(); });
                        });
                    });
                });

                col.Item().Height(20);
                col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                col.Item().Height(8);

                col.Item().Text(
                    "Ovo obavještenje/obavijest izdaje se prema podacima iz evidencije prebivališta - boravišta koja se vodi kod ovog Organa / " +
                    "Ово обавјештење издаје се према подацима из евиденције пребивалишта - боравишта која се води код овог Органа."
                ).FontSize(8).FontColor(Colors.Grey.Darken2);

                col.Item().Height(30);

                col.Item().Row(row =>
                {
                    row.RelativeItem();
                    row.RelativeItem().AlignCenter().Column(sig =>
                    {
                        sig.Item().LineHorizontal(0.5f).LineColor(Colors.Black);
                        sig.Item().AlignCenter().Text("/Potpisno službenog lica-osobe\nПотпис службеног лица").FontSize(8).Italic();
                    });
                });

                col.Item().Height(30);
                col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                col.Item().Height(6);

                if (isDraft)
                {
                    col.Item().AlignCenter().Text("— DRAFT PREVIEW — NOT AN OFFICIAL DOCUMENT —")
                        .FontSize(12).Bold().FontColor(Colors.Red.Medium);
                    col.Item().Height(4);
                }

                col.Item().Text(
                    "Lični/osobni podaci iz ovog obrasca bit će obrađeni u svrhe određene Zakonom o prebivalištu i boravištu državljana BiH " +
                    "i biti predmetom prava i zaštite propisane Zakonom o zaštiti ličnih/osobnih podataka. / " +
                    "Лични подаци из овог обрасца биће обрађени у сврхе одређене Законом о пребивалишту и боравишту држављана БиХ " +
                    "и биће предметом права и заштите прописане Законом о заштити личних података."
                ).FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });
    });

    return document.GeneratePdf();
}
}
