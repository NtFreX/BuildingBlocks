using System;
using System.Linq;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Desktop
{
    class InputHandler
    {
        private readonly InputSnapshot inputs;

        public Vector2 MousePosition => inputs.MousePosition;

        public InputHandler(InputSnapshot inputs)
        {
            this.inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        }

        public bool IsKeyDown(Key key)
            => inputs?.KeyEvents.Any(x => x.Key == key) ?? false;
        public bool IsMouseDown(MouseButton btn)
            => inputs?.IsMouseDown(btn) ?? false;
    }
}
