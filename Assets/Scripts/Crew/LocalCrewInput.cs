using BelowTheWing.Cargo;
using BelowTheWing.Vehicles;
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
                Held(keyboard, CrewKeys.Right) - Held(keyboard, CrewKeys.Left),
                Held(keyboard, CrewKeys.Forward) - Held(keyboard, CrewKeys.Back));

            m_Jump.ForgetOnceAStepHasSeenIt(Time.fixedTimeAsDouble);
            if (keyboard[CrewKeys.Jump].wasPressedThisFrame)
            {
                m_Jump.Ask(Time.fixedTimeAsDouble);
            }

            var holdingOn = keyboard[CrewKeys.Jump].isPressed;

            Current = new CrewIntent(
                move,
                sprint: keyboard[CrewKeys.Sprint].isPressed || keyboard[CrewKeys.SprintToo].isPressed,
                brake: holdingOn ? 1f : 0f,
                jump: m_Jump.Asked,
                crouch: keyboard[CrewKeys.Crouch].isPressed,
                hoist: holdingOn);

            Look();

            if (keyboard[CrewKeys.Drive].wasPressedThisFrame && !ParkedWhateverTheyAreLookingAt())
            {
                m_Character.Seat?.Toggle(m_Character);
            }

            if (keyboard[CrewKeys.Hitch].wasPressedThisFrame)
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
                hand.Press(Time.time, m_Character.LookingAlong());
            }

            if (button.wasReleasedThisFrame)
            {
                hand.Release(Time.time, ThrowingTowards());
            }
        }

        bool ParkedWhateverTheyAreLookingAt()
        {
            var looked = Aiming.At<CartBrake>(
                m_Character.LookingAlong(), m_Character.ReachMetres, m_Character.ConeDegrees);

            return looked != null && looked.Toggle();
        }

        Vector3 ThrowingTowards() => m_Character.LookingAlong().direction;

        static float Held(Keyboard keyboard, Key key) => keyboard[key].isPressed ? 1f : 0f;
    }
}
