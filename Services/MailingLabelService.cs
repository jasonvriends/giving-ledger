using Envelope_Steward.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Envelope_Steward.Services
{
    public static class MailingLabelService
    {
        public static void GenerateLabels(IEnumerable<MemberRecord> members, LabelSpec spec, string outputPath)
        {
            var list = members
                .Where(m => !string.IsNullOrWhiteSpace(m.FirstName + m.LastName))
                .ToList();

            // Group members into rows of spec.Cols
            var rows = list
                .Select((m, i) => new { m, i })
                .GroupBy(x => x.i / spec.Cols)
                .Select(g => g.Select(x => x.m).ToList())
                .ToList();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginTop(spec.MTopPt,    Unit.Point);
                    page.MarginBottom(spec.MBottomPt, Unit.Point);
                    page.MarginLeft(spec.MLeftPt,  Unit.Point);
                    page.MarginRight(spec.MRightPt, Unit.Point);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        foreach (var row in rows)
                        {
                            col.Item().Height(spec.LabelHPt).Row(r =>
                            {
                                for (int i = 0; i < spec.Cols; i++)
                                {
                                    r.ConstantItem(spec.LabelWPt).Padding(5).Column(label =>
                                    {
                                        if (i < row.Count)
                                        {
                                            var m = row[i];
                                            string name = $"{m.FirstName} {m.LastName}".Trim();
                                            string prov = m.Province.Trim();
                                            string postal = m.PostalCode.Trim();
                                            string cityLine = string.Join("  ",
                                                new[] { m.City.Trim(), prov.Length > 0 ? prov : null, postal.Length > 0 ? postal : null }
                                                .Where(x => x != null)!).Trim();

                                            label.Item().Text(name).Bold();
                                            if (!string.IsNullOrWhiteSpace(m.StreetAddress))
                                                label.Item().Text(m.StreetAddress.Trim());
                                            if (!string.IsNullOrWhiteSpace(cityLine))
                                                label.Item().Text(cityLine);
                                        }
                                    });

                                    if (i < spec.Cols - 1 && spec.ColGapPt > 0)
                                        r.ConstantItem(spec.ColGapPt);
                                }
                            });

                            if (spec.RowGapPt > 0)
                                col.Item().Height(spec.RowGapPt);
                        }
                    });
                });
            }).GeneratePdf(outputPath);
        }
    }
}
