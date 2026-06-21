namespace Envelope_Steward.Models
{
    public class MemberRecord
    {
        public int Id { get; set; }
        public string EnvelopeNumber { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string StreetAddress { get; set; } = "";
        public string City { get; set; } = "";
        public string Province { get; set; } = "";
        public string PostalCode { get; set; } = "";
        public string HomePhone { get; set; } = "";
        public string Email { get; set; } = "";
        public bool FullMember { get; set; }
        public bool ShutIn { get; set; }
        public bool Active { get; set; }
        public string Status { get; set; } = "";

        public string DisplayName
        {
            get
            {
                var first = FirstName?.Trim() ?? "";
                var last = LastName?.Trim() ?? "";
                if (!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(last))
                    return $"{first} {last}";
                return string.IsNullOrEmpty(first) ? last : first;
            }
        }

        public string ComboLabel =>
            string.IsNullOrEmpty(EnvelopeNumber)
                ? DisplayName
                : $"{EnvelopeNumber} - {DisplayName}";

        public string AddressBlock =>
            $"{StreetAddress}\n{City}, {Province}  {PostalCode}".Trim('\n', ',', ' ');
    }
}
