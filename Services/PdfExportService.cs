using System.Collections.Generic;
using BlazePort.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BlazePort.Services;

public sealed class PdfExportService
{
    public byte[] BuildResultsPdf(string targetHost, IReadOnlyList<ExportRow> rows)
    {
        var safeHost = string.IsNullOrWhiteSpace(targetHost) ? "host" : targetHost;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text($"BlazePort Results for {safeHost}")
                    .SemiBold()
                    .FontSize(16);

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); // Name
                        columns.RelativeColumn(1); // Port
                        columns.RelativeColumn(3); // Modes
                        columns.RelativeColumn(3); // Status
                        columns.RelativeColumn(2); // Ping ms
                        columns.RelativeColumn(4); // Error
                    });

                    // Header row
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Name");
                        header.Cell().Element(HeaderCell).Text("Port");
                        header.Cell().Element(HeaderCell).Text("Modes");
                        header.Cell().Element(HeaderCell).Text("Status");
                        header.Cell().Element(HeaderCell).Text("Ping ms");
                        header.Cell().Element(HeaderCell).Text("Error");
                    });

                    foreach (var row in rows)
                    {
                        table.Cell().Element(Cell).Text(row.Name ?? string.Empty);
                        table.Cell().Element(Cell).Text(row.Port.ToString());
                        table.Cell().Element(Cell).Text(row.Modes ?? string.Empty);
                        table.Cell().Element(Cell).Text(row.Status ?? string.Empty);
                        table.Cell().Element(Cell).Text(row.PingMs?.ToString() ?? string.Empty);
                        table.Cell().Element(Cell).Text(row.Error ?? string.Empty);
                    }

                    static IContainer HeaderCell(IContainer container) =>
                        container
                            .PaddingVertical(4)
                            .PaddingHorizontal(2)
                            .Background(Colors.Grey.Lighten3)
                            .Border(0.5f)
                            .BorderColor(Colors.Grey.Medium)
                            .DefaultTextStyle(x => x.SemiBold());

                    static IContainer Cell(IContainer container) =>
                        container
                            .PaddingVertical(2)
                            .PaddingHorizontal(2)
                            .Border(0.5f)
                            .BorderColor(Colors.Grey.Lighten3);
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
        long? PingMs,
        string? Error);
}

