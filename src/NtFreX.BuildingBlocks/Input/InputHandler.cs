using Veldrid;

namespace NtFreX.BuildingBlocks.Input
{
    public class InputHandler
    {
        private readonly HashSet<Key> _pressedKeys = new ();
        private readonly HashSet<MouseButton> _pressedMouseButtons = new ();

        public InputSnapshot CurrentSnapshot { get; private set; } = new EmptyInputSnapshot();

        public void Update(InputSnapshot snapshot)
        {
            CurrentSnapshot = snapshot;

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

        public void HandleKeyEvents(Key key)
            => _pressedKeys.Remove(key);
        public void HandleMouseEvents(MouseButton btn)
            => _pressedMouseButtons.Remove(btn);
    }
}
