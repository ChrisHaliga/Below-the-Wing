using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BelowTheWing.Vehicles;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BelowTheWing.Diagnostics
{
    /// <summary>
    /// Counting what physics is actually being asked to do.
    ///
    /// The cost of this game is the number of bodies awake at once, and a body that has gone to
    /// sleep costs nothing until something wakes it. Neither number is visible from looking at the
    /// screen, so they get counted.
    /// </summary>
    public static class BodyCensus
    {
        /// <summary>How many of these bodies physics is still integrating.</summary>
        public static int AwakeCount(IReadOnlyList<Rigidbody> bodies)
        {
            var awake = 0;

            foreach (var body in bodies)
            {
                if (body != null && !body.IsSleeping())
                {
                    awake++;
                }
            }

            return awake;
        }
    }

    /// <summary>
    /// An on-screen readout of what the simulation is doing, for a developer rather than a player.
    ///
    /// Two things it shows cannot be seen any other way. How many bodies are awake says whether a
    /// train has settled or is quietly jittering for ever, which looks identical from outside. And
    /// who owns each vehicle says whether taking over a train actually moved all of it, which has
    /// no visible effect at all until a second player is there to be broken by it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RampReadout : MonoBehaviour
    {
        [SerializeField, Tooltip("Key that shows and hides the readout.")]
        Key m_ToggleKey = Key.F3;

        readonly Stopwatch m_SinceStepBegan = new Stopwatch();
        readonly List<CartChain> m_Trains = new List<CartChain>();
        readonly List<Rigidbody> m_Bodies = new List<Rigidbody>();

        IOwnershipBroker m_Broker;

        /// <summary>Whether the readout is currently drawn.</summary>
        public bool Visible { get; set; } = true;

        /// <summary>How many rigidbodies physics is currently integrating.</summary>
        public int AwakeBodyCount => BodyCensus.AwakeCount(m_Bodies);

        /// <summary>How long the last physics step took, in milliseconds.</summary>
        public float PhysicsStepMilliseconds { get; private set; }

        /// <summary>
        /// One line per train saying which machine is simulating it, and whether every member of
        /// that train agrees.
        ///
        /// Built fresh on every read rather than cached, because the interesting moment is the one
        /// where ownership changes, and a snapshot taken when the readout was wired up would show
        /// the state it was in before the thing worth seeing happened.
        /// </summary>
        public IReadOnlyList<string> OwnershipLines
        {
            get
            {
                var lines = new List<string>(m_Trains.Count);

                foreach (var train in m_Trains)
                {
                    lines.Add(DescribeOwnership(train));
                }

                return lines;
            }
        }

        /// <summary>Tells the readout what to watch.</summary>
        public void Observe(IReadOnlyList<CartChain> trains, IOwnershipBroker broker)
        {
            m_Broker = broker;

            m_Trains.Clear();
            m_Trains.AddRange(trains);

            m_Bodies.Clear();
            foreach (var train in trains)
            {
                foreach (var member in train.Members)
                {
                    m_Bodies.Add(member.Body);
                }
            }
        }

        /// <summary>
        /// How far the worst-placed member of a train is from where its owner says it should be, in
        /// metres. Zero for a train this machine owns.
        ///
        /// The worst rather than the average, because a train comes apart one vehicle at a time and
        /// an average over five hides the one that has gone.
        /// </summary>
        public static float WorstDrift(CartChain train)
        {
            var worst = 0f;

            foreach (var member in train.Members)
            {
                var keptInStep = member.GetComponent<IKeepsInStep>();
                if (keptInStep != null)
                {
                    worst = Mathf.Max(worst, keptInStep.MetresOutOfPlace);
                }
            }

            return worst;
        }

        string DescribeOwnership(CartChain train)
        {
            var owners = train.Members.Select(member => m_Broker.OwnerOf(member)).Distinct().ToList();
            var mine = train.Leader.OursToMove ? "ours" : "theirs";

            // Whether this machine is in charge, and how far its copy has drifted, are the two facts
            // that say whether a disagreement between two screens is a tuning problem or an
            // architectural one. Neither can be seen by looking at the apron: both screens look
            // perfectly reasonable on their own.
            var drift = train.Leader.OursToMove ? "" : $"  off by {WorstDrift(train):F2} m";

            if (owners.Count == 1)
            {
                return $"{train.Leader.DisplayName} (+{train.Members.Count - 1}): {mine}, owner {owners[0]}{drift}";
            }

            // The failure this readout exists to catch. A train whose members are being simulated by
            // different machines has couplings with one end on each, and the solver on both sides is
            // working against a body it cannot move.
            return $"{train.Leader.DisplayName} (+{train.Members.Count - 1}): SPLIT across owners "
                   + string.Join(", ", owners) + drift;
        }

        void OnEnable() => StartCoroutine(TimePhysicsSteps());

        void FixedUpdate() => m_SinceStepBegan.Restart();

        IEnumerator TimePhysicsSteps()
        {
            // A coroutine yielding on WaitForFixedUpdate resumes after the physics step has run,
            // which makes the gap since FixedUpdate began the time that step actually took.
            var afterPhysics = new WaitForFixedUpdate();

            while (true)
            {
                yield return afterPhysics;
                PhysicsStepMilliseconds = (float)m_SinceStepBegan.Elapsed.TotalMilliseconds;
            }
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[m_ToggleKey].wasPressedThisFrame)
            {
                Visible = !Visible;
            }
        }

        void OnGUI()
        {
            if (!Visible)
            {
                return;
            }

            var lines = new List<string>
            {
                $"awake bodies   {AwakeBodyCount} / {m_Bodies.Count}",
                $"physics step   {PhysicsStepMilliseconds:F2} ms"
            };
            lines.AddRange(OwnershipLines);

            GUI.Box(new Rect(10f, 10f, 420f, 24f + (18f * lines.Count)), "");
            GUILayout.BeginArea(new Rect(20f, 18f, 400f, 18f * (lines.Count + 1)));
            foreach (var line in lines)
            {
                GUILayout.Label(line);
            }

            GUILayout.EndArea();
        }
    }
}
