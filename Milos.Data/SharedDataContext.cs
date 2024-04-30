using System.Runtime.Serialization;

namespace Milos.Data;

/// <summary>
///     This class provides an abstraction of data connection
///     information that is free of database semantics when used.
///     This object can be used to associate multiple database
///     operations with each other. For instance, it can be used
///     to span transactions over multiple business objects,
///     without exposing database functionality on the business object.
/// </summary>
public class SharedDataContext
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="dataService">Reference to the data service.</param>
    public SharedDataContext(IDataService dataService) => DataService = dataService;

    /// <summary>
    ///     Reference to the encapsulated data service
    /// </summary>
    /// <remarks>
    ///     This property should only be used by Milos, unless
    ///     you have a very clear understanding of what it does
    ///     and how it is to be used. If you do not know this information,
    ///     then you probably do not want to use this property at all.
    /// </remarks>
    public IDataService DataService { get; }

    /// <summary>
    ///     Begins a new server-side transaction
    /// </summary>
    /// <returns>True if a new transaction has been started</returns>
    public bool BeginTransaction() => DataService.BeginTransaction();

    /// <summary>
    ///     Aborts a server-side transaction and rolls back changes
    /// </summary>
    /// <returns>True if abort was successful</returns>
    public bool AbortTransaction() => DataService.AbortTransaction();

    /// <summary>
    ///     Commits a server-side transaction
    /// </summary>
    /// <returns>True if commit was successful</returns>
    public bool CommitTransaction() => DataService.CommitTransaction();
}

/// <summary>
///     Defines what restrictions are to be applied when data contexts are shared.
/// </summary>
public enum ContextSharingRestriction
{
    /// <summary>
    ///     No restrictions. The developer is in charge of making sure
    ///     it makes sense to share the data context.
    /// </summary>
    None,

    /// <summary>
    ///     Allows sharing of the context when both involved (business) objects
    ///     share the same global database access configuration. (Matching
    ///     database prefix).
    /// </summary>
    DatabaseMatch,

    /// <summary>
    ///     Verifies that the accessed database settings are identical. This includes
    ///     things such as server name, user name, access method, and the like.
    /// </summary>
    ExactMatchOnly
}

/// <summary>
///     Exception used to indicate aborted shared transactions
/// </summary>
[Serializable]
public class AbortedSharedTransactionException : Exception
{
    /// <summary>
    ///     Standard Constructor
    /// </summary>
    public AbortedSharedTransactionException() { }

    /// <summary>
    ///     Standard Constructor
    /// </summary>
    /// <param name="message">Error message</param>
    public AbortedSharedTransactionException(string message) : base(message) { }

    /// <summary>
    ///     Standard Constructor
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public AbortedSharedTransactionException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    ///     Standard Constructor
    /// </summary>
    /// <param name="info">Serialization info</param>
    /// <param name="context">Streaming context</param>
    protected AbortedSharedTransactionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
///     Occurs in scenarios where context sharing is required, but the contexts don't match
/// </summary>
[Serializable]
public class ContextSharingRestrictionViolatedException : Exception
{
    /// <summary>
    ///     Standard Constructor
    /// </summary>
    public ContextSharingRestrictionViolatedException() { }

    /// <summary>
    ///     Standard Constructor
    /// </summary>
    /// <param name="message">Error message</param>
    public ContextSharingRestrictionViolatedException(string message) : base(message) { }

    /// <summary>
    ///     Standard Constructor
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public ContextSharingRestrictionViolatedException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    ///     Standard Constructor
    /// </summary>
    /// <param name="info">Serialization info</param>
    /// <param name="context">Streaming context</param>
    protected ContextSharingRestrictionViolatedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}