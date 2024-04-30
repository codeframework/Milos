namespace Milos.Business.Financial;

/// <summary>
/// The class encapsulates basic invoice object functionality
/// </summary>
public class InvoiceBusinessObject : BusinessObject, IInvoiceBusinessObject
{
    /// <summary>
    /// Configures settings in the current BO
    /// </summary>
    protected override void Configure()
    {
        MasterEntity = "Invoices";
        PrimaryKeyField = "pk_invoices";
    }

    /// <summary>
    /// Saves the line items
    /// </summary>
    /// <param name="parentPk">Invoice PK</param>
    /// <param name="existingDataSet">Existing data set</param>
    /// <returns>True or False</returns>
    protected override bool SaveSecondaryTables(Guid parentPk, DataSet existingDataSet) => SaveTable(existingDataSet.Tables["LineItems"], "pk_lineitems");

    /// <summary>
    /// Saves the line items
    /// </summary>
    /// <param name="parentPk">Invoice PK</param>
    /// <param name="existingDataSet">Existing data set</param>
    protected override void AddNewSecondaryTables(Guid parentPk, DataSet existingDataSet)
    {
        NewSecondaryEntity("lineitems", existingDataSet);

        // CL on 07/13/2005
        //	 We want the DefaultView to be sorted by nLineNumber since that's what we
        //   use to run through the lines on the collection in the same order 
        //	 they were added to it.
        existingDataSet.Tables["LineItems"].DefaultView.Sort = "nLineNumber";
    }

    /// <summary>
    /// Loads secondary tables
    /// </summary>
    /// <param name="parentPk">Parent PK</param>
    /// <param name="existingDataSet">Existing DataSet</param>
    protected override void LoadSecondaryTables(Guid parentPk, DataSet existingDataSet)
    {
        base.LoadSecondaryTables(parentPk, existingDataSet);
        LoadLineItemsSecondaryTable(parentPk, existingDataSet);
    }

    /// <summary>
    /// Loads the line items for an invoice
    /// </summary>
    /// <param name="parentPk">Parent PK</param>
    /// <param name="existingDataSet">Existing DataSet</param>
    /// <remarks>
    /// This method has been designed to be overridden in subclasses.
    /// Override this method if you intend to use a different line item collection.
    /// </remarks>
    protected virtual void LoadLineItemsSecondaryTable(Guid parentPk, DataSet existingDataSet)
    {
        using var command = GetMultipleRecordsByKeyCommand("LineItems", "*", "fk_invoices", parentPk);
        ExecuteQuery(command, "LineItems", existingDataSet);

        // CL on 07/13/2005
        //	 We want the DefaultView to be sorted by nLineNumber since that's what we
        //   use to run through the lines on the collection in the same order 
        //	 they were added to it.
        existingDataSet.Tables["LineItems"].DefaultView.Sort = "nLineNumber";
    }

    /// <summary>
    /// Returns a quick list of all invoices for a certain name
    /// </summary>
    /// <param name="namePk">Name</param>
    /// <returns>DataSet</returns>
    public DataSet GetQuickListByNamePK(Guid namePk)
    {
        using var command = NewDbCommand($"SELECT pk_invoices, cInvoiceNumber, dBillDate, iPaid, nNetDays FROM {MasterEntity} WHERE fk_name = @FK and (iPaid = @One or iPaid = @Zero) order by dBillDate desc");
        AddDbCommandParameter(command, "@FK", namePk);
        AddDbCommandParameter(command, "@One", 1);
        AddDbCommandParameter(command, "@Zero", 0);
        return ExecuteQuery(command, "Invoices");
    }

    /// <summary>
    /// Returns a detailed list of all invoices for a certain name
    /// </summary>
    /// <param name="namePk">Name</param>
    /// <returns>DataSet</returns>
    public DataSet GetDetailedListByNamePK(Guid namePk)
    {
        using var command = NewDbCommand("SELECT Invoices.PK_Invoices, Invoices.FK_Name, Invoices.FK_Address, Invoices.FK_Subscription, Invoices.cInvoiceNumber, Invoices.dBillDate, Invoices.iPaid, Invoices.bToBePrinted, Invoices.bTobeSent, SUM( CASE WHEN LineItems.bTaxable = 1 THEN nQuantity * nPrice * (1 + (LineItems.nTaxRate / 100)) ELSE nQuantity * nPrice END ) AS Total FROM Invoices JOIN LineItems ON FK_Invoices = PK_Invoices WHERE Invoices.FK_Name = @NamePK GROUP BY Invoices.PK_Invoices, Invoices.FK_Name, Invoices.FK_Address, Invoices.FK_Subscription, Invoices.cInvoiceNumber, Invoices.dBillDate, Invoices.iPaid, Invoices.bToBePrinted, Invoices.bTobeSent, FK_Invoices");
        AddDbCommandParameter(command, "@NamePK", namePk);
        return ExecuteQuery(command, "Invoices");
    }
}