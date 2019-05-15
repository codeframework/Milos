using System;
using System.Data;
using Milos.Data;

namespace Milos.BusinessObjects
{
    /// <summary>
    ///     This interface is the very fundamental definition of the interface
    ///     used by all business entities
    /// </summary>
    public interface IBusinessEntity : IDisposable, IDeletable, ISavable, IVerifyable // , ISerializable
    {
        /// <summary>
        ///     This property returns the current entity PK as a Guid
        /// </summary>
        Guid PK { get; }

        /// <summary>
        ///     Integer primary key
        /// </summary>
        int PKInteger { get; }

        /// <summary>
        ///     String primary key
        /// </summary>
        string PKString { get; }

        /// <summary>
        ///     This property returns the string representation of the current PK
        /// </summary>
        string Id { get; }

        /// <summary>
        ///     Primary key type utilized by this object
        /// </summary>
        KeyType PrimaryKeyType { get; }

        /// <summary>
        ///     Indicates whether the objects data contains any chances, such as
        ///     modifications, deletes, or additions.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        ///     State (new, updated, deleted,...) of the current entity.
        /// </summary>
        DataRowState EntityState { get; }

        /// <summary>
        ///     Business object associated with this entity
        /// </summary>
        IBusinessObject AssociatedBusinessObject { get; }

        /// <summary>
        ///     Accepts all changes in the business entity and therefore clears the dirty flag
        /// </summary>
        void IgnoreIsDirty();

        /// <summary>
        ///     Returns the data set used internally.
        /// </summary>
        /// <returns>DataSet</returns>
        DataSet GetInternalData();

        /// <summary>
        ///     Returns whether or not that field's value is currently null/nothing
        /// </summary>
        /// <param name="fieldName">Field name as it appears in the data set</param>
        /// <returns>True or false</returns>
        bool IsFieldNull(string fieldName);

        /// <summary>
        ///     Returns all internal data as a serialized XML string.
        /// </summary>
        /// <returns>XML string</returns>
        string GetRawData();

        /// <summary>
        ///     Saves the current data.
        /// </summary>
        /// <param name="businessObject">The business object.</param>
        /// <returns>True or false</returns>
        bool Save(IBusinessObject businessObject);

        /// <summary>
        ///     Removes (deletes) the current entity object
        /// </summary>
        /// <returns>True or false</returns>
        bool Remove();

        /// <summary>
        ///     This method generates the appropriate business object for the current entity.
        ///     It serves as a factory.
        ///     This method has to be overridden in subclasses if used by the entity.
        /// </summary>
        /// <returns>BusinessObject</returns>
        IBusinessObject GetBusinessObject();
    }
}