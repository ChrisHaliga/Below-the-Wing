using BelowTheWing.Crew;
using BelowTheWing.Menu;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    static class MenuBackdropGizmos
    {
        const float StandRingSegments = 24f;
        const float ShotReachMetres = 6f;
        const float PlateMarkerMetres = 0.12f;

        static readonly Color StandColour = new Color(0.30f, 0.85f, 1f, 0.9f);
        static readonly Color PlateColour = new Color(1f, 0.82f, 0.25f, 0.9f);
        static readonly Color ShotColour = new Color(1f, 0.45f, 0.35f, 0.9f);

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
        static void Draw(MenuBackdrop backdrop, GizmoType how)
        {
            var profile = AssetDatabase.LoadAssetAtPath<CrewProfile>(ContentPaths.CrewProfilePath);

            DrawTheStands(backdrop, profile);

            DrawTheShot(backdrop.CartShot, "Cart shot");
            DrawTheShot(backdrop.InsideShot, "Inside shot");
        }

        static void DrawTheStands(MenuBackdrop backdrop, CrewProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            for (var stand = 0; stand < backdrop.LobbyCrew.Count; stand++)
            {
                var figure = backdrop.LobbyCrew[stand];

                if (figure == null)
                {
                    continue;
                }

                var feet = figure.transform.position - (Vector3.up * (profile.heightMetres * 0.5f));

                Gizmos.color = StandColour;
                Standing(feet, profile.radiusMetres, profile.heightMetres);

                Gizmos.color = PlateColour;
                Gizmos.DrawWireSphere(
                    feet + (Vector3.up * backdrop.PlateHeightMetres), PlateMarkerMetres);

                Handles.color = StandColour;
                Handles.Label(feet + (Vector3.up * (profile.heightMetres + 0.2f)), $"Crew {stand + 1}");
            }
        }

        static void Standing(Vector3 feet, float radiusMetres, float heightMetres)
        {
            Ring(feet, radiusMetres);
            Ring(feet + (Vector3.up * heightMetres), radiusMetres);

            for (var corner = 0; corner < 4; corner++)
            {
                var turn = Quaternion.Euler(0f, corner * 90f, 0f) * Vector3.forward * radiusMetres;
                Gizmos.DrawLine(feet + turn, feet + turn + (Vector3.up * heightMetres));
            }
        }

        static void Ring(Vector3 middle, float radiusMetres)
        {
            var was = middle + (Vector3.forward * radiusMetres);

            for (var step = 1; step <= StandRingSegments; step++)
            {
                var turned = Quaternion.Euler(0f, step / StandRingSegments * 360f, 0f);
                var next = middle + (turned * Vector3.forward * radiusMetres);

                Gizmos.DrawLine(was, next);
                was = next;
            }
        }

        static void DrawTheShot(Transform shot, string called)
        {
            if (shot == null)
            {
                return;
            }

            var looking = shot.rotation;
            var from = shot.position;

            Gizmos.color = ShotColour;
            Gizmos.DrawWireSphere(from, 0.15f);
            Gizmos.DrawLine(from, from + (looking * Vector3.forward * ShotReachMetres));

            var lens = Camera.main != null ? Camera.main.fieldOfView : 60f;
            var half = Mathf.Tan(lens * 0.5f * Mathf.Deg2Rad) * ShotReachMetres;
            var wide = half * (Camera.main != null ? Camera.main.aspect : 16f / 9f);

            var middle = from + (looking * Vector3.forward * ShotReachMetres);

            var corners = new[]
            {
                middle + (looking * new Vector3(-wide, -half, 0f)),
                middle + (looking * new Vector3(wide, -half, 0f)),
                middle + (looking * new Vector3(wide, half, 0f)),
                middle + (looking * new Vector3(-wide, half, 0f))
            };

            for (var corner = 0; corner < corners.Length; corner++)
            {
                Gizmos.DrawLine(from, corners[corner]);
                Gizmos.DrawLine(corners[corner], corners[(corner + 1) % corners.Length]);
            }

            Handles.color = ShotColour;
            Handles.Label(from + (Vector3.up * 0.4f), called);
        }
    }
}
