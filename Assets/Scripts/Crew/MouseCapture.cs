using UnityEngine;
using UnityEngine.InputSystem;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// When the game holds the mouse pointer, and when it hands it back to the desktop.
    ///
    /// Held means the pointer is locked to the middle of the window and invisible, which is what
    /// lets a player turn all the way round without the pointer wandering off onto another monitor
    /// and leaving them clicking on something else. The price is that while it is held the player
    /// cannot reach anything outside the game, so the ways out matter as much as the way in.
    ///
    /// Kept apart from the component that acts on the answer because the rules are the part worth
    /// reading, and they are worth being able to check without a window to lose focus.
    /// </summary>
    public static class MouseCaptureRules
    {
        /// <summary>
        /// Whether the pointer is still the game's once this frame's input has been taken into
        /// account.
        ///
        /// The two ways out are the ones a player already knows without being told: press Escape,
        /// or move to another window. Coming back is a click and only a click, so that a pointer
        /// drifting back across the window is not treated as a request to be swallowed by it.
        /// </summary>
        public static bool HeldAfterThisFrame(bool held, bool windowHasFocus, bool escapePressed, bool clickPressed)
        {
            if (!windowHasFocus)
            {
                return false;
            }

            // Ahead of the click deliberately. A player reaching for the way out should get it on
            // the first press rather than fighting whatever else their hand is doing.
            if (escapePressed)
            {
                return false;
            }

            return clickPressed || held;
        }
    }

    /// <summary>
    /// The mouse pointer itself, as something that can be taken and given back.
    ///
    /// An interface because the real pointer belongs to the operating system. A headless test run
    /// has no window, so what was done to the cursor cannot be read back off it -- and a component
    /// whose entire job is to hide a pointer, tested only against a pointer that cannot be hidden,
    /// proves nothing at all.
    /// </summary>
    public interface IMousePointer
    {
        /// <summary>Locks the pointer to the middle of the window and hides it.</summary>
        void Hold();

        /// <summary>Hands the pointer back to the desktop, visible and free to move.</summary>
        void Release();
    }

    /// <summary>The pointer the person at this machine is actually moving.</summary>
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

    /// <summary>
    /// Takes the mouse pointer away from the desktop while its owner is playing, and gives it back.
    ///
    /// Lives only on the character belonging to the person sitting at this machine, and only from
    /// the moment they arrive on the apron. That is why the host and join screen, drawn before any
    /// of this exists, still has a pointer that can press its buttons: nothing has taken it yet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MouseCapture : MonoBehaviour
    {
        bool m_WindowHasFocus = true;
        IMousePointer m_Pointer;

        /// <summary>Whether the pointer currently belongs to the game rather than to the desktop.</summary>
        public bool Held { get; private set; }

        /// <summary>
        /// The pointer being taken and given back. Defaults to the real one, and is worth setting
        /// only where there is no real one to take.
        /// </summary>
        public IMousePointer Pointer
        {
            get => m_Pointer ??= new SystemMousePointer();
            set => m_Pointer = value;
        }

        /// <summary>
        /// How much of the pointer's movement the game should act on this frame.
        ///
        /// Nothing at all once the pointer has been handed back. A player who pressed Escape to
        /// click on something else is still moving their mouse, and every one of those movements
        /// would otherwise be swinging the camera round behind their back.
        /// </summary>
        public Vector2 Movement(Vector2 pointerDelta) => Held ? pointerDelta : Vector2.zero;

        /// <summary>Tells this that the game window has come to the front, or gone behind something.</summary>
        public void WindowFocusChanged(bool hasFocus)
        {
            m_WindowHasFocus = hasFocus;
            Settle(escapePressed: false, clickPressed: false);
        }

        /// <summary>What the player did this frame with the two controls that move the pointer in or out.</summary>
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
            // Being built at all means the local player has arrived on the apron, which is the
            // moment playing starts.
            m_WindowHasFocus = true;
            Held = true;
            Apply();
        }

        void OnDisable()
        {
            // A session that drops destroys the local player. Handing the pointer back is not
            // tidiness: a player left with neither a game nor a pointer is worse off than the
            // wandering pointer this component exists to prevent.
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
