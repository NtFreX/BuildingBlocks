using System.Diagnostics;
using System.Numerics;

namespace NtFreX.BuildingBlocks.Mesh
{
    internal class RenderQueue : IEnumerable<IRenderable>
    {
        private const int DefaultCapacity = 250;

        private readonly List<RenderItemIndex> _indices = new List<RenderItemIndex>(DefaultCapacity);
        private readonly List<IRenderable> _renderables = new List<IRenderable>(DefaultCapacity);

        public int Count => _renderables.Count;

        public void Clear()
        {
            _indices.Clear();
            _renderables.Clear();
        }

        public void AddRange(List<IRenderable> Renderables, Vector3 viewPosition)
        {
            for (int i = 0; i < Renderables.Count; i++)
            {
                IRenderable Renderable = Renderables[i];
                if (Renderable != null)
                {
                    Add(Renderable, viewPosition);
                }
            }
        }

        public void AddRange(IReadOnlyList<IRenderable> Renderables, Vector3 viewPosition)
        {
            for (int i = 0; i < Renderables.Count; i++)
            {
                IRenderable Renderable = Renderables[i];
                if (Renderable != null)
                {
                    Add(Renderable, viewPosition);
                }
            }
        }

        public void AddRange(IEnumerable<IRenderable> Renderables, Vector3 viewPosition)
        {
            foreach (IRenderable item in Renderables)
            {
                if (item != null)
                {
                    Add(item, viewPosition);
                }
            }
        }

        public void Add(IRenderable item, Vector3 viewPosition)
        {
            int index = _renderables.Count;
            _indices.Add(new RenderItemIndex(item.GetRenderOrderKey(viewPosition), index));
            _renderables.Add(item);
            Debug.Assert(_renderables.IndexOf(item) == index);
        }

        public void Sort()
        {
            _indices.Sort();
        }

        public void Sort(Comparer<RenderOrderKey> keyComparer)
        {
            _indices.Sort(
                (RenderItemIndex first, RenderItemIndex second)
                    => keyComparer.Compare(first.Key, second.Key));
        }

        public void Sort(Comparer<RenderItemIndex> comparer)
        {
            _indices.Sort(comparer);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_indices, _renderables);
        }

        IEnumerator<IRenderable> IEnumerable<IRenderable>.GetEnumerator() => GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<IRenderable>
        {
            private readonly List<RenderItemIndex> _indices;
            private readonly List<IRenderable> _Renderables;
            private int _nextItemIndex;
            private IRenderable? _currentItem;

            public Enumerator(List<RenderItemIndex> indices, List<IRenderable> Renderables)
            {
                _indices = indices;
                _Renderables = Renderables;
                _nextItemIndex = 0;
                _currentItem = null;
            }

            public IRenderable Current => _currentItem!;
            object System.Collections.IEnumerator.Current => _currentItem!;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_nextItemIndex >= _indices.Count)
                {
                    _currentItem = null;
                    return false;
                }
                else
                {
                    var currentIndex = _indices[_nextItemIndex];
                    _currentItem = _Renderables[currentIndex.ItemIndex];
                    _nextItemIndex += 1;
                    return true;
                }
            }

            public void Reset()
            {
                _nextItemIndex = 0;
                _currentItem = null;
            }
        }
    }
}
