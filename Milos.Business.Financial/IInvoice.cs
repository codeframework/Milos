using System;
using System.Data;
using Milos.BusinessObjects;

namespace Milos.Business.DocumentManagement
{
    /// <summary>
    /// Invoice Interface
    /// </summary>
    public interface IInvoice
    {
        /// <summary>
        /// Account (number) the invoice is linked to as a whole
        /// </summary>
        string Account { get; set; }
        /// <summary>
        /// Address entity linked to this invoice
        /// </summary>
        AddressEntity Address { get; }
        /// <summary>
        /// Address FK linked to this invoice
        /// </summary>
        Guid AddressFk { get; set; }
        /// <summary>
        /// Bill date (usually same as invoice date)
        /// </summary>
        DateTime BillDate { get; set; }
        /// <summary>
        /// Customer Message
        /// </summary>
        string CustomerMessage { get; set; }
        /// <summary>
        /// Discount days
        /// </summary>
        int DiscountDays { get; set; }
        /// <summary>
        /// Discount percentage
        /// </summary>
        decimal DiscountPercentage { get; set; }
        /// <summary>
        /// Invoice date
        /// </summary>
        DateTime InvoiceDate { get; set; }
        /// <summary>
        /// Invoice due date
        /// </summary>
        DateTime InvoiceDueDate { get; }
        /// <summary>
        /// Invoice number
        /// </summary>
        string InvoiceNumber { get; set; }
        /// <summary>
        /// Total amount for all items (no tax)
        /// </summary>
        decimal ItemTotal { get; }
        /// <summary>
        /// Line items collection
        /// </summary>
        ILineItemCollection LineItems { get; }
        /// <summary>
        /// Memo (free form text)
        /// </summary>
        string Memo { get; set; }
        /// <summary>
        /// Name linked to this invoice
        /// </summary>
        Guid NameFk { get; set; }
        /// <summary>
        /// Net days
        /// </summary>
        int NetDays { get; set; }
        /// <summary>
        /// Invoice status
        /// </summary>
        InvoiceStatus Paid { get; set; }
        /// <summary>
        /// Purchase Order Number
        /// </summary>
        string PONumber { get; set; }
        /// <summary>
        /// Shipping and handling amount
        /// </summary>
        decimal ShippingAndHandling { get; }
        /// <summary>
        /// Is the invoice taxable?
        /// </summary>
        bool Taxable { get; set; }
        /// <summary>
        /// Invoice tax rate
        /// </summary>
        decimal TaxRate { get; set; }
        /// <summary>
        /// Tax text
        /// </summary>
        string TaxText { get; set; }
        /// <summary>
        /// Invoice template
        /// </summary>
        string Template { get; set; }
        /// <summary>
        /// Payment terms
        /// </summary>
        string Terms { get; set; }
        /// <summary>
        /// Is the invoice yet to be printed?
        /// </summary>
        bool ToBePrinted { get; set; }
        /// <summary>
        /// Is the invoice yet to be sent?
        /// </summary>
        bool ToBeSent { get; set; }
        /// <summary>
        /// Invoice total
        /// </summary>
        decimal Total { get; }
        /// <summary>
        /// Total tax amount of the invoice
        /// </summary>
        decimal TotalTax { get; }
        /// <summary>
        /// Total with shipping and handling but without tax
        /// </summary>
        decimal TotalWithSHNoTax { get; }
    }


    /// <summary>
    /// Basic interface for a line item collection
    /// </summary>
    public interface ILineItemCollection : IEntitySubItemCollection 
    {
        /// <summary>
        /// Should line number integrity be maintained?
        /// </summary>
        bool MaintainLineNumberIntegrity { get; set; }
        /// <summary>
        /// Strongly typed indexer (returns a specific line item instance)
        /// </summary>
        new ILineItem this[int index] { get; }
        /// <summary>
        /// Strongly typed Add() method
        /// </summary>
        /// <returns>New line item instance</returns>
        new ILineItem Add();
		/// <summary>
		/// Inserts a new line right after a given line.
		/// </summary>
		/// <param name="line">The line that is a reference as to where the new line must be inserted after.</param>
		/// <returns>New line item instance</returns>
		ILineItem InsertAfter(ILineItem line);
		/// <summary>
		/// Inserts a new line right before a given line.
		/// </summary>
		/// <param name="line">The line that is a reference as to where the new line must be inserted before.</param>
		/// <returns>New line item instance</returns>
		ILineItem InsertBefore(ILineItem line);
    }


    /// <summary>
    /// Basic line item interface
    /// </summary>
    public interface ILineItem : IEntitySubItemCollectionItem
    {
        /// <summary>
        /// Linked account number
        /// </summary>
        string Account { get; set; }
        /// <summary>
        /// Retrieves an array of all child line items in a hierarchical
        /// line item scenario
        /// </summary>
        /// <returns>Array of line items</returns>
        ILineItem[] GetChildLineItems();
        /// <summary>
        /// Item description
        /// </summary>
        string ItemDescription { get; set; }
        /// <summary>
        /// Item description HTML formatted
        /// </summary>
        string ItemDescriptionHtml { get; }
        /// <summary>
        /// Item ID
        /// </summary>
        Guid ItemId { get; set; }
        /// <summary>
        /// Item number
        /// </summary>
        string ItemNumber { get; set; }
        /// <summary>
        /// Item title
        /// </summary>
        string ItemTitle { get; set; }
        /// <summary>
        /// Line number
        /// </summary>
        int LineNumber { get; set; }
        /// <summary>
        /// Moves the line down
        /// </summary>
        /// <returns></returns>
        bool MoveDown();
        /// <summary>
        /// Moves the line up
        /// </summary>
        /// <returns></returns>
        bool MoveUp();
        /// <summary>
        /// Parent line item entity
        /// </summary>
        ILineItem ParentItem { get; }
        /// <summary>
        /// Parent item ID (only used in hierarchical line item scenarios)
        /// </summary>
        Guid ParentItemId { get; set; }
        /// <summary>
        /// Item price
        /// </summary>
        decimal Price { get; set; }
        /// <summary>
        /// Item quantity
        /// </summary>
        decimal Quantity { get; set; }
        /// <summary>
        /// Item SKU (same as item number)
        /// </summary>
        string SKU { get; set; }
        /// <summary>
        /// Is the item taxable?
        /// </summary>
        bool Taxable { get; set; }
        /// <summary>
        /// What is the tax amount
        /// </summary>
        decimal TaxAmount { get; }
        /// <summary>
        /// What is the item's tax rate?
        /// </summary>
        decimal TaxRate { get; set; }
        /// <summary>
        /// Line total
        /// </summary>
        decimal Total { get; }
        /// <summary>
        /// Line total without tax
        /// </summary>
        decimal TotalWithoutTax { get; }
    }


    /// <summary>
    /// Invoice business object interface
    /// </summary>
    public interface IInvoiceBusinessObject : IBusinessObject 
    {
        /// <summary>
        /// Returns a quick list of all invoices for a certain name
        /// </summary>
        /// <param name="namePk">Name</param>
        /// <returns>DataSet</returns>
        DataSet GetQuickListByNamePK(Guid namePk);
        /// <summary>
        /// Returns a detailed list of all invoices for a certain name
        /// </summary>
        /// <param name="namePk">Name</param>
        /// <returns>DataSet</returns>
        DataSet GetDetailedListByNamePK(Guid namePk);
    }
}