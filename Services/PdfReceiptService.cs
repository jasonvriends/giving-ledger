using Envelope_Steward.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Envelope_Steward.Services
{
    public static class PdfReceiptService
    {
        public record ReceiptData(
            int ReceiptNumber,
            string MemberName,
            string AddressLine1,
            string AddressLine2,
            string EnvelopeNumber,
            int TaxYear,
            decimal TotalAmount,
            DateTime DateIssued
        );

        /// <summary>
        /// Generates a PDF with 3 receipt copies per page for a single donor.
        /// Returns the path to the saved PDF.
        /// </summary>
        public static string GenerateReceipt(ReceiptData data, ChurchSettings church, string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);

            var safeName = string.Concat(data.MemberName.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"Receipt_{data.ReceiptNumber}_{safeName}_{data.TaxYear}.pdf";
            var filePath = Path.Combine(outputFolder, fileName);

            // Letter = 612 × 792 pt.  Margin 10 mm = 28.35 pt each side.
            // Content height ≈ 792 − 56.7 = 735.3 pt.
            // 2 dividers of 5 pt = 10 pt.  Each receipt = (735.3 − 10) / 3 ≈ 241 pt.
            const float ReceiptHeight = 241f;
            const float DividerHeight = 5f;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(10, Unit.Millimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        for (int copy = 0; copy < 3; copy++)
                        {
                            col.Item().Height(ReceiptHeight).Border(1).BorderColor(Colors.Black).Padding(10).Column(receipt =>
                            {
                                // ── Row 1: church info left | receipt # / reg # right ──
                                receipt.Item().Row(row =>
                                {
                                    row.RelativeItem(3).Column(left =>
                                    {
                                        left.Item().Text(church.ChurchName).Bold().FontSize(11);
                                        left.Item().Text(church.Address).FontSize(10);
                                        left.Item().Text(church.CityProvincePostal).FontSize(10);
                                    });
                                    row.RelativeItem(2).Column(right =>
                                    {
                                        right.Item().AlignRight().Text(txt =>
                                        {
                                            txt.Span("Receipt #").FontSize(9);
                                            txt.Span($"  {data.ReceiptNumber}").Bold().FontSize(11);
                                        });
                                        right.Item().AlignRight().Text(txt =>
                                        {
                                            txt.Span("Reg.#").FontSize(9).FontColor(Colors.Grey.Darken1);
                                            txt.Span($"  {church.RegNumber}").FontSize(9);
                                        });
                                    });
                                });

                                // ── Row 2: OFFICIAL RECEIPT centred ──
                                receipt.Item().PaddingVertical(5).AlignCenter()
                                    .Text("OFFICIAL RECEIPT").FontSize(14).Bold();

                                // ── Row 3: description left | amount right ──
                                receipt.Item().Row(row =>
                                {
                                    row.RelativeItem().AlignMiddle()
                                        .Text($"Charitable Donations During the Year {data.TaxYear} with Thanks")
                                        .FontSize(9).Italic();
                                    row.AutoItem().PaddingLeft(12).Text(txt =>
                                    {
                                        txt.Span("Amount:").Bold().FontSize(10);
                                        txt.Span($"  ${data.TotalAmount:N2}").Bold().FontSize(11);
                                    });
                                });

                                // ── Row 4: donor address left | signature line right ──
                                receipt.Item().PaddingTop(8).Row(row =>
                                {
                                    row.RelativeItem().Column(left =>
                                    {
                                        left.Item().Text(data.MemberName).Bold().FontSize(10);
                                        if (!string.IsNullOrEmpty(data.AddressLine1))
                                            left.Item().Text(data.AddressLine1).FontSize(10);
                                        if (!string.IsNullOrEmpty(data.AddressLine2))
                                            left.Item().Text(data.AddressLine2).FontSize(10);
                                    });
                                    row.RelativeItem().Column(right =>
                                    {
                                        right.Item().PaddingTop(16).LineHorizontal(0.5f).LineColor(Colors.Black);
                                        right.Item().PaddingTop(2).AlignCenter()
                                            .Text("Authorized Signature").FontSize(8).FontColor(Colors.Grey.Darken2);
                                    });
                                });

                                // ── Row 5: envelope # left | date issued right ──
                                receipt.Item().PaddingTop(6).Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text($"Envelope #  {data.EnvelopeNumber}").FontSize(9);
                                    row.AutoItem()
                                        .Text($"Date Issued:  {data.DateIssued:MMMM/d/yyyy}").FontSize(9);
                                });
                            });

                            if (copy < 2)
                                col.Item().Height(DividerHeight);
                        }
                    });
                });
            }).GeneratePdf(filePath);

            return filePath;
        }

        /// <summary>
        /// Batch-generates receipts for all members in the summary DataTable.
        /// Returns count of receipts generated.
        /// </summary>
        public static (int Count, string OutputFolder) GenerateBatchReceipts(
            System.Data.DataTable summaryTable,
            int taxYear,
            ChurchSettings church,
            Action<int, int, string>? progress = null)
        {
            var outputFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "EnvelopeSteward",
                $"Receipts_{taxYear}");

            int total = summaryTable.Rows.Count;
            int done = 0;

            foreach (System.Data.DataRow row in summaryTable.Rows)
            {
                int memberId = Convert.ToInt32(row["MemberId"]);
                decimal amount = Convert.ToDecimal(row["Receipt Total"]);
                string memberName = row["Member"]?.ToString() ?? "";
                string envNum = row["Env #"]?.ToString() ?? "";
                string address = row["Address"]?.ToString() ?? "";
                string city = row["City"]?.ToString() ?? "";
                string province = row["Province"]?.ToString() ?? "";
                string postal = row["Postal"]?.ToString() ?? "";

                string addressLine2 = $"{city}, {province}  {postal}".Trim(' ', ',');

                int receiptNum = DataAccess.GetAndIncrementReceiptNumber();

                var data = new ReceiptData(
                    ReceiptNumber: receiptNum,
                    MemberName: memberName,
                    AddressLine1: address,
                    AddressLine2: addressLine2,
                    EnvelopeNumber: envNum,
                    TaxYear: taxYear,
                    TotalAmount: amount,
                    DateIssued: DateTime.Today
                );

                string pdfPath = GenerateReceipt(data, church, outputFolder);
                DataAccess.RecordReceipt(receiptNum, memberId, taxYear, amount, pdfPath);

                done++;
                progress?.Invoke(done, total, memberName);
            }

            return (done, outputFolder);
        }
    }
}
