using System;
using System.Data;

namespace Milos.BusinessObjects
{
    /// <summary>
    /// Basic entity sub item collection interface
    /// </summary>
    public interface IEntityXlinkSubItemCollection : IEntitySubItemCollection
    {
        /// <summary>
        /// Sets internally used data tables. This is usually done on or immediately after instantiation.
        /// </summary>
        /// <param name="table">Cross-Link Table between the main entity and the related table.</param>
        /// <param name="xlinkTargetTable">Table that holds the actual data.</param>
        void SetTable(DataTable table, DataTable xlinkTargetTable);
        /// <summary>
        /// This method adds a new record to the x-link DataSet and links to the specified foreign key record
        /// </summary>
        /// <param name="targetItemId">Target item ID (such as the primary of a group when linking to a group record)</param>
        /// <returns>New item</returns>
        IEntitySubItemCollectionItem Add(Guid targetItemId);
        /// <summary>
        /// This method adds a new record to the x-link DataSet and links to the specified foreign key record
        /// </summary>
        /// <param name="targetItemId">Target item ID (such as the primary of a group when linking to a group record)</param>
        /// <returns>New item</returns>
        IEntitySubItemCollectionItem Add(int targetItemId);
        /// <summary>
        /// This method adds a new record to the x-link DataSet and links to the record 
        /// identified by it's descriptive text.
        /// The field used for this operation is defined in the strTargetTextField field.
        /// </summary>
        /// <param name="targetItemText">text used by the target table. For instance, if you want to link to a "People" category, "People" would be the text passed along here.</param>
        /// <returns>New item</returns>
        IEntitySubItemCollectionItem Add(string targetItemText);
    }
}
