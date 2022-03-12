using System.Collections.Concurrent;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Factories
{
    static class ResourceSetFactory
    {
        private static readonly ConcurrentDictionary<ResourceSetDescription, ResourceSet> resourceSets = new ();

        public static ResourceSet GetResourceSet(ResourceFactory resourceFactory, ResourceSetDescription description, string name)
        {
            if (!resourceSets.TryGetValue(description, out var set))
            {
                set = resourceFactory.CreateResourceSet(ref description);
                set.Name = name;
                resourceSets.AddOrUpdate(description, set, (_, value) => value);
            }
            return set;
        }

        public static void Dispose()
        {
            foreach (var resourceSet in resourceSets.Values)
            {
                resourceSet.Dispose();
            }
            resourceSets.Clear();
        }
    }
}
