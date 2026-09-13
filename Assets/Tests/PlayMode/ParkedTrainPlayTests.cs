using System.Collections;
using BelowTheWing.Cargo;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// A train nobody is driving.
    ///
    /// It stands still because friction holds it, and for no other reason. Nothing freezes it, and
    /// its suspension goes on carrying it every step whether anything is happening or not.
    ///
    /// That matters because of what a frozen vehicle does when something hits it. A body held up by
    /// a force applied every step cannot be put to sleep and left to look after itself: the moment
    /// it is asleep, whatever was holding it up stops, and the first thing to touch it drives it
    /// into the ground before the springs come back. It reads as something heavy having landed on
    /// the train, it only ever happens to a train that has been standing a while, and it is what
    /// this fixture exists to keep out.
    /// </summary>
    public sealed class ParkedTrainPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;
        VehicleProfile m_CartProfile;
        BagProfile m_BagProfile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron(400f);
            m_TractorProfile = TestProfiles.Tractor();
            m_CartProfile = TestProfiles.Cart();
            m_BagProfile = TestProfiles.CheckedBag();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
            Object.DestroyImmediate(m_CartProfile);
            Object.DestroyImmediate(m_BagProfile);
        }

        CartChain ATrainAt(Vector3 where, string called) => m_Apron.AddTrain(
            m_TractorProfile, m_CartProfile, cartCount: 3, where, called,
            tractorShape: TestShapes.Tractor(), cartShape: TestShapes.Cart());

        /// <summary>Drops a bag into a cart from above, hard, and reports how far the cart is driven under.</summary>
        IEnumerator StrikeAndMeasure(VehicleController cart, System.Action<float> report)
        {
            var shape = cart.GetComponent<VehicleShape>();
            var restingAt = cart.transform.position.y;

            var go = new GameObject("Bag");
            go.transform.position = cart.transform.TransformPoint(new Vector3(0f, shape.InteriorLocal.max.y + 2f, 0f));
            go.AddComponent<Rigidbody>();
            go.AddComponent<BoxCollider>();
            go.AddComponent<Bag>().Configure(m_BagProfile);
            m_Apron.Track(go.GetComponent<Bag>());
            go.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, -25f, 0f);

            var lowest = float.MaxValue;
            for (var step = 0; step < 150; step++)
            {
                yield return new WaitForFixedUpdate();
                lowest = Mathf.Min(lowest, cart.transform.position.y);
            }

            report(restingAt - lowest);
        }

        [UnityTest]
        public IEnumerator ATrainNobodyIsDrivingStaysAwake()
        {
            var train = ATrainAt(Vector3.zero, "Tug 1");

            yield return Steps.Seconds(8f);

            foreach (var member in train.Members)
            {
                Assert.That(member.Body.IsSleeping(), Is.False,
                    $"'{member.name}' has been put to sleep after standing for eight seconds. A " +
                    "sleeping vehicle is one whose suspension has stopped running, and the next " +
                    "thing to touch it goes straight through where its springs should have been");
            }
        }

        [UnityTest]
        public IEnumerator ATrainNobodyIsDrivingDoesNotCreep()
        {
            var train = ATrainAt(Vector3.zero, "Tug 1");
            yield return Steps.Seconds(2f);

            var parkedAt = new Vector3[train.Members.Count];
            var facing = new Quaternion[train.Members.Count];
            for (var i = 0; i < train.Members.Count; i++)
            {
                parkedAt[i] = train.Members[i].transform.position;
                facing[i] = train.Members[i].transform.rotation;
            }

            yield return Steps.Seconds(8f);

            for (var i = 0; i < train.Members.Count; i++)
            {
                var member = train.Members[i];
                var wandered = Vector3.Distance(member.transform.position, parkedAt[i]);

                Assert.That(wandered, Is.LessThan(0.01f),
                    $"'{member.name}' wandered {wandered * 100f:F1} cm in eight seconds with nobody " +
                    "near it. Standing still has to come from the tyres holding, because nothing " +
                    "freezes a parked train any more");
                Assert.That(Quaternion.Angle(member.transform.rotation, facing[i]), Is.LessThan(1f),
                    $"'{member.name}' turned on the spot while parked");
            }
        }

        [UnityTest]
        public IEnumerator AHardKnockLandsTheSameOnATrainThatHasStoodForAges()
        {
            var justStopped = ATrainAt(Vector3.zero, "Tug 1");
            var standingAges = ATrainAt(new Vector3(20f, 0f, 0f), "Tug 2");

            yield return Steps.Seconds(8f);

            var deep = 0f;
            yield return StrikeAndMeasure(standingAges.Members[2], dip => deep = dip);

            // The other one is driven and brought to a halt immediately before its knock, so that
            // the only difference between the two is how long each has been standing. Struck while
            // both had been parked for ages, this compares a train with itself and passes whatever
            // the suspension is doing.
            justStopped.Leader.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(2f);
            justStopped.Leader.IntentSource = new FixedIntent(brake: 1f);
            yield return Steps.Seconds(2.5f);

            Assert.That(justStopped.Leader.Body.linearVelocity.magnitude, Is.LessThan(0.2f),
                "it has to have come to a stop for this to be a comparison of two parked trains");

            justStopped.Leader.IntentSource = null;

            var fresh = 0f;
            yield return StrikeAndMeasure(justStopped.Members[2], dip => fresh = dip);

            Assert.That(deep, Is.EqualTo(fresh).Within(0.002f),
                $"a bag thrown into a cart that had been standing drove it {deep * 100f:F2} cm under, " +
                $"against {fresh * 100f:F2} cm for the same throw into a train that had only just " +
                "stopped moving. However long a train has been parked cannot change what hitting it " +
                "does: a difference here means the suspension was not running on one of them");

            Assert.That(deep, Is.LessThan(0.03f),
                $"the cart was driven {deep * 100f:F2} cm into the ground by a 20 kg bag. Its springs " +
                "carry 550 kg; a bag has no business moving it that far");
        }
    }
}
