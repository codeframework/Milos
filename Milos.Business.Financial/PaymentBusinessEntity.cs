using System;
using Milos.BusinessObjects;
using Milos.Core.Utilities;

namespace Milos.Business.Financial
{
    /// <summary>
    /// Payment object.
    /// </summary>
    public class PaymentBusinessEntity : BusinessEntity
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public PaymentBusinessEntity() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Item ID</param>
        public PaymentBusinessEntity(Guid id) : base(id) { }

        /// <summary>
        /// Invoice ID (PK) for (optionally) linked invoices
        /// </summary>
        public Guid InvoiceId
        {
            get => ReadFieldValue<Guid>("fk_invoices");
            set => WriteFieldValue("fk_invoices", value);
        }

        /// <summary>
        /// Date received
        /// </summary>
        public DateTime DateReceived
        {
            get => ReadFieldValue<DateTime>("dReceived");
            set => WriteFieldValue("dReceived", value);
        }

        /// <summary>
        /// Date deposited
        /// </summary>
        public DateTime DateDeposited
        {
            get => ReadFieldValue<DateTime>("dDeposited");
            set => WriteFieldValue("dDeposited", value);
        }

        /// <summary>
        /// Payment amount
        /// </summary>
        public decimal Amount
        {
            get => ReadFieldValue<decimal>("nAmount");
            set => WriteFieldValue("nAmount", value);
        }

        /// <summary>
        /// Currency
        /// </summary>
        public string Currency
        {
            get => ReadFieldValue<string>("cCurrency");
            set => WriteFieldValue("cCurrency", value);
        }

        /// <summary>
        /// Exchange Rate
        /// </summary>
        public decimal ExchangeRate
        {
            get => ReadFieldValue<decimal>("nExchangeRate");
            set => WriteFieldValue("nExchangeRate", value);
        }

        /// <summary>
        /// Payment Type
        /// </summary>
        public PayType PaymentType
        {
            get => ReadFieldValue<PayType>("iPayType");
            set => WriteFieldValue("iPayType", value);
        }

        /// <summary>
        /// Check Number
        /// </summary>
        public string CheckNumber
        {
            get => ReadFieldValue<string>("cCheckNumber");
            set => WriteFieldValue("cCheckNumber", value);
        }

        /// <summary>
        /// Check Number
        /// </summary>
        public string Comment
        {
            get => ReadFieldValue<string>("tComment");
            set => WriteFieldValue("tComment", value);
        }

        /// <summary>
        /// Credit Card Number
        /// </summary>
        public string CreditCardNumber
        {
            get => ReadFieldValue<string>("cCreditCardNumber");
            set => WriteFieldValue("cCreditCardNumber", value);
        }

        /// <summary>
        /// Last 5 Digits of Credit Card Number
        /// </summary>
        public string CreditCardNumber5Digits
        {
            get
            {
                var creditCardNumber = CreditCardNumber;
                creditCardNumber = creditCardNumber.Replace(" ", "");
                if (creditCardNumber.Length > 5) creditCardNumber = creditCardNumber.Substring(creditCardNumber.Length - 5, 5);
                return creditCardNumber;
            }
        }

        /// <summary>
        /// Credit card type
        /// </summary>
        public CreditCardTypeEnum CreditCardType
        {
            get
            {
                var creditCardNumber = CreditCardNumber.Substring(0, 1);
                switch (creditCardNumber)
                {
                    case "3":
                        return CreditCardTypeEnum.AmericanExpress;
                    case "4":
                        return CreditCardTypeEnum.Visa;
                    case "5":
                        return CreditCardTypeEnum.MasterCard;
                    case "6":
                        return CreditCardTypeEnum.DiscoverCard;
                    default:
                        return CreditCardTypeEnum.UnknownCard;
                }
            }
        }

        /// <summary>
        /// Credit card type text
        /// </summary>
        public string CreditCardTypeText => StringHelper.SpaceCamelCase(CreditCardType.ToString());

        /// <summary>
        /// Credit Card Name
        /// </summary>
        public string CreditCardName
        {
            get => ReadFieldValue<string>("cCreditCardName");
            set => WriteFieldValue("cCreditCardName", value);
        }

        /// <summary>
        /// Credit Card Expiration Date
        /// </summary>
        public string CreditCardExpirationDate
        {
            get => ReadFieldValue<string>("cCreditCardExpDate");
            set => WriteFieldValue("cCreditCardExpDate", value);
        }

        /// <summary>
        /// Required method used to instantiate the business object component of this entity
        /// </summary>
        /// <returns>Business object</returns>
        public override IBusinessObject GetBusinessObject() => new PaymentBusinessObject();
    }

    /// <summary>
    /// Payment type
    /// </summary>
    public enum PayType
    {
        /// <summary>
        /// Credit Card
        /// </summary>
        CreditCard,

        /// <summary>
        /// Check
        /// </summary>
        Check,

        /// <summary>
        /// Cash
        /// </summary>
        Cash,

        /// <summary>
        /// Wire transfer
        /// </summary>
        Transfer,

        /// <summary>
        /// Credit applied as a payment. This could be a manual adjustment
        /// </summary>
        StoreCredit
    }

    /// <summary>
    /// Credit card type (such as Visa, MasterCard,...)
    /// </summary>
    public enum CreditCardTypeEnum
    {
        /// <summary>
        /// Visa Card
        /// </summary>
        Visa,

        /// <summary>
        /// Master Card
        /// </summary>
        MasterCard,

        /// <summary>
        /// Amex
        /// </summary>
        AmericanExpress,

        /// <summary>
        /// Discover card
        /// </summary>
        DiscoverCard,

        /// <summary>
        /// Unknown credit card
        /// </summary>
        UnknownCard
    }

    [Obsolete("Use PaymentBusinessEntity instead.")]
    public class PaymentEntity : PaymentBusinessEntity { }
}