using UnityEngine;

namespace BelowTheWing.Menu
{
    public readonly struct CameraShot
    {
        const float RoomAroundTheEdges = 1.08f;

        public CameraShot(
            Vector3 subject, float radiusMetres, float heightMetres, float degreesPerSecond)
        {
            Subject = subject;
            RadiusMetres = radiusMetres;
            HeightMetres = heightMetres;
            DegreesPerSecond = degreesPerSecond;
        }

        public Vector3 Subject { get; }

        public float RadiusMetres { get; }

        public float HeightMetres { get; }

        public float DegreesPerSecond { get; }

        public float AngleAfter(float fromDegrees, float seconds)
            => Mathf.Repeat(fromDegrees + (DegreesPerSecond * seconds), 360f);

        public Vector3 PlacedAt(float angleDegrees)
        {
            var around = angleDegrees * Mathf.Deg2Rad;

            return Subject + new Vector3(
                Mathf.Sin(around) * RadiusMetres,
                HeightMetres,
                Mathf.Cos(around) * RadiusMetres);
        }

        public Quaternion FacingFrom(float angleDegrees)
            => Quaternion.LookRotation(Subject - PlacedAt(angleDegrees), Vector3.up);

        public static CameraShot Between(CameraShot from, CameraShot to, float howFar)
        {
            var eased = Eased(Mathf.Clamp01(howFar));

            return new CameraShot(
                Vector3.Lerp(from.Subject, to.Subject, eased),
                Mathf.Lerp(from.RadiusMetres, to.RadiusMetres, eased),
                Mathf.Lerp(from.HeightMetres, to.HeightMetres, eased),
                Mathf.Lerp(from.DegreesPerSecond, to.DegreesPerSecond, eased));
        }

        public static CameraShot Framing(
            Bounds holding, float fieldOfViewDegrees, float aspect, float degreesPerSecond)
        {
            var acrossTheGround = new Vector2(holding.extents.x, holding.extents.z).magnitude;
            var vertical = Mathf.Max(fieldOfViewDegrees, 1f) * 0.5f * Mathf.Deg2Rad;
            var horizontal = Mathf.Atan(Mathf.Tan(vertical) * Mathf.Max(aspect, 0.1f));

            var far = Mathf.Max(
                acrossTheGround / Mathf.Tan(horizontal),
                holding.extents.y / Mathf.Tan(vertical));

            return new CameraShot(
                holding.center,
                (far + acrossTheGround) * RoomAroundTheEdges,
                holding.center.y + holding.extents.y,
                degreesPerSecond);
        }

        static float Eased(float howFar)
            => howFar < 0.5f
                ? 2f * howFar * howFar
                : 1f - (Mathf.Pow((-2f * howFar) + 2f, 2f) * 0.5f);
    }
}
