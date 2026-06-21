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

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(15, Unit.Millimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        for (int copy = 0; copy < 3; copy++)
                        {
                            col.Item().Border(1).Padding(10).Column(receipt =>
                            {
                                // Header: church left, receipt # right
                                receipt.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(left =>
                                    {
                                        left.Item().Text(church.ChurchName).Bold().FontSize(11);
                                        left.Item().Text(church.Address);
                                        left.Item().Text(church.CityProvincePostal);
                                    });
                                    row.RelativeItem().AlignRight().Column(right =>
                                    {
                                        right.Item().AlignRight().Text(txt =>
                                        {
                                            txt.Span("Receipt #  ").FontSize(10);
                                            txt.Span(data.ReceiptNumber.ToString()).Bold().FontSize(10);
                                        });
                                        right.Item().AlignRight().Text(txt =>
                                        {
                                            txt.Span("Reg.#  ").FontSize(10);
                                            txt.Span(church.RegNumber).FontSize(10);
                                        });
                                    });
                                });

                                receipt.Item().PaddingVertical(6).AlignCenter()
                                    .Text("OFFICIAL RECEIPT").FontSize(13).Bold();

                                // Amount row
                                receipt.Item().Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text($"Charitable Donations During the Year {data.TaxYear} with Thanks");
                                    row.AutoItem().PaddingLeft(10).Text(txt =>
                                    {
                                        txt.Span("Amount:  ");
                                        txt.Span($"${data.TotalAmount:N2}").Bold();
                                    });
                                });

                                receipt.Item().PaddingTop(8).Row(row =>
                                {
                                    // Member address left
                                    row.RelativeItem().Column(left =>
                                    {
                                        left.Item().Text(data.MemberName).Bold();
                                        if (!string.IsNullOrEmpty(data.AddressLine1))
                                            left.Item().Text(data.AddressLine1);
                                        if (!string.IsNullOrEmpty(data.AddressLine2))
                                            left.Item().Text(data.AddressLine2);
                                    });
                                    // Signature right
                                    row.RelativeItem().AlignRight().Column(right =>
                                    {
                                        right.Item().PaddingTop(14).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                                        right.Item().AlignCenter().Text("Authorized Signature").FontSize(8).FontColor(Colors.Grey.Darken2);
                                    });
                                });

                                receipt.Item().PaddingTop(6).Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text($"Envelope #  {data.EnvelopeNumber}");
                                    row.AutoItem()
                                        .Text($"Date Issued:  {data.DateIssued:MMMM/dd/yyyy}");
                                });
                            });

                            if (copy < 2)
                                col.Item().Height(8);
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
