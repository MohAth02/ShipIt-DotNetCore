using System.Data;

namespace ShipIt.Models.DataModels
{
    public class InboundOrderDataModel : DataModel
    {
        [DatabaseColumnName("hld")]
        public int Held { get; set; }

        [DatabaseColumnName("gtin_cd")]
        public string Gtin { get; set; }

        [DatabaseColumnName("gtin_nm")]
        public string ProductName { get; set; }

        [DatabaseColumnName("l_th")]
        public int LowerThreshold { get; set; }

        [DatabaseColumnName("min_qt")]
        public int MinimumOrderQuantity { get; set; }

        [DatabaseColumnName("gcp_cd")]
        public string Gcp { get; set; }

        [DatabaseColumnName("gln_nm")]
        public string CompanyName { get; set; }

        [DatabaseColumnName("gln_addr_02")]
        public string CompanyAddr2 { get; set; }

        [DatabaseColumnName("gln_addr_03")]
        public string CompanyAddr3 { get; set; }

        [DatabaseColumnName("gln_addr_04")]
        public string CompanyAddr4 { get; set; }

        [DatabaseColumnName("gln_addr_postalcode")]
        public string CompanyPostalCode { get; set; }

        [DatabaseColumnName("gln_addr_city")]
        public string CompanyCity { get; set; }

        [DatabaseColumnName("contact_tel")]
        public string CompanyTel { get; set; }

        [DatabaseColumnName("contact_mail")]
        public string CompanyMail { get; set; }

        public InboundOrderDataModel(IDataReader dataReader)
            : base(dataReader)
        {
        }
    }
}
