using System;
using Milos.BusinessObjects;
using Milos.Core.Configuration;

namespace Milos.Business.Names
{
	/// <summary>
	/// Name address collection interface
	/// </summary>
	public interface INameAddressCollection : IEntityXlinkSubItemCollection
	{
		/// <summary>
		/// Indexer
		/// </summary>
		new INameAddressEntity this[int index] { get; }
		/// <summary>
		/// Adds a new address of a certain type
		/// </summary>
		/// <param name="type">Address type</param>
		INameAddressEntity Add(AddressType type);
		/// <summary>
		/// Adds a new address of a certain type
		/// </summary>
		/// <param name="type">Address type (string)</param>
		new INameAddressEntity Add(string type);
	}

	/// <summary>
	/// Summary description for NameAddressCollection.
	/// </summary>
    public class NameAddressCollection : EntityXlinkSubItemCollection, INameAddressCollection
	{
		/// <summary>
		/// For internal use only
		/// </summary>
		private static Guid _defaultCountryId = Guid.Empty;

		/// <summary>
		/// Defines the internal default country setting.
		/// If the setting is empty, the default country is read from a config file.
		/// If there is no setting in a config file either, then we assume "US".
		/// Note: This is the country code.
		/// </summary>
		private static string _defaultCountry = string.Empty;

		/// <summary>
		/// Defines the default country for new addresses.
		/// The default country can be changed by either overriding the defaultCountry field in a subclass,
		/// or by setting the "DefaultCountry" setting in the application settings (app.config).
		/// </summary>
		protected static string DefaultCountry
		{
			get
			{
				if (string.IsNullOrEmpty(_defaultCountry))
                {
                    // No setting yet. We need to load if from the config file
                    _defaultCountry = ConfigurationSettings.Settings.IsSettingSupported("DefaultCountry") ? ConfigurationSettings.Settings["DefaultCountry"] : string.Empty;

                    if (string.IsNullOrEmpty(_defaultCountry))
                        // We still haven't found the default country. All we can do now is go with a basic assumption
                        _defaultCountry = "US";
                }
				return _defaultCountry;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
        /// <param name="parentEntity">Parent Entity</param>
        public NameAddressCollection(IBusinessEntity parentEntity) : base(parentEntity) {}

		/// <summary>
		/// The indexer must be overridden to return the appropriate type
		/// </summary>
		public new INameAddressEntity this[int index] => (INameAddressEntity)GetItemByIndex(index);

        /// <summary>
		/// Configures the object to operate seamlessly, without much additional code required
		/// </summary>
		protected override void Configure()
		{
			PrimaryKeyField = "pk_placement";
			ForeignKeyField = "fk_name";
			ParentTableName = "names";
			ParentTablePrimaryKeyField = "pk_name";
			TargetForeignKeyField = "fk_address";
			TargetPrimaryKeyField = "pk_address";
			TargetTextField = string.Empty;
			AutoAddTarget = true;
		}

		/// <summary>
		/// This is needed for the collection to generate and serve up new item instances
		/// </summary>
		/// <returns>Item object</returns>
		public override IEntitySubItemCollectionItem GetItemObject() => new NameAddressEntity(this);

        /// <summary>
		/// Adds a new address of a certain type
		/// </summary>
		/// <param name="type">Address type</param>
		public INameAddressEntity Add(AddressType type)
		{
			var entAddress = (INameAddressEntity)Add();
			entAddress.TypeStrong = type;
			return entAddress;
		}

		/// <summary>
		/// Adds a new address of a certain type
		/// </summary>
		/// <param name="type">Address type (string)</param>
		public new INameAddressEntity Add(string type)
		{
			var entAddress = (INameAddressEntity)Add();
			entAddress.Type = type;
			return entAddress;
		}

		/// <summary>
		/// Adds a new address of a certain type
		/// </summary>
		/// <returns>Address type</returns>
		public override IEntitySubItemCollectionItem Add()
		{
			var entAddress = (INameAddressEntity)base.Add();
			
			// We need to find the default country's PK
			if (_defaultCountryId == Guid.Empty)
			{
				// We have not yet found out what the default country is
                using (var bizCountry = new CountryBusinessObject())
                {
                    var guidCountry = bizCountry.GetCountryIDByCode(DefaultCountry);
                    if (guidCountry == Guid.Empty)
                    {
                        // Something went wrong. Apparently the specified country is not in the database.
                        // At this point, we attempt to find any country at all.
                        guidCountry = bizCountry.GetAnyCountryID();
                        if (guidCountry != Guid.Empty)
                            // OK, we found one. This is probably not very useful business-wise, but at least the system will not crash.
                            _defaultCountryId = guidCountry;
                        else
                        {
                            // If no country was found at all, we can not assign a default value. 
                            // Unfortunately, this means that the system is likely to be unstable further down the road.
                            throw new NotSupportedException("No countries defined.");
                        }
                    }
                    else
                        _defaultCountryId = guidCountry;
                }
            }
			entAddress.CountryID = _defaultCountryId;

			return (EntitySubItemCollectionItem)entAddress;
		}

	}

	/// <summary>
	/// This address type will be stored in the database (in the crosslink table)
	/// </summary>
	public enum AddressType
	{
		/// <summary>
		/// Mailing address
		/// </summary>
		Mailing = 0,
		/// <summary>
		/// Billing address
		/// </summary>
		Billing = 1,
		/// <summary>
		/// Home address
		/// </summary>
		Home = 2,
		/// <summary>
		/// Office address
		/// </summary>
		Office = 3,
		/// <summary>
		/// Shipping address
		/// </summary>
		Shipping = 4,
		/// <summary>
		/// Other address
		/// </summary>
		Other = 5
	}

	/// <summary>
	/// Different address format options
	/// (generally based on country-specific standards)
	/// </summary>
	public enum AddressFormat
	{
		/// <summary>
		/// City, State, ZIP
		/// </summary>
		CityStateZip,
		/// <summary>
		/// Postal code, city
		/// </summary>
		PostalCodeCity,
		/// <summary>
		/// City, postal code
		/// </summary>
		CityPostalCode,
		/// <summary>
		/// Postal code, ciry, state
		/// </summary>
		PostalCodeCityState

	}

	/// <summary>
	/// Basic name/address entity interface
	/// </summary>
	public interface INameAddressEntity : IEntitySubItemCollectionItem 
	{
		/// <summary>
		/// This method returns a well formatted address string, based on the country
		/// </summary>
		/// <param name="includeName">Specifies whether potential address name information shall be included in the output string</param>
		/// <returns>Well formatted address string</returns>
		string GetFormattedAddress(bool includeName = false);
		/// <summary>
		/// This method returns a well formatted address string (HTML), based on the country
		/// </summary>
		/// <param name="includeName">Specifies whether potential address name information shall be included in the output string</param>
		/// <returns>Well formatted address string</returns>
		string GetHtmlFormattedAddress(bool includeName = false);
		/// <summary>
		/// Address primary key
		/// </summary>
		Guid AddressId { get; }
		/// <summary>
		/// Street (Address 1)
		/// </summary>
		string Street { get; set; }
		/// <summary>
		/// Street 2 (Address 2)
		/// </summary>
		string Street2 { get; set; }
		/// <summary>
		/// Street 3 (Address 3)
		/// </summary>
		string Street3 { get; set; }
		/// <summary>
		/// City
		/// </summary>
		string City { get; set; }
		/// <summary>
		/// State (where applicable)
		/// Usually a state code, although for non-US countries, this may be a full name.
		/// </summary>
		string State { get; set; }
		/// <summary>
		/// ZIP or Postal Code (where applicable)
		/// </summary>
		string Zip { get; set; }
		/// <summary>
		/// Foreign key for assigned country
		/// </summary>
        Guid CountryID { get; set; }
		/// <summary>
		/// Address name (optional)
		/// This can be used whenever this particular address uses a different name.
		/// </summary>
		string AddressName { get; set; }
		/// <summary>
		/// Address company name (optional)
		/// This can be used whenever this particular address uses a different company name.
		/// </summary>
		string AddressCompany { get; set; }

        /// <summary>
        /// Instantiates and returns a new country entity
        /// based on the country assigned to this entity.
        /// </summary>
        CountryBusinessEntity NewCountryEntity();
		/// <summary>
		/// Gets or sets the address type (strongly typed)
		/// Note that the values available through this setting are limited to the values
		/// available in the AddressType enum. Unknown underlying values are exposed as "Other".
		/// Note: Use the regular "Type" property to get access to all string values.
		/// </summary>
		AddressType TypeStrong { get; set; }
		/// <summary>
		/// Gets or sets the address type.
		/// Note that this property allows direct access to the underlying string value.
		/// The strings to not have to match the AddressType enum. Strings that do not 
		/// match that enum will be exposed as "Other" through the "TypeStrong" property.
		/// </summary>
		string Type { get; set; }
	}

    /// <summary>
    /// Sub-tems for different addresses
    /// </summary>
    public class NameAddressEntity : EntitySubItemCollectionXLinkItem, INameAddressEntity
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentCollection">Parent collection</param>
        public NameAddressEntity(IEntitySubItemCollection parentCollection) : base(parentCollection) { }

        /// <summary>
        /// This method returns a well formatted address string, based on the country
        /// </summary>
        /// <returns>Well formatted address string</returns>
        public string GetFormattedAddress() => GetFormattedAddress(false);

        /// <summary>
        /// This method returns a well formatted address string, based on the country
        /// </summary>
        /// <param name="includeName">Specifies whether potential address name information shall be included in the output string</param>
        /// <returns>Well formatted address string</returns>
        public string GetFormattedAddress(bool includeName)
        {
            // We need the country object to perform this operation
            using (var entCountry = NewCountryEntity())
            {
                // Now, we can concatenate the appropriate string
                string address = string.Empty;
                if (includeName)
                {
                    if (!string.IsNullOrEmpty(AddressCompany)) address += AddressCompany.Trim() + "\r\n";
                    if (!string.IsNullOrEmpty(AddressName)) address += AddressName.Trim() + "\r\n";
                }

                address += Street.Trim() + "\r\n";
                if (!string.IsNullOrEmpty(Street2)) address += Street2.Trim() + "\r\n";
                if (!string.IsNullOrEmpty(Street3)) address += Street3.Trim() + "\r\n";
                switch (entCountry.AddressFormat)
                {
                    case AddressFormat.CityStateZip:
                        address += City.Trim() + ", " + State.Trim() + " " + Zip.Trim() + "\r\n";
                        break;
                    case AddressFormat.CityPostalCode:
                        address += City.Trim() + ", " + Zip.Trim() + "\r\n";
                        break;
                    case AddressFormat.PostalCodeCity:
                        address += Zip.Trim() + " " + City.Trim() + "\r\n";
                        break;
                    case AddressFormat.PostalCodeCityState:
                        address += Zip.Trim() + " " + City.Trim() + ", " + State.Trim() + "\r\n";
                        break;
                }

                address += entCountry.Name.Trim();

                return address;
            }
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
                    if (!string.IsNullOrEmpty(AddressCompany)) address += "<b>" + AddressCompany.Trim() + "</b><br>";
                    if (!string.IsNullOrEmpty(AddressName)) address += "<b>" + AddressName.Trim() + "</b><br>";
                }

                address += Street.Trim() + "<br>";
                if (!string.IsNullOrEmpty(Street2)) address += Street2.Trim() + "<br>";
                if (!string.IsNullOrEmpty(Street3)) address += Street3.Trim() + "<br>";
                switch (entCountry.AddressFormat)
                {
                    case AddressFormat.CityStateZip:
                        address += City.Trim() + ", " + State.Trim() + " " + Zip.Trim() + "<br>";
                        break;
                    case AddressFormat.CityPostalCode:
                        address += City.Trim() + ", " + Zip.Trim() + "<br>";
                        break;
                    case AddressFormat.PostalCodeCity:
                        address += Zip.Trim() + " " + City.Trim() + "<br>";
                        break;
                    case AddressFormat.PostalCodeCityState:
                        address += Zip.Trim() + " " + City.Trim() + ", " + State.Trim() + "<br>";
                        break;
                }

                address += entCountry.Name.Trim();

                return address;
            }
        }

        /// <summary>
        /// Address primary key
        /// </summary>
        public Guid AddressId => ReadFieldValue<Guid>("pk_address", XLinkItemAccessMode.TargetTable);

        /// <summary>
        /// Street (Address 1)
        /// </summary>
        public virtual string Street
        {
            get => ReadFieldValue<string>("cStreet", XLinkItemAccessMode.TargetTable);
            set => WriteFieldValue("cStreet", value, XLinkItemAccessMode.TargetTable);
        }

        /// <summary>
        /// Street 2 (Address 2)
        /// </summary>
        public virtual string Street2
        {
            get => ReadFieldValue<string>("cStreet2", XLinkItemAccessMode.TargetTable);
            set => WriteFieldValue("cStreet2", value, XLinkItemAccessMode.TargetTable);
        }

        /// <summary>
        /// Street 3 (Address 3)
        /// </summary>
        public virtual string Street3
        {
            get => ReadFieldValue<string>("cStreet3", XLinkItemAccessMode.TargetTable);
            set => WriteFieldValue("cStreet3", value, XLinkItemAccessMode.TargetTable);
        }

        /// <summary>
        /// City
        /// </summary>
        public virtual string City
        {
            get => ReadFieldValue<string>("cCity", XLinkItemAccessMode.TargetTable);
            set => WriteFieldValue("cCity", value, XLinkItemAccessMode.TargetTable);
        }

        /// <summary>
        /// State (where applicable)
        /// Usually a state code, although for non-US countries, this may be a full name.
        /// </summary>
        public virtual string State
        {
            get => ReadFieldValue<string>("cState", XLinkItemAccessMode.TargetTable);
            set => WriteFieldValue("cState", value, XLinkItemAccessMode.TargetTable);
        }

        /// <summary>
        /// ZIP or Postal Code (where applicable)
        /// </summary>
        public virtual string Zip
        {
            get => ReadFieldValue<string>("cZip", XLinkItemAccessMode.TargetTable);
            set => WriteFieldValue("cZip", value, XLinkItemAccessMode.TargetTable);
        }

        /// <summary>
        /// Foreign key for assigned country
        /// </summary>
        public virtual Guid CountryID
        {
            get => ReadFieldValue<Guid>("fk_country", XLinkItemAccessMode.TargetTable);
            set => WriteFieldValue("fk_country", value, XLinkItemAccessMode.TargetTable);
        }

        /// <summary>
        /// Address name (optional)
        /// This can be used whenever this particular address uses a different name.
        /// </summary>
        public virtual string AddressName
        {
            get => ReadFieldValue<string>("cAddressName", XLinkItemAccessMode.TargetTable);
            set => WriteFieldValue("cAddressName", value, XLinkItemAccessMode.TargetTable);
        }

        /// <summary>
        /// Address company name (optional)
        /// This can be used whenever this particular address uses a different company name.
        /// </summary>
        public virtual string AddressCompany
        {
            get => ReadFieldValue<string>("cAddressCompany", XLinkItemAccessMode.TargetTable);
            set => WriteFieldValue("cAddressCompany", value, XLinkItemAccessMode.TargetTable);
        }

        /// <summary>
        /// Instantiates and returns a new country entity
        /// based on the country assigned to this entity.
        /// </summary>
        public virtual CountryBusinessEntity NewCountryEntity() => new CountryBusinessEntity(CountryID);

        /// <summary>
        /// Gets or sets the address type (strongly typed)
        /// Note that the values available through this setting are limited to the values
        /// available in the AddressType enum. Unknown underlying values are exposed as "Other".
        /// Note: Use the regular "Type" property to get access to all string values.
        /// </summary>
        public virtual AddressType TypeStrong
        {
            get
            {
                try
                {
                    return (AddressType) Enum.Parse(typeof(AddressType), ReadFieldValue<string>("cType"), true);
                }
                catch
                {
                    return AddressType.Other;
                }
            }
            set => WriteFieldValue("cType", value.ToString(), XLinkItemAccessMode.CurrentTable);
        }

        /// <summary>
        /// Gets or sets the address type.
        /// Note that this property allows direct access to the underlying string value.
        /// The strings to not have to match the AddressType enum. Strings that do not 
        /// match that enum will be exposed as "Other" through the "TypeStrong" property.
        /// </summary>
        public virtual string Type
        {
            get => ReadFieldValue<string>("cType");
            set => WriteFieldValue("cType", value, XLinkItemAccessMode.CurrentTable);
        }
    }
}
