using System.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Envelope_Steward.Services
{
    public static class ReportPdfService
    {
        // Columns that should be right-aligned (money or numeric).
        private static readonly HashSet<string> RightAlignedCols = new(StringComparer.OrdinalIgnoreCase)
        {
            "Total", "Receipt Total", "Amount", "Change"
        };

        public static void GenerateReport(DataTable dt, string title, string churchName, string outputPath)
        {
            // Exclude internal ID columns.
            var cols = dt.Columns.Cast<DataColumn>()
                .Where(c => !c.ColumnName.Equals("MemberId", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter.Landscape());
                    page.Margin(15, Unit.Millimetre);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(h =>
                    {
                        if (!string.IsNullOrWhiteSpace(churchName))
                            h.Item().Text(churchName).Bold().FontSize(12);
                        h.Item().Text(title).Bold().FontSize(11);
                        h.Item().PaddingTop(2).Text($"Generated: {DateTime.Today:MMMM d, yyyy}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                        h.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Black);
                    });

                    page.Content().PaddingTop(8).Table(table =>
                    {
                        // Equal-width columns
                        table.ColumnsDefinition(cd =>
                        {
                            foreach (var _ in cols) cd.RelativeColumn();
                        });

                        // Header row
                        foreach (var col in cols)
                        {
                            bool rightAlign = RightAlignedCols.Contains(col.ColumnName) ||
                                             (col.DataType == typeof(double) || col.DataType == typeof(decimal));
                            table.Cell()
                                .Background(Colors.Grey.Darken3)
                                .Padding(4)
                                .AlignMiddle()
                                .Element(c => rightAlign ? c.AlignRight() : c.AlignLeft())
                                .Text(col.ColumnName).FontColor(Colors.White).Bold().FontSize(8);
                        }

                        // Data rows
                        bool alt = false;
                        foreach (DataRow row in dt.Rows)
                        {
                            string bg = alt ? Colors.Grey.Lighten4 : Colors.White;
                            alt = !alt;

                            foreach (var col in cols)
                            {
                                bool rightAlign = RightAlignedCols.Contains(col.ColumnName) ||
                                                 col.DataType == typeof(double) || col.DataType == typeof(decimal);
                                var raw = row[col];
                                string text;
                                if (raw == DBNull.Value || raw == null)
                                    text = "";
                                else if (raw is double d)
                                    text = d.ToString("C2");
                                else if (raw is decimal m)
                                    text = m.ToString("C2");
                                else
                                    text = raw.ToString() ?? "";

                                // Try to format string values that look like money
                                if (!rightAlign && text.Length > 0 &&
                                    decimal.TryParse(text, System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture, out var dm) &&
                                    RightAlignedCols.Contains(col.ColumnName))
                                {
                                    text = dm.ToString("C2");
                                    rightAlign = true;
                                }

                                table.Cell()
                                    .Background(bg)
                                    .BorderBottom(0.25f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(4)
                                    .AlignMiddle()
                                    .Element(c => rightAlign ? c.AlignRight() : c.AlignLeft())
                                    .Text(text).FontSize(9);
                            }
                        }
                    });

                    page.Footer().AlignRight()
                        .Text(t =>
                        {
                            t.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                            t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                            t.Span(" of ").FontSize(8).FontColor(Colors.Grey.Darken1);
                            t.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                });
            }).GeneratePdf(outputPath);
        }
    }
}
