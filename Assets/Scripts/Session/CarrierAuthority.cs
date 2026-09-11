using BelowTheWing.Cargo;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// Tells every carrier belonging to this object whether this machine is the one that decides
    /// what falls off it.
    ///
    /// Whether a corner has been taken hard enough to empty a cart has to be judged where the cart
    /// really is. The machine that owns a cart is driving it; every other machine is steering a copy
    /// toward reports that arrive twenty times a second, and the nudges that does register as
    /// sideways acceleration the cart never actually felt. Judged there, bags would leap off decks
    /// on every screen but the one where the cart is genuinely being driven.
    ///
    /// So the owner judges, and what it decides reaches everybody else as news through the cargo's
    /// own replication rather than being worked out again.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarrierAuthority : NetworkBehaviour
    {
        CarrierWatch[] m_Watches;

        void Awake() => m_Watches = GetComponentsInChildren<CarrierWatch>(includeInactive: true);

        void FixedUpdate()
        {
            // Read afresh every step rather than caught when it changes, for the same reason
            // vehicles and characters do: an object spawned elsewhere runs its spawn callback before
            // ownership has been applied, and no later change event arrives to correct it.
            var ours = IsOwner;

            foreach (var watch in m_Watches)
            {
                if (watch != null)
                {
                    watch.OursToDecide = ours;
                }
            }
        }
    }
}
