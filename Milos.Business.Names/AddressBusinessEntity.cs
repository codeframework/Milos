using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Milos.BusinessObjects;

namespace Milos.Business.Names
{
    /// <summary>
    /// Summary description for AddressEntity.
    /// </summary>
    public class AddressBusinessEntity : BusinessEntity
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AddressBusinessEntity() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Address ID</param>
        public AddressBusinessEntity(Guid id) : base(id) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nameDataSet">Name DataSet</param>
        public AddressBusinessEntity(DataSet nameDataSet) : base(nameDataSet) { }

        /// <summary>
        /// Returns a business object
        /// </summary>
        /// <returns></returns>
        public override IBusinessObject GetBusinessObject() => new AddressBusinessObject();

        public static AddressBusinessEntity LoadEntity(Guid id) => new AddressBusinessEntity(id);
        public static AddressBusinessEntity NewEntity() => new AddressBusinessEntity();

        /// <summary>
        /// This method returns a well formatted address string, based on the country
        /// </summary>
        /// <param name="includeName">Specifies whether potential address name information shall be included in the output string</param>
        /// <returns>Well formatted address string</returns>
        public string GetFormattedAddress(bool includeName = false)
        {
            // We need the country object to perform this operation
            using (var entCountry = NewCountryEntity())
            {
                // Now, we can concatenate the appropriate string
                var address = string.Empty;
                if (includeName)
                {
                    var nameFound = false;
                    if (!string.IsNullOrEmpty(AddressCompany)) 
                    {
                        nameFound = true;
                        address += AddressCompany.Trim() + "\n";
                    }
                    if (!string.IsNullOrEmpty(AddressName)) 
                    {
                        nameFound = true;
                        address += AddressName.Trim() + "\n";
                    }

                    if (!nameFound) // We didn't fine the name yet, so we get it from the associated name record
                        address += GetAddressNameFromData() + "\n";
                }

                address += Street.Trim() + "\n";
                if (!string.IsNullOrEmpty(Street2)) address += Street2.Trim() + "\n";
                if (!string.IsNullOrEmpty(Street3)) address += Street3.Trim() + "\n";
                switch (entCountry.AddressFormat)
                {
                    case AddressFormat.CityStateZip:
                        address += City.Trim() + ", " + State.Trim() + " " + Zip.Trim() + "\n";
                        break;
                    case AddressFormat.CityPostalCode:
                        address += City.Trim() + ", " + Zip.Trim() + "\n";
                        break;
                    case AddressFormat.PostalCodeCity:
                        address += Zip.Trim() + " " + City.Trim() + "\n";
                        break;
                    case AddressFormat.PostalCodeCityState:
                        address += Zip.Trim() + " " + City.Trim() + ", " + State.Trim() + "\n";
                        break;
                }

                address += entCountry.Name.Trim();
                return address;
            }
        }

        private string GetAddressNameFromData()
        {
            using (var biz = new AddressBusinessObject())
                return biz.GetNameByAddressId(PK);
        }

        /// <summary>
        /// This method returns a well formatted address string (HTML), based on the country
        /// </summary>
        /// <param name="includeName">Specifies whether potential address name information shall be included in the output string</param>
        /// <returns>Well formatted address string</returns>
        public string GetHtmlFormattedAddress(bool includeName = false)
        {
            // We need the country object to perform this operation
            using (var entCountry = NewCountryEntity())
            {
                // Now, we can concatenate the appropriate string
                var address = string.Empty;
                if (includeName)
                {
                    var nameFound = false;
                    if (!string.IsNullOrEmpty(AddressCompany)) 
                    {
                        nameFound = true;
                        address += "<b>" + AddressCompany.Trim() + "</b><br/>";
                    }
                    if (!string.IsNullOrEmpty(AddressName)) 
                    {
                        nameFound = true;
                        address += "<b>" + AddressName.Trim() + "</b><br/>";
                    }

                    if (!nameFound) // We didn't fine the name yet, so we get it from the associated name record
                        address += "<b>" + GetAddressNameFromData().Replace("\n", "<br/>") + "</b><br/>";
                }
                {
                    if (AddressCompany.Length > 0) address += "<b>" + AddressCompany.Trim() + "</b><br/>";
                    if (AddressName.Length > 0) address += "<b>" + AddressName.Trim() + "</b><br/>";
                }

                address += Street.Trim() + "<br/>";
                if (Street2.Length > 0) address += Street2.Trim() + "<br/>";
                if (Street3.Length > 0) address += Street3.Trim() + "<br/>";
                switch (entCountry.AddressFormat)
                {
                    case AddressFormat.CityStateZip:
                        address += City.Trim() + ", " + State.Trim() + " " + Zip.Trim() + "<br/>";
                        break;
                    case AddressFormat.CityPostalCode:
                        address += City.Trim() + ", " + Zip.Trim() + "<br/>";
                        break;
                    case AddressFormat.PostalCodeCity:
                        address += Zip.Trim() + " " + City.Trim() + "<br/>";
                        break;
                    case AddressFormat.PostalCodeCityState:
                        address += Zip.Trim() + " " + City.Trim() + ", " + State.Trim() + "<br/>";
                        break;
                }

                address += entCountry.Name.Trim();

                return address;
            }
        }

        /// <summary>
        /// Street (Address 1)
        /// </summary>
        public string Street
        {
            get => ReadFieldValue<string>("cStreet");
            set => WriteFieldValue("cStreet", value);
        }

        /// <summary>
        /// Street 2 (Address 2)
        /// </summary>
        public string Street2
        {
            get => ReadFieldValue<string>("cStreet2");
            set => WriteFieldValue("cStreet2", value);
        }

        /// <summary>
        /// Street 3 (Address 3)
        /// </summary>
        public string Street3
        {
            get => ReadFieldValue<string>("cStreet3");
            set => WriteFieldValue("cStreet3", value);
        }

        /// <summary>
        /// City
        /// </summary>
        public string City
        {
            get => ReadFieldValue<string>("cCity");
            set => WriteFieldValue("cCity", value);
        }

        /// <summary>
        /// State (where applicable)
        /// Usually a state code, although for non-US countries, this may be a full name.
        /// </summary>
        public string State
        {
            get => ReadFieldValue<string>("cState");
            set => WriteFieldValue("cState", value);
        }

        /// <summary>
        /// ZIP or Postal Code (where applicable)
        /// </summary>
        public string Zip
        {
            get => ReadFieldValue<string>("cZip");
            set => WriteFieldValue("cZip", value);
        }

        /// <summary>
        /// Foreign key for assigned country
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Too late now")]
        public Guid CountryID
        {
            get => ReadFieldValue<Guid>("fk_country");
            set => WriteFieldValue("fk_country", value);
        }

        /// <summary>
        /// Address name (optional)
        /// This can be used whenever this particular address uses a different name.
        /// </summary>
        public string AddressName
        {
            get => ReadFieldValue<string>("cAddressName");
            set => WriteFieldValue("cAddressName", value);
        }

        /// <summary>
        /// Address company name (optional)
        /// This can be used whenever this particular address uses a different company name.
        /// </summary>
        public string AddressCompany
        {
            get => ReadFieldValue<string>("cAddressCompany");
            set => WriteFieldValue("cAddressCompany", value);
        }

        /// <summary>
        /// Instantiates and returns a new country entity
        /// based on the country assigned to this entity.
        /// </summary>
        public CountryBusinessEntity NewCountryEntity() => new CountryBusinessEntity(CountryID);

        /// <summary>
        /// Gets or sets (parses) the complete address
        /// </summary>
        public string FullAddress
        {
            get => GetFormattedAddress();
            set
            {
                // TODO: This is a temporary implementation only
                // We need to parse the address here, and also raise events in case the parsing failed.
            }
        }
    }

    [Obsolete("Use AddressBusinessEntity instead.")]
    public class AddressEntity : AddressBusinessEntity
    {
        public AddressEntity() { }

        public AddressEntity(Guid id) : base(id) { }

        public AddressEntity(DataSet nameDataSet) : base(nameDataSet) { }
    }
}