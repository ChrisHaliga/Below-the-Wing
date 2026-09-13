using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Cargo
{
    /// <summary>How far a hand reaches, how hard it grips, and how hard it throws.</summary>
    [Serializable]
    public struct HandSettings
    {
        [Tooltip("How far from the hand something can be and still be taken, in metres. Also how " +
                 "far a player can move from the point they took hold of while still holding on.")]
        public float reachMetres;

        [Tooltip("How hard a hand pulls a carried thing toward itself, in newtons per metre.")]
        public float carrySpringNewtonsPerMetre;

        [Tooltip("How much a carried thing's swinging is damped, in newtons per metre per second.")]
        public float carryDamperNewtonsPerMetrePerSecond;

        [Tooltip("The most a hand pulls a carried thing with, in newtons. Something knocked out " +
                 "of reach against that pull is out of the hand. Has to be well over a bag's weight " +
                 "to lift one, and the holder is pulled with the same force, so a hand that could " +
                 "pull harder than a person can lean would drag them after a hit bag.")]
        public float carryGripNewtons;

        [Tooltip("How much an arm gives once a player is at the end of its reach, in newtons per " +
                 "metre. Softer arms stretch further but spread a jolt over more steps, which is " +
                 "what lets a grip survive a corner and break in a crash.")]
        public float armSpringNewtonsPerMetre;

        [Tooltip("How much a stretched arm's rebound is damped, in newtons per metre per second.")]
        public float armDamperNewtonsPerMetrePerSecond;

        [Tooltip("The force at which a grip on something is broken, in newtons.")]
        public float gripBreakForceNewtons;

        [Tooltip("Seconds of holding before a release counts as a throw rather than putting down.")]
        public float minimumChargeSeconds;

        [Tooltip("Seconds of holding for a throw at full strength.")]
        public float fullChargeSeconds;

        [Tooltip("Metres per second a thing leaves the hand at when barely charged.")]
        public float gentleSpeed;

        [Tooltip("Metres per second a thing leaves the hand at when fully charged.")]
        public float hardestSpeed;


        /// <summary>A hand that can put a bag down gently, throw it across the apron, or hang on.</summary>
        public static HandSettings Default => new HandSettings
        {
            reachMetres = 1.2f,
            carrySpringNewtonsPerMetre = 3000f,
            carryDamperNewtonsPerMetrePerSecond = 300f,
            carryGripNewtons = 800f,
            armSpringNewtonsPerMetre = 10000f,
            armDamperNewtonsPerMetrePerSecond = 300f,
            gripBreakForceNewtons = 6000f,
            minimumChargeSeconds = 0.15f,
            fullChargeSeconds = 1.2f,
            gentleSpeed = 2f,
            hardestSpeed = 12f
        };
    }

    /// <summary>
    /// One hand: what it is carrying or holding onto, and what one button does to it.
    ///
    /// A player has two of these and works them with two buttons, and neither hand knows what the
    /// other is doing. Everything a hand does is a joint: a carried thing is a body pulled to the
    /// hand by a spring, a thing held onto is a tether the player can move about on the end of.
    /// Both have limits, so a bag can be knocked out of a hand by a passing cart and a grip can be
    /// torn off by a collision. Nothing a hand does is replicated as a fact; the bodies involved
    /// report their own motion and every other machine sees them where they are.
    ///
    /// What a hand may do with a thing is the thing's to say, through <see cref="HandUse"/>. A
    /// hand that decided for itself would be a list of exceptions, and the first thing added that
    /// was on no list would fall into whichever branch the list left open.
    /// </summary>
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

        /// <param name="anchor">Where this hand is, on the character it belongs to.</param>
        /// <param name="body">The character's body, which a carried thing is pulled toward and a
        /// held thing tethers.</param>
        public Hand(Transform anchor, Rigidbody body, HandSettings settings)
        {
            m_Anchor = anchor != null ? anchor : throw new ArgumentNullException(nameof(anchor));
            m_Body = body != null ? body : throw new ArgumentNullException(nameof(body));
            m_Settings = settings;
        }

        /// <summary>The body this hand is carrying, or null.</summary>
        public Rigidbody Carrying => m_Carry != null ? m_Carried : null;

        /// <summary>
        /// The body this hand is holding onto, or null. Null while holding onto something that has
        /// no body of its own -- a rail bolted to the world -- so <see cref="Empty"/> is the thing
        /// to ask about whether the hand is free.
        /// </summary>
        public Rigidbody HoldingOnto => m_Tether != null ? m_Tether.connectedBody : null;

        /// <summary>Whether this hand has nothing in it.</summary>
        public bool Empty => m_Carry == null && m_Tether == null;

        /// <summary>Whether a throw is being wound up.</summary>
        public bool WindingUp => m_Carry != null && m_WoundUpAt >= 0f;

        /// <summary>How far into a wind-up, from 0 to 1.</summary>
        public float Charge(float now)
            => WindingUp ? Mathf.Clamp01((now - m_WoundUpAt) / Mathf.Max(m_Settings.fullChargeSeconds, 1e-3f)) : 0f;

        /// <summary>
        /// The button went down. An empty hand takes the nearest thing in reach, by whatever that
        /// thing says may be done with it; a carrying hand starts winding up a throw.
        /// </summary>
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

        /// <summary>
        /// The button came up. A tether is let go of; a wind-up becomes a throw, or a tap sets the
        /// thing down; the release of the press that picked something up does nothing at all.
        /// </summary>
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
                // The release of the click that picked it up. A click that picked something up and
                // put it straight back down would be a click that did nothing.
                m_TookHoldOnThisPress = false;
                return;
            }

            // A release with no wind-up behind it -- the press was never seen -- is a put-down.
            var heldFor = WindingUp ? now - m_WoundUpAt : 0f;
            var charge = Charge(now);
            var bag = m_Carried;

            PutDown();

            if (heldFor >= m_Settings.minimumChargeSeconds)
            {
                Throw(bag, charge, facing);
            }
        }

        /// <summary>Notices anything that broke since the last step. Called every fixed step.</summary>
        public void Tick()
        {
            // A carried thing knocked out of reach against everything the hand could pull with
            // is out of the hand. The pull is capped, so a hard enough hit always wins. Reach is
            // measured to the thing's surface, the same way it was measured when it was taken.
            if (m_Carrying && (m_Carry == null || m_CarriedPart == null || !InReach(m_CarriedPart)))
            {
                PutDown();
            }

            // A grip that breaks is destroyed by the physics engine, and the only sign is that it
            // has gone. A thing held onto that is destroyed -- a cart whose owner left the session
            // -- leaves the tether anchored to the empty air where it was, so that is let go of too.
            if (m_Tethered && (m_Tether == null || (m_TetherHadABody && m_Tether.connectedBody == null)))
            {
                LetGoOfTheTether();
            }
        }

        /// <summary>Lets go of whatever this hand holds, without throwing it.</summary>
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

        /// <summary>
        /// The thing in reach that a hand can use and is closest to it, or null.
        ///
        /// Closest by surface rather than by centre, because what a hand reaches for is the side of
        /// a cart, not the middle of it. The character's own body is never a candidate.
        /// </summary>
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

        /// <summary>Only a body the solver moves can be pulled to a hand.</summary>
        static bool CanBePickedUp(Rigidbody body) => body != null && !body.isKinematic;

        void PickUp(Collider part)
        {
            var bag = part.attachedRigidbody;
            m_Carried = bag;
            m_CarriedPart = part;
            m_Carrying = true;

            // Taken up the way the hand sits, whatever angle it was lying at. The joint below holds
            // the relative rotation it is created with, and reads that off the transforms, so both
            // the body and its transform are turned before it exists -- and a bag that came up
            // spinning would otherwise spin in the hand for ever.
            bag.transform.rotation = m_Body.transform.rotation;
            bag.rotation = m_Body.transform.rotation;
            bag.angularVelocity = Vector3.zero;

            // The joint lives on the bag and reaches for the character, so that the bag is the
            // thing being pulled and the character keeps their footing.
            //
            // A spring and damper on every axis, not a spring along the line to the hand. A
            // distance spring damps only stretch, and a bag hanging from one swings like a pendulum
            // for ever -- a hand holds a bag still, in every direction.
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

            // And held the way it was taken up: a carried thing turns with the player and never on
            // its own. The pull to the hand stays a spring; the grip on its orientation is rigid.
            m_Carry.angularXMotion = ConfigurableJointMotion.Locked;
            m_Carry.angularYMotion = ConfigurableJointMotion.Locked;
            m_Carry.angularZMotion = ConfigurableJointMotion.Locked;

            // A held thing that touches its holder is, to the solver, two bodies overlapping, and
            // the answer to that is to shove them apart every step for as long as it lasts -- which
            // sends the holder skidding across the apron for as long as they hold it.
            m_Carry.enableCollision = false;

            m_TookHoldOnThisPress = true;
            m_WoundUpAt = -1f;
        }

        void PutDown()
        {
            if (m_Carry != null)
            {
                // Now, not at the end of the frame: a thrown bag still tied to the hand for one
                // more step is hauled straight back.
                UnityEngine.Object.DestroyImmediate(m_Carry);
            }

            m_Carry = null;
            m_Carried = null;
            m_CarriedPart = null;
            m_Carrying = false;
            m_TookHoldOnThisPress = false;
            m_WoundUpAt = -1f;
        }

        /// <summary>
        /// Sends a thing off where the player is looking, pitch included, on top of whatever speed
        /// the player already has. Looking up is how a lob is aimed; a throw from a moving cart
        /// lands further, and that is what makes it a throw from a moving cart.
        /// </summary>
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

        /// <summary>
        /// Tethers the character to the point of the thing they took hold of.
        ///
        /// The joint lives on the character and reaches for the thing, because the thing may be
        /// somebody else's -- a cart another machine is simulating -- and what happens on this
        /// machine has to be something only this machine's bodies carry. The character can move
        /// anywhere within arm's reach of the point they grabbed; past that the arm stretches a
        /// little and then holds, and if it is asked for more than a grip can give, it lets go.
        /// </summary>
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
