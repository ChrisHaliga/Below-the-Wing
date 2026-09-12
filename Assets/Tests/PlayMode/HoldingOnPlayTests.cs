using System.Collections;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// Holding onto something that moves.
    ///
    /// A tether: a joint with a limit of arm's reach and a break force of grip strength. The player
    /// walks freely on the end of it, is dragged along by whatever they are holding, and is thrown
    /// if something hits it hard enough to break their grip. It exists while a button is down and
    /// on the holder's machine only.
    /// </summary>
    public sealed class HoldingOnPlayTests
    {
        TestApron m_Apron;
        CrewProfile m_Profile;
        CrewCharacter m_Crew;
        Hands m_Hands;
        Transform m_LeftAnchor;
        Transform m_RightAnchor;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_Profile = TestProfiles.CrewMember();
            m_Crew = m_Apron.AddCrew(m_Profile, new Vector3(0f, 0.9f, 0f));

            m_LeftAnchor = Anchor("Left Hand", new Vector3(-0.35f, 0.3f, 0.6f));
            m_RightAnchor = Anchor("Right Hand", new Vector3(0.35f, 0.3f, 0.6f));
            m_Hands = new Hands(m_LeftAnchor, m_RightAnchor, m_Crew.Body, HandSettings.Default);
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_Profile);
        }

        Transform Anchor(string name, Vector3 local)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(m_Crew.transform, worldPositionStays: false);
            anchor.localPosition = local;
            return anchor;
        }

        /// <summary>A cart's worth of body that says it can be held onto, standing on the apron.</summary>
        Rigidbody ACartAt(Vector3 where)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Cart";
            go.transform.localScale = new Vector3(1.8f, 1.2f, 3.5f);
            go.transform.position = where;
            var body = go.AddComponent<Rigidbody>();
            body.mass = 550f;
            go.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;
            m_Apron.Track(body);
            return body;
        }

        [UnityTest]
        public IEnumerator PressingTethersToTheCart()
        {
            var cart = ACartAt(new Vector3(0f, 0.6f, 2.5f));
            yield return Steps.Seconds(0.5f);

            m_Hands.Right.Press(Time.time);

            Assert.That(m_Hands.Right.HoldingOnto, Is.SameAs(cart));
        }

        [UnityTest]
        public IEnumerator ATetheredPlayerGoesWhereTheCartGoes()
        {
            var cart = ACartAt(new Vector3(0f, 0.6f, 2.5f));
            yield return Steps.Seconds(0.5f);

            m_Hands.Right.Press(Time.time);
            var startedAt = m_Crew.transform.position;

            cart.linearVelocity = new Vector3(0f, 0f, 3f);
            for (var i = 0; i < 100; i++)
            {
                cart.linearVelocity = new Vector3(0f, 0f, 3f);
                m_Hands.Tick();
                yield return new WaitForFixedUpdate();
            }

            Assert.That(m_Hands.Right.HoldingOnto, Is.SameAs(cart), "still holding");
            Assert.That(m_Crew.transform.position.z - startedAt.z, Is.GreaterThan(4f),
                "the cart went six metres and the player on the end of the tether has to have " +
                "come with it");
        }

        [UnityTest]
        public IEnumerator YouCanWalkWithinArmsReachAndNoFurther()
        {
            var cart = ACartAt(new Vector3(0f, 0.6f, 2.5f));
            yield return Steps.Seconds(0.5f);

            m_Hands.Right.Press(Time.time);
            var grabbedAt = m_RightAnchor.position;

            // Walking straight away from it, hard, for a long time.
            m_Crew.IntentSource = new HeldKeys(new Vector2(0f, -1f), sprint: true);
            yield return Steps.Seconds(2f);

            Assert.That(m_Hands.Right.HoldingOnto, Is.SameAs(cart), "walking is not letting go");
            Assert.That(Vector3.Distance(m_RightAnchor.position, grabbedAt), Is.GreaterThan(0.5f),
                "and within reach they walk freely: a hold that pins the player to the spot is a seat");
            Assert.That(Vector3.Distance(m_RightAnchor.position, grabbedAt),
                Is.LessThan(HandSettings.Default.reachMetres + 0.4f),
                "the tether stops you at the end of your arm, give or take the stretch in it; a " +
                "hold that let you walk off with your hand still on the rail is not a hold");
        }

        [UnityTest]
        public IEnumerator ReleasingLetsGo()
        {
            var cart = ACartAt(new Vector3(0f, 0.6f, 2.5f));
            yield return Steps.Seconds(0.5f);

            m_Hands.Right.Press(Time.time);
            yield return Steps.Seconds(0.2f);
            Assert.That(m_Hands.Right.HoldingOnto, Is.SameAs(cart), "holding, before letting go");
            m_Hands.Right.Release(Time.time, Vector3.forward);

            var startedAt = m_Crew.transform.position;
            for (var i = 0; i < 50; i++)
            {
                cart.linearVelocity = new Vector3(0f, 0f, 3f);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(m_Hands.Right.Empty, Is.True);
            Assert.That(Vector3.Distance(m_Crew.transform.position, startedAt), Is.LessThan(0.5f),
                "let go of, the cart drives off without you");
        }

        [UnityTest]
        public IEnumerator ACornerThatWouldThrowABagDoesNotBreakAGrip()
        {
            var cart = ACartAt(new Vector3(0f, 0.6f, 2.5f));
            yield return Steps.Seconds(0.5f);

            m_Hands.Right.Press(Time.time);

            // Fifteen metres a second squared sideways: well past what keeps a bag on a deck.
            for (var i = 0; i < 25; i++)
            {
                cart.linearVelocity += new Vector3(0.3f, 0f, 0f);
                m_Hands.Tick();
                yield return new WaitForFixedUpdate();
            }

            Assert.That(m_Hands.Right.HoldingOnto, Is.SameAs(cart),
                "holding on is for exactly this. A grip a corner can break is no better than not " +
                "holding on at all");
        }

        [UnityTest]
        public IEnumerator AHardEnoughHitBreaksTheGripAndThrowsThePlayer()
        {
            var cart = ACartAt(new Vector3(0f, 0.6f, 2.5f));
            yield return Steps.Seconds(0.5f);

            m_Hands.Right.Press(Time.time);
            yield return Steps.Seconds(0.2f);
            Assert.That(m_Hands.Right.HoldingOnto, Is.SameAs(cart), "holding, before the hit");

            // A head-on hit from a tractor: the cart's speed changes by twenty metres a second in
            // one step, and the tether has to haul eighty kilograms after it just as fast.
            cart.linearVelocity = new Vector3(0f, 0f, 20f);
            yield return Steps.Seconds(0.3f);
            m_Hands.Tick();

            Assert.That(m_Hands.Right.Empty, Is.True,
                "surviving a head-on collision by holding a rail would read as the game ignoring " +
                "the crash");
        }

        [UnityTest]
        public IEnumerator OneBrokenGripLeavesTheOtherHolding()
        {
            var left = ACartAt(new Vector3(-2.3f, 0.6f, 0.6f));
            var right = ACartAt(new Vector3(2.3f, 0.6f, 0.6f));
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time);
            m_Hands.Right.Press(Time.time);
            Assert.That(m_Hands.Left.HoldingOnto, Is.SameAs(left));
            Assert.That(m_Hands.Right.HoldingOnto, Is.SameAs(right));

            right.linearVelocity = new Vector3(20f, 0f, 0f);
            yield return Steps.Seconds(0.3f);
            m_Hands.Tick();

            Assert.That(m_Hands.Right.Empty, Is.True, "the one that was hit");
            Assert.That(m_Hands.Left.HoldingOnto, Is.SameAs(left),
                "the other hand had nothing to do with it");
        }
    }
}
