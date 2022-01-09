using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Android
{
    public class AndroidInputSnapshot : InputSnapshot
    {
        public IReadOnlyList<KeyEvent> KeyEvents => new List<KeyEvent>();
        public IReadOnlyList<MouseEvent> MouseEvents => new List<MouseEvent>();
        public IReadOnlyList<char> KeyCharPresses => new List<char>();
        public Vector2 MousePosition => Vector2.Zero;
        public float WheelDelta => 0f;
        public bool IsMouseDown(MouseButton button) => false;
    }
}