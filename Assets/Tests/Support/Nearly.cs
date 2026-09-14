using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    public static class Nearly
    {
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
