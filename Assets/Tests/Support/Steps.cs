using System.Collections;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    public static class Steps
    {
        public static IEnumerator Seconds(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
