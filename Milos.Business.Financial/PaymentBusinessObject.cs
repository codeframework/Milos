using System;
using System.Data;
using Milos.BusinessObjects;

namespace Milos.Business.Financial
{
    /// <summary>
    /// The class encapsulates basic payments functionality
    /// that can be used to records payments that may be
    /// linked to an invoice
    /// </summary>
    public class PaymentBusinessObject : BusinessObject
    {
        /// <summary>
        /// Configures settings in the current BO
        /// </summary>
        protected override void Configure()
        {
            MasterEntity = "Payments";
            PrimaryKeyField = "pk_payments";
        }

        /// <summary>
        ///  This method is overridden in order to add default values
        /// </summary>
        /// <returns>DataSet</returns>
        public override DataSet AddNew()
        {
            var dsInternal = base.AddNew();
            dsInternal.Tables[MasterEntity].Rows[0]["cCurrency"] = "U$";
            dsInternal.Tables[MasterEntity].Rows[0]["nExchangeRate"] = 1;
            dsInternal.Tables[MasterEntity].Rows[0]["dReceived"] = DateTime.Today;
            dsInternal.Tables[MasterEntity].Rows[0]["nAmount"] = 0;
            return dsInternal;
        }

        /// <summary>
        /// Returns the payment id for a certain invoice pk.
        /// Note that this method only works reliable if a single payment covered the entire invoice!
        /// </summary>
        /// <param name="invoicePK">Invoice ID</param>
        /// <returns>Payment PK</returns>
        public Guid GetPaymentPKByInvoicePK(Guid invoicePK)
        {
            using (var command = NewDbCommand("SELECT pk_payments FROM Payments WHERE fk_invoices = @fk"))
            {
                AddDbCommandParameter(command, "@fk", invoicePK);
                return (Guid) ExecuteScalar(command);
            }
        }

        /// <summary>
        /// Returns a DataSet with all the payment information for a certain invoice
        /// </summary>
        /// <param name="invoicePK">Invoice ID</param>
        /// <param name="includeDetails">Should detailed information be included?</param>
        /// <returns>DataSet with payments table</returns>
        public DataSet GetPaymentsByInvoicePK(Guid invoicePK, bool includeDetails)
        {
            if (includeDetails)
            {
                using (var command = NewDbCommand("SELECT * FROM Payments where fk_invoices = @fk"))
                {
                    AddDbCommandParameter(command, "@fk", invoicePK);
                    return ExecuteQuery(command, "payments");
                }
            }

            using (var comSelect = NewDbCommand("SELECT pk_payments, fk_invoices, dReceived, nAmount, cCurrency, nExchangeRate, iPayType FROM Payments where fk_invoices = @fk"))
            {
                AddDbCommandParameter(comSelect, "@fk", invoicePK);
                return ExecuteQuery(comSelect, "payments");
            }
        }
    }
}