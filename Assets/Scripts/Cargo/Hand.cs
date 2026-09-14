using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Cargo
{
    [Serializable]
    public struct HandSettings
    {
        [Tooltip("Reach, m")]
        public float reachMetres;

        [Tooltip("Carry spring, N/m")]
        public float carrySpringNewtonsPerMetre;

        [Tooltip("Carry damping, N/(m/s)")]
        public float carryDamperNewtonsPerMetrePerSecond;

        [Tooltip("Most a carry may pull, N")]
        public float carryGripNewtons;

        [Tooltip("Arm spring, N/m")]
        public float armSpringNewtonsPerMetre;

        [Tooltip("Arm damping, N/(m/s)")]
        public float armDamperNewtonsPerMetrePerSecond;

        [Tooltip("Force that tears a grip off, N")]
        public float gripBreakForceNewtons;

        [Tooltip("Shortest throw wind-up, s")]
        public float minimumChargeSeconds;

        [Tooltip("Wind-up for a full throw, s")]
        public float fullChargeSeconds;

        [Tooltip("Speed of a gentle throw, m/s")]
        public float gentleSpeed;

        [Tooltip("Speed of a full throw, m/s")]
        public float hardestSpeed;

        public static HandSettings Default => new HandSettings
        {
            reachMetres = 1.2f,
            carrySpringNewtonsPerMetre = 3000f,
            carryDamperNewtonsPerMetrePerSecond = 300f,
            carryGripNewtons = 800f,
            armSpringNewtonsPerMetre = 10000f,
            armDamperNewtonsPerMetrePerSecond = 300f,

            gripBreakForceNewtons = 12000f,
            minimumChargeSeconds = 0.15f,
            fullChargeSeconds = 1.2f,
            gentleSpeed = 2f,
            hardestSpeed = 12f
        };
    }

    public sealed class Hand
    {
        readonly Transform m_Anchor;
        readonly Rigidbody m_Body;
        readonly HandSettings m_Settings;
        readonly List<Collider> m_InReach = new List<Collider>();

        Rigidbody m_Carried;
        Collider m_CarriedPart;
        ConfigurableJoint m_Carry;
        bool m_Carrying;
        bool m_TookHoldOnThisPress;
        float m_WoundUpAt = -1f;

        ConfigurableJoint m_Tether;
        bool m_Tethered;
        bool m_TetherHadABody;

        public Hand(Transform anchor, Rigidbody body, HandSettings settings)
        {
            m_Anchor = anchor != null ? anchor : throw new ArgumentNullException(nameof(anchor));
            m_Body = body != null ? body : throw new ArgumentNullException(nameof(body));
            m_Settings = settings;
        }

        public Rigidbody Carrying => m_Carry != null ? m_Carried : null;

        public Rigidbody HoldingOnto => m_Tether != null ? m_Tether.connectedBody : null;

        public bool Empty => m_Carry == null && m_Tether == null;

        public bool WindingUp => m_Carry != null && m_WoundUpAt >= 0f;

        public float Charge(float now)
            => WindingUp ? Mathf.Clamp01((now - m_WoundUpAt) / Mathf.Max(m_Settings.fullChargeSeconds, 1e-3f)) : 0f;

        public void Press(float now)
        {
            if (m_Carry != null)
            {
                m_WoundUpAt = now;
                return;
            }

            if (m_Tether != null)
            {
                return;
            }

            var nearest = Nearest(out var use);
            if (nearest == null)
            {
                return;
            }

            switch (use.As)
            {
                case HandUse.Category.Carry:
                    PickUp(nearest);
                    break;
                case HandUse.Category.HoldOnto:
                    HoldOnto(nearest);
                    break;
            }
        }

        public void Release(float now, Vector3 facing)
        {
            if (m_Tether != null)
            {
                LetGoOfTheTether();
                return;
            }

            if (m_Carry == null)
            {
                return;
            }

            if (m_TookHoldOnThisPress)
            {
                m_TookHoldOnThisPress = false;
                return;
            }

            var heldFor = WindingUp ? now - m_WoundUpAt : 0f;
            var charge = Charge(now);
            var bag = m_Carried;

            PutDown();

            if (heldFor >= m_Settings.minimumChargeSeconds)
            {
                Throw(bag, charge, facing);
            }
        }

        public void Tick()
        {
            if (m_Carrying && (m_Carry == null || m_CarriedPart == null || !InReach(m_CarriedPart)))
            {
                PutDown();
            }

            if (m_Tethered && (m_Tether == null || (m_TetherHadABody && m_Tether.connectedBody == null)))
            {
                LetGoOfTheTether();
            }
        }

        public void LetGo()
        {
            if (m_Tether != null)
            {
                LetGoOfTheTether();
            }

            if (m_Carrying)
            {
                PutDown();
            }
        }

        bool InReach(Collider part)
            => Vector3.Distance(part.ClosestPoint(m_Anchor.position), m_Anchor.position) <= m_Settings.reachMetres;

        Collider Nearest(out HandUse use)
        {
            use = null;
            Collider best = null;
            var bestDistance = float.MaxValue;

            m_InReach.Clear();
            m_InReach.AddRange(Physics.OverlapSphere(
                m_Anchor.position, m_Settings.reachMetres, ~0, QueryTriggerInteraction.Ignore));

            foreach (var candidate in m_InReach)
            {
                if (candidate.attachedRigidbody == m_Body || !HandUse.TryFind(candidate, out var says))
                {
                    continue;
                }

                if (says.As == HandUse.Category.Carry && !CanBePickedUp(candidate.attachedRigidbody))
                {
                    continue;
                }

                var distance = Vector3.Distance(candidate.ClosestPoint(m_Anchor.position), m_Anchor.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                    use = says;
                }
            }

            return best;
        }

        static bool CanBePickedUp(Rigidbody body) => body != null && !body.isKinematic;

        void PickUp(Collider part)
        {
            var bag = part.attachedRigidbody;
            m_Carried = bag;
            m_CarriedPart = part;
            m_Carrying = true;

            bag.transform.rotation = m_Body.transform.rotation;
            bag.rotation = m_Body.transform.rotation;
            bag.angularVelocity = Vector3.zero;

            m_Carry = bag.gameObject.AddComponent<ConfigurableJoint>();
            m_Carry.autoConfigureConnectedAnchor = false;
            m_Carry.connectedBody = m_Body;
            m_Carry.anchor = Vector3.zero;
            m_Carry.connectedAnchor = m_Body.transform.InverseTransformPoint(m_Anchor.position);

            var pull = new JointDrive
            {
                positionSpring = m_Settings.carrySpringNewtonsPerMetre,
                positionDamper = m_Settings.carryDamperNewtonsPerMetrePerSecond,
                maximumForce = m_Settings.carryGripNewtons
            };
            m_Carry.xDrive = pull;
            m_Carry.yDrive = pull;
            m_Carry.zDrive = pull;
            m_Carry.targetPosition = Vector3.zero;

            m_Carry.angularXMotion = ConfigurableJointMotion.Locked;
            m_Carry.angularYMotion = ConfigurableJointMotion.Locked;
            m_Carry.angularZMotion = ConfigurableJointMotion.Locked;

            m_Carry.enableCollision = false;

            m_TookHoldOnThisPress = true;
            m_WoundUpAt = -1f;
        }

        void PutDown()
        {
            if (m_Carry != null)
            {
                UnityEngine.Object.DestroyImmediate(m_Carry);
            }

            m_Carry = null;
            m_Carried = null;
            m_CarriedPart = null;
            m_Carrying = false;
            m_TookHoldOnThisPress = false;
            m_WoundUpAt = -1f;
        }

        void Throw(Rigidbody bag, float charge, Vector3 facing)
        {
            if (bag == null)
            {
                return;
            }

            var speed = Mathf.Lerp(m_Settings.gentleSpeed, m_Settings.hardestSpeed, charge);
            var direction = facing.sqrMagnitude > 1e-6f ? facing.normalized : m_Body.transform.forward;

            bag.linearVelocity = m_Body.linearVelocity + (direction * speed);
        }

        void HoldOnto(Collider thing)
        {
            var grabbedAt = thing.ClosestPoint(m_Anchor.position);
            var body = thing.attachedRigidbody;

            m_Tether = m_Body.gameObject.AddComponent<ConfigurableJoint>();
            m_Tether.autoConfigureConnectedAnchor = false;
            m_Tether.connectedBody = body;
            m_Tether.anchor = m_Body.transform.InverseTransformPoint(m_Anchor.position);
            m_Tether.connectedAnchor = body != null ? body.transform.InverseTransformPoint(grabbedAt) : grabbedAt;

            m_Tether.xMotion = ConfigurableJointMotion.Limited;
            m_Tether.yMotion = ConfigurableJointMotion.Limited;
            m_Tether.zMotion = ConfigurableJointMotion.Limited;
            m_Tether.angularXMotion = ConfigurableJointMotion.Free;
            m_Tether.angularYMotion = ConfigurableJointMotion.Free;
            m_Tether.angularZMotion = ConfigurableJointMotion.Free;

            m_Tether.linearLimit = new SoftJointLimit { limit = m_Settings.reachMetres, bounciness = 0f };
            m_Tether.linearLimitSpring = new SoftJointLimitSpring
            {
                spring = m_Settings.armSpringNewtonsPerMetre,
                damper = m_Settings.armDamperNewtonsPerMetrePerSecond
            };

            m_Tether.breakForce = m_Settings.gripBreakForceNewtons;
            m_Tether.enableCollision = true;

            m_Tethered = true;
            m_TetherHadABody = body != null;
        }

        void LetGoOfTheTether()
        {
            if (m_Tether != null)
            {
                UnityEngine.Object.DestroyImmediate(m_Tether);
            }

            m_Tether = null;
            m_Tethered = false;
        }
    }
}
