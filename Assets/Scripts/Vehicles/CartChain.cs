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
    /// A chain is two separate ideas that have to be kept apart.
    ///
    /// Its <b>membership</b> -- which vehicles belong to this train and in what order -- is true on
    /// every machine in the session. It has to be, because taking a train over means asking for all
    /// of its members at once, and a machine that believes a tractor is on its own will ask for the
    /// tractor and drive away leaving its carts behind, simulated by somebody else.
    ///
    /// Its <b>couplings</b> -- the hinges that physically hold it together -- exist only on the one
    /// machine simulating it. A joint whose two ends are being integrated by different physics
    /// engines has half its constraint solver working against a body it cannot move, and that is
    /// where these trains come apart. Everywhere else the members are replicated copies following
    /// their owner, and hinges between them would only fight that.
    ///
    /// A chain owns any couplings it created and is the only thing that destroys them. There is no
    /// such thing as a dormant coupling left lying around switched off.
    /// </summary>
    public sealed class CartChain
    {
        readonly List<VehicleController> m_Members;

        /// <summary>
        /// The coupling between each member and the one behind it, or null where there is none.
        /// Always one shorter than the member list: the vehicle at the back has nothing hooked on.
        /// Every entry is null on a machine that is not simulating this train.
        /// </summary>
        readonly List<HingeJoint> m_Couplings;

        readonly List<Rigidbody> m_Bodies;
        readonly ContactBlackout m_Crashing = new ContactBlackout(BlackoutSettings.Default);
        float m_SecondsStill;
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

        /// <summary>
        /// Every vehicle in the train, from the one at the front to the one at the back.
        /// A chain always has at least one member.
        /// </summary>
        public IReadOnlyList<VehicleController> Members => m_Members;

        /// <summary>The vehicle at the front, which is the one a player drives.</summary>
        public VehicleController Leader => m_Members[0];

        /// <summary>
        /// Every member's rigidbody, in order.
        ///
        /// Built once with the chain rather than gathered on demand, because it is wanted on every
        /// physics step by whatever is keeping this train in step with the machine that owns it.
        /// </summary>
        public IReadOnlyList<Rigidbody> Bodies => m_Bodies;

        /// <summary>
        /// How this train's couplings are set up. Carried so that a train made longer or shorter at
        /// runtime keeps the joint limits and solver effort the original had, rather than silently
        /// acquiring whatever the defaults happen to be.
        /// </summary>
        public ChainJointSettings Settings => m_Settings;

        /// <summary>
        /// Whether this machine is holding the train together with real joints, rather than
        /// watching a train somebody else is simulating.
        /// </summary>
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

                // A vehicle on its own has no couplings to engage, and is therefore as hooked
                // together as it will ever be. Answering false for it -- which a count check does --
                // means every uncoupled vehicle in the game reports that it is waiting to be joined
                // up, and anything that gates behaviour on this refuses to act on a lone tractor.
                return true;
            }
        }

        /// <summary>
        /// Records that these vehicles form one train, front to back.
        ///
        /// This establishes membership only. Nothing is physically joined until
        /// <see cref="EngageCouplings"/> is called, which the machine simulating the train does and
        /// no other machine should.
        /// </summary>
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

        /// <summary>
        /// Physically hooks the train together on this machine. Does nothing to couplings that are
        /// already in place, so it is safe to call whenever ownership is confirmed.
        /// </summary>
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

        /// <summary>
        /// Takes the couplings off, leaving membership intact. Called on a machine that has handed
        /// the train to somebody else: the train still exists and is still one thing, it is simply
        /// being simulated elsewhere now.
        /// </summary>
        public void ReleaseCouplings()
        {
            for (var i = 0; i < m_Couplings.Count; i++)
            {
                Unhitch(m_Couplings[i]);
                m_Couplings[i] = null;
            }
        }

        /// <summary>
        /// Puts a whole train to sleep once all of it has stood still for long enough.
        ///
        /// A whole train, because sleep decided one vehicle at a time does not work on things that
        /// are coupled together. A cart that goes quiet is put to sleep on its own, and a step later
        /// the hinge to its neighbour -- still awake -- wakes it up again. The two trade being
        /// asleep back and forth and neither ever comes to rest, so a train that has been let go of
        /// keeps creeping across the apron and goes on costing for the rest of the session.
        ///
        /// Getting there needs an explicit push in the first place. Any force wakes a rigidbody, so
        /// a vehicle holding itself up on its own suspension is woken every step by the very force
        /// keeping it standing, and could never drop off on its own. For the same reason this
        /// reports whether it has just sent the train to sleep: the caller has to stop there and
        /// apply nothing further this step, or it wakes the train it has only now settled.
        /// </summary>
        public bool SettleOnceEverythingHasStopped(
            bool nobodyIsDriving, float deltaTime, float secondsNeeded,
            float stillMetresPerSecond, float stillRadiansPerSecond)
        {
            // Waking is a decision for the whole train too, and for the same reason. A train slept
            // as one thing and then woken one vehicle at a time is a tractor that drives off leaving
            // four sleeping carts standing on the apron, stretching the couplings between them until
            // the solver gives up.
            if (!nobodyIsDriving || HalfAsleep())
            {
                m_SecondsStill = 0f;
                WakeEverything();
                return false;
            }

            if (!EverythingHasStopped(stillMetresPerSecond, stillRadiansPerSecond))
            {
                m_SecondsStill = 0f;
                return false;
            }

            m_SecondsStill += deltaTime;
            if (m_SecondsStill < secondsNeeded)
            {
                return false;
            }

            foreach (var member in m_Members)
            {
                member.Body.Sleep();
            }

            return true;
        }

        /// <summary>
        /// Whether this train has stopped going anywhere.
        ///
        /// Judged on where the train as a whole is heading rather than on each vehicle separately.
        /// Five bodies held together by hinges never all read as still at the same instant: the
        /// solver leaves them trading small pushes, and those cancel out across the train while
        /// being individually larger than any threshold worth using. Asked one vehicle at a time,
        /// a parked train reports itself as moving for ever and never sleeps.
        ///
        /// A single vehicle thrown out of an otherwise still train is caught separately, because
        /// there the answer really is that something is happening.
        /// </summary>
        public bool EverythingHasStopped(float stillMetresPerSecond, float stillRadiansPerSecond)
        {
            const float RunawayMultiple = 6f;

            var travel = Vector3.zero;
            var turn = Vector3.zero;

            foreach (var member in m_Members)
            {
                var body = member.Body;

                if (body.linearVelocity.magnitude > stillMetresPerSecond * RunawayMultiple
                    || body.angularVelocity.magnitude > stillRadiansPerSecond * RunawayMultiple)
                {
                    return false;
                }

                travel += body.linearVelocity;
                turn += body.angularVelocity;
            }

            travel /= m_Members.Count;
            turn /= m_Members.Count;

            return travel.magnitude <= stillMetresPerSecond && turn.magnitude <= stillRadiansPerSecond;
        }

        /// <summary>
        /// Whether some of this train is asleep and the rest of it is not.
        ///
        /// Never a state worth being in. It happens when something bumps a parked train: physics
        /// wakes the vehicle that was hit and leaves the rest asleep, so the couplings between them
        /// have one live end and one immovable one, and the train is pulled apart at the seam.
        /// </summary>
        public bool HalfAsleep()
        {
            var asleep = 0;

            foreach (var member in m_Members)
            {
                if (member.Body.IsSleeping())
                {
                    asleep++;
                }
            }

            return asleep > 0 && asleep < m_Members.Count;
        }

        void WakeEverything()
        {
            foreach (var member in m_Members)
            {
                if (member.Body.IsSleeping())
                {
                    member.Body.WakeUp();
                }
            }
        }

        /// <summary>
        /// How much say the machine that owns this train currently has over where it is.
        ///
        /// Nothing at all for a moment after anything hits it, then easing back. A whole train goes
        /// quiet together because a collision that displaces one cart displaces everything hitched
        /// to it -- correcting any member mid-crash puts a force on one end of a hinge whose other
        /// end is still being thrown about.
        /// </summary>
        public float OwnersSay => m_Crashing.Authority;

        /// <summary>Something hit this train. Leave the crash alone for a moment.</summary>
        public void Struck() => m_Crashing.Touched();

        /// <summary>Moves the blackout on by a step.</summary>
        public void TickBlackout(float deltaTime) => m_Crashing.Tick(deltaTime);

        /// <summary>Whether this vehicle is part of this train.</summary>
        public bool Contains(VehicleController vehicle) => m_Members.Contains(vehicle);

        /// <summary>
        /// Asks to simulate the whole train on this machine, reporting whether that was granted.
        /// Every member moves together or none of them does.
        /// </summary>
        public void RequestOwnership(IOwnershipBroker broker, Action<bool> onResult)
            => broker.RequestAll(m_Members, onResult);

        /// <summary>
        /// Unhooks the train after the given member and returns the two trains that result. Any
        /// coupling at the break is destroyed rather than left in place switched off.
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
            m_Couplings[memberIndex] = null;

            var frontMembers = m_Members.GetRange(0, memberIndex + 1);
            var frontCouplings = m_Couplings.GetRange(0, memberIndex);

            var backStart = memberIndex + 1;
            var backMembers = m_Members.GetRange(backStart, m_Members.Count - backStart);
            var backCouplings = m_Couplings.GetRange(backStart, m_Couplings.Count - backStart);

            return (new CartChain(frontMembers, frontCouplings, m_Settings),
                    new CartChain(backMembers, backCouplings, m_Settings));
        }

        /// <summary>
        /// The coupling between a member and the one behind it, or null if there is none -- either
        /// because that member is at the back, or because this machine is not simulating the train.
        /// </summary>
        public Joint CouplingBehind(int memberIndex)
            => memberIndex >= 0 && memberIndex < m_Couplings.Count ? m_Couplings[memberIndex] : null;

        static HingeJoint Hitch(VehicleController inFront, VehicleController behind, ChainJointSettings settings)
        {
            // The joint lives on the vehicle being towed, which is the one whose movement it
            // constrains, and connects forward to the one doing the towing.
            var coupling = behind.gameObject.AddComponent<HingeJoint>();
            coupling.connectedBody = inFront.Body;

            // Each end is anchored at its own vehicle's hitch. Parked at coupling distance -- which
            // is how the layout places them -- those two points land on top of one another, so the
            // joint begins life already satisfied and has nothing to pull against. A coupling that
            // starts out violated never stops trying to close, and the solver drags the whole train
            // along for as long as the session lasts.
            //
            // Re-hitching a train that has moved since it was parked does not get that for free. A
            // train is only ever hitched here at layout time or when a machine picks up one it owns
            // outright, and in the second case the members are wherever their owner last put them,
            // which is coupling distance apart because that is what the joints were holding them at.
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
