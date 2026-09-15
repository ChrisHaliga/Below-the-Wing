using BelowTheWing.Cargo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BelowTheWing.Crew
{
    [RequireComponent(typeof(CrewCharacter))]
    [RequireComponent(typeof(MouseCapture))]
    [DisallowMultipleComponent]
    public sealed class LocalCrewInput : MonoBehaviour, ICrewIntentSource
    {
        CrewCharacter m_Character;
        MouseCapture m_Pointer;
        bool m_PointerWasHeld;

        PressLatch m_Jump;

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

            m_Jump.ForgetOnceAStepHasSeenIt(Time.fixedTimeAsDouble);
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                m_Jump.Ask(Time.fixedTimeAsDouble);
            }

            Current = new CrewIntent(
                move,
                sprint: keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed,
                brake: keyboard.spaceKey.isPressed ? 1f : 0f,

                jump: m_Jump.Asked,

                crouch: keyboard.cKey.isPressed,

                hoist: keyboard.spaceKey.isPressed);

            Look();

            if (keyboard.eKey.wasPressedThisFrame)
            {
                m_Character.Seat?.Toggle(m_Character);
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                m_Character.Hitching?.Act();
            }

            Handle();
        }

        void Look()
        {
            var mouse = Mouse.current;
            if (mouse == null || m_Character.Camera == null)
            {
                return;
            }

            m_Character.Camera.Look(m_Pointer.Movement(mouse.delta.ReadValue()));
        }

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
                hand.Press(Time.time, Aim());
            }

            if (button.wasReleasedThisFrame)
            {
                hand.Release(Time.time, ThrowingTowards());
            }
        }

        Ray Aim()
        {
            var eye = m_Character.Camera;
            return eye != null
                ? new Ray(eye.transform.position, eye.transform.forward)
                : new Ray(m_Character.transform.position, m_Character.transform.forward);
        }

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
