using UnityEngine;
using UnityEngine.InputSystem;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// The keyboard and mouse, for the one character on this machine that belongs to this player.
    ///
    /// This is the only place in the game that reads a physical input device. Everything downstream
    /// takes a <see cref="CrewIntent"/> and cannot tell whether it came from a player, from another
    /// machine, or from a test, which is what allows all three to work the same way.
    ///
    /// Reading devices directly rather than through an input asset, because the controls are four
    /// keys and a mouse and are going to be replaced along with everything else here.
    /// </summary>
    [RequireComponent(typeof(CrewCharacter))]
    [DisallowMultipleComponent]
    public sealed class LocalCrewInput : MonoBehaviour, ICrewIntentSource
    {
        CrewCharacter m_Character;

        public CrewIntent Current { get; private set; } = CrewIntent.Idle;

        void Awake()
        {
            m_Character = GetComponent<CrewCharacter>();
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
                brake: keyboard.spaceKey.isPressed ? 1f : 0f);

            Look();

            if (keyboard.eKey.wasPressedThisFrame)
            {
                m_Character.Seat?.Toggle(m_Character);
            }
        }

        void Look()
        {
            var mouse = Mouse.current;
            if (mouse == null || m_Character.Camera == null)
            {
                return;
            }

            m_Character.Camera.Look(mouse.delta.ReadValue());
        }

        static float Held(Keyboard keyboard, Key key) => keyboard[key].isPressed ? 1f : 0f;
    }
}
