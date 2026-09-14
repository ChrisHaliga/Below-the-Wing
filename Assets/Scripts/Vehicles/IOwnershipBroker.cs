using System;
using System.Collections.Generic;

namespace BelowTheWing.Vehicles
{
    public interface IOwnershipBroker
    {
        ulong LocalClientId { get; }

        ulong OwnerOf(VehicleController vehicle);

        bool OwnedByUs(VehicleController vehicle);

        void RequestAll(IReadOnlyList<VehicleController> vehicles, Action<bool> onResult);

        void HandBack(IReadOnlyList<VehicleController> vehicles);
    }
}
