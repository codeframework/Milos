using System;

namespace Milos.BusinessObjects
{
    /// <summary>
    ///     This attribute indicates to cloning functionality
    ///     that it should ignore the current property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NotClonableAttribute : Attribute
    {
    }
}