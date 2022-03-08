using System.Diagnostics.CodeAnalysis;

namespace NtFreX.BuildingBlocks.Standard.Extensions;

public static class EqualsExtensions
{
    public static bool EqualsReferenceType<T>(in T? one, in T? two)
        where T: class
    {
        if (ReferenceEquals(one, two))
            return true;
        if (one is null)
            return false;
        if (two is null)
            return false;
        return one.Equals(two);
    }

    public static bool EqualsValueType<T>(in T? one, in T? two)
        where T: struct
    {
        if (!one.HasValue && !two.HasValue)
            return true;
        if (!one.HasValue)
            return false;
        if (!two.HasValue)
            return false;
        return one.Equals(two);
    }

    public static bool EqualsObject<T>(in T one, [NotNullWhen(true)] in object? two)
    {
        _ = one ?? throw new ArgumentNullException(nameof(one));

        if (two is null) return false;
        var objType = two.GetType();
        if (objType != typeof(T)) return false;
        return one.Equals((T)two);
    }
}
