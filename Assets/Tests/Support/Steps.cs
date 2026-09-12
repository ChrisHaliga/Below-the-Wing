using System.Collections;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    /// <summary>
    /// Letting physics run for a while inside a play mode test.
    ///
    /// Yielding a fixed update at a time is the only honest way to wait: yielding on wall-clock
    /// seconds runs however many physics steps the machine happened to fit in, so a test that waits
    /// "two seconds" for a cart to settle waits a different number of steps on every machine it runs
    /// on, and passes or fails accordingly.
    /// </summary>
    public static class Steps
    {
        /// <summary>Runs the physics for this many seconds, one fixed step at a time.</summary>
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
