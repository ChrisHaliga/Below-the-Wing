using System;
using System.Collections.Generic;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Who is currently simulating a vehicle, and how that changes hands.
    ///
    /// In this game every client simulates the objects it owns and sees replicated copies of the
    /// rest, so "who owns this" decides which machine's physics is the real one. Vehicles are
    /// described in terms of this interface rather than the networking library directly, which is
    /// what keeps the driving code independent of how -- or whether -- the game is networked.
    ///
    /// The all-or-nothing shape of <see cref="RequestAll"/> is deliberate and load bearing. A
    /// tractor and its carts are held together by joints, and a joint whose two ends are being
    /// simulated by different machines has half its constraint solver working against a body that
    /// cannot respond. A chain must therefore change hands as one thing or not at all.
    /// </summary>
    public interface IOwnershipBroker
    {
        /// <summary>Identifier of the machine this code is running on.</summary>
        ulong LocalClientId { get; }

        /// <summary>Which machine is currently simulating this vehicle.</summary>
        ulong OwnerOf(VehicleController vehicle);

        /// <summary>
        /// Whether this machine is the one simulating this vehicle.
        ///
        /// Asked as its own question rather than by comparing <see cref="OwnerOf"/> with
        /// <see cref="LocalClientId"/>, because both can answer "no idea" -- a vehicle that is not
        /// spawned, a machine not in a session -- and two don't-knows compare equal. That reads as
        /// "yes, ours", which is how a machine that owns nothing at all concludes it owns the whole
        /// apron.
        /// </summary>
        bool OwnedByUs(VehicleController vehicle);

        /// <summary>
        /// Asks to simulate every one of these vehicles, and reports back whether that was granted.
        ///
        /// Either all of them transfer or none of them do. A partial grant is reported as a
        /// refusal and leaves ownership exactly as it was.
        /// </summary>
        void RequestAll(IReadOnlyList<VehicleController> vehicles, Action<bool> onResult);

        /// <summary>
        /// Gives these vehicles up, so that whoever wants them next may have them.
        ///
        /// Needed because a request granted is not always a request still wanted: an answer can
        /// arrive after the player who asked has walked away, and keeping a train nobody is driving
        /// would leave it simulated here and unavailable to everybody else.
        /// </summary>
        void HandBack(IReadOnlyList<VehicleController> vehicles);
    }
}
