using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Cargo
{
    /// <summary>How hard a throw is, from a tap to a full wind-up.</summary>
    [Serializable]
    public struct ThrowSettings
    {
        [Tooltip("How far a player can reach to pick something up, in metres.")]
        public float reachMetres;

        [Tooltip("Seconds of holding before a throw counts as more than putting the thing down.")]
        public float minimumChargeSeconds;

        [Tooltip("Seconds of holding for a throw at full strength.")]
        public float fullChargeSeconds;

        [Tooltip("Metres per second a bag leaves the hands at when barely charged.")]
        public float gentleSpeed;

        [Tooltip("Metres per second a bag leaves the hands at when fully charged.")]
        public float hardestSpeed;

        [Tooltip("How much of the throw goes upward rather than forward, from 0 to 1.")]
        public float lift;

        /// <summary>A throw that can put a bag down gently or put it through a window.</summary>
        public static ThrowSettings Default => new ThrowSettings
        {
            reachMetres = 2f,
            minimumChargeSeconds = 0.15f,
            fullChargeSeconds = 1.2f,
            gentleSpeed = 2f,
            hardestSpeed = 12f,
            lift = 0.35f
        };
    }

    /// <summary>
    /// A pair of hands: picking things up, winding up, and letting go.
    ///
    /// The hands are a carrier like any other, which is what makes a bag carried by a rider a bag
    /// attached to a player attached to a cart. Unwinding that in the right order is the case most
    /// likely to be discovered late, so it is the same mechanism the whole way down rather than a
    /// special case for holding.
    ///
    /// The wind-up exists so that putting a bag down and throwing it across the apron are the same
    /// control. A tap is a drop; holding is a throw; and there has to be a way to set something down
    /// without launching it, or stacking a cart becomes a game of not flinching.
    /// </summary>
    public sealed class Hands
    {
        readonly Carrier m_Holding;
        readonly Func<IReadOnlyList<Carried>> m_Nearby;
        readonly ThrowSettings m_Throw;


        float m_ChargingSince = -1f;

        public Hands(Carrier holding, Func<IReadOnlyList<Carried>> nearby, ThrowSettings settings)
        {
            m_Holding = holding ?? throw new ArgumentNullException(nameof(holding));
            m_Nearby = nearby ?? throw new ArgumentNullException(nameof(nearby));
            m_Throw = settings;
        }

        /// <summary>
        /// The body these hands are part of, if it is something that can itself be carried.
        ///
        /// A person can ride a cart, so a person is carriable, so a person turns up in any list of
        /// things that might be picked up -- and their own body is closer to their own hands than
        /// any bag will ever be. Picking it up makes them kinematic and riding their own hands,
        /// which move with them, so they slide off across the apron unable to walk.
        ///
        /// Looked up when it is needed rather than remembered when the hands are made, because the
        /// hands can be made before the rest of the body is.
        /// </summary>
        Carried Wearer => m_Holding.GetComponentInParent<Carried>();

        /// <summary>What is being held, or null.</summary>
        public Carried Carrying => m_Holding.Riding.Count > 0 ? m_Holding.Riding[0] : null;

        /// <summary>Whether the hands are full.</summary>
        public bool Full => Carrying != null;

        /// <summary>Whether a throw is being wound up right now.</summary>
        public bool WindingUp => m_ChargingSince >= 0f;

        /// <summary>
        /// How far into a wind-up, from 0 to 1. Replicated so that other machines can see somebody
        /// about to throw something at them.
        /// </summary>
        public float Charge(float now)
        {
            if (!WindingUp || m_Throw.fullChargeSeconds <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01((now - m_ChargingSince) / m_Throw.fullChargeSeconds);
        }

        /// <summary>
        /// The nearest thing within reach that could be picked up, or null.
        ///
        /// Only things loose in the world. Something already riding a cart is that cart's, and
        /// reaching into a passing train to pluck a bag off it is a different action with different
        /// rules about who owns what.
        /// </summary>
        public Carried WorthPickingUp(float now)
        {
            Carried nearest = null;
            var nearestDistance = m_Throw.reachMetres;
            var wearer = Wearer;

            foreach (var candidate in m_Nearby())
            {
                if (candidate == null || candidate == wearer || candidate.Attached || !candidate.WouldSettle(now))
                {
                    continue;
                }

                var away = Vector3.Distance(m_Holding.transform.position, candidate.transform.position);
                if (away <= nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = away;
                }
            }

            return nearest;
        }

        /// <summary>Picks up whatever is in reach. Does nothing if the hands are already full.</summary>
        public bool PickUp(float now)
        {
            if (Full)
            {
                return false;
            }

            var wanted = WorthPickingUp(now);
            if (wanted == null)
            {
                return false;
            }

            wanted.AttachTo(m_Holding);

            return true;
        }

        /// <summary>Starts winding up a throw. Nothing happens if the hands are empty.</summary>
        public void StartWindingUp(float now)
        {
            if (Full && !WindingUp)
            {
                m_ChargingSince = now;
            }
        }

        /// <summary>
        /// Lets go. A quick tap sets the bag down; a full wind-up throws it.
        ///
        /// The thrown bag keeps whatever the hands were doing before the throw is added, and the
        /// hands keep whatever the player was riding -- so a bag thrown forward from a moving cart
        /// carries the cart with it and goes further than the same throw standing still. That chain
        /// is the reason throwing belongs with the physics rather than with the controls.
        /// </summary>
        public Carried LetGo(float now, Vector3 facing)
        {
            var held = Carrying;
            if (held == null)
            {
                m_ChargingSince = -1f;
                return null;
            }

            // A tap is not a throw. Below the minimum the bag is simply set down, which is what
            // stacking a cart needs -- without it, putting something anywhere becomes a game of
            // not flinching.
            var wound = WindingUp && now - m_ChargingSince >= m_Throw.minimumChargeSeconds;
            var speed = wound ? Mathf.Lerp(m_Throw.gentleSpeed, m_Throw.hardestSpeed, Charge(now)) : 0f;

            var away = (facing.normalized + (Vector3.up * m_Throw.lift)).normalized * speed;

            m_ChargingSince = -1f;
            held.Wake(now, away);

            return held;
        }
    }
}
