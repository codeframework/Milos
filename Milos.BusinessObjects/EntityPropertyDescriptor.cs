using System.Reflection;

namespace Milos.BusinessObjects;

/// <summary>
///     Property descriptor used by business entities
/// </summary>
public class EntityPropertyDescriptor : PropertyDescriptor
{
    /// <summary>
    ///     For internal use only
    /// </summary>
    private readonly object propertyParentReference;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityPropertyDescriptor" /> class.
    /// </summary>
    /// <param name="name">The name of the property</param>
    /// <param name="propertyReference">A reference to object that contains the actual property</param>
    public EntityPropertyDescriptor(string name, object propertyReference) : base(name, null) => propertyParentReference = propertyReference;

    /// <summary>
    ///     When overridden in a derived class, gets a value indicating whether this property is read-only.
    /// </summary>
    /// <value></value>
    /// <returns>true if the property is read-only; otherwise, false.</returns>
    public override bool IsReadOnly
    {
        get
        {
            var property = propertyParentReference.GetType().GetProperty(Name, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
                return !property.CanWrite;
            return true;
        }
    }

    /// <summary>
    ///     When overridden in a derived class, gets the type of the property.
    /// </summary>
    /// <value></value>
    /// <returns>A <see cref="T:System.Type" /> that represents the type of the property.</returns>
    public override Type PropertyType
    {
        get
        {
            var property = propertyParentReference.GetType().GetProperty(Name, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
                return property.PropertyType;
            return typeof(object);
        }
    }

    /// <summary>
    ///     When overridden in a derived class, gets the type of the component this property is bound to.
    /// </summary>
    /// <value></value>
    /// <returns>
    ///     A <see cref="T:System.Type" /> that represents the type of component this property is bound to. When the
    ///     <see cref="M:System.ComponentModel.PropertyDescriptor.GetValue(System.Object)" /> or
    ///     <see cref="M:System.ComponentModel.PropertyDescriptor.SetValue(System.Object,System.Object)" /> methods are
    ///     invoked, the object specified might be an instance of this type.
    /// </returns>
    public override Type ComponentType => propertyParentReference.GetType();

    /// <summary>
    ///     When overridden in a derived class, resets the value for this property of the component to the default value.
    /// </summary>
    /// <param name="component">The component with the property value that is to be reset to the default value.</param>
    public override void ResetValue(object component)
    {
        // Nothing to do here, since we do not support this at all.
    }

    /// <summary>
    ///     When overridden in a derived class, sets the value of the component to a different value.
    /// </summary>
    /// <param name="component">The component with the property value that is to be set.</param>
    /// <param name="value">The new value.</param>
    public override void SetValue(object component, object value)
    {
        var property = component.GetType().GetProperty(Name, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null)
            property.SetValue(component, value, null);
    }

    /// <summary>
    ///     When overridden in a derived class, determines a value indicating whether
    ///     the value of this property needs to be persisted.
    /// </summary>
    /// <param name="component">The component with the property to be examined for persistence.</param>
    /// <returns>
    ///     true if the property should be persisted; otherwise, false.
    /// </returns>
    public override bool ShouldSerializeValue(object component) => false;

    /// <summary>
    ///     When overridden in a derived class, gets the current value of the
    ///     property on a component.
    /// </summary>
    /// <param name="component">The component with the property for which to retrieve the value.</param>
    /// <returns>
    ///     The value of a property for a given component.
    /// </returns>
    public override object GetValue(object component)
    {
        var property = component.GetType().GetProperty(Name, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null)
            return property.GetValue(component, null);
        return null;
    }

    /// <summary>
    ///     When overridden in a derived class, returns whether resetting an object changes its value.
    /// </summary>
    /// <param name="component">The component to test for reset capability.</param>
    /// <returns>
    ///     true if resetting the component changes its value; otherwise, false.
    /// </returns>
    public override bool CanResetValue(object component) => false;
}