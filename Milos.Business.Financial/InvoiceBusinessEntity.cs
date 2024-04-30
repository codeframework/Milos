
namespace Milos.Business.Financial;

/// <summary>
/// Invoice object.
/// </summary>
public class InvoiceEntity : BusinessEntity, IInvoice
{
    /// <summary>
    /// Constructor
    /// </summary>
    public InvoiceEntity() { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="id">Item id</param>
    public InvoiceEntity(Guid id) : base(id) { }

    /// <summary>
    /// Required method used to instantiate the business object component of this entity
    /// </summary>
    /// <returns>Business object</returns>
    public override IBusinessObject GetBusinessObject() => new InvoiceBusinessObject();

    /// <summary>
    /// Loads collections attached to the current entity
    /// </summary>
    protected override void LoadSubItemCollections() => LoadLineItemCollection();

    /// <summary>
    /// This method loads the line item collection.
    /// </summary>
    /// <remarks>
    /// This method has been designed to be overridden in subclasses.
    /// Override this method if you intend to use a different line item collection.
    /// </remarks>
    protected virtual void LoadLineItemCollection()
    {
        LineItems = new LineItemCollection(this);
        LineItems.SetTable(GetInternalData().Tables["lineItems"]);
    }

    /// <summary>
    ///  Line items collection
    /// </summary>
    public ILineItemCollection LineItems { get; protected set; }

    /// <summary>
    /// Invoice Number
    /// </summary>
    public string InvoiceNumber
    {
        get => ReadFieldValue<string>("cInvoiceNumber");
        set => WriteFieldValue("cInvoiceNumber", value);
    }

    /// <summary>
    /// Invoice/Bill Date
    /// </summary>
    public DateTime InvoiceDate
    {
        get => ReadFieldValue<DateTime>("dBillDate");
        set => WriteFieldValue("dBillDate", value);
    }

    /// <summary>
    /// Invoice/Bill Due Date
    /// </summary>
    public DateTime InvoiceDueDate => InvoiceDate.AddDays(NetDays);

    /// <summary>
    /// Invoice/Bill Date
    /// </summary>
    public DateTime BillDate
    {
        get => ReadFieldValue<DateTime>("dBillDate");
        set => WriteFieldValue("dBillDate", value);
    }

    /// <summary>
    /// Is this invoice taxable?
    /// </summary>
    public bool Taxable
    {
        get => ReadFieldValue<bool>("bTaxable");
        set => WriteFieldValue("bTaxable", value);
    }

    /// <summary>
    /// Tax rate (if taxable)
    /// </summary>
    public decimal TaxRate
    {
        get => ReadFieldValue<decimal>("nTaxRate");
        set => WriteFieldValue("nTaxRate", value);
    }

    /// <summary>
    /// Tax rate (if taxable)
    /// </summary>
    public string TaxText
    {
        get => ReadFieldValue<string>("cTaxText");
        set => WriteFieldValue("cTaxText", value);
    }

    /// <summary>
    /// Invoice Memo (Text)
    /// </summary>
    public string Memo
    {
        get => ReadFieldValue<string>("cMemo");
        set => WriteFieldValue("cMemo", value);
    }

    /// <summary>
    /// Message to the customer
    /// </summary>
    public string CustomerMessage
    {
        get => ReadFieldValue<string>("cCustomerMessage");
        set => WriteFieldValue("cCustomerMessage", value);
    }

    /// <summary>
    /// Has this invoice been paid?
    /// </summary>
    public InvoiceStatus Paid
    {
        get => ReadFieldValue<InvoiceStatus>("iPaid");
        set => WriteFieldValue("iPaid", value);
    }

    /// <summary>
    /// Is this invoice yet to be printed?
    /// </summary>
    public bool ToBePrinted
    {
        get => ReadFieldValue<bool>("bToBePrinted");
        set => WriteFieldValue("bToBePrinted", value);
    }

    /// <summary>
    /// Is this invoice yet to be sent?
    /// </summary>
    public bool ToBeSent
    {
        get => ReadFieldValue<bool>("bToBeSent");
        set => WriteFieldValue("bToBeSent", value);
    }

    /// <summary>
    /// Account (number) the invoice is linked to as a whole
    /// </summary>
    public string Account
    {
        get => ReadFieldValue<string>("cAccount");
        set => WriteFieldValue("cAccount", value);
    }

    /// <summary>
    /// Invoice payment terms
    /// </summary>
    public string Terms
    {
        get => ReadFieldValue<string>("cTerms");
        set => WriteFieldValue("cTerms", value);
    }

    /// <summary>
    /// Net days (payment terms)
    /// </summary>
    public int NetDays
    {
        get => ReadFieldValue<int>("nNetDays");
        set => WriteFieldValue("nNetDays", value);
    }

    /// <summary>
    /// Discount days (payment terms)
    /// </summary>
    public int DiscountDays
    {
        get => ReadFieldValue<int>("nDiscountDays");
        set => WriteFieldValue("nDiscountDays", value);
    }

    /// <summary>
    /// Discount percentage (payment terms)
    /// </summary>
    public decimal DiscountPercentage
    {
        get => ReadFieldValue<decimal>("nDiscountPercentage");
        set => WriteFieldValue("nDiscountPercentage", value);
    }

    /// <summary>
    /// Purchase Order (PO) Number
    /// </summary>
    public string PONumber
    {
        get => ReadFieldValue<string>("cPONumber");
        set => WriteFieldValue("cPONumber", value);
    }

    /// <summary>
    /// Invoice Template (generally used for printing purposes)
    /// </summary>
    public string Template
    {
        get => ReadFieldValue<string>("cTemplate");
        set => WriteFieldValue("cTemplate", value);
    }

    /// <summary>
    /// Invoice sub-total without tax and SH
    /// </summary>
    public decimal ItemTotal => LineItems.Sum(entLine => entLine.TotalWithoutTax);

    /// <summary>
    /// Total amount of sales taxes
    /// </summary>
    public decimal TotalTax => LineItems.Cast<LineItemEntity>().Sum(entLine => entLine.TaxAmount);

    /// <summary>
    /// Invoice total including sh but no tax
    /// </summary>
    public decimal TotalWithSHNoTax => ItemTotal+ ShippingAndHandling;

    /// <summary>
    /// Invoice total including sh and tax
    /// </summary>
    public decimal Total => ItemTotal + ShippingAndHandling + TotalTax;

    /// <summary>
    /// Shipping and Handling charge
    /// </summary>
    public decimal ShippingAndHandling => 0;

    /// <summary>
    /// Address FK
    /// </summary>
    public Guid AddressFk
    {
        get => ReadFieldValue<Guid>("fk_address");
        set => WriteFieldValue("fk_address", value);
    }

    /// <summary>
    /// Name FK
    /// </summary>
    public Guid NameFk
    {
        get => ReadFieldValue<Guid>("fk_name");
        set => WriteFieldValue("fk_name", value);
    }

    /// <summary>
    /// Address
    /// </summary>
    public AddressBusinessEntity Address
    {
        get
        {
            try
            {
                return new AddressBusinessEntity(AddressFk);
            }
            catch
            {
                return null;
            }
        }
    }
}

/// <summary>
/// Invoice Status
/// </summary>
public enum InvoiceStatus
{
    /// <summary>
    /// Unpaid
    /// </summary>
    Unpaid = 0,

    /// <summary>
    /// Paid
    /// </summary>
    Paid = 1,

    /// <summary>
    /// Void
    /// </summary>
    Void = 2,

    /// <summary>
    /// Deleted
    /// </summary>
    Deleted = 3
}