using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace NtFreX.BuildingBlocks.Mesh.Data.Specialization;

public class MeshDataSpecializationDictionary : IEnumerable<MeshDataSpecialization>
{
    private Dictionary<Type, MeshDataSpecialization> specializations { get; set; } = new();

    public event EventHandler<Type>? SpecializationChanged;

    private void AddOrUpdateCore(Type key, MeshDataSpecialization specialization)
    {
        if (specializations.ContainsKey(key))
            specializations[key] = specialization;
        else
            specializations.Add(key, specialization);

        SpecializationChanged?.Invoke(this, key);
    }

    public void AddOrUpdate(MeshDataSpecialization[] items)
    {
        foreach(var item in items)
        {
            var key = item.GetType();
            AddOrUpdateCore(key, item);
        }
    }

    public void AddOrUpdate<T>(T item)
        where T: MeshDataSpecialization
    {
        var key = typeof(T);
        AddOrUpdateCore(key, item);
    }

    public bool Contains<T>()
        where T : MeshDataSpecialization
        => specializations.ContainsKey(typeof(T));

    public bool TryGet<T>([NotNullWhen(true)] out T? item)
        where T : MeshDataSpecialization
    {
        if (specializations.TryGetValue(typeof(T), out var specialization))
        {
            item = (T) specialization;
            return true;
        }

        item = default;
        return false;
    }

    public T Get<T>()
        where T: MeshDataSpecialization
        => (T)specializations[typeof(T)];

    public void Remove<T>()
        where T : MeshDataSpecialization
    {
        var key = typeof(T);
        if (specializations.ContainsKey(key))
        {
            specializations.Remove(key);
            SpecializationChanged?.Invoke(this, key);
        }
    }

    public override int GetHashCode()
    {
        var hashCode = specializations.Count;
        foreach (var value in specializations.Values)
        {
            hashCode = HashCode.Combine(hashCode, value.GetHashCode());
        }
        return hashCode;
    }

    public IEnumerator<MeshDataSpecialization> GetEnumerator()
        => specializations.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => specializations.Values.GetEnumerator();
}
