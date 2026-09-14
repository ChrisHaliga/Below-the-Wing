using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BelowTheWing.Vehicles;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BelowTheWing.Diagnostics
{
    [DisallowMultipleComponent]
    public sealed class RampReadout : MonoBehaviour
    {
        [SerializeField, Tooltip("Key that shows and hides the readout")]
        Key m_ToggleKey = Key.F3;

        readonly Stopwatch m_SinceStepBegan = new Stopwatch();
        readonly List<CartChain> m_Trains = new List<CartChain>();
        readonly List<ContactTally> m_Tallies = new List<ContactTally>();

        IOwnershipBroker m_Broker;

        public bool Visible { get; set; } = true;

        public float PhysicsStepMilliseconds { get; private set; }

        public int ContactCount { get; private set; }

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

        public void Observe(IReadOnlyList<CartChain> trains, IOwnershipBroker broker)
        {
            m_Broker = broker;

            m_Trains.Clear();
            m_Trains.AddRange(trains);

            m_Tallies.Clear();

            foreach (var train in trains)
            {
                foreach (var member in train.Members)
                {
                    var tally = member.GetComponent<ContactTally>();
                    if (tally != null)
                    {
                        m_Tallies.Add(tally);
                    }
                }
            }
        }

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

            var drift = train.Leader.OursToMove ? "" : $"  off by {WorstDrift(train):F2} m";

            if (owners.Count == 1)
            {
                return $"{train.Leader.DisplayName} (+{train.Members.Count - 1}): {mine}, owner {owners[0]}{drift}";
            }

            return $"{train.Leader.DisplayName} (+{train.Members.Count - 1}): SPLIT across owners "
                   + string.Join(", ", owners) + drift;
        }

        void OnEnable() => StartCoroutine(TimePhysicsSteps());

        void FixedUpdate()
        {
            ContactCount = CountContacts();
            m_SinceStepBegan.Restart();
        }

        int CountContacts()
        {
            var touching = 0;

            foreach (var tally in m_Tallies)
            {
                if (tally != null)
                {
                    touching += tally.Touching;
                }
            }

            return touching / 2;
        }

        IEnumerator TimePhysicsSteps()
        {
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
                $"physics step   {PhysicsStepMilliseconds:F2} ms",
                $"contacts       {ContactCount}"
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
