using UnityEngine;
using UnityEngine.Events;

namespace BelowTheWing.Apron
{
    /// <summary>
    /// A thing on the apron that does something when a player walks up and presses it.
    ///
    /// A collider, a label, and an action somebody wired to it in the editor. It has no idea what
    /// it is connected to, what the action means, or why anybody would press it -- which is the
    /// whole design. A button that knows it sets a speed is a button that will eventually know
    /// which speeds are the dangerous ones, and then the game has learned about a test rig.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public sealed class PressableControl : MonoBehaviour
    {
        [SerializeField, Tooltip("What is written on it. Whatever somebody typed, and nothing else.")]
        string m_Label = "";

        [SerializeField, Tooltip("How close a player has to be to press it, in metres.")]
        float m_ReachMetres = 2.5f;

        [SerializeField, Tooltip("What happens when it is pressed. Wired up in the editor.")]
        UnityEvent m_Pressed = new UnityEvent();

        /// <summary>What is written on it.</summary>
        public string Label => m_Label;

        /// <summary>How close somebody has to be.</summary>
        public float ReachMetres => m_ReachMetres;

        /// <summary>What happens when it is pressed.</summary>
        public UnityEvent Pressed => m_Pressed;

        /// <summary>Whether somebody standing here could press it.</summary>
        public bool WithinReachOf(Vector3 standingAt)
            => Vector3.Distance(standingAt, transform.position) <= m_ReachMetres;

        /// <summary>
        /// Presses it, if the player is close enough.
        ///
        /// The distance is checked here rather than trusted from the caller, because a control that
        /// can be pressed from anywhere is a control anybody can press from inside a moving cart on
        /// the other side of the apron.
        /// </summary>
        public bool PressFrom(Vector3 standingAt)
        {
            if (!WithinReachOf(standingAt))
            {
                return false;
            }

            m_Pressed.Invoke();
            return true;
        }

        /// <summary>Names and labels it, for something building one rather than authoring it.</summary>
        public void Called(string label, float reachMetres)
        {
            m_Label = label;
            m_ReachMetres = reachMetres;
        }
    }
}
