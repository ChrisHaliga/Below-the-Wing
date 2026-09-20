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
        Rigidbody m_Cart;
        Vector3 m_CartWas;

        int m_Hands;
        bool m_Hooked;
        bool m_CameBack;

        bool ItHooksShut => m_Rail.latchHoldsAtNewtons > 0f;

        public float Openness { get; private set; }

        public bool Latched { get; private set; }

        public bool Hooked => m_Hooked;

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
            m_Cart = m_Track != null ? m_Track.connectedBody : null;
            m_CartWas = m_Cart != null ? m_Cart.linearVelocity : Vector3.zero;

            Hook(ItHooksShut);
        }

        public void TakeHold()
        {
            m_Hands++;
            Hook(false);
            m_CameBack = false;
        }

        public bool HooksShut => ItHooksShut;

        public void RunsOn(DoorRailSettings rail)
        {
            m_Rail = rail;
            Hook(ItHooksShut);

            var body = GetComponent<Rigidbody>();
            if (body != null && rail.poleKg > 0f)
            {
                body.mass = rail.poleKg;
            }

            if (m_Track == null)
            {
                return;
            }

            var drive = m_Track.zDrive;
            drive.positionSpring = 0f;
            drive.positionDamper = Mathf.Max(rail.dragNewtonsPerMetrePerSecond, 0f);
            drive.maximumForce = rail.holdsAtNewtons;
            m_Track.zDrive = drive;

            var stop = m_Track.linearLimit;
            stop.bounciness = Mathf.Clamp01(rail.bounceOffTheEnd);
            m_Track.linearLimit = stop;
        }

        public void LetGo() => m_Hands = Mathf.Max(m_Hands - 1, 0);

        void FixedUpdate()
        {
            if (transform.parent == null)
            {
                return;
            }

            Openness = SlidingDoor.OpennessAt(
                Vector3.Dot(transform.localPosition - m_ShutAt, m_Along), m_TravelMetres);

            MindTheHook();
            HoldItAtItsEnd();

            var weight = SlidingDoor.ShapeWeight(Openness);

            Show(m_Panel, weight);
            Show(m_Fabric, weight);

            StretchTheCover();
        }

        void MindTheHook()
        {
            var shake = HowHardTheCartIsShaken();

            if (!ItHooksShut || m_Hands > 0)
            {
                Hook(false);
                return;
            }

            if (m_Hooked)
            {
                if (m_Rail.unhooksAboveMetresPerSecondSquared > 0f
                    && shake > m_Rail.unhooksAboveMetresPerSecondSquared)
                {
                    Hook(false);
                }

                return;
            }

            var shut = Openness <= Mathf.Clamp(m_Rail.seatedWithinFraction, 0f, 0.5f);

            if (!shut)
            {
                m_CameBack = true;
                return;
            }

            if (m_CameBack && Mathf.Abs(SpeedAlongTheRail()) < CatchesBelowMetresPerSecond)
            {
                Hook(true);
                m_CameBack = false;
            }
        }

        void Hook(bool on)
        {
            m_Hooked = on;

            if (m_Track == null)
            {
                return;
            }

            m_Track.connectedAnchor = on ? m_ShutAt : m_ShutAt + (m_Along * (m_TravelMetres * 0.5f));

            var stop = m_Track.linearLimit;
            stop.limit = on ? HookedSlackMetres : m_TravelMetres * 0.5f;
            m_Track.linearLimit = stop;
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

        void HoldItAtItsEnd()
        {
            var body = GetComponent<Rigidbody>();

            if (body == null || m_Hooked)
            {
                return;
            }

            Latched = m_Hands == 0
                      && m_Rail.latchHoldsAtNewtons > 0f
                      && SlidingDoor.Seated(Openness, m_Rail.seatedWithinFraction);

            var holds = WhatHoldsIt();

            if (holds <= 0f)
            {
                return;
            }

            var howFarShort = (SlidingDoor.EndItSettlesTo(Openness) - Openness) * m_TravelMetres;
            var alongTheRail = Vector3.Dot(body.linearVelocity - CartVelocity(), OpensToward);

            var push = Mathf.Clamp(
                (howFarShort * SlidingDoor.SeatingStiffnessNewtonsPerMetre)
                - (alongTheRail * SlidingDoor.SeatingDampingFor(body.mass)),
                -holds,
                holds);

            body.AddForce(OpensToward * push);
        }

        Vector3 CartVelocity() => m_Cart != null ? m_Cart.linearVelocity : Vector3.zero;

        float SpeedAlongTheRail()
        {
            var body = GetComponent<Rigidbody>();

            return body == null
                ? 0f
                : Vector3.Dot(body.linearVelocity - CartVelocity(), OpensToward);
        }

        float WhatHoldsIt()
        {
            if (m_Hands > 0)
            {
                return 0f;
            }

            return Latched ? m_Rail.latchHoldsAtNewtons : Mathf.Max(m_Rail.seatsAtNewtons, 0f);
        }

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
