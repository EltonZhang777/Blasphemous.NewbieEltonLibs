using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.NewbieEltonLibs.Components;

/// <summary>
/// Provides deferred access to public fields whose declared type matches <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The field type to collect.</typeparam>
public class ItemCollection<T>
{
    /// <summary>
    /// Gets the current values of matching fields on the runtime type.
    /// </summary>
    public IEnumerable<T> Items
    {
        get
        {
            return GetType()
                .GetFields()
                .Where(itemField => itemField.FieldType == typeof(T))
                .Select(itemField => (T)itemField.GetValue(this));
        }
    }
}