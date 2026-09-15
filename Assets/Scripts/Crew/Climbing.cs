using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Crew
{
    public sealed class Climbing : IOfferSomething
    {
        public const string WhatToPress = "Press space to climb in";

        public const float OverTheTopMetres = 0.15f;

        public const float TheLeastOfAHopMetres = 0.1f;

        public const float LandWellInsideMetres = 0.1f;

        public const float GiveUpAfterSeconds = 1.5f;

        public const float SettlesForSeconds = 0.35f;

        public ClimbZone Offered { get; private set; }

        public ClimbZone Hauling { get; private set; }

        float m_NearestOffered;
        float m_HauledFor;
        float m_SettlingFor;

        public string Message => Offered != null ? WhatToPress : "";

        public CrewPrompt Prompt => Offered != null ? CrewPrompt.Offer : CrewPrompt.None;

        public void NothingInReach()
        {
            Offered = null;
            m_NearestOffered = float.MaxValue;
        }

        public void Consider(
            ClimbZone zone, Vector3 feet, float canGetOverMetres, float duckedToMetres)
        {
            if (Hauling != null
                || !CanGetOver(zone, feet, canGetOverMetres)
                || !FitsThrough(zone, duckedToMetres))
            {
                return;
            }

            var away = Vector3.Distance(feet, zone.TopOfTheEdge);
            if (away >= m_NearestOffered)
            {
                return;
            }

            Offered = zone;
            m_NearestOffered = away;
        }

        public static bool FitsThrough(ClimbZone zone, float duckedToMetres)
            => zone != null
               && zone.CeilingOfTheLoadSpace.y - zone.TopOfTheEdge.y >= duckedToMetres;

        public static bool CanGetOver(ClimbZone zone, Vector3 feet, float canGetOverMetres)
        {
            if (zone == null || zone.Vehicle == null)
            {
                return false;
            }

            if (feet.y >= zone.FloorOfTheLoadSpace.y - TheLeastOfAHopMetres)
            {
                return false;
            }

            return zone.TopOfTheEdge.y - feet.y <= canGetOverMetres;
        }

        public Vector3 TakeHold(Vector3 feet, float duckedToMetres, float gravity)
        {
            if (Offered == null)
            {
                return Vector3.zero;
            }

            Hauling = Offered;
            m_HauledFor = 0f;
            m_SettlingFor = SettlesForSeconds;
            NothingInReach();

            var top = Hauling.TopOfTheEdge;
            var ceiling = Hauling.CeilingOfTheLoadSpace;

            var overTheEdge = top.y + OverTheTopMetres;
            var headWouldTouch = ceiling.y - duckedToMetres;

            return (Vector3.up * StraightUpFor(Mathf.Min(overTheEdge, headWouldTouch) - feet.y, gravity))
                   + Hauling.Vehicle.Body.GetPointVelocity(top);
        }

        public Vector3 Haul(
            Vector3 feet, Vector3 movingAt, float shoulderWidthMetres, float deltaTime, float gravity)
        {
            if (Hauling == null)
            {
                return Vector3.zero;
            }

            m_HauledFor += deltaTime;

            if (Hauling.Vehicle == null || m_HauledFor > GiveUpAfterSeconds)
            {
                LetGo();
                return Vector3.zero;
            }

            var top = Hauling.TopOfTheEdge;
            if (feet.y < top.y || movingAt.y > 0f)
            {
                return Vector3.zero;
            }

            var inward = Hauling.Inward;
            var acrossTheGround = new Vector3(top.x - feet.x, 0f, top.z - feet.z);
            var stillToGo = Mathf.Max(
                Vector3.Dot(acrossTheGround, inward) + shoulderWidthMetres + LandWellInsideMetres, 0f);

            var backDownToTheEdge = Mathf.Max(feet.y - top.y, TheLeastOfAHopMetres);
            var beforeTheyAreLevelWithItAgain =
                Mathf.Sqrt(2f * backDownToTheEdge / Mathf.Max(gravity, 1e-4f));

            LetGo();

            return inward * (stillToGo / beforeTheyAreLevelWithItAgain);
        }

        public bool HandsFull => Hauling != null || m_SettlingFor > 0f;

        public void LetGo()
        {
            Hauling = null;
            m_HauledFor = 0f;
        }

        public void Settle(float deltaTime)
        {
            if (m_SettlingFor <= 0f)
            {
                return;
            }

            m_SettlingFor = Mathf.Max(0f, m_SettlingFor - deltaTime);
        }

        public static float StraightUpFor(float riseMetres, float gravity)
            => Mathf.Sqrt(2f * Mathf.Max(gravity, 1e-4f) * Mathf.Max(riseMetres, TheLeastOfAHopMetres));
    }
}
