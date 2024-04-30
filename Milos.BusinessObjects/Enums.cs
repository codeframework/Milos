namespace Milos.BusinessObjects;

/// <summary>
///     Defines the type of a business rule violation
/// </summary>
public enum RuleViolationType
{
    /// <summary>
    ///     Serious (critical) violation. Data can not be saved!
    /// </summary>
    Violation,

    /// <summary>
    ///     Minor violation. Saving data is not recommended, but supported.
    /// </summary>
    Warning
}

/// <summary>
///     Defines what data is to be updated in save operations
/// </summary>
public enum DataSaveMode
{
    /// <summary>
    ///     Process all changes
    /// </summary>
    AllChanges,

    /// <summary>
    ///     Process deleted records only
    /// </summary>
    DeletesOnly,

    /// <summary>
    ///     Process all changes except deleted records
    /// </summary>
    AllChangesExceptDeletes
}

public enum QueryType
{
    Count,
    Rows,
    Report
}

public interface IDeletable
{
    /// <summary>
    /// Delete
    /// </summary>
    /// <returns>Success (true or false)</returns>
    bool Delete();
}

/// <summary>
/// Standard verifyable interface
/// </summary>
public interface IVerifyable
{
    /// <summary>
    /// Verify
    /// </summary>
    bool Verify();
}

/// <summary>
/// Standard savable interface
/// </summary>
public interface ISavable
{
    /// <summary>
    /// Save
    /// </summary>
    /// <returns>Success (true or false)</returns>
    bool Save();
}

/// <summary>
/// Standard is-dirty interface (can be used to indicate whether data is dirty)
/// </summary>
public interface IDirty
{
    /// <summary>
    /// Gets a value indicating whether this instance is dirty (has modified data).
    /// </summary>
    /// <value><c>true</c> if this instance is dirty; otherwise, <c>false</c>.</value>
    bool IsDirty { get; }
}

/// <summary>
/// Defines how invalid field values are handled during updates.
/// </summary>
/// <remarks>The specified behavior may be ignored (equivalent to the IgnoreInvalidValues setting) whenever
/// the current database does not provide schema information (such as field length), or when the 
/// AutoLoadSchemaInformation property is set to false.</remarks>
public enum InvalidFieldBehavior
{
    /// <summary>
    /// Fix invalid values if possible, and reject otherwise
    /// </summary>
    FixInvalidValues,

    /// <summary>
    /// Ignore values and set them anyway
    /// </summary>
    IgnoreInvalidValues,

    /// <summary>
    /// Reject invalid values
    /// </summary>
    RejectInvalidValues
}

/// <summary>
/// Defines the access mode for cross-link tables
/// </summary>
public enum XLinkItemAccessMode
{
    /// <summary>
    /// Current (parent) table
    /// </summary>
    CurrentTable,

    /// <summary>
    /// Target (child) table
    /// </summary>
    TargetTable
}

/// <summary>
/// Data load state
/// </summary>
public enum EntityLoadState
{
    /// <summary>
    /// Load is complete
    /// </summary>
    LoadComplete,

    /// <summary>
    /// Load in progress
    /// </summary>
    Loading,

    /// <summary>
    /// Entity's data has been deleted and is thus invalid.
    /// </summary>
    Deleted
}

/// <summary>
/// This enum defines the launch mode of a business entity
/// (such as new or load)
/// </summary>
public enum EntityLaunchMode
{
    /// <summary>
    /// New entity
    /// </summary>
    New,

    /// <summary>
    /// Existing entity
    /// </summary>
    Load,

    /// <summary>
    /// Existing entity based on a data set
    /// </summary>
    PassData,

    /// <summary>
    /// Triggers the custom load method
    /// </summary>
    Custom
}