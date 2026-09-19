using System;
using System.Collections.Generic;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    internal readonly struct MeasuredVehicle
    {
        public readonly VehicleShape.Measurements Shape;
        public readonly IReadOnlyList<Transform> Wheels;

        public MeasuredVehicle(VehicleShape.Measurements shape, IReadOnlyList<Transform> wheels)
        {
            Shape = shape;
            Wheels = wheels;
        }
    }

    internal static class ModelMeasure
    {
        internal static MeasuredVehicle MeasureTheTractor(GameObject tractor, Transform model)
        {
            var measured = Wheels(tractor, model, new[]
            {
                "Steer_Left/Wheel_Front_Left",
                "Steer_Right/Wheel_Front_Right",
                "Wheel_Back_Left",
                "Wheel_Back_Right"
            });

            var bodywork = MeshBoxLocal(tractor, model, "Body");

            return new MeasuredVehicle(
                new VehicleShape.Measurements
                {
                    Wheels = measured.Placements,
                    FrontCouplingLocal = null,
                    RearCouplingLocal = MarkerLocal(tractor, model, "HITCH_Female"),
                    SeatLocal = MarkerLocal(tractor, model, "SEAT"),
                    EnvelopeSizeMetres = bodywork.size,
                    EnvelopeCentreLocal = bodywork.center,
                    InteriorLocal = new Bounds(Vector3.zero, Vector3.zero),
                    SolidParts = SolidPieces(tractor, model, new[]
                    {
                        "Body",
                        "Frame",
                        "Frame_Supports",
                        "Tire_Cover",
                        "Cushion_Seat",
                        "Cushion_Backrest",
                        "Dashboard"
                    })
                },
                measured.Visible);
        }

        internal static MeasuredVehicle MeasureTheBeltLoader(GameObject loader, Transform model)
        {
            var measured = Wheels(loader, model, new[]
            {
                "Wheel_Front_Left",
                "Wheel_Front_Right",
                "Wheel_Back_Left",
                "Wheel_Back_Right"
            });

            var chassis = MeshBoxLocal(loader, model, "Body");
            var belt = MeshBoxLocal(loader, model, "Belt");
            var envelope = EverythingItIsMadeOf(loader, model);

            return new MeasuredVehicle(
                new VehicleShape.Measurements
                {
                    Wheels = measured.Placements,
                    FrontCouplingLocal = null,
                    RearCouplingLocal = null,
                    SeatLocal = WhereADriverPerches(chassis, belt),
                    EnvelopeSizeMetres = envelope.size,
                    EnvelopeCentreLocal = envelope.center,
                    InteriorLocal = new Bounds(Vector3.zero, Vector3.zero),
                    SolidParts = SolidPieces(loader, model, new[] { "Body", "Belt" })
                },
                measured.Visible);
        }

        const float DriverStandsThisFarAlongTheChassis = 0.15f;

        static Vector3 WhereADriverPerches(Bounds chassis, Bounds belt)
            => new Vector3(
                (chassis.min.x + belt.min.x) * 0.5f,
                chassis.max.y,
                Mathf.Lerp(chassis.min.z, chassis.max.z, DriverStandsThisFarAlongTheChassis));

        static List<VehicleShape.SolidPart> SolidPieces(
            GameObject vehicle, Transform model, IReadOnlyList<string> paths)
        {
            var parts = new List<VehicleShape.SolidPart>(paths.Count);

            foreach (var path in paths)
            {
                parts.Add(SolidAsModelled(vehicle, model, path));
            }

            return parts;
        }

        internal static MeasuredVehicle MeasureTheCart(GameObject cart, Transform model)
        {
            const float slabThicknessMetres = 0.15f;
            const float lipHeightMetres = 0.18f;
            const float lipThicknessMetres = 0.05f;

            var measured = Wheels(cart, model, new[] { "Wheel_1", "Wheel_2", "Wheel_3", "Wheel_4" });

            var loadSpace = TheSpaceTheDoorsCloseOver(cart, model);
            var envelope = EverythingItIsMadeOf(cart, model);

            var deckTopMetres = loadSpace.min.y;
            var roofUnderside = loadSpace.max.y;
            var clearInsideMetres = loadSpace.size.y;
            var deckWidthMetres = loadSpace.size.x;
            var deckLengthMetres = loadSpace.size.z;

            var solid = new List<VehicleShape.SolidPart>
            {
                new VehicleShape.SolidPart(
                    "Deck",
                    new Vector3(deckWidthMetres, slabThicknessMetres, deckLengthMetres),
                    new Vector3(0f, deckTopMetres - (slabThicknessMetres * 0.5f), 0f)),

                new VehicleShape.SolidPart(
                    "Lip left",
                    new Vector3(lipThicknessMetres, lipHeightMetres, deckLengthMetres),
                    new Vector3(
                        -((deckWidthMetres * 0.5f) - (lipThicknessMetres * 0.5f)),
                        deckTopMetres + (lipHeightMetres * 0.5f),
                        0f)),

                new VehicleShape.SolidPart(
                    "Lip right",
                    new Vector3(lipThicknessMetres, lipHeightMetres, deckLengthMetres),
                    new Vector3(
                        (deckWidthMetres * 0.5f) - (lipThicknessMetres * 0.5f),
                        deckTopMetres + (lipHeightMetres * 0.5f),
                        0f)),

                new VehicleShape.SolidPart(
                    "End front",
                    new Vector3(deckWidthMetres, clearInsideMetres, slabThicknessMetres),
                    new Vector3(
                        0f,
                        (deckTopMetres + roofUnderside) * 0.5f,
                        (deckLengthMetres + slabThicknessMetres) * 0.5f)),

                new VehicleShape.SolidPart(
                    "End rear",
                    new Vector3(deckWidthMetres, clearInsideMetres, slabThicknessMetres),
                    new Vector3(
                        0f,
                        (deckTopMetres + roofUnderside) * 0.5f,
                        -(deckLengthMetres + slabThicknessMetres) * 0.5f)),

                new VehicleShape.SolidPart(
                    "Roof",
                    new Vector3(deckWidthMetres, slabThicknessMetres, deckLengthMetres),
                    new Vector3(0f, roofUnderside + (slabThicknessMetres * 0.5f), 0f))
            };

            return new MeasuredVehicle(
                new VehicleShape.Measurements
                {
                    Wheels = measured.Placements,
                    FrontCouplingLocal = MarkerLocal(cart, model, "HITCH_Male"),
                    RearCouplingLocal = MarkerLocal(cart, model, "HITCH_Female"),
                    EnvelopeSizeMetres = envelope.size,
                    EnvelopeCentreLocal = envelope.center,
                    InteriorLocal = loadSpace,
                    SolidParts = solid
                },
                measured.Visible);
        }

        static Bounds TheSpaceTheDoorsCloseOver(GameObject cart, Transform model)
        {
            var doors = new Bounds();
            var any = false;

            foreach (var part in model.GetComponentsInChildren<Transform>(true))
            {
                if (part == model || !SlidingDoors.IsAPanel(part.name, out _) || MeshOn(part) == null)
                {
                    continue;
                }

                var box = MeshBoxLocal(cart, part);

                if (any)
                {
                    doors.Encapsulate(box);
                }
                else
                {
                    doors = box;
                    any = true;
                }
            }

            if (!any)
            {
                throw Unmeasurable(cart, "its model has no doors to take a load space from");
            }

            return doors;
        }

        internal static Bounds MeasureTheAircraft(GameObject aircraft, Transform model)
            => EverythingItIsMadeOf(aircraft, model);

        static bool IsCouplingHardware(string name)
            => name.StartsWith("Hitch", StringComparison.OrdinalIgnoreCase);

        static Bounds EverythingItIsMadeOf(GameObject vehicle, Transform model)
        {
            var all = new Bounds();
            var anything = false;

            foreach (var part in model.GetComponentsInChildren<Transform>(true))
            {
                var filter = part.GetComponent<MeshFilter>();
                var skinned = part.GetComponent<SkinnedMeshRenderer>();
                var mesh = filter != null ? filter.sharedMesh
                    : skinned != null ? skinned.sharedMesh
                    : null;

                if (mesh == null || IsCouplingHardware(part.name))
                {
                    continue;
                }

                var box = MeshBoxLocal(vehicle, part);

                if (anything)
                {
                    all.Encapsulate(box);
                }
                else
                {
                    all = box;
                    anything = true;
                }
            }

            if (!anything)
            {
                throw Unmeasurable(vehicle, "its model has no meshes to take an envelope from");
            }

            return all;
        }

        static (List<VehicleShape.WheelPlacement> Placements, List<Transform> Visible) Wheels(
            GameObject vehicle, Transform model, IReadOnlyList<string> paths)
        {
            var placements = new List<VehicleShape.WheelPlacement>();
            var visible = new List<Transform>();

            foreach (var path in paths)
            {
                var wheel = PartOfTheModel(vehicle, model, path);
                var box = MeshBoxLocal(vehicle, wheel);
                var across = Mathf.Max(box.size.x, Mathf.Max(box.size.y, box.size.z));

                placements.Add(new VehicleShape.WheelPlacement(
                    vehicle.transform.InverseTransformPoint(wheel.position), across * 0.5f));

                visible.Add(wheel);
            }

            return (placements, visible);
        }

        static Vector3 MarkerLocal(GameObject vehicle, Transform model, string name)
        {
            var found = Named(vehicle, model, name);

            if (MeshOn(found) != null)
            {
                throw Unmeasurable(vehicle,
                    $"'{name}' is a mesh rather than a marker, and reading a coupling off the metal " +
                    "around it puts that coupling tens of centimetres out");
            }

            return vehicle.transform.InverseTransformPoint(found.position);
        }

        static Transform PartOfTheModel(GameObject vehicle, Transform model, string name)
        {
            var found = Named(vehicle, model, name);

            if (MeshOn(found) == null)
            {
                throw Unmeasurable(vehicle, $"'{name}' is a marker rather than a part of the model");
            }

            return found;
        }

        static Transform Named(GameObject vehicle, Transform model, string name)
        {
            var byPath = model.Find(name);
            if (byPath != null)
            {
                return byPath;
            }

            Transform found = null;

            foreach (var candidate in model.GetComponentsInChildren<Transform>(true))
            {
                if (candidate == model || candidate.name != name)
                {
                    continue;
                }

                if (found != null)
                {
                    throw Unmeasurable(vehicle,
                        $"its model has more than one '{name}', so there is no saying which one a " +
                        "measurement would be taken from");
                }

                found = candidate;
            }

            if (found != null)
            {
                return found;
            }

            var leaf = name.Substring(name.LastIndexOf('/') + 1);

            foreach (var candidate in model.GetComponentsInChildren<Transform>(true))
            {
                if (candidate == model || candidate.name != leaf)
                {
                    continue;
                }

                if (found != null)
                {
                    throw Unmeasurable(vehicle,
                        $"its model has more than one '{leaf}', so there is no saying which one a " +
                        "measurement would be taken from");
                }

                found = candidate;
            }

            return found ?? throw Unmeasurable(vehicle, $"its model has no '{name}' anywhere inside it");
        }

        static Mesh MeshOn(Transform part)
        {
            var filter = part.GetComponent<MeshFilter>();
            if (filter != null)
            {
                return filter.sharedMesh;
            }

            var skinned = part.GetComponent<SkinnedMeshRenderer>();
            return skinned != null ? skinned.sharedMesh : null;
        }

        static InvalidOperationException Unmeasurable(GameObject vehicle, string why)
            => new InvalidOperationException(
                $"'{vehicle.name}' cannot be measured and so cannot be built: {why}.");

        static Bounds MeshBoxLocal(GameObject vehicle, Transform model, string path)
            => MeshBoxLocal(vehicle, PartOfTheModel(vehicle, model, path));

        static VehicleShape.SolidPart SolidAsModelled(GameObject vehicle, Transform model, string path)
        {
            var part = PartOfTheModel(vehicle, model, path);
            var mesh = MeshOn(part);

            if (mesh == null)
            {
                throw Unmeasurable(vehicle, $"'{path}' in its model has no mesh to be solid in");
            }

            var onTheVehicle = vehicle.transform.worldToLocalMatrix * part.localToWorldMatrix;

            return new VehicleShape.SolidPart(
                part.name,
                mesh,
                onTheVehicle.GetPosition(),
                onTheVehicle.rotation,
                onTheVehicle.lossyScale);
        }

        static Bounds MeshBoxLocal(GameObject vehicle, Transform part)
        {
            var mesh = MeshOn(part);
            var least = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var most = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (var corner = 0; corner < 8; corner++)
            {
                var offset = Vector3.Scale(
                    mesh.bounds.extents,
                    new Vector3(
                        (corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f));

                var inVehicle = vehicle.transform.InverseTransformPoint(
                    part.TransformPoint(mesh.bounds.center + offset));

                least = Vector3.Min(least, inVehicle);
                most = Vector3.Max(most, inVehicle);
            }

            var box = new Bounds();
            box.SetMinMax(least, most);
            return box;
        }
    }
}
