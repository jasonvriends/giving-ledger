using Envelope_Steward.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Envelope_Steward.Services
{
    public static class MailingLabelService
    {
        // Avery 5160 specs (Letter page, 30 labels — 3 cols × 10 rows)
        // Label:  2.625" W × 1.0" H
        // Gaps:   0.125" H gap between columns (no vertical gap between rows)
        // Margins: 0.5" top/bottom, 0.1875" left/right
        //
        // In QuestPDF units (points): 1" = 72pt
        private const float LabelW   = 189f;  // 2.625"
        private const float LabelH   = 72f;   // 1.0"
        private const float ColGap   = 9f;    // 0.125"
        private const float PageMarginH = 36f;  // 0.5"
        private const float PageMarginV = 13.5f; // 0.1875"

        public static void GenerateLabels(IEnumerable<MemberRecord> members, string outputPath)
        {
            var list = members
                .Where(m => !string.IsNullOrWhiteSpace(m.FirstName + m.LastName))
                .ToList();

            // Group into rows of 3
            var rows = list
                .Select((m, i) => new { m, i })
                .GroupBy(x => x.i / 3)
                .Select(g => g.Select(x => x.m).ToList())
                .ToList();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginTop(PageMarginH, Unit.Point);
                    page.MarginBottom(PageMarginH, Unit.Point);
                    page.MarginLeft(PageMarginV, Unit.Point);
                    page.MarginRight(PageMarginV, Unit.Point);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        foreach (var row in rows)
                        {
                            col.Item().Height(LabelH).Row(r =>
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    r.ConstantItem(LabelW).Padding(5).Column(label =>
                                    {
                                        if (i < row.Count)
                                        {
                                            var m = row[i];
                                            string name = $"{m.FirstName} {m.LastName}".Trim();
                                            string city = m.City.Trim();
                                            string prov = m.Province.Trim();
                                            string postal = m.PostalCode.Trim();
                                            string cityLine = string.Join("  ",
                                                new[] { city, prov.Length > 0 ? prov : null, postal.Length > 0 ? postal : null }
                                                .Where(x => x != null)!).Trim();

                                            label.Item().Text(name).Bold();
                                            if (!string.IsNullOrWhiteSpace(m.StreetAddress))
                                                label.Item().Text(m.StreetAddress.Trim());
                                            if (!string.IsNullOrWhiteSpace(cityLine))
                                                label.Item().Text(cityLine);
                                        }
                                    });

                                    if (i < 2)
                                        r.ConstantItem(ColGap); // horizontal gap spacer
                                }
                            });
                        }
                    });
                });
            }).GeneratePdf(outputPath);
        }
    }
}
