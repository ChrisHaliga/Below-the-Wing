using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [Serializable]
    public struct ChainJointSettings
    {
        [Tooltip("Coupling swing to either side, degrees")]
        public float yawLimitDegrees;

        [Tooltip("Position solver iterations per body")]
        public int solverPositionIterations;

        [Tooltip("Velocity solver iterations per body")]
        public int solverVelocityIterations;

        public static ChainJointSettings Default => new ChainJointSettings
        {
            yawLimitDegrees = 75f,
            solverPositionIterations = 16,
            solverVelocityIterations = 4
        };
    }

    public sealed class CartChain
    {
        readonly List<VehicleController> m_Members;

        readonly List<HingeJoint> m_Couplings;

        readonly List<Rigidbody> m_Bodies;
        readonly ContactBlackout m_Crashing = new ContactBlackout(BlackoutSettings.Default);
        readonly ChainJointSettings m_Settings;

        CartChain(List<VehicleController> members, List<HingeJoint> couplings, ChainJointSettings settings)
        {
            m_Members = members;
            m_Couplings = couplings;
            m_Settings = settings;

            m_Bodies = new List<Rigidbody>(members.Count);
            foreach (var member in members)
            {
                m_Bodies.Add(member.Body);
            }

            foreach (var member in m_Members)
            {
                member.Chain = this;
            }

            ApplySolverEffort();
        }

        public IReadOnlyList<VehicleController> Members => m_Members;

        public VehicleController Leader => m_Members[0];

        public IReadOnlyList<Rigidbody> Bodies => m_Bodies;

        public ChainJointSettings Settings => m_Settings;

        public bool CouplingsEngaged
        {
            get
            {
                foreach (var coupling in m_Couplings)
                {
                    if (coupling == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public static CartChain Couple(IReadOnlyList<VehicleController> frontToBack, ChainJointSettings settings)
        {
            if (frontToBack == null || frontToBack.Count == 0)
            {
                throw new ArgumentException("A train needs at least one vehicle in it.", nameof(frontToBack));
            }

            var members = new List<VehicleController>(frontToBack);
            var couplings = new List<HingeJoint>(new HingeJoint[Mathf.Max(0, members.Count - 1)]);

            return new CartChain(members, couplings, settings);
        }

        public void EngageCouplings()
        {
            for (var i = 0; i < m_Couplings.Count; i++)
            {
                if (m_Couplings[i] == null)
                {
                    m_Couplings[i] = Hitch(inFront: m_Members[i], behind: m_Members[i + 1], m_Settings);
                }
            }
        }

        public void ReleaseCouplings()
        {
            for (var i = 0; i < m_Couplings.Count; i++)
            {
                Unhitch(m_Couplings[i]);
                m_Couplings[i] = null;
            }
        }

        public float CorrectionAuthority => m_Crashing.Authority;

        public void NoteCollision() => m_Crashing.Touched();

        public void TickBlackout(float deltaTime) => m_Crashing.Tick(deltaTime);

        public bool Contains(VehicleController vehicle) => m_Members.Contains(vehicle);

        public void RequestOwnership(IOwnershipBroker broker, Action<bool> onResult)
            => broker.RequestAll(m_Members, onResult);

        public (CartChain Front, CartChain Back) SplitAfter(int memberIndex)
        {
            if (memberIndex < 0 || memberIndex >= m_Members.Count - 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(memberIndex),
                    $"There is no coupling behind member {memberIndex} of a {m_Members.Count} vehicle train.");
            }

            Unhitch(m_Couplings[memberIndex]);
            m_Couplings[memberIndex] = null;

            var frontMembers = m_Members.GetRange(0, memberIndex + 1);
            var frontCouplings = m_Couplings.GetRange(0, memberIndex);

            var backStart = memberIndex + 1;
            var backMembers = m_Members.GetRange(backStart, m_Members.Count - backStart);
            var backCouplings = m_Couplings.GetRange(backStart, m_Couplings.Count - backStart);

            return (new CartChain(frontMembers, frontCouplings, m_Settings),
                    new CartChain(backMembers, backCouplings, m_Settings));
        }

        public Joint CouplingBehind(int memberIndex)
            => memberIndex >= 0 && memberIndex < m_Couplings.Count ? m_Couplings[memberIndex] : null;

        static HingeJoint Hitch(VehicleController inFront, VehicleController behind, ChainJointSettings settings)
        {
            if (behind.FrontHitchLocal == null || inFront.RearHitchLocal == null)
            {
                Debug.LogError(
                    $"'{behind.name}' cannot be hitched behind '{inFront.name}': " +
                    $"{(behind.FrontHitchLocal == null ? behind.name + " has no coupling at its front" : inFront.name + " has no coupling at its back")}.",
                    behind);

                return null;
            }

            var coupling = behind.gameObject.AddComponent<HingeJoint>();
            coupling.connectedBody = inFront.Body;

            var ourEnd = behind.FrontHitchLocal.Value;
            var theirEnd = inFront.RearHitchLocal.Value;
            var meetAt = 0.5f * (ourEnd.y + theirEnd.y);

            coupling.autoConfigureConnectedAnchor = false;
            coupling.anchor = new Vector3(0f, meetAt, ourEnd.z);
            coupling.connectedAnchor = new Vector3(0f, meetAt, theirEnd.z);

            coupling.axis = Vector3.up;
            coupling.useLimits = true;
            coupling.limits = new JointLimits
            {
                min = -settings.yawLimitDegrees,
                max = settings.yawLimitDegrees
            };

            coupling.enablePreprocessing = false;

            return coupling;
        }

        static void Unhitch(HingeJoint coupling)
        {
            if (coupling == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(coupling);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(coupling);
            }
        }

        void ApplySolverEffort()
        {
            var coupled = m_Members.Count > 1;

            foreach (var member in m_Members)
            {
                member.Body.solverIterations = coupled
                    ? m_Settings.solverPositionIterations
                    : Physics.defaultSolverIterations;

                member.Body.solverVelocityIterations = coupled
                    ? m_Settings.solverVelocityIterations
                    : Physics.defaultSolverVelocityIterations;
            }
        }
    }
}
