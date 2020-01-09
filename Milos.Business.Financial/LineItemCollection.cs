using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Milos.BusinessObjects;

namespace Milos.Business.Financial
{
    /// <summary>
    /// Collections of line items in an invoice
    /// </summary>
    public class LineItemCollection : EntitySubItemCollection, ILineItemCollection
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentEntity">Parent Entity</param>
        public LineItemCollection(IBusinessEntity parentEntity) : base(parentEntity) { }

        /// <summary>
        /// Defines whether or not this collection automatically maintains integrity of line numbers.
        /// If true (default), duplicate line numbers as well as "holes" in the number sequence are fixed automatically.
        /// </summary>
        public bool MaintainLineNumberIntegrity { get; set; } = true;

        /// <summary>
        /// Configures the current object
        /// </summary>
        protected override void Configure()
        {
            PrimaryKeyField = "pk_lineitems";
            ForeignKeyField = "fk_invoices";
            ParentTableName = "Invoices";
            ParentTablePrimaryKeyField = "pk_invoices";
        }

        public new ILineItem GetItemByIndex(int index) => (ILineItem)GetItemByIndex(index, false);

        /// <summary>
        /// Returns a new item that is used as a member of the collection
        /// </summary>
        /// <returns></returns>
        public override IEntitySubItemCollectionItem GetItemObject() => new LineItemEntity(this);

        /// <summary>
        /// The indexer must be overridden to return the appropriate type
        /// </summary>
        public new ILineItem this[int index] => (LineItemEntity) GetItemByIndex(index);

        /// <summary>
        /// Adds a line number to the newly created item
        /// </summary>
        /// <returns>Line item</returns>
        public new ILineItem Add()
        {
            var newLine = (LineItemEntity) base.Add();
            newLine.LineNumber = Count; // 1-based numbering!!!
            return newLine;
        }

        /// <summary>
        /// Remove an item from the collection.
        /// </summary>
        /// <param name="index"></param>
        /// <remarks>
        /// CL on 07/15/2005
        ///   A line item may have children lines. In such case, the children
        ///   must be removed as well. Notice that a child line could 
        ///   potentially have children of its own, so those should be removed as well.
        ///   The logic here should take care of all that.
        ///   
        ///   Notice that as we remove things from the collection, the indexes
        ///   get re-evaluated. That means we cannot just loop through the collection
        ///   removing things because the internal data of the virtual collection changes,
        ///   and therefore the iterator won't work right any longer.
        /// </remarks>
        public override void Remove(int index)
        {
            // Keep track of the PK to remove.
            // We cannot rely on the index because we'll remove any
            // children the line may have, and that may change the indexing on the collection.
            var lineToRemove = this[index].PK;

            // We grab the line that is to be removed so that we can grab its children as well.
            var line = this[index];
            var children = line.GetChildLineItems();
            var pksToRemove = new List<Guid>();
            pksToRemove.AddRange(children.Select(t => t.PK));

            var lineCounter = 0;

            // We keep a loop running until we've removed all the PKs.
            while (pksToRemove.Count > 0)
            {
                var pkToCheck = this[lineCounter].PK;

                // We check whether the line being evaluated is part of the list of 
                // PKs to be removed.
                if (pksToRemove.Contains(pkToCheck))
                {
                    // We found a winner, so remove it from the collection.
                    // Notice that at this point we'll be calling the Remove method
                    // recursively. That means we'll take care of parent, child, grandchild, etc.
                    Remove(lineCounter);
                    // Also remove it from the list of PKs to be removed.
                    pksToRemove.Remove(pkToCheck);
                    // Since we've found our guy, reset line counter 
                    // so that we start iterating the collection from the top.
                    lineCounter = 0;
                }
                else
                    // If we did not find our guy, we increment the lineCounter and will keep looking through the collection.
                    lineCounter++;
            }

            // At this point we should be done removing all the line items hanging off 
            // the "parent" line, so we go ahead and find it on the collection, and remove it.
            // We iterate through the collection because the line's index probably won't 
            // be what it used to be when the Remove method was first called.
            for (var line2Counter = 0; line2Counter < Count; line2Counter++)
                if (this[line2Counter].PK == lineToRemove)
                {
                    // We call Remove on the base-class because we don't this local version (which is recursive) to run again.
                    base.Remove(index);
                    break;
                }
        }

        /// <summary>
        /// Inserts a new line right after a given line.
        /// </summary>
        /// <param name="line">New line item</param>
        /// <returns></returns>
        public virtual ILineItem InsertAfter(ILineItem line)
        {
            var newLine = (LineItemEntity) Add();

            // Default line number to the last line.
            // (There's a flaw on the logic that validates the line number
            // where it doesn't work right if LineNumber is zero).
            // TODO: Fix this at some point, since it has a performance hit,
            //       given the lines of code it has to execute in order to handle the line number).
            newLine.LineNumber = Count;

            // The new line should be placed immediately under the line being passed.
            newLine.LineNumber = line.LineNumber + 1;

            return newLine;
        }

        /// <summary>
        /// Inserts a new line right before a given line.
        /// </summary>
        /// <param name="line">New line item</param>
        /// <returns></returns>
        public virtual ILineItem InsertBefore(ILineItem line)
        {
            var newLine = (LineItemEntity) Add();

            // Default line number to the last line.
            // (There's a flaw on the logic that validates the line number
            // where it doesn't work right if LineNumber is zero).
            // TODO: Fix this at some point, since it has a performance hit,
            //       given the lines of code it has to execute in order to handle the line number).
            newLine.LineNumber = Count;

            // The new line should be placed immediately before the line being passed.
            newLine.LineNumber = line.LineNumber == 1 ? 1 : line.LineNumber;

            return newLine;
        }

        public new IEnumerator<ILineItem> GetEnumerator()
        {
            for (var count = 0; count < Count; count++)
                yield return this[count];
        }
    }

    /// <summary>
    /// Item within the line item collection
    /// (represents a row in an invoice)
    /// </summary>
    public class LineItemEntity : EntitySubItemCollectionItem, ILineItem
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentCollection">Parent Collection</param>
        public LineItemEntity(IEntitySubItemCollection parentCollection) : base(parentCollection) { }

        /// <summary>
        /// Item number/identifier (character)
        /// </summary>
        public string ItemNumber
        {
            get => ReadFieldValue<string>("citemid");
            set => WriteFieldValue("citemid", value);
        }

        /// <summary>
        /// Item SKU (id)
        /// </summary>
        public string SKU
        {
            get => ItemNumber;
            set => ItemNumber = value;
        }

        /// <summary>
        /// Foreign key for a potentially linked item of an item database
        /// </summary>
        public Guid ItemId
        {
            get => ReadFieldValue<Guid>("fk_items");
            set => WriteFieldValue("fk_items", value);
        }

        /// <summary>
        /// Possible self-reference to a hierarchical parent item.
        /// </summary>
        /// <remarks>
        /// Items with a self reference to a parent item are special cases.
        /// For instance, an item may be a modifier for another item. 
        /// Perhaps someone purchased a pair of pants (which would be one line item)
        /// and had some custom work done on them (which would be another line item 
        /// linked to the first one).
        /// </remarks>
        public Guid ParentItemId
        {
            get => ReadFieldValue<Guid>("fk_parentitem");
            set => WriteFieldValue("fk_parentitem", value);
        }

        /// <summary>
        /// Returns the parent line item in a hierarchical line item scenario.
        /// </summary>
        /// <remarks>Returns null if the item does not have a parent.</remarks>
        public ILineItem ParentItem
        {
            get
            {
                var parentId = ParentItemId;
                if (parentId != Guid.Empty)
                {
                    var currentCollection = (LineItemCollection) ParentCollection;
                    foreach (var currentItem in currentCollection)
                        if (currentItem.PK == parentId)
                            // We found it
                            return currentItem;

                    // We would only get here if we didn't find the item
                    return null;
                }

                return null;
            }
        }

        /// <summary>
        /// Item title (name)
        /// </summary>
        public string ItemTitle
        {
            get => ReadFieldValue<string>("cItemTitle");
            set => WriteFieldValue("cItemTitle", value);
        }

        /// <summary>
        /// Item description
        /// </summary>
        public string ItemDescription
        {
            get => ReadFieldValue<string>("cDescription");
            set => WriteFieldValue("cDescription", value);
        }

        /// <summary>
        /// Item description (HTML formatting)
        /// </summary>
        public string ItemDescriptionHtml => ReadFieldValue<string>("cDescription").Replace("\n", "<br>");

        /// <summary>
        /// Sequential line number of the line item
        /// </summary>
        /// <remarks>
        /// This is a 1-based list since this is not a technical thing, but 
        /// a value with business meaning.
        /// </remarks>
        public int LineNumber
        {
            get => ReadFieldValue<int>("nLineNumber");
            set
            {
                if (!((LineItemCollection) ParentCollection).MaintainLineNumberIntegrity)
                {
                    // We do not care about line number integrity
                    // However, we still want the other lines to move 'out of the way'
                    CheckForDuplicateLineNumbers(value, LineNumber, true);
                    WriteFieldValue("nLineNumber", value);
                }
                else
                {
                    // We need to make sure the line number is valid
                    if (value > 0 && value <= ParentCollection.Count)
                    {
                        // We need to make sure we do not create duplicate line numbers
                        CheckForDuplicateLineNumbers(value, LineNumber, false);
                        WriteFieldValue("nLineNumber", value);
                        //this.CheckForHolesInLineNumberSequence();
                    }
                    else
                        throw new ArgumentOutOfRangeException("LineNumber", value, "Line number out of valid range.");
                }
            }
        }

        /// <summary>
        /// Item quantity
        /// </summary>
        public decimal Quantity
        {
            get => ReadFieldValue<decimal>("nQuantity");
            set => WriteFieldValue("nQuantity", value);
        }

        /// <summary>
        /// Item price (single item)
        /// </summary>
        public decimal Price
        {
            get => ReadFieldValue<decimal>("nPrice");
            set => WriteFieldValue("nPrice", value);
        }

        /// <summary>
        /// Item total (current line only)
        /// </summary>
        public decimal TotalWithoutTax => Price * Quantity;

        /// <summary>
        /// Item total (current line only) including sales tax (if applicable)
        /// </summary>
        public decimal Total => TotalWithoutTax + TotalWithoutTax * (TaxRate / 100);

        /// <summary>
        /// Tax amount (current line only) if sales tax applies
        /// </summary>
        public decimal TaxAmount => Taxable != true ? 0 : TotalWithoutTax * (TaxRate / 100);

        /// <summary>
        /// Is the current item taxable (sales tax)?
        /// </summary>
        public bool Taxable
        {
            get => ReadFieldValue<bool>("bTaxable");
            set => WriteFieldValue("bTaxable", value);
        }

        /// <summary>
        /// Tax Rate (only applicable if the item is taxable)
        /// </summary>
        public decimal TaxRate
        {
            get => Taxable ? ReadFieldValue<decimal>("nTaxRate") : 0;
            set
            {
                WriteFieldValue("nTaxRate", value);
                Taxable = value != 0;
            }
        }

        /// <summary>
        /// Account
        /// </summary>
        public string Account
        {
            get => ReadFieldValue<string>("cAccount");
            set => WriteFieldValue("cAccount", value);
        }

        /// <summary>
        /// Returns an array of child line items in hierarchical scenarios
        /// </summary>
        /// <returns>Array of child line items</returns>
        /// <remarks>This method is not very fast and should be used as little as possible.</remarks>
        public List<ILineItem> GetChildLineItems()
        {
            var currentTable = CurrentRow.Table;
            var parentFieldName = "fk_parentitem"; // TODO: Make this more generic supporting mapping!!!
            var childRows = currentTable.Select($"{parentFieldName} = '{Id}'", "nLineNumber");
            if (childRows.Length > 0)
            {
                var counter = -1;
                var entities = new List<ILineItem>();
                foreach (var childRow in childRows)
                {
                    counter++;
                    var pkFieldName = "Pk_LineItems"; // TODO: Make this more generic supporting mapping!!!
                    foreach (LineItemEntity currentEntity in (LineItemCollection) ParentCollection)
                        if (currentEntity.PK == (Guid) childRow[pkFieldName])
                        {
                            entities[counter] = currentEntity;
                            break;
                        }
                }

                return entities;
            }

            return new List<ILineItem>();
        }

        /// <summary>
        /// This method checks whether a line number with the specified
        /// new number already exists in the line items collection.
        /// </summary>
        /// <param name="newNumber">New line number</param>
        /// <param name="currentNumber">Current/old line number</param>
        /// <param name="forceIntegrity">Force integrity, even if integrity checks are turned off?</param>
        protected virtual void CheckForDuplicateLineNumbers(int newNumber, int currentNumber, bool forceIntegrity)
        {
            if (!forceIntegrity)
                if (!((LineItemCollection) ParentCollection).MaintainLineNumberIntegrity)
                    // Nothing to do since this collection does not care about line number integrity
                    return;

            // TODO: This should be rewritten more generically
            //       1) There should be a method on the collection to get all the records 
            //          associated with it.
            //       2) There should be a way to easily get a mapped field name
            var tabCurrent = CurrentRow.Table;
            var fieldName = "nLineNumber";

            if (newNumber < currentNumber)
            {
                // Moving a line "up" (lower line number)
                var rowsToChange = tabCurrent.Select($"{fieldName} >= {newNumber.ToString(CultureInfo.InvariantCulture)} and {fieldName} < {currentNumber.ToString(CultureInfo.InvariantCulture)}");
                foreach (var rowToChange in rowsToChange)
                    rowToChange[fieldName] = (int) rowToChange[fieldName] + 1;
            }
            else
            {
                // Moving a line "down" (higher line number)

                if (currentNumber == 0)
                    WriteFieldValue("nLineNumber", ParentCollection.Count);
                else
                {
                    var rowsToChange = tabCurrent.Select($"{fieldName} >= {currentNumber.ToString(CultureInfo.InvariantCulture)} and {fieldName} < {newNumber.ToString(CultureInfo.InvariantCulture)}");
                    foreach (var rowToChange in rowsToChange)
                        rowToChange[fieldName] = (int) rowToChange[fieldName] - 1;
                }
            }
        }

        /// <summary>
        /// Verifies that there are no "holes" in the sequence of line numbers
        /// </summary>
        protected virtual void CheckForHolesInLineNumberSequence()
        {
            // TODO: This should be rewritten more generically
            //       1) There should be a method on the collection to get all the records 
            //          associated with it.
            //       2) There should be a way to easily get a mapped field name
            var tabCurrent = CurrentRow.Table;
            var fieldName = "nLineNumber";
            var lineCounter = 0;
            var currentView = tabCurrent.DefaultView; //new DataView(tabCurrent,"",fieldName,DataViewRowState.OriginalRows);

            foreach (DataRowView currentRow in currentView)
                if (currentRow[fieldName] == DBNull.Value)
                    currentRow[fieldName] = currentRow.DataView.Count;
                else
                {
                    lineCounter++;
                    if ((int) currentRow[fieldName] != lineCounter)
                        currentRow[fieldName] = lineCounter;
                }
        }

        /// <summary>
        /// Moves the current line item up by decreasing its line number
        /// </summary>
        /// <returns>True of false</returns>
        public virtual bool MoveUp()
        {
            if (LineNumber > 1)
            {
                LineNumber--;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Moves the current line item up by increasing its line number
        /// </summary>
        /// <returns>True of false</returns>
        public virtual bool MoveDown()
        {
            // TODO: This needs to also support hierarchical line items!!!
            //       Example: A line item is a pizza with two toppings as related line items
            //                When we now move the pizza down, we do not want the pizza to 
            //                appear below the first topping, but below the next pizza

            if (LineNumber < ParentCollection.Count) // 1-based!
            {
                LineNumber++;
                return true;
            }

            return false;
        }
    }
}