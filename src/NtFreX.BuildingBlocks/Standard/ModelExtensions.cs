using NtFreX.BuildingBlocks.Behaviors;
using NtFreX.BuildingBlocks.Mesh;
using System.Numerics;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Standard;

public static class ModelExtensions
{
    public static IEnumerable<Model> AddBehavoirs(this IEnumerable<Model> models, Func<Model, IBehavior> behaviorResolver)
    {
        foreach (var model in models)
        {
            model.AddBehavoirs(behaviorResolver(model));
        }
        return models;
    }

    public static IEnumerable<Model> AddBehavoirs(this IEnumerable<Model> models, params IBehavior[] behaviors)
    {
        foreach (var model in models)
        {
            model.AddBehavoirs(behaviors);
        }
        return models;
    }
}
