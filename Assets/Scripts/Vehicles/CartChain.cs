using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>How the joints between coupled vehicles are set up.</summary>
    [Serializable]
    public struct ChainJointSettings
    {
        [Tooltip("How far a coupling may swing to either side, in degrees. Stops a train folding " +
                 "back through itself when it jackknifes.")]
        public float yawLimitDegrees;

        [Tooltip("Position solver iterations for every body in a chain. Coupled bodies need more " +
                 "than the project default or the joints visibly stretch under load.")]
        public int solverPositionIterations;

        [Tooltip("Velocity solver iterations for every body in a chain.")]
        public int solverVelocityIterations;

        /// <summary>Settings that hold a short train together without straining the solver.</summary>
        public static ChainJointSettings Default => new ChainJointSettings
        {
            yawLimitDegrees = 75f,
            solverPositionIterations = 16,
            solverVelocityIterations = 4
        };
    }

    /// <summary>
    /// A tractor and the carts hooked up behind it, treated as a single thing.
    ///
    /// Physically a chain is a row of separate rigidbodies held together by hinges. For almost
    /// every purpose above physics, though, it has to behave as one object: it is taken over as
    /// one, handed back as one, and reported as one. Splitting that -- letting two machines
    /// simulate different carts of the same train -- puts a joint across a boundary where half the
    /// constraint solver is working against a body it cannot move, and that is where these trains
    /// come apart.
    ///
    /// A chain owns the joints it created and is the only thing that destroys them. There is no
    /// such thing as a dormant coupling left lying around switched off.
    /// </summary>
    public sealed class CartChain
    {
        readonly List<VehicleController> m_Members;

        /// <summary>
        /// The coupling between each member and the one behind it. One shorter than the member
        /// list, because the vehicle at the back has nothing hooked on to it.
        /// </summary>
        readonly List<HingeJoint> m_Couplings;

        readonly ChainJointSettings m_Settings;

        CartChain(List<VehicleController> members, List<HingeJoint> couplings, ChainJointSettings settings)
        {
            m_Members = members;
            m_Couplings = couplings;
            m_Settings = settings;

            foreach (var member in m_Members)
            {
                member.Chain = this;
            }

            ApplySolverEffort();
        }

        /// <summary>
        /// Every vehicle in the train, from the one at the front to the one at the back.
        /// A chain always has at least one member.
        /// </summary>
        public IReadOnlyList<VehicleController> Members => m_Members;

        /// <summary>The vehicle at the front, which is the one a player drives.</summary>
        public VehicleController Leader => m_Members[0];

        /// <summary>
        /// Hooks a row of vehicles together, front to back, and returns the train they form.
        /// Each vehicle is joined to the one ahead of it at their facing hitch points.
        /// </summary>
        public static CartChain Couple(IReadOnlyList<VehicleController> frontToBack, ChainJointSettings settings)
        {
            if (frontToBack == null || frontToBack.Count == 0)
            {
                throw new ArgumentException("A train needs at least one vehicle in it.", nameof(frontToBack));
            }

            var members = new List<VehicleController>(frontToBack);
            var couplings = new List<HingeJoint>(members.Count - 1);

            for (var i = 0; i < members.Count - 1; i++)
            {
                couplings.Add(Hitch(inFront: members[i], behind: members[i + 1], settings));
            }

            return new CartChain(members, couplings, settings);
        }

        /// <summary>Whether this vehicle is part of this train.</summary>
        public bool Contains(VehicleController vehicle) => m_Members.Contains(vehicle);

        /// <summary>
        /// Asks to simulate the whole train on this machine, reporting whether that was granted.
        /// Every member moves together or none of them does.
        /// </summary>
        public void RequestOwnership(IOwnershipBroker broker, Action<bool> onResult)
            => broker.RequestAll(m_Members, onResult);

        /// <summary>
        /// Unhooks the train after the given member, destroying the coupling between it and the
        /// next one, and returns the two trains that result.
        /// </summary>
        public (CartChain Front, CartChain Back) SplitAfter(int memberIndex)
        {
            if (memberIndex < 0 || memberIndex >= m_Members.Count - 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(memberIndex),
                    $"There is no coupling behind member {memberIndex} of a {m_Members.Count} vehicle train.");
            }

            Unhitch(m_Couplings[memberIndex]);

            var frontMembers = m_Members.GetRange(0, memberIndex + 1);
            var frontCouplings = m_Couplings.GetRange(0, memberIndex);

            var backStart = memberIndex + 1;
            var backMembers = m_Members.GetRange(backStart, m_Members.Count - backStart);
            var backCouplings = m_Couplings.GetRange(backStart, m_Couplings.Count - backStart);

            return (new CartChain(frontMembers, frontCouplings, m_Settings),
                    new CartChain(backMembers, backCouplings, m_Settings));
        }

        /// <summary>Destroys every coupling in this train, leaving its members unhitched.</summary>
        public void Dissolve()
        {
            foreach (var coupling in m_Couplings)
            {
                Unhitch(coupling);
            }

            m_Couplings.Clear();

            foreach (var member in m_Members)
            {
                Couple(new[] { member }, m_Settings);
            }

            m_Members.Clear();
        }

        /// <summary>
        /// The coupling between a member and the one behind it, or null if there is none.
        /// Exists so that a train can be checked for couplings it should no longer have.
        /// </summary>
        public Joint CouplingBehind(int memberIndex)
            => memberIndex >= 0 && memberIndex < m_Couplings.Count ? m_Couplings[memberIndex] : null;

        static HingeJoint Hitch(VehicleController inFront, VehicleController behind, ChainJointSettings settings)
        {
            // The joint lives on the vehicle being towed, which is the one whose movement it
            // constrains, and connects forward to the one doing the towing.
            var coupling = behind.gameObject.AddComponent<HingeJoint>();
            coupling.connectedBody = inFront.Body;

            // Each end is anchored at its own vehicle's hitch. Those two points land on top of one
            // another when the vehicles are parked at coupling distance, so the joint begins life
            // already satisfied and has nothing to pull against.
            //
            // A coupling that starts out violated never stops trying to close, and the solver drags
            // the whole train along for as long as the session lasts.
            coupling.autoConfigureConnectedAnchor = false;
            coupling.anchor = behind.FrontHitchLocal;
            coupling.connectedAnchor = inFront.RearHitchLocal;

            coupling.axis = Vector3.up;
            coupling.useLimits = true;
            coupling.limits = new JointLimits
            {
                min = -settings.yawLimitDegrees,
                max = settings.yawLimitDegrees
            };

            // Preprocessing lets the solver take shortcuts on joints it considers over-constrained,
            // which in a chain shows up as couplings that quietly give way under load.
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
            // A vehicle on its own is an ordinary rigidbody and needs no special treatment. It is
            // being tied to others that makes the solver work harder, so the extra effort is spent
            // only where there is a coupling to hold.
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
