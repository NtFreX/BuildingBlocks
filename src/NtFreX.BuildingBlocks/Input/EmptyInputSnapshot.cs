using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Input
{
    public class EmptyInputSnapshot : InputSnapshot
    {
        public IReadOnlyList<KeyEvent> KeyEvents => Array.Empty<KeyEvent>();
        public IReadOnlyList<MouseEvent> MouseEvents => Array.Empty<MouseEvent>();
        public IReadOnlyList<char> KeyCharPresses => Array.Empty<char>();
        public Vector2 MousePosition => Vector2.Zero;
        public float WheelDelta => 0f;
        public bool IsMouseDown(MouseButton button) => false;
    }
}
