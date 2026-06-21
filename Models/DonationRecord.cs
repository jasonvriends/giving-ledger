namespace Envelope_Steward.Models
{
    public class DonationRecord
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = "";
        public string EnvelopeNumber { get; set; } = "";
        public int OfferingTypeId { get; set; }
        public string OfferingTypeName { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public string Notes { get; set; } = "";
    }
}
