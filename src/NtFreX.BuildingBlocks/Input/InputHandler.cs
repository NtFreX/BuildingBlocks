using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Input
{
    public class InputHandler
    {
        private static HashSet<Key> _pressedKeys = new HashSet<Key>();
        private static HashSet<MouseButton> _pressedMouseButtons = new HashSet<MouseButton>();

        public Vector2 MousePosition { get; private set; }

        public void Update(InputSnapshot snapshot)
        {
            MousePosition = snapshot.MousePosition;

            for (int i = 0; i < snapshot.KeyEvents.Count; i++)
            {
                var keyEvent = snapshot.KeyEvents[i];
                if (keyEvent.Down)
                {
                    _pressedKeys.Add(keyEvent.Key);
                }
                else
                {
                    _pressedKeys.Remove(keyEvent.Key);
                }
            }

            for (int i = 0; i < snapshot.MouseEvents.Count; i++)
            {
                var mouseEvent = snapshot.MouseEvents[i];
                if (mouseEvent.Down)
                {
                    _pressedMouseButtons.Add(mouseEvent.MouseButton);
                }
                else
                {
                    _pressedMouseButtons.Remove(mouseEvent.MouseButton);
                }
            }
        }

        public static InputHandler Empty { get; } = new InputHandler();

        public bool IsKeyDown(Key key)
            => _pressedKeys.Contains(key);
        public bool IsMouseDown(MouseButton btn)
            => _pressedMouseButtons.Contains(btn);
    }
}
