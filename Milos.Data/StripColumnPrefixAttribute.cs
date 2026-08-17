using System;

namespace Milos.Data;

/// <summary>
/// When applied to a result-mapping class, instructs the data service to automatically
/// strip a column-name prefix before matching column names to properties.
/// 
/// With no argument, any run of leading lowercase characters is stripped automatically,
/// which handles common Hungarian-notation prefixes such as "c", "str", "dt", "int", etc.
/// 
/// An explicit prefix string can be supplied when the prefix is fixed and well-known.
/// </summary>
/// <example>
/// Auto-strip (strips any leading lowercase chars):
/// <code>
/// [StripColumnPrefix]
/// public class MyResult { public string FirstName { get; set; } }
/// // "cFirstName" → "FirstName"  ✓
/// // "strLastName" → "LastName"  ✓
/// </code>
/// 
/// Explicit prefix:
/// <code>
/// [StripColumnPrefix("tbl")]
/// public class MyResult { ... }
/// // "tblFirstName" → "FirstName"  ✓
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public sealed class StripColumnPrefixAttribute : Attribute
{
    /// <summary>
    /// The explicit prefix to strip. When null, leading lowercase characters are stripped automatically.
    /// </summary>
    public string Prefix { get; }

    /// <summary>
    /// Enables automatic prefix stripping: any run of leading lowercase characters is removed.
    /// </summary>
    public StripColumnPrefixAttribute() { }

    /// <summary>
    /// Enables stripping of a specific, fixed prefix.
    /// </summary>
    /// <param name="prefix">The exact prefix to remove from column names before property matching.</param>
    public StripColumnPrefixAttribute(string prefix) => Prefix = prefix;
}
