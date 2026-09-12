using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    /// <summary>
    /// Comparing positions to within a tolerance, since floats never land exactly.
    ///
    /// For use with NUnit's <c>Is.EqualTo(...).Using(...)</c>, which otherwise compares a vector
    /// component by component to the last bit.
    /// </summary>
    public static class Nearly
    {
        /// <summary>Two positions within this many metres of each other count as the same.</summary>
        public static IEqualityComparer<Vector3> Within(float metres) => new Comparer(metres);

        sealed class Comparer : IEqualityComparer<Vector3>
        {
            readonly float m_Tolerance;

            public Comparer(float tolerance) => m_Tolerance = tolerance;

            public bool Equals(Vector3 a, Vector3 b) => Vector3.Distance(a, b) <= m_Tolerance;

            public int GetHashCode(Vector3 of) => of.GetHashCode();
        }
    }
}
