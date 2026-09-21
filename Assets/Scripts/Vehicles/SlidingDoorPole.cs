using BelowTheWing.Cargo;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SlidingDoorPole : MonoBehaviour, IUnhookWhenHeld
    {
        const float HookedSlackMetres = 0.002f;
        const float CatchesBelowMetresPerSecond = 0.2f;

        SkinnedMeshRenderer m_Panel;
        SkinnedMeshRenderer m_Fabric;
        Transform m_Cover;
        Vector3 m_ShutAt;
        Vector3 m_Along;
        float m_TravelMetres;
        float m_FixedPoleAt;

        DoorRailSettings m_Rail;
        ConfigurableJoint m_Track;
        Rigidbody m_Body;
        Rigidbody m_Cart;
        Vector3 m_CartWas;

        int m_Hands;
        Transform m_Hand;
        bool m_Hooked;
        float m_HookedAt;
        bool m_CameBack;

        bool ItHooks => m_Rail.latchHoldsAtNewtons > 0f;

        public float Openness { get; private set; }

        public bool Latched { get; private set; }

        public bool Hooked => m_Hooked;

        public bool HooksShut => ItHooks;

        public float TravelMetres => m_TravelMetres;

        public Vector3 AlongTheRail => m_Along;

        public Vector3 OpensToward
            => transform.parent != null
                ? transform.parent.TransformDirection(m_Along)
                : m_Along;

        public void Runs(
            SkinnedMeshRenderer panel, SkinnedMeshRenderer fabric, Transform cover,
            Vector3 shutAtLocal, Vector3 alongLocal, float travelMetres, float fixedPoleAt,
            DoorRailSettings rail)
        {
            m_Panel = panel;
            m_Fabric = fabric;
            m_Cover = cover;
            m_ShutAt = shutAtLocal;
            m_Along = alongLocal.normalized;
            m_TravelMetres = travelMetres;
            m_FixedPoleAt = fixedPoleAt;
            m_Rail = rail;

            m_Track = GetComponent<ConfigurableJoint>();
            m_Body = GetComponent<Rigidbody>();
            m_Cart = m_Track != null ? m_Track.connectedBody : null;
            m_CartWas = m_Cart != null ? m_Cart.linearVelocity : Vector3.zero;

            if (ItHooks)
            {
                Hook(SlidingDoor.Shut);
            }
        }

        public void RunsOn(DoorRailSettings rail)
        {
            m_Rail = rail;

            if (m_Body != null && rail.poleKg > 0f)
            {
                m_Body.mass = rail.poleKg;
            }

            if (m_Track != null)
            {
                var drive = m_Track.zDrive;
                drive.positionSpring = 0f;
                drive.positionDamper = Mathf.Max(rail.dragNewtonsPerMetrePerSecond, 0f);
                drive.maximumForce = rail.holdsAtNewtons;
                m_Track.zDrive = drive;

                var stop = m_Track.linearLimit;
                stop.bounciness = Mathf.Clamp01(rail.bounceOffTheEnd);
                m_Track.linearLimit = stop;
            }

            if (ItHooks)
            {
                Hook(SlidingDoor.EndItSettlesTo(Openness));
            }
            else
            {
                Unhook();
            }
        }

        public void TakeHold(Transform hand)
        {
            m_Hands++;
            m_Hand = hand;

            Unhook();
            m_CameBack = false;
        }

        public void LetGo()
        {
            m_Hands = Mathf.Max(m_Hands - 1, 0);

            if (m_Hands == 0)
            {
                m_Hand = null;
            }
        }

        void FixedUpdate()
        {
            if (transform.parent == null || m_Body == null)
            {
                return;
            }

            Openness = SlidingDoor.OpennessAt(
                Vector3.Dot(transform.localPosition - m_ShutAt, m_Along), m_TravelMetres);

            MindTheHook();

            if (m_Hands > 0)
            {
                FollowTheHand();
            }
            else
            {
                HoldItAtItsEnd();
            }

            var weight = SlidingDoor.ShapeWeight(Openness);

            Show(m_Panel, weight);
            Show(m_Fabric, weight);

            StretchTheCover();
        }

        void MindTheHook()
        {
            var shake = HowHardTheCartIsShaken();
            var seated = SlidingDoor.Seated(Openness, m_Rail.seatedWithinFraction);

            if (!seated)
            {
                m_CameBack = true;
            }

            if (!ItHooks || m_Hands > 0)
            {
                Unhook();
                return;
            }

            if (m_Hooked)
            {
                if (m_Rail.unhooksAboveMetresPerSecondSquared > 0f
                    && shake > m_Rail.unhooksAboveMetresPerSecondSquared)
                {
                    Unhook();
                }

                return;
            }

            if (!seated)
            {
                return;
            }

            if (m_CameBack && Mathf.Abs(SpeedAlongTheRail()) < CatchesBelowMetresPerSecond)
            {
                Hook(SlidingDoor.EndItSettlesTo(Openness));
                m_CameBack = false;
            }
        }

        void Hook(float end)
        {
            m_Hooked = true;
            m_HookedAt = end;

            if (m_Track == null)
            {
                return;
            }

            m_Track.connectedAnchor = m_ShutAt + (m_Along * (m_TravelMetres * end));

            var stop = m_Track.linearLimit;
            stop.limit = HookedSlackMetres;
            m_Track.linearLimit = stop;
        }

        void Unhook()
        {
            if (!m_Hooked)
            {
                return;
            }

            m_Hooked = false;

            if (m_Track == null)
            {
                return;
            }

            m_Track.connectedAnchor = m_ShutAt + (m_Along * (m_TravelMetres * 0.5f));

            var stop = m_Track.linearLimit;
            stop.limit = m_TravelMetres * 0.5f;
            m_Track.linearLimit = stop;
        }

        void FollowTheHand()
        {
            if (m_Hand == null || m_Rail.followsAHandNewtonsPerMetre <= 0f)
            {
                return;
            }

            var reach = Vector3.Dot(m_Hand.position - transform.position, OpensToward);

            var push = Mathf.Clamp(
                (reach * m_Rail.followsAHandNewtonsPerMetre)
                - (SpeedAlongTheRail() * SlidingDoor.CriticalDampingFor(
                    m_Rail.followsAHandNewtonsPerMetre, m_Body.mass)),
                -m_Rail.holdsAtNewtons,
                m_Rail.holdsAtNewtons);

            m_Body.AddForce(OpensToward * push);
        }

        void HoldItAtItsEnd()
        {
            if (m_Hooked)
            {
                return;
            }

            Latched = m_Rail.latchHoldsAtNewtons > 0f
                      && SlidingDoor.Seated(Openness, m_Rail.seatedWithinFraction);

            var holds = Latched ? m_Rail.latchHoldsAtNewtons : Mathf.Max(m_Rail.seatsAtNewtons, 0f);

            if (holds <= 0f)
            {
                return;
            }

            var howFarShort = (SlidingDoor.EndItSettlesTo(Openness) - Openness) * m_TravelMetres;

            var push = Mathf.Clamp(
                (howFarShort * SlidingDoor.SeatingStiffnessNewtonsPerMetre)
                - (SpeedAlongTheRail() * SlidingDoor.SettlingDampingFor(holds)),
                -holds,
                holds);

            m_Body.AddForce(OpensToward * push);
        }

        float HowHardTheCartIsShaken()
        {
            if (m_Cart == null)
            {
                return 0f;
            }

            var now = m_Cart.linearVelocity;
            var shake = (now - m_CartWas).magnitude / Mathf.Max(Time.fixedDeltaTime, 1e-5f);

            m_CartWas = now;

            return shake;
        }

        float SpeedAlongTheRail()
            => Vector3.Dot(m_Body.linearVelocity - CartVelocity(), OpensToward);

        Vector3 CartVelocity() => m_Cart != null ? m_Cart.linearVelocity : Vector3.zero;

        void StretchTheCover()
        {
            if (m_Cover == null)
            {
                return;
            }

            var along = transform.localPosition.z;
            var was = m_Cover.localScale;

            m_Cover.localPosition = new Vector3(
                m_ShutAt.x, m_ShutAt.y, SlidingDoor.CoverSitsAt(along, m_FixedPoleAt));

            m_Cover.localScale = new Vector3(
                was.x, was.y, Mathf.Max(SlidingDoor.CoveredMetres(along, m_FixedPoleAt), 0.001f));
        }

        static void Show(SkinnedMeshRenderer on, float weight)
        {
            if (on == null || on.sharedMesh == null || on.sharedMesh.blendShapeCount == 0)
            {
                return;
            }

            on.SetBlendShapeWeight(0, weight);
        }
    }
}
