using System;
using System.Data;
using Milos.BusinessObjects;

namespace Milos.Business.Names
{
    /// <summary>
    /// This class provides functionality to manipulate country information
    /// </summary>
    public class CountryBusinessObject : BusinessObject
    {
        /// <summary>
        /// Configures settings in the current BO
        /// </summary>
        protected override void Configure()
        {
            MasterEntity = "Country";
            PrimaryKeyField = "pk_country";
            DefaultOrder = "cname";
        }

        /// <summary>
        /// Returns the primary key of the country identified by its code.
        /// </summary>
        /// <param name="countryCode">Country code (such as "US")</param>
        /// <returns>Primary key of the country</returns>
        public Guid GetCountryIDByCode(string countryCode)
        {
            try
            {
                // TODO: We muse use a business object here!!! Then see if the rule exclusion above can be removed.
                using (var comSelect = NewDbCommand("SELECT pk_country FROM Country where ccode = @Code"))
                {
                    AddDbCommandParameter(comSelect, "@Code", countryCode);
                    return (Guid) ExecuteScalar(comSelect);
                }
            }
            catch
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Returns all states for a certain country.
        /// </summary>
        /// <param name="countryId">Country ID</param>
        /// <returns>List of states</returns>
        public DataSet GetStatesByCountryID(Guid countryId)
        {
            // TODO: We must use a business object here
            using (var comSelect = NewDbCommand("SELECT * FROM States WHERE fk_country = @PK"))
            {
                AddDbCommandParameter(comSelect, "@PK", countryId);
                return ExecuteQuery(comSelect, "states");
            }
        }

        /// <summary>
        /// Returns the ID of a random country (usually the first in the database).
        /// This is needed for some internal stuff, but not very useful otherwise... :-)
        /// </summary>
        /// <returns>Random country guid</returns>
        public Guid GetAnyCountryID()
        {
            using (var comSelect = NewDbCommand("SELECT TOP 1 pk_country FROM Country"))
                return (Guid) ExecuteScalar(comSelect);
        }

        /// <summary>
        /// Returns the name of a country based on the 2-digit ISO region code.
        /// </summary>
        /// <param name="isoRegion">Region code (2 digit), such as "US"</param>
        /// <returns>Country name</returns>
        public virtual string GetCountryNameByISORegion(string isoRegion)
        {
            using (var cmd = NewDbCommand($"SELECT cName FROM {MasterEntity} WHERE cCode LIKE @Code"))
            {
                AddDbCommandParameter(cmd, "@Code", isoRegion);
                return ExecuteScalar(cmd).ToString();
            }
        }
    }
}