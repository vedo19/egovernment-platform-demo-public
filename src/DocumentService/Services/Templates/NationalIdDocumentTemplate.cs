using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DocumentService.Services;

namespace DocumentService.Services.Templates;

public static class NationalIdDocumentTemplate
{
// ── CIPS-style National ID document ──
    public static byte[] Generate(CitizenData citizen, string referenceNumber, DateTime? expiresAt, bool isDraft)
    {
        // Split full name into first/last
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

                    // ── Top right form number ──
                    col.Item().AlignRight()
                        .Text("PBA-3/ПБА-3")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);

                    col.Item().Height(10);

                    // ── Issuing organ ──
                    col.Item().Column(inner =>
                    {
                        inner.Item().Text("Ministry of Civil Affairs, Republic of Democria")
                            .FontSize(10).Bold();
                        inner.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                        inner.Item().Text("/Naziv organa — Назив органа/")
                            .FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                    });

                    col.Item().Height(12);

                    // ── Document number and date ──
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

                    // ── Legal basis paragraph ──
                    col.Item().Text(
                        "Na osnovu/temelju člana/članka 26. Zakona o jedinstvenom matičnom broju (\"Sl. Glasnik BiH\" br. 32/01) " +
                        "i člana 12. Zakona o centralnoj evidenciji i razmjeni podataka (\"Sl. Glasnik BiH\" br. 32/01 i 16/02), na zahtjev / " +
                        "На основу члана 26. Закона о јединственом матичном броју (\"Сл. Гласник БиХ\" бр. 32/01) и члана 12. Закона о " +
                        "централној евиденцији и размјени података (\"Сл. Гласник БиХ\" бр. 32/01 и 16/02), на захтјев"
                    ).FontSize(8).FontColor(Colors.Grey.Darken2);

                    col.Item().Height(12);

                    // ── Name line ──
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

                    // ── Main title ──
                    col.Item().AlignCenter().Text("OBAVJEŠTENJE / OBAVIJEST / ОБАВЈЕШТЕЊЕ")
                        .FontSize(16).Bold();

                    col.Item().Height(8);

                    col.Item().AlignCenter().Text(
                        "da je uveden u evidenciju prebivališta-boravišta sa ličnim/osobnim podacima / да је\n" +
                        "уведен у евиденцију пребивалишта-боравишта са личним подацима"
                    ).FontSize(9).Italic();

                    col.Item().Height(16);

                    // ── Two-column data grid ──
                    col.Item().Row(row =>
                    {
                        // Left column
                        row.RelativeItem().Column(left =>
                        {
                            left.Spacing(6);

                            left.Item().Text(text =>
                            {
                                text.Span("JMB/JMБ: ").FontSize(9);
                                text.Span(citizen.NationalId).FontSize(9).Bold();
                            });

                            left.Item().Text(text =>
                            {
                                text.Span("Ime/Име: ").FontSize(9);
                                text.Span(firstName).FontSize(9).Bold();
                            });

                            left.Item().Text(text =>
                            {
                                text.Span("Prezime/Презиме: ").FontSize(9);
                                text.Span(lastName).FontSize(9).Bold();
                            });

                            left.Item().Text(text =>
                            {
                                text.Span("Spol/Пол: ").FontSize(9);
                                text.Span(citizen.Gender.Length > 0 ? citizen.Gender[0].ToString().ToUpperInvariant() : "—").FontSize(9).Bold();
                            });

                            left.Item().Column(inner =>
                            {
                                inner.Item().Text("Datum rođenja/").FontSize(9);
                                inner.Item().Text(text =>
                                {
                                    text.Span("Датум рођења: ").FontSize(9);
                                    text.Span(citizen.DateOfBirth.ToString("dd.MM.yyyy")).FontSize(9).Bold();
                                });
                            });

                            left.Item().Column(inner =>
                            {
                                inner.Item().Text("Općina rođenja/").FontSize(9);
                                inner.Item().Text(text =>
                                {
                                    text.Span("Општина рођења: ").FontSize(9);
                                    text.Span(citizen.City.ToUpperInvariant()).FontSize(9).Bold();
                                });
                            });

                            left.Item().Column(inner =>
                            {
                                inner.Item().Text("Mjesto rođenja/").FontSize(9);
                                inner.Item().Text(text =>
                                {
                                    text.Span("Мјесто рођења: ").FontSize(9);
                                    text.Span(citizen.City.ToUpperInvariant()).FontSize(9).Bold();
                                });
                            });

                            left.Item().Column(inner =>
                            {
                                inner.Item().Text("Mjesto prebivališta/").FontSize(9);
                                inner.Item().Text(text =>
                                {
                                    text.Span("Мјесто пребивалишта: ").FontSize(9);
                                    text.Span(citizen.City.ToUpperInvariant()).FontSize(9).Bold();
                                });
                            });

                            left.Item().Column(inner =>
                            {
                                inner.Item().Text("Poštanski broj/").FontSize(9);
                                inner.Item().Text("Поштански број: ").FontSize(9);
                            });
                        });

                        row.ConstantItem(20); // spacer

                        // Right column
                        row.RelativeItem().Column(right =>
                        {
                            right.Spacing(6);

                            right.Item().Column(inner =>
                            {
                                inner.Item().Text("Općina prebivališta/").FontSize(9);
                                inner.Item().Text(text =>
                                {
                                    text.Span("Општина пребивалишта: ").FontSize(9);
                                    text.Span(citizen.City.ToUpperInvariant()).FontSize(9).Bold();
                                });
                            });

                            right.Item().Column(inner =>
                            {
                                inner.Item().Text("Adresa prebivališta/ Адреса").FontSize(9);
                                inner.Item().Text(text =>
                                {
                                    text.Span("пребивалишта: ").FontSize(9);
                                    text.Span(citizen.Address.ToUpperInvariant()).FontSize(9).Bold();
                                });
                            });

                            right.Item().Text(text =>
                            {
                                text.Span("Entitet/Ентитет: ").FontSize(9);
                                text.Span("FEDERACIJA BOSNE I HERCEGOVINE").FontSize(9).Bold();
                            });

                            right.Item().Text(text =>
                            {
                                text.Span("Kanton/Кантон: ").FontSize(9);
                                text.Span("Not provided").FontSize(9).Bold();
                            });

                            right.Item().Column(inner =>
                            {
                                inner.Item().Text("Državljanstvo/").FontSize(9);
                                inner.Item().Text(text =>
                                {
                                    text.Span("Држављанство: ").FontSize(9);
                                    text.Span("BOSNA I HERCEGOVINA").FontSize(9).Bold();
                                });
                            });

                            right.Item().Column(inner =>
                            {
                                inner.Item().Text("Promjena imena/").FontSize(9);
                                inner.Item().Text("Промјена имена:").FontSize(9);
                            });

                            right.Item().Column(inner =>
                            {
                                inner.Item().Text("Vrsta prebivališta/ Врста").FontSize(9);
                                inner.Item().Text(text =>
                                {
                                    text.Span("пребивалишта: ").FontSize(9);
                                    text.Span("STALNO").FontSize(9).Bold();
                                });
                            });
                        });
                    });

                    col.Item().Height(20);
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                    col.Item().Height(8);

                    // ── Disclaimer ──
                    col.Item().Text(
                        "Ovo obavještenje/obavijest izdaje se prema podacima iz evidencije prebivališta - boravišta koja se vodi kod ovog Organa / " +
                        "Ово обавјештење издаје се према подацима из евиденције пребивалишта - боравишта која се води код овог Органа."
                    ).FontSize(8).FontColor(Colors.Grey.Darken2);

                    col.Item().Height(30);

                    // ── Signature line ──
                    col.Item().Row(row =>
                    {
                        row.RelativeItem();
                        row.RelativeItem().AlignCenter().Column(sig =>
                        {
                            sig.Item().LineHorizontal(0.5f).LineColor(Colors.Black);
                            sig.Item().AlignCenter().Text("/Potpisno službenog lica-osobe\nПотпис службеног лица")
                                .FontSize(8).Italic();
                        });
                    });

                    col.Item().Height(30);
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                    col.Item().Height(6);

                    // ── Draft watermark ──
                    if (isDraft)
                    {
                        col.Item().AlignCenter().Text("— DRAFT PREVIEW — NOT AN OFFICIAL DOCUMENT —")
                            .FontSize(12).Bold().FontColor(Colors.Red.Medium);
                        col.Item().Height(4);
                    }

                    // ── Bottom legal notice ──
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
