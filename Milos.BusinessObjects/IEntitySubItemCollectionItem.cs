using System;
using System.Data;

namespace Milos.BusinessObjects
{
    /// <summary>
    /// This interface defines the most fundamental interface used by a collection entity
    /// </summary>
    public interface IEntitySubItemCollectionItem
    {
        /// <summary>
        /// State (new, updated, deleted,...) of the current item.
        /// </summary>
        DataRowState ItemState { get; }

        /// <summary>
        /// Primary key of the entity
        /// </summary>
        Guid PK { get; }

        /// <summary>
        /// Primary key of the entity
        /// </summary>
        int PKInteger { get; }

        /// <summary>
        /// Primary key of the entity
        /// </summary>
        string PKString { get; }

        /// <summary>
        /// Primary key of the entity
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Name of the primary key field
        /// </summary>
        string PrimaryKeyField { get; set; }

        /// <summary>
        /// Sets the datarow this entity represents
        /// </summary>
        /// <param name="currentRow">Row object</param>
        void SetCurrentRow(DataRow currentRow);

        /// <summary>
        /// Returns whether or not that field's value is currently null/nothing
        /// </summary>
        /// <param name="fieldName">Field name as it appears in the data set</param>
        /// <returns>True or false</returns>
        bool IsFieldNull(string fieldName);

        /// <summary>
        /// Removes the current item from the collection
        /// </summary>
        void Remove();
    }
}