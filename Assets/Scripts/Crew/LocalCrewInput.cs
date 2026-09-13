using BelowTheWing.Cargo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// The keyboard and mouse, for the one character on this machine that belongs to this player.
    ///
    /// The only place that turns a physical input device into anything the game acts on. Everything
    /// downstream takes a <see cref="CrewIntent"/> and cannot tell whether it came from a player,
    /// from another machine, or from a test, which is what allows all three to work the same way.
    /// (Two things read keys of their own. The debug readout has a key to show and hide itself,
    /// which is a developer switch rather than a game control. And <see cref="MouseCapture"/> reads
    /// Escape and the left button, because whether the game holds the pointer is a decision about
    /// the window rather than about what a character does. Neither has a <see cref="CrewIntent"/>
    /// downstream of it.)
    ///
    /// Reading devices directly rather than through an input asset, because the controls are four
    /// keys and a mouse and are going to be replaced along with everything else here.
    /// </summary>
    [RequireComponent(typeof(CrewCharacter))]
    [RequireComponent(typeof(MouseCapture))]
    [DisallowMultipleComponent]
    public sealed class LocalCrewInput : MonoBehaviour, ICrewIntentSource
    {
        CrewCharacter m_Character;
        MouseCapture m_Pointer;
        bool m_PointerWasHeld;

        public CrewIntent Current { get; private set; } = CrewIntent.Idle;

        void Awake()
        {
            m_Character = GetComponent<CrewCharacter>();
            m_Pointer = GetComponent<MouseCapture>();
            m_Character.IntentSource = this;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                Current = CrewIntent.Idle;
                return;
            }

            var move = new Vector2(
                Held(keyboard, Key.D) - Held(keyboard, Key.A),
                Held(keyboard, Key.W) - Held(keyboard, Key.S));

            Current = new CrewIntent(
                move,
                sprint: keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed,
                brake: keyboard.spaceKey.isPressed ? 1f : 0f,

                // Space is the brake while driving and the jump while on foot. The same key for
                // "get off the ground" either way, and never both at once, because a player in a
                // seat is not standing on anything.
                jump: keyboard.spaceKey.wasPressedThisFrame,

                // Held rather than toggled. A cart interior is low enough that a player wants to be
                // sure they are still crouched without watching their own knees.
                crouch: keyboard.cKey.isPressed);

            Look();

            if (keyboard.eKey.wasPressedThisFrame)
            {
                m_Character.Seat?.Toggle(m_Character);
            }

            // One key for both halves of coupling. Which one it does depends on what is in reach,
            // so a player never has to remember which of two keys they wanted.
            if (keyboard.qKey.wasPressedThisFrame)
            {
                m_Character.Hitching?.Act();
            }

            Handle();
        }

        /// <summary>
        /// Mouse movement, but only while the pointer belongs to the game.
        ///
        /// Once it has been handed back the player is using it somewhere else -- on the desktop, on
        /// another monitor -- and every one of those movements would otherwise still be swinging
        /// the camera behind them.
        /// </summary>
        void Look()
        {
            var mouse = Mouse.current;
            if (mouse == null || m_Character.Camera == null)
            {
                return;
            }

            m_Character.Camera.Look(m_Pointer.Movement(mouse.delta.ReadValue()));
        }

        /// <summary>
        /// The two mouse buttons, one per hand: the left button works the left hand and the right
        /// button the right.
        ///
        /// Each is a press and a release and nothing more. What a press means -- take hold of
        /// something, start winding up a throw -- is the hand's to decide from what it is holding,
        /// so that the keyboard never has to know.
        ///
        /// A press counts only if the pointer already belonged to the game on the previous frame,
        /// so the click that brings it back from the desktop is not a grab whichever order this and
        /// the pointer's own component run in. A release always counts: a button let go of while
        /// the pointer was elsewhere still lets go of the cart.
        /// </summary>
        void Handle()
        {
            var mouse = Mouse.current;
            var hands = m_Character.Handling;
            var pressesCount = m_PointerWasHeld;
            m_PointerWasHeld = m_Pointer.Held;

            if (mouse == null || hands == null)
            {
                return;
            }

            Work(hands.Left, mouse.leftButton, pressesCount);
            Work(hands.Right, mouse.rightButton, pressesCount);
        }

        void Work(Hand hand, ButtonControl button, bool pressesCount)
        {
            if (pressesCount && button.wasPressedThisFrame)
            {
                hand.Press(Time.time);
            }

            if (button.wasReleasedThisFrame)
            {
                hand.Release(Time.time, ThrowingTowards());
            }
        }

        /// <summary>
        /// Which way a throw goes: along the camera, pitch included. Looking up is how a lob is
        /// aimed into a cart, and a throw that only ever went level could not be.
        /// </summary>
        Vector3 ThrowingTowards()
        {
            if (m_Character.Camera == null)
            {
                return m_Character.transform.forward;
            }

            return m_Character.Camera.transform.forward;
        }

        static float Held(Keyboard keyboard, Key key) => keyboard[key].isPressed ? 1f : 0f;
    }
}
