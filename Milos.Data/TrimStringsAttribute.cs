namespace Milos.Data;

/// <summary>
/// When applied to a result-mapping class, instructs the data service to automatically
/// trim leading and trailing whitespace from all string values read from the data source.
/// This is useful when mapping fixed-length or poorly-padded database columns (e.g. CHAR fields).
/// </summary>
/// <example>
/// <code>
/// [TrimStrings]
/// public class CustomerResult
/// {
///     public string FirstName { get; set; }  // "  John  " → "John"
///     public string LastName  { get; set; }  // "  Doe   " → "Doe"
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public sealed class TrimStringsAttribute : Attribute { }
