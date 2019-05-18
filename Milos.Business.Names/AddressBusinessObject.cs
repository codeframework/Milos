using Milos.BusinessObjects;

namespace Milos.Business.Names
{
    /// <summary>
    /// Summary description for AddressBusinessObject.
    /// </summary>
    public class AddressBusinessObject : BusinessObject
    {
        /// <summary>
        /// Configures settings in the current BO
        /// </summary>
        protected override void Configure()
        {
            MasterEntity = "Address";
            PrimaryKeyField = "pk_address";
        }
    }
}