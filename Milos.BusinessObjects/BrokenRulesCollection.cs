using System;
using System.Collections;
using System.Data;
using System.Text;
using Milos.Core.Utilities;

namespace Milos.BusinessObjects
{
    /// <summary>
    ///     Broken Business Rules collection class
    /// </summary>
    public class BrokenRulesCollection : ICollection
    {
        /// <summary>
        ///     Stores the ultimate associated business entity this collection lives in.
        ///     If the collection lives in a business entity, this property returns the same
        ///     reference as the BusinessEntity property. If the collection lives in
        ///     either a SubItem or SubItem collection, this property returns the business entity
        ///     that those objects live in.
        /// </summary>
        private BusinessEntity associatedBusinessEntity;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentBusinessEntity">Business entity object this collection lives in.</param>
        public BrokenRulesCollection(BusinessEntity parentBusinessEntity)
        {
            BusinessEntity = parentBusinessEntity;
            if (parentBusinessEntity != null)
                BrokenRulesTableName = parentBusinessEntity.BrokenRulesTableName;
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentBusinessEntitySubItem">Business entity sub item object this collection lives in.</param>
        public BrokenRulesCollection(EntitySubItemCollectionItem parentBusinessEntitySubItem)
        {
            BusinessEntitySubItem = parentBusinessEntitySubItem;
            BrokenRulesTableName = ((BusinessEntity) parentBusinessEntitySubItem.ParentEntity).BrokenRulesTableName;
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parentBusinessEntitySubItemCollection">Business entity sub item collection this collection lives in.</param>
        public BrokenRulesCollection(EntitySubItemCollection parentBusinessEntitySubItemCollection)
        {
            SubItemCollection = parentBusinessEntitySubItemCollection;
            BrokenRulesTableName = ((BusinessEntity) parentBusinessEntitySubItemCollection.ParentEntity).BrokenRulesTableName;
        }

        /// <summary>
        ///     Initializes a new instance of the DeletionBrokenRulesCollection class.
        /// </summary>
        public BrokenRulesCollection(DataSet brokenRulesDataSet) : this((BusinessEntity) null) => InternalDataSet = brokenRulesDataSet;

        /// <summary>
        ///     Gets reference to the underlying dataset that contains the broken rules data.
        /// </summary>
        protected DataSet InternalDataSet { get; }

        /// <summary>
        ///     Gets the name of the broken rules table.
        /// </summary>
        /// <value>The name of the broken rules table.</value>
        public string BrokenRulesTableName { get; protected set; } = "__BrokenRules";

        /// <summary>
        ///     Internal reference to the business entity this collection lives in.
        /// </summary>
        internal BusinessEntity BusinessEntity { get; }

        /// <summary>
        ///     Internal reference to the business entity sub-item this collection lives in.
        /// </summary>
        internal EntitySubItemCollectionItem BusinessEntitySubItem { get; }

        /// <summary>
        ///     Inernal reference to the business entity sub item collection this collection lives in.
        /// </summary>
        internal EntitySubItemCollection SubItemCollection { get; }

        /// <summary>
        ///     Gets the ultimate associated business entity this collection lives in.
        ///     If the collection lives in a business entity, this property returns the same
        ///     reference as the BusinessEntity property. If the collection lives in
        ///     either a SubItem or SubItem collection, this property returns the business entity
        ///     that those objects live in.
        /// </summary>
        /// <value>The associated business entity.</value>
        public BusinessEntity AssociatedBusinessEntity
        {
            get
            {
                if (associatedBusinessEntity == null)
                {
                    if (BusinessEntity != null)
                        associatedBusinessEntity = BusinessEntity;
                    else if (SubItemCollection != null)
                        associatedBusinessEntity = SubItemCollection.ParentEntity as BusinessEntity;
                    else if (BusinessEntitySubItem != null)
                        associatedBusinessEntity = BusinessEntitySubItem.ParentEntity as BusinessEntity;
                }

                return associatedBusinessEntity;
            }
        }

        /// <summary>
        ///     Returns a broken rule by index
        /// </summary>
        public virtual BrokenRule this[int index]
        {
            get
            {
                if (AssociatedBusinessEntity == null) return null;

                var internalData = AssociatedBusinessEntity.GetInternalData();

                if (!DataSetContainsBrokenRulesTable(internalData))
                    // There are no broken rules, so the index is wrong, no matter what
                    throw new IndexOutOfBoundsException("Index out of bounds.");

                var brokenRulesTable = internalData.Tables[BrokenRulesTableName];

                if (BusinessEntity != null)
                    return MakeBrokenRuleForDataRow(brokenRulesTable.Rows[index]);
                if (SubItemCollection != null)
                    return GetBrokenRuleForSubItemCollection(index, brokenRulesTable);
                if (BusinessEntitySubItem != null)
                    return GetBrokenRuleForSubItem(index, brokenRulesTable);
                return null;
            }
        }


        /// <summary>
        ///     Gets a reference to an object that can be used to synchronize access to this collection.
        /// </summary>
        public object SyncRoot
        {
            get
            {
                if (BusinessEntity.GetInternalData().Tables.Contains(BusinessEntity.BrokenRulesTableName))
                    return BusinessEntity.GetInternalData().Tables[BusinessEntity.BrokenRulesTableName].Rows.SyncRoot;
                return null;
            }
        }

        /// <summary>
        ///     Defines whether this class is thread-safe
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                if (BusinessEntity.GetInternalData().Tables.Contains(BusinessEntity.BrokenRulesTableName))
                    return BusinessEntity.GetInternalData().Tables[BusinessEntity.BrokenRulesTableName].Rows.IsSynchronized;
                return false;
            }
        }

        /// <summary>
        ///     Number of broken rules in the collection
        /// </summary>
        public virtual int Count
        {
            get
            {
                var internalDataSet = AssociatedBusinessEntity.GetInternalData();

                if (!DataSetContainsBrokenRulesTable(internalDataSet))
                    // There isn't even a broken rules table in the current data set,
                    // so there certainly isn't any information about what rules
                    // might be broken.
                    return 0;

                if (BusinessEntity != null) return internalDataSet.Tables[BrokenRulesTableName].Rows.Count;
                if (SubItemCollection != null) return CalculateBrokenRulesCountForTable(internalDataSet, SubItemCollection.InternalDataTable.TableName);
                if (BusinessEntitySubItem != null) return CalculateBrokenRulesCountForTable(internalDataSet, BusinessEntitySubItem.TableName);
                return 0;
            }
        }

        /// <summary>
        ///     Copies the items within this collection to a one-dimensional array starting at the specified index
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="index">Index</param>
        public void CopyTo(Array array, int index)
        {
            if (BusinessEntity.GetInternalData().Tables.Contains(BusinessEntity.BrokenRulesTableName))
                BusinessEntity.GetInternalData().Tables[BusinessEntity.BrokenRulesTableName].Rows.CopyTo(array, index);
        }

        /// <summary>
        ///     Returns an enumerator for the business rule collection
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            var totalCount = Count;
            for (var count = 0; count < totalCount; count++)
                yield return this[count];
        }

        /// <summary>
        ///     Returns all rule violations as a string
        /// </summary>
        /// <returns>Text description of all broken rules</returns>
        public string GetAllViolationsHTML() => GetAllViolations(string.Empty, "<br>", false);

        /// <summary>
        ///     Makes a BrokenRule object based on the given row.
        /// </summary>
        /// <param name="brokenRuleRow">The broken rule row.</param>
        /// <returns></returns>
        protected virtual BrokenRule MakeBrokenRuleForDataRow(DataRow brokenRuleRow) => new BrokenRule(brokenRuleRow, this);

        /// <summary>
        ///     Gets the broken rule for sub item collection.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="brokenRulesTable">The broken rules table.</param>
        /// <returns></returns>
        private BrokenRule GetBrokenRuleForSubItemCollection(int index, DataTable brokenRulesTable)
        {
            var brokenRulesCounter = -1;
            foreach (DataRow brokenRule in brokenRulesTable.Rows)
            {
                var currentTableName = SubItemCollection.InternalDataTable.TableName;
                if (StringHelper.Compare(brokenRule["TableName"].ToString(), currentTableName))
                {
                    brokenRulesCounter++;
                    if (brokenRulesCounter == index)
                        return MakeBrokenRuleForDataRow(brokenRule);
                }
            }

            return null;
        }

        /// <summary>
        ///     Gets the broken rule for sub item.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="brokenRulesTable">The broken rules table.</param>
        /// <returns></returns>
        private BrokenRule GetBrokenRuleForSubItem(int index, DataTable brokenRulesTable)
        {
            var brokenRulesCounter = -1;
            var currentTableName = BusinessEntitySubItem.TableName;
            var currentItemIndex = BusinessEntitySubItem.IndexInCollection;
            foreach (DataRow brokenRule in brokenRulesTable.Rows)
                if (StringHelper.Compare(brokenRule["TableName"].ToString(), currentTableName))
                    if ((int) brokenRule["RowIndex"] == currentItemIndex)
                    {
                        brokenRulesCounter++;
                        if (brokenRulesCounter == index)
                            return MakeBrokenRuleForDataRow(brokenRule);
                    }

            return null;
        }

        /// <summary>
        ///     Returns all rule violations as a string
        /// </summary>
        /// <param name="separationStringBefore">String inserted before each violated rule</param>
        /// <param name="separationStringAfter">String inserted after each violated rule</param>
        /// <param name="includeTableName">Should the table name be included in parenthesis for each violation?</param>
        /// <returns>Text description of all broken rules</returns>
        public string GetAllViolations(string separationStringBefore, string separationStringAfter, bool includeTableName = false)
        {
            var sb = new StringBuilder();
            foreach (BrokenRule rule in this)
            {
                sb.Append(separationStringBefore);
                sb.Append(rule.Message);
                if (includeTableName)
                    sb.Append(" (" + rule.TableName + ")");
                sb.Append(separationStringAfter);
            }

            return sb.ToString();
        }

        /// <summary>
        ///     Returns all rule violations as a string
        /// </summary>
        /// <param name="separationStringAfter">String inserted after each violated rule</param>
        /// <returns>Text description of all broken rules</returns>
        public string GetAllViolations(string separationStringAfter) => GetAllViolations(string.Empty, separationStringAfter);

        /// <summary>
        ///     Returns all rule violations as a string
        /// </summary>
        /// <param name="includeTableName">Should the table name be included?</param>
        /// <returns>Text description of all broken rules</returns>
        public string GetAllViolations(bool includeTableName) => GetAllViolations(string.Empty, "\r\n", includeTableName);

        /// <summary>
        ///     Returns all rule violations as a string
        /// </summary>
        /// <returns>Text description of all broken rules</returns>
        public string GetAllViolations() => GetAllViolations(string.Empty, "\r\n");

        /// <summary>
        ///     Returns all rule violations as a string
        /// </summary>
        /// <param name="useBullets">Defines whether or not a list format should be used</param>
        /// <returns>Text description of all broken rules</returns>
        public string GetAllViolationsHTML(bool useBullets)
        {
            if (useBullets)
                return "<ul>" + GetAllViolations("<li>", string.Empty) + "</ul>";
            return GetAllViolations(string.Empty, "<br>");
        }

        /// <summary>
        ///     Copies the items within this collection to a one-dimensional array starting at the specified index.
        ///     This overload performs that operation in a strongly typed fashion.
        /// </summary>
        /// <param name="array">Array of BrokenRules</param>
        /// <param name="index">Index</param>
        public void CopyTo(BrokenRule[] array, int index)
        {
            if (BusinessEntity.GetInternalData().Tables.Contains(BusinessEntity.BrokenRulesTableName))
            {
                var counter = 0;
                foreach (DataRow oRow in BusinessEntity.GetInternalData().Tables[BusinessEntity.BrokenRulesTableName].Rows)
                {
                    if (counter >= index)
                        array[counter] = new BrokenRule(oRow, this);
                    counter++;
                }
            }
        }

        /// <summary>
        ///     Checks whether the given DataSet contains the BrokenRules table.
        /// </summary>
        /// <param name="dsInternal">The ds internal.</param>
        /// <returns></returns>
        private bool DataSetContainsBrokenRulesTable(DataSet dsInternal) => dsInternal.Tables.Contains(BrokenRulesTableName);

        /// <summary>
        ///     Calculates the broken rules count for the given table.
        /// </summary>
        /// <param name="data">The DataSet.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        private int CalculateBrokenRulesCountForTable(DataSet data, string tableName)
        {
            var brokenRulesCounter = 0;

            var brokenRules = data.Tables[BrokenRulesTableName];
            foreach (DataRow brokenRule in brokenRules.Rows)
                if (StringHelper.Compare(brokenRule["TableName"].ToString(), tableName))
                    brokenRulesCounter++;
            return brokenRulesCounter;
        }

        /// <summary>
        ///     Displays all violations
        /// </summary>
        /// <returns>Violations text</returns>
        public override string ToString() => GetAllViolations();
    }
}