using UnityEngine;
using UnityEngine.InputSystem;

namespace BelowTheWing.Crew
{
    public static class MouseCaptureRules
    {
        public static bool HeldAfterThisFrame(bool held, bool windowHasFocus, bool escapePressed, bool clickPressed)
        {
            if (!windowHasFocus)
            {
                return false;
            }

            if (escapePressed)
            {
                return false;
            }

            return clickPressed || held;
        }
    }

    public interface IMousePointer
    {
        void Hold();

        void Release();
    }

    public sealed class SystemMousePointer : IMousePointer
    {
        public void Hold()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Release()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    [DisallowMultipleComponent]
    public sealed class MouseCapture : MonoBehaviour
    {
        bool m_WindowHasFocus = true;
        IMousePointer m_Pointer;

        public bool Held { get; private set; }

        public IMousePointer Pointer
        {
            get => m_Pointer ??= new SystemMousePointer();
            set => m_Pointer = value;
        }

        public Vector2 Movement(Vector2 pointerDelta) => Held ? pointerDelta : Vector2.zero;

        public void WindowFocusChanged(bool hasFocus)
        {
            m_WindowHasFocus = hasFocus;
            Settle(escapePressed: false, clickPressed: false);
        }

        public void React(bool escapePressed, bool clickPressed) => Settle(escapePressed, clickPressed);

        void Settle(bool escapePressed, bool clickPressed)
        {
            var wanted = MouseCaptureRules.HeldAfterThisFrame(Held, m_WindowHasFocus, escapePressed, clickPressed);
            if (wanted == Held)
            {
                return;
            }

            Held = wanted;
            Apply();
        }

        void Apply()
        {
            if (Held)
            {
                Pointer.Hold();
            }
            else
            {
                Pointer.Release();
            }
        }

        void OnEnable()
        {
            m_WindowHasFocus = true;
            Held = true;
            Apply();
        }

        void OnDisable()
        {
            Held = false;
            Apply();
        }

        void OnApplicationFocus(bool hasFocus) => WindowFocusChanged(hasFocus);

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            React(
                escapePressed: keyboard != null && keyboard.escapeKey.wasPressedThisFrame,
                clickPressed: mouse != null && mouse.leftButton.wasPressedThisFrame);
        }
    }
}
