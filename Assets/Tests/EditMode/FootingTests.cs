using BelowTheWing.Crew;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Whether somebody still has their feet under them.
    ///
    /// A rider is held on a deck by friction, and friction has a limit. What decides whether they
    /// keep their footing is how hard the deck itself changed velocity -- not how far they are from
    /// the speed they are asking for, because a player who has just pressed a key is a long way from
    /// that too and is in no danger of falling over.
    /// </summary>
    public sealed class FootingTests
    {
        const float Grip = 10f;
        const float Step = 0.02f;

        GameObject m_Deck;
        GameObject m_Apron;

        [SetUp]
        public void SetUp()
        {
            m_Deck = new GameObject("Deck");
            m_Apron = new GameObject("Apron");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Deck);
            Object.DestroyImmediate(m_Apron);
        }

        Footing Standing(Vector3 on)
        {
            var footing = new Footing();
            footing.Settle(m_Deck, on, on, Grip, Step);
            return footing;
        }

        [Test]
        public void ADeckPullingHarderThanTheirGripTakesTheirFeetAway()
        {
            var footing = Standing(Vector3.zero);

            // Twelve metres per second squared sideways, which is what a cart taken hard round a
            // corner at speed does.
            footing.Settle(m_Deck, new Vector3(12f * Step, 0f, 0f), Vector3.zero, Grip, Step);

            Assert.That(footing.Lost, Is.True,
                "a deck that changed velocity faster than their feet can hold has to take them out " +
                "from under them, or riding a cart is something no corner can ever cost a player");
        }

        [Test]
        public void ADeckPullingWithinTheirGripLeavesThemStanding()
        {
            var footing = Standing(Vector3.zero);

            // Eight, which is a tractor pulling away flat out.
            footing.Settle(m_Deck, new Vector3(0f, 0f, 8f * Step), Vector3.zero, Grip, Step);

            Assert.That(footing.Lost, Is.False,
                "a tractor pulling away at everything it has must not shake a rider off the cart " +
                "behind it, or a train can never be boarded while it is being driven");
        }

        [Test]
        public void TheirOwnWalkingIsNeverWhatTakesTheirFeetAway()
        {
            var footing = Standing(Vector3.zero);

            // Standing still on a still deck and asking to walk at four metres a second: an enormous
            // difference between what they are doing and what they want, and no danger at all.
            for (var step = 0; step < 10; step++)
            {
                footing.Settle(m_Deck, Vector3.zero, new Vector3(0f, 0f, 4f), Grip, Step);
            }

            Assert.That(footing.Lost, Is.False,
                "judged on the gap between them and what they are asking for, every player loses " +
                "their footing the instant they press a key");
        }

        [Test]
        public void TheyGetTheirFeetBackOnceTheyAreMovingWithItAgain()
        {
            var footing = Standing(Vector3.zero);
            var deck = new Vector3(12f * Step, 0f, 0f);

            footing.Settle(m_Deck, deck, Vector3.zero, Grip, Step);
            Assert.That(footing.Lost, Is.True, "thrown first");

            // The deck holds its new speed and friction drags them up to it.
            footing.Settle(m_Deck, deck, deck, Grip, Step);

            Assert.That(footing.Lost, Is.False,
                "somebody who has caught up with what they are standing on is standing on it. " +
                "Never getting their feet back is a player who walks once and then never again");
        }

        [Test]
        public void SlidingSlowlyIsStillSliding()
        {
            var footing = Standing(Vector3.zero);
            var deck = new Vector3(12f * Step, 0f, 0f);

            footing.Settle(m_Deck, deck, Vector3.zero, Grip, Step);
            footing.Settle(m_Deck, deck, deck - new Vector3(2f, 0f, 0f), Grip, Step);

            Assert.That(footing.Lost, Is.True,
                "two metres a second of skid across a deck is not standing on it, and a player who " +
                "can walk out of a slide never slides anywhere");
        }

        [Test]
        public void SteppingOntoSomethingElseIsNotAYank()
        {
            // Riding a deck that is doing six metres a second, then stepping off onto the apron.
            var moving = new Vector3(0f, 0f, 6f);
            var footing = Standing(moving);
            footing.Settle(m_Deck, moving, moving, Grip, Step);

            footing.Settle(m_Apron, Vector3.zero, moving, Grip, Step);

            Assert.That(footing.Lost, Is.False,
                "the apron did not pull them anywhere -- they walked onto it. Read as one surface " +
                "changing speed, stepping off any moving deck floors you");
        }
    }
}
