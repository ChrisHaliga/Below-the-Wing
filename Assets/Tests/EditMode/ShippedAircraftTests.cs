using BelowTheWing.Apron;
using BelowTheWing.Wiring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class ShippedAircraftTests
    {
        const float WingspanMetres = 21.21f;
        const float NoseToTailMetres = 27.44f;
        const float TailTopMetres = 6.22f;
        const float BellyAboveTheApronMetres = 0.84f;

        GameObject m_Aircraft;

        [SetUp]
        public void SetUp()
            => m_Aircraft = (GameObject)PrefabUtility.InstantiatePrefab(
                ShippedContent.Prefab(ShippedContent.AircraftPrefabPath));

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Aircraft);

        [Test]
        public void TheApronsAircraftIsDrawnAsTheRegionalJetModel()
        {
            var look = m_Aircraft.transform.Find(GreyboxShape.LookName);

            Assert.That(look, Is.Not.Null, "the aircraft has nothing to look at");
            Assert.That(m_Aircraft.GetComponent<ApronAppearance>().DrawnAs,
                Is.EqualTo(ApronAppearance.Shape.AlreadyModelled),
                "the aircraft would draw a stand-in capsule over the model it already has");

            Assert.That(look.GetComponentsInChildren<MeshRenderer>(), Is.Not.Empty,
                "the aircraft says it is already modelled and then draws nothing");
        }

        [Test]
        public void TheAircraftIsAsBigAsTheModelSaysItIs()
        {
            var shape = m_Aircraft.GetComponent<AircraftShape>();

            Assert.That(shape, Is.Not.Null,
                "nothing on the aircraft says how much room it takes up, so the layout cannot keep " +
                "anything clear of it");

            Assert.That(shape.EnvelopeSizeMetres.x, Is.EqualTo(WingspanMetres).Within(0.05f));
            Assert.That(shape.EnvelopeSizeMetres.z, Is.EqualTo(NoseToTailMetres).Within(0.05f));

            Assert.That(shape.EnvelopeCentreLocal.y + (shape.EnvelopeSizeMetres.y * 0.5f),
                Is.EqualTo(TailTopMetres).Within(0.05f),
                "the room the aircraft fills has to sit where the aircraft is, above the apron. " +
                "Centred on its own origin it claims three metres of cellar and loses its fin");
        }

        [Test]
        public void TheAircraftSitsAtTheHeightItsOwnModelPutsItAt()
        {
            m_Aircraft.transform.position = Vector3.zero;

            var lowest = float.MaxValue;

            foreach (var drawn in m_Aircraft.GetComponentsInChildren<MeshRenderer>(true))
            {
                lowest = Mathf.Min(lowest, drawn.bounds.min.y);
            }

            Assert.That(lowest, Is.EqualTo(BellyAboveTheApronMetres).Within(0.05f),
                $"the belly sits {lowest:0.00} m above the apron with the aircraft placed at ground " +
                "level. crj_200.fbx models no landing gear, so its own origin is the ground point " +
                "and the belly stands one gear leg above it. Zero here would bury the fuselage");
        }

        [Test]
        public void TheAircraftPointsItsNoseTheWayTheLayoutFacesIt()
        {
            var engines = Mesh("Engine");
            var airframe = Mesh("Body");

            Assert.That(engines, Is.Not.Null, "the model has no Engine to read a facing from");
            Assert.That(airframe, Is.Not.Null, "the model has no Body to read a facing from");

            Assert.That(engines.bounds.center.z, Is.LessThan(airframe.bounds.center.z),
                "a CRJ carries its engines at the tail, so engines ahead of the airframe's centre " +
                "means the aircraft is parked facing backwards");
        }

        [Test]
        public void TheAircraftIsSolidWhereverItIsDrawn()
        {
            var widest = 0f;

            foreach (var drawn in m_Aircraft.GetComponentsInChildren<MeshFilter>(true))
            {
                var solid = drawn.GetComponent<MeshCollider>();

                Assert.That(solid, Is.Not.Null,
                    $"'{drawn.name}' is drawn and has nothing solid on it, so crew walk through it");
                Assert.That(solid.sharedMesh, Is.SameAs(drawn.sharedMesh),
                    $"'{drawn.name}' is solid in a different shape from the one it is drawn in");

                widest = Mathf.Max(widest, solid.bounds.size.x);
            }

            Assert.That(widest, Is.GreaterThan(20f),
                $"the widest thing solid on the aircraft spans {widest:0.0} m. A capsule down the " +
                "fuselage leaves both wings walk-through");
        }

        [Test]
        public void TheNarrowbodyItReplacedIsGone()
        {
            foreach (var path in new[]
                     {
                         "Assets/Content/Aircraft/NarrowbodyAirliner.asset",
                         ShippedContent.PrefabFolder + "/NarrowbodyAirliner.prefab"
                     })
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<Object>(path), Is.Null,
                    $"{path} is still in the project. Two aircraft in the folder is two things to " +
                    "keep true, and only one of them is ever spawned");
            }
        }

        Renderer Mesh(string called)
        {
            foreach (var drawn in m_Aircraft.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (drawn.name == called)
                {
                    return drawn;
                }
            }

            return null;
        }
    }
}
