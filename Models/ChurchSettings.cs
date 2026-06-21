namespace Envelope_Steward.Models
{
    public class ChurchSettings
    {
        public string ChurchName { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string Province { get; set; } = "";
        public string PostalCode { get; set; } = "";
        public string RegNumber { get; set; } = "";
        public string AuthorizedSigner { get; set; } = "";
        public string LogoPath { get; set; } = "";
        public int NextReceiptNumber { get; set; } = 1;

        public string CityProvincePostal =>
            $"{City}, {Province}  {PostalCode}".Trim(' ', ',');
    }
}
