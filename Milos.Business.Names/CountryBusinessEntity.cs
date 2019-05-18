using System;
using System.Data;
using Milos.BusinessObjects;

namespace Milos.Business.Names
{
    /// <summary>
    /// Represents an individual country
    /// </summary>
    public class CountryBusinessEntity : BusinessEntity
    {
        /// <summary>
        ///  Constructor
        /// </summary>
        public CountryBusinessEntity() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Country ID</param>
        public CountryBusinessEntity(Guid id) : base(id) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initialData">Initial data</param>
        public CountryBusinessEntity(DataSet initialData) : base(initialData) { }

        /// <summary>
        /// Country name
        /// </summary>
        public string Name
        {
            get => ReadFieldValue<string>("cname");
            set => WriteFieldValue("cname", value);
        }

        /// <summary>
        /// Country Code
        /// </summary>
        public string Code
        {
            get => ReadFieldValue<string>("ccode");
            set => WriteFieldValue("ccode", value);
        }

        /// <summary>
        /// Returns the address format utilized by the country.
        /// Note: This is an optional piece of information.
        /// If the database does not support this information,
        /// all formats will be reported as AddressFormat.CityStateZip (US default)
        /// </summary>
        public AddressFormat AddressFormat
        {
            get
            {
                try
                {
                    return ReadFieldValue<AddressFormat>("iaddrformat");
                }
                catch
                {
                    return AddressFormat.CityStateZip;
                }
            }
            set
            {
                // We make sure that this information is in the database. 
                // We do NOT want any automatic adding of this column!
                try
                {
                    GetInternalData().Tables[MasterEntity].Rows[0]["iaddrformat"] = (int) value;
                }
                catch { }
            }
        }

        /// <summary>
        /// Returns a country business object
        /// </summary>
        /// <returns>Country business object</returns>
        public override IBusinessObject GetBusinessObject() => new CountryBusinessObject();
    }

    [Obsolete("Use CountryBusinessEntity instead")]
    public class CountryEntity : CountryBusinessEntity
    {
        /// <summary>
        ///  Constructor
        /// </summary>
        public CountryEntity() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Country ID</param>
        public CountryEntity(Guid id) : base(id) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initialData">Initial data</param>
        public CountryEntity(DataSet initialData) : base(initialData) { }
    }
}