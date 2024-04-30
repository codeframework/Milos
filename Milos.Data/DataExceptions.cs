using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Milos.Data;

/// <summary>
///     This exception gets raised when the data service tries to read configuration settings
///     from the application's configuration file that wasn't there.
/// </summary>
[Serializable]
public class MissingDataConfigurationException : Exception
{
    /// <summary>
    ///     Internal field to store the setting information
    /// </summary>
    private readonly string setting = string.Empty;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="info">Serialization Info</param>
    /// <param name="context">Streaming Context</param>
    protected MissingDataConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    /// <param name="e">Base exception</param>
    public MissingDataConfigurationException(string message, Exception e) : base(message, e) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    public MissingDataConfigurationException() : base("Data Configuration Missing.") { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    public MissingDataConfigurationException(string message) : base(message) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    /// <param name="setting">Missing setting</param>
    public MissingDataConfigurationException(string message, string setting) : base(message + " (" + setting + ")")
    {
        this.setting = setting;
    }

    /// <summary>
    ///     Specifies which setting(s) was missing in particular.
    /// </summary>
    public string Setting => setting;

    /// <summary>
    ///     Provides details on how to fix the problem.
    /// </summary>
    public string Details => "A configuration setting that was required by the data access layer was not available. To fix this problem, make sure your application has a config file (such as app.config or web.config) with the appropriate database settings.";

    /// <summary>
    ///     For internal use only
    /// </summary>
    /// <param name="info">Serialization info</param>
    /// <param name="context">Streaming context</param>
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue("Setting", Setting);
        info.AddValue("Details", Details);
    }
}

/// <summary>
///     This exception fires whenever the factory fails to instantiate a
///     data service object for an unknown reason. The original exception
///     (if it exists) is exposed through the OriginalException property.
/// </summary>
[Serializable]
public class DataServiceInstantiationException : Exception
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="innerException">Original Exception</param>
    public DataServiceInstantiationException(Exception innerException) : base("Error instantiating Data Service.", innerException) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="info">Serialization Info</param>
    /// <param name="context">Streaming Context</param>
    protected DataServiceInstantiationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    /// <param name="e">Base exception</param>
    public DataServiceInstantiationException(string message, Exception e) : base(message, e) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    public DataServiceInstantiationException(string message) : base(message) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    public DataServiceInstantiationException() : base("Error instantiating Data Service.") { }
}

/// <summary>
///     This exception is thrown whenever a data service is asked to update/process
///     a data source using a method that is not supported (such as trying to
///     update a MySql database through stored procedures).
/// </summary>
[Serializable]
public class UnsupportedProcessMethodException : Exception
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="innerException">Original Exception</param>
    public UnsupportedProcessMethodException(Exception innerException) : base("Unsupported data update method.", innerException) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="info">Serialization Info</param>
    /// <param name="context">Streaming Context</param>
    protected UnsupportedProcessMethodException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    /// <param name="e">Base exception</param>
    public UnsupportedProcessMethodException(string message, Exception e) : base(message, e) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    public UnsupportedProcessMethodException(string message) : base(message) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    public UnsupportedProcessMethodException() : base("Unsupported data update method.") { }
}

/// <summary>
///     This exception is thrown whenever a command of the wrong type is executed.
///     For instance, this exception may be thrown whenever a stored procedure facade
///     is asked to execute a dynamic SQL statement.
/// </summary>
[Serializable]
public class UnsupportedCommandTypeException : Exception
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="innerException">Original Exception</param>
    public UnsupportedCommandTypeException(Exception innerException) : base("Unsupported command type.", innerException) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="info">Serialization Info</param>
    /// <param name="context">Streaming Context</param>
    protected UnsupportedCommandTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    /// <param name="e">Base exception</param>
    public UnsupportedCommandTypeException(string message, Exception e) : base(message, e) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    public UnsupportedCommandTypeException(string message) : base(message) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    public UnsupportedCommandTypeException() : base("Unsupported command type.") { }
}

/// <summary>
///     This exception is thrown by the stored procedure facade when it
///     is asked to execute a simulated stored procedure that does not exist
///     as a method on the facade.
/// </summary>
[Serializable]
public class UnknownProcedureException : Exception
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="innerException">Original Exception</param>
    public UnknownProcedureException(Exception innerException) : base("Procedure not found.", innerException) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="info">Serialization Info</param>
    /// <param name="context">Streaming Context</param>
    protected UnknownProcedureException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    /// <param name="e">Base exception</param>
    public UnknownProcedureException(string message, Exception e) : base(message, e) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    public UnknownProcedureException(string message) : base(message) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    public UnknownProcedureException() : base("Procedure not found.") { }
}

/// <summary>
///     This exception gets raised whenever a command object is passed to a method
///     that expects a different command object. For instance, this could happen
///     when a method that expects an SqlCommand object receives an OracleCommand object.
/// </summary>
[Serializable]
public class UnsupportedCommandObjectException : Exception
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="info">Serialization Info</param>
    /// <param name="context">Streaming Context</param>
    protected UnsupportedCommandObjectException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    /// <param name="e">Base exception</param>
    public UnsupportedCommandObjectException(string message, Exception e) : base(message, e) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="message">Message</param>
    public UnsupportedCommandObjectException(string message) : base(message) { }

    /// <summary>
    ///     Constructor
    /// </summary>
    public UnsupportedCommandObjectException() { }
}