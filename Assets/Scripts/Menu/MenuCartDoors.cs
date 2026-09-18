using System.Collections.Generic;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Menu
{
    [DisallowMultipleComponent]
    public sealed class MenuCartDoors : MonoBehaviour
    {
        [SerializeField, Tooltip("The cart whose doors the menu slides open")]
        GameObject m_Cart;

        [SerializeField, Tooltip("Shove given to each door pole, newton-seconds. Higher slams harder")]
        float m_ShoveNewtonSeconds = 38f;

        [SerializeField, Tooltip("How long the far doors wait after the near ones are shoved, seconds")]
        float m_SecondSetWaitsSeconds = 3.3f;

        [SerializeField, Tooltip("The rail the menu builds the doors on. The game uses DoorRailSettings.Default")]
        DoorRailSettings m_Rail = MenuRail;

        readonly List<SlidingDoorPole> m_Near = new List<SlidingDoorPole>();
        readonly List<SlidingDoorPole> m_Far = new List<SlidingDoorPole>();
        readonly List<SlidingDoorPole> m_Poles = new List<SlidingDoorPole>();

        bool m_Open;
        bool m_NearShoved = true;
        bool m_FarShoved = true;
        float m_Since;

        public GameObject Cart => m_Cart;

        public bool BothSetsAreMoving => m_NearShoved && m_FarShoved;

        public float Openness
        {
            get
            {
                var most = 0f;

                foreach (var pole in m_Poles)
                {
                    most = Mathf.Max(most, pole.Openness);
                }

                return most;
            }
        }

        public static float WaitFor(bool nearTheCamera, float secondSetWaits)
            => nearTheCamera ? 0f : Mathf.Max(secondSetWaits, 0f);

        public static float ShoveFor(bool open, float newtonSeconds)
            => (open ? 1f : -1f) * Mathf.Max(newtonSeconds, 0f);

        public static bool NearTheCamera(float poleAcrossTheCart) => poleAcrossTheCart > 0f;

        public void Open(bool open)
        {
            if (open == m_Open)
            {
                return;
            }

            m_Open = open;
            m_Since = 0f;
            m_NearShoved = false;
            m_FarShoved = false;
        }

        void OnEnable()
        {
            if (m_Cart == null)
            {
                throw MisbuiltException.Refuse(this, "has no cart, so there are no doors to open");
            }

            RaiseTheRig();
        }

        void RaiseTheRig()
        {
            SlidingDoors.Build(m_Cart, m_Cart.GetComponent<VehicleShape>(), null, m_Rail);

            m_Poles.Clear();
            m_Near.Clear();
            m_Far.Clear();

            foreach (var pole in m_Cart.GetComponentsInChildren<SlidingDoorPole>(true))
            {
                pole.enabled = true;

                pole.GetComponent<Rigidbody>().isKinematic = false;

                m_Poles.Add(pole);
                (NearTheCamera(pole.transform.localPosition.x) ? m_Near : m_Far).Add(pole);
            }

            if (m_Poles.Count == 0)
            {
                throw MisbuiltException.Refuse(this, "raised no door poles on its cart");
            }

            foreach (var pole in m_Poles)
            {
                AimAt(pole, open: false);
            }
        }

        static DoorRailSettings MenuRail
        {
            get
            {
                var rail = DoorRailSettings.Default;

                rail.dragNewtonsPerMetrePerSecond = 20f;
                rail.bounceOffTheEnd = 0.25f;
                rail.settlesAtNewtonsPerMetre = 30f;

                return rail;
            }
        }

        static void AimAt(SlidingDoorPole pole, bool open)
        {
            var rail = pole.GetComponent<ConfigurableJoint>();

            rail.targetPosition = new Vector3(
                0f,
                0f,
                (open ? -1f : 1f) * pole.AlongTheRail.z * pole.TravelMetres * 0.5f);
        }

        void Update()
        {
            if (m_NearShoved && m_FarShoved)
            {
                return;
            }

            m_Since += Time.deltaTime;

            if (!m_NearShoved)
            {
                Shove(m_Near);
                m_NearShoved = true;
            }

            if (!m_FarShoved && m_Since >= WaitFor(nearTheCamera: false, m_SecondSetWaitsSeconds))
            {
                Shove(m_Far);
                m_FarShoved = true;
            }
        }

        void Shove(IReadOnlyList<SlidingDoorPole> poles)
        {
            var shove = ShoveFor(m_Open, m_ShoveNewtonSeconds);

            foreach (var pole in poles)
            {
                AimAt(pole, m_Open);

                pole.GetComponent<Rigidbody>().AddForce(pole.OpensToward * shove, ForceMode.Impulse);
            }
        }
    }
}
