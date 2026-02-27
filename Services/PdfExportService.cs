using BlazePort.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BlazePort.Services;

public sealed class PdfExportService
{
    public byte[] BuildResultsPdf(string targetHost, string mode, IReadOnlyList<ExportRow> rows)
    {
        var safeHost = string.IsNullOrWhiteSpace(targetHost) ? "host" : targetHost;
        var now = DateTime.Now;

        var totalPorts = rows.Count;
        var pingOkCount = rows.Count(r => r.PingOk == true);
        var telnetOkCount = rows.Count(r => r.TelnetOk == true);
        var successRate = totalPorts == 0 ? 0.0 : (telnetOkCount * 100.0) / totalPorts;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(25);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Column(col =>
                {
                    // ── Başlık ──
                    col.Item()
                        .PaddingBottom(6)
                        .Text("BlazePort")
                        .Bold()
                        .FontSize(22)
                        .FontColor(Colors.Blue.Darken2);

                    // ── Bilgi satırı: sol (Mode + Server) | sağ (Tarih) ──
                    col.Item().PaddingBottom(6).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(text =>
                            {
                                text.Span("Mode: ").SemiBold();
                                text.Span(mode);
                            });
                            left.Item().Text(text =>
                            {
                                text.Span("Server: ").SemiBold();
                                text.Span(safeHost);
                            });
                        });

                        row.RelativeItem().AlignRight().AlignBottom().Text(text =>
                        {
                            text.Span(now.ToString("dd.MM.yyyy HH:mm"));
                        });
                    });

                    // ── Ayırıcı çizgi ──
                    col.Item()
                        .PaddingVertical(4)
                        .LineHorizontal(1)
                        .LineColor(Colors.Grey.Lighten1);

                    // ── Özet satırı ──
                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.AutoItem().Text(text =>
                        {
                            text.Span("Total Ports: ").SemiBold();
                            text.Span(totalPorts.ToString());
                        });

                        row.AutoItem().PaddingLeft(20).Text(text =>
                        {
                            text.Span("Ping OK: ").SemiBold();
                            text.Span($"{pingOkCount}/{totalPorts}");
                        });

                        row.AutoItem().PaddingLeft(20).Text(text =>
                        {
                            text.Span("Telnet OK: ").SemiBold();
                            text.Span($"{telnetOkCount}/{totalPorts}");
                        });

                        row.AutoItem().PaddingLeft(20).Text(text =>
                        {
                            text.Span("Success Rate: ").SemiBold();
                            text.Span($"{successRate:0.0}%");
                        });
                    });

                    // ── Sonuç tablosu ──
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(22);  // #
                            columns.RelativeColumn(3);   // Name
                            columns.RelativeColumn(1);   // Port
                            columns.RelativeColumn(2);   // Modes
                            columns.RelativeColumn(1.5f); // Status
                            columns.ConstantColumn(30);  // Ping (✓/✗)
                            columns.RelativeColumn(1);   // Ping ms
                            columns.ConstantColumn(35);  // Telnet (✓/✗)
                            columns.RelativeColumn(1);   // Telnet ms
                            columns.RelativeColumn(3);   // Error
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("#");
                            header.Cell().Element(HeaderCell).Text("Name");
                            header.Cell().Element(HeaderCell).Text("Port");
                            header.Cell().Element(HeaderCell).Text("Modes");
                            header.Cell().Element(HeaderCell).Text("Status");
                            header.Cell().Element(HeaderCell).Text("Ping");
                            header.Cell().Element(HeaderCell).Text("Ping ms");
                            header.Cell().Element(HeaderCell).Text("Telnet");
                            header.Cell().Element(HeaderCell).Text("Telnet ms");
                            header.Cell().Element(HeaderCell).Text("Error");
                        });

                        for (var i = 0; i < rows.Count; i++)
                        {
                            var row = rows[i];

                            table.Cell().Element(Cell).Text((i + 1).ToString());
                            table.Cell().Element(Cell).Text(row.Name ?? string.Empty);
                            table.Cell().Element(Cell).Text(row.Port.ToString());
                            table.Cell().Element(Cell).Text(row.Modes ?? string.Empty);
                            table.Cell().Element(Cell).Text(row.Status ?? string.Empty);

                            // Ping ✓/✗
                            table.Cell().Element(Cell).AlignCenter().Text(text =>
                            {
                                if (row.PingOk == true)
                                    text.Span("✓").FontColor(Colors.Green.Darken2).SemiBold();
                                else
                                    text.Span("✗").FontColor(Colors.Red.Darken2).SemiBold();
                            });

                            table.Cell().Element(Cell).Text(row.PingMs?.ToString() ?? "-");

                            // Telnet ✓/✗
                            table.Cell().Element(Cell).AlignCenter().Text(text =>
                            {
                                if (row.TelnetOk == true)
                                    text.Span("✓").FontColor(Colors.Green.Darken2).SemiBold();
                                else
                                    text.Span("✗").FontColor(Colors.Red.Darken2).SemiBold();
                            });

                            table.Cell().Element(Cell).Text(row.ConnectTimeMs?.ToString() ?? "-");
                            table.Cell().Element(Cell).Text(row.Error ?? string.Empty);
                        }

                        static IContainer HeaderCell(IContainer container) =>
                            container
                                .PaddingVertical(4)
                                .PaddingHorizontal(3)
                                .Background(Colors.Blue.Lighten4)
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Medium)
                                .DefaultTextStyle(x => x.SemiBold().FontSize(8));

                        static IContainer Cell(IContainer container) =>
                            container
                                .PaddingVertical(2)
                                .PaddingHorizontal(3)
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten2);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public sealed record ExportRow(
        string Name,
        int Port,
        string Modes,
        string Status,
        bool? PingOk,
        long? PingMs,
        bool? TelnetOk,
        long? ConnectTimeMs,
        string? Error);
}
