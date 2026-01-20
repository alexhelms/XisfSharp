using System.Collections.ObjectModel;
using XisfSharp.Properties;

namespace XisfSharp;

public class ReadOnlyXisfPropertyCollection : ReadOnlyCollection<XisfProperty>
{
    private readonly XisfPropertyCollection _collection;

    public ReadOnlyXisfPropertyCollection(XisfPropertyCollection collection)
        : base(collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        _collection = collection;
    }

    public XisfProperty? this[string id]
        => _collection[id];

    public bool TryGetProperty(string id, out XisfProperty? property)
        => _collection.TryGetProperty(id, out property);

    public bool ContainsId(string id)
        => _collection.ContainsId(id);
}
