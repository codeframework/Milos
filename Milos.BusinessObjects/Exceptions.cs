using System;
using System.Runtime.Serialization;
using Milos.Data;

namespace Milos.BusinessObjects
{
    /// <summary>
    ///     This exception is thrown whenever a business object attempts to access
    ///     a database by means of a data service, yet no data service is available.
    /// </summary>
    [Serializable]
    public class NoDataServiceAvailableException : Exception
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="innerException">Original Exception</param>
        public NoDataServiceAvailableException(Exception innerException) : base("No data service available.", innerException) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="info">Serialization Info</param>
        /// <param name="context">Streaming Context</param>
        protected NoDataServiceAvailableException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="e">Base exception</param>
        public NoDataServiceAvailableException(string message, Exception e) : base(message, e) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message">Message</param>
        public NoDataServiceAvailableException(string message) : base(message) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        public NoDataServiceAvailableException() : base("No data service available.") { }
    }

    /// <summary>
    ///     This exception is thrown whenever a data service is asked to update/process
    ///     a data source using a method that is not supported (such as trying to
    ///     update a MySql database through stored procedures).
    /// </summary>
    [Serializable]
    public class TransactionException : Exception
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="innerException">Original Exception</param>
        public TransactionException(Exception innerException) : base("Transaction operation failed.", innerException) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="info">Serialization Info</param>
        /// <param name="context">Streaming Context</param>
        protected TransactionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="e">Base exception</param>
        public TransactionException(string message, Exception e) : base(message, e) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message">Message</param>
        public TransactionException(string message) : base(message) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        public TransactionException() : base("Transaction operation failed.") { }
    }

    /// <summary>
    ///     Exception that is thrown whenever an unsupported key type is accessed
    /// </summary>
    [Serializable]
    public class UnsupportedKeyTypeException : Exception
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="info">Serialization Info</param>
        /// <param name="context">Streaming Context</param>
        protected UnsupportedKeyTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="e">Base exception</param>
        public UnsupportedKeyTypeException(string message, Exception e) : base(message, e) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        public UnsupportedKeyTypeException() : base("Key type not supported.") { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message">Message</param>
        public UnsupportedKeyTypeException(string message) : base(message) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="type">Type</param>
        public UnsupportedKeyTypeException(KeyType type) : base("Key type '@type' not supported.") { }
    }

    /// <summary>
    ///     This exception indicates that an attempt was made to access
    ///     a field that does not exist in the data store (in memory)
    /// </summary>
    [Serializable]
    public class FieldDoesntExistException : Exception
    {
        /// <summary>
        ///     Standard Constructor
        /// </summary>
        public FieldDoesntExistException() { }

        /// <summary>
        ///     Standard Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        public FieldDoesntExistException(string message) : base(message) { }

        /// <summary>
        ///     Standard Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public FieldDoesntExistException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        ///     Standard Constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        protected FieldDoesntExistException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    ///     Exception class used for enumeration errors.
    ///     The error is raised when an enumeration finds its enumeration source in disarray
    ///     and thus overshoots the sources bounds
    /// </summary>
    [Serializable]
    public class IndexOutOfBoundsException : Exception
    {
        /// <summary>
        ///     Default Constructor.
        /// </summary>
        public IndexOutOfBoundsException() : base("Index out of bounds.") { }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="message">Exception message</param>
        public IndexOutOfBoundsException(string message) : base(message) { }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public IndexOutOfBoundsException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected IndexOutOfBoundsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    ///     This exception is thrown when one of the methods on a business item collection
    ///     is used with types it was not designed for.
    /// </summary>
    [Serializable]
    public class InvalidObjectTypeInEntityException : Exception
    {
        /// <summary>
        ///     Default Constructor.
        /// </summary>
        public InvalidObjectTypeInEntityException() { }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="message">Exception message</param>
        public InvalidObjectTypeInEntityException(string message) : base(message) { }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public InvalidObjectTypeInEntityException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected InvalidObjectTypeInEntityException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    ///     Exception thrown when the caller tries to add a linked item that does not match any items in the target table.
    /// </summary>
    [Serializable]
    public class TargetItemNotFoundException : Exception
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="info">Serialization Info</param>
        /// <param name="context">Streaming Context</param>
        protected TargetItemNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="e">Base exception</param>
        public TargetItemNotFoundException(string message, Exception e) : base(message, e) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        public TargetItemNotFoundException() : base("Collection target not found.") { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message">Message</param>
        public TargetItemNotFoundException(string message) : base(message) { }
    }

    [Serializable]
    internal class DeletedEntityException : Exception
    {
        public DeletedEntityException() { }

        public DeletedEntityException(string message) : base(message) { }

        public DeletedEntityException(string message, Exception innerException) : base(message, innerException) { }

        protected DeletedEntityException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    ///     Exception used to indicate that business objects are not compatible
    /// </summary>
    [Serializable]
    public class IncompatibleBusinessObjectException : Exception
    {
        /// <summary>
        ///     Standard Constructor
        /// </summary>
        public IncompatibleBusinessObjectException() { }

        /// <summary>
        ///     Standard Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        public IncompatibleBusinessObjectException(string message) : base(message) { }

        /// <summary>
        ///     Standard Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public IncompatibleBusinessObjectException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        ///     Standard Constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        protected IncompatibleBusinessObjectException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    ///     Exception used to indicate that business objects are not compatible
    /// </summary>
    [Serializable]
    public class AtomicSaveFailedException : Exception
    {
        /// <summary>
        ///     Standard Constructor
        /// </summary>
        public AtomicSaveFailedException() { }

        /// <summary>
        ///     Standard Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        public AtomicSaveFailedException(string message) : base(message) { }

        /// <summary>
        ///     Standard Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public AtomicSaveFailedException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        ///     Standard Constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        protected AtomicSaveFailedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}