using UnityEngine;

namespace BelowTheWing.Cargo
{
    public interface IUnhookWhenHeld
    {
        void TakeHold(Transform hand);

        void LetGo();
    }
}
