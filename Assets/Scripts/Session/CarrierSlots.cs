using System.Collections.Generic;
using BelowTheWing.Cargo;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// Naming one particular carrier over the wire.
    ///
    /// A cart deck, a pair of hands and a belt are all <see cref="Carrier"/> components, and a
    /// single networked object can have several of them -- a player has hands, and later will have
    /// a back and a trolley. Saying "riding on object 47" is therefore not enough; the message has
    /// to say which carrier on object 47.
    ///
    /// The name used is a position in a list: every carrier belonging to an object, in the order the
    /// hierarchy gives them. That is stable across machines because every machine builds the object
    /// from the same prefab, and it needs nothing stored in the scene -- an identifier authored by
    /// hand is one more thing that can be left off a new prefab and fail silently.
    ///
    /// One rule makes it work: the search stops at any nested networked object. Objects do end up
    /// inside each other at runtime -- a player who climbs into a tractor becomes a child of it, and
    /// a player has hands, and hands are a carrier -- so a naive search of the outer object would
    /// suddenly find the inner object's carriers and every number after them would shift
    /// mid-session. Anything networked in its own right owns its own carriers.
    /// </summary>
    public static class CarrierSlots
    {
        /// <summary>Nothing is riding on anything. Used where a slot is required but unused.</summary>
        public const int None = -1;

        /// <summary>Every carrier belonging to this object, in the order every machine will see.</summary>
        public static void CarriersOf(NetworkObject networked, List<Carrier> into)
        {
            into.Clear();

            if (networked != null)
            {
                Collect(networked.transform, into, isTheObjectItself: true);
            }
        }

        /// <summary>
        /// Which slot this carrier occupies on the networked object it belongs to, or
        /// <see cref="None"/> if it belongs to no spawned object.
        /// </summary>
        public static int SlotOf(Carrier carrier, List<Carrier> scratch)
        {
            var networked = carrier != null ? carrier.GetComponentInParent<NetworkObject>() : null;
            if (networked == null)
            {
                return None;
            }

            CarriersOf(networked, scratch);

            return scratch.IndexOf(carrier);
        }

        /// <summary>The carrier in that slot on that object, or null if either is unknown here.</summary>
        public static Carrier Resolve(NetworkManager manager, ulong objectId, int slot, List<Carrier> scratch)
        {
            if (manager == null
                || slot < 0
                || !manager.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var networked))
            {
                return null;
            }

            CarriersOf(networked, scratch);

            return slot < scratch.Count ? scratch[slot] : null;
        }

        static void Collect(Transform at, List<Carrier> into, bool isTheObjectItself)
        {
            if (!isTheObjectItself && at.GetComponent<NetworkObject>() != null)
            {
                return;
            }

            at.GetComponents(s_OnThisTransform);
            into.AddRange(s_OnThisTransform);

            for (var i = 0; i < at.childCount; i++)
            {
                Collect(at.GetChild(i), into, isTheObjectItself: false);
            }
        }

        static readonly List<Carrier> s_OnThisTransform = new List<Carrier>();
    }
}
