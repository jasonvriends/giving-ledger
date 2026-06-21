namespace Envelope_Steward.Models
{
    public class OfferingTypeRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool TaxReceiptable { get; set; } = true;

        public string DisplayName =>
            string.IsNullOrEmpty(Description) ? Name : Description;

        public override string ToString() => DisplayName;
    }
}
