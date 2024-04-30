namespace Milos.BusinessObjects;

/// <summary>
/// Event arguments for cancelable events
/// </summary>
public class CancelableEventArgs : EventArgs
{
    /// <summary>
    /// Indicates whether the event is to be canceled.
    /// </summary>
    public bool Cancel { get; set; }
}

/// <summary>
/// Event arguments for data source changed events
/// </summary>
public class DataSourceChangedEventArgs : EventArgs
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="tableName">Table name</param>
    public DataSourceChangedEventArgs(string fieldName, string tableName)
    {
        FieldName = fieldName;
        TableName = tableName;
    }

    /// <summary>
    /// Name of the changed field
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Name of the changed table
    /// </summary>
    public string TableName { get; }
}