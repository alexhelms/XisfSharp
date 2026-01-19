using System.Collections.ObjectModel;
using XisfSharp.Properties;

namespace XisfSharp;

public class XisfPropertyCollection : Collection<XisfProperty>
{
    private readonly Dictionary<string, XisfProperty> _propertiesById = [];

    protected override void InsertItem(int index, XisfProperty item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_propertiesById.ContainsKey(item.Id))
            throw new ArgumentException($"A property with Id '{item.Id}' already exists.", nameof(item));

        _propertiesById.Add(item.Id, item);
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, XisfProperty item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var oldItem = this[index];

        // If replacing with a different Id, check for duplicates
        if (oldItem.Id != item.Id && _propertiesById.ContainsKey(item.Id))
            throw new ArgumentException($"A property with Id '{item.Id}' already exists.", nameof(item));

        _propertiesById.Remove(oldItem.Id);
        _propertiesById.Add(item.Id, item);
        base.SetItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        var item = this[index];
        _propertiesById.Remove(item.Id);
        base.RemoveItem(index);
    }

    protected override void ClearItems()
    {
        _propertiesById.Clear();
        base.ClearItems();
    }

    public XisfProperty? this[string id]
        => _propertiesById.GetValueOrDefault(id);

    public bool TryGetProperty(string id, out XisfProperty? property)
        => _propertiesById.TryGetValue(id, out property);

    public bool ContainsId(string id)
        => _propertiesById.ContainsKey(id);
}
