using System.Collections;
using BelowTheWing.Apron;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// A thing on the apron that does whatever somebody wired it to.
    ///
    /// The test that matters is the one about what it does not know. A button that learns what its
    /// numbers mean is the first step towards the game knowing that a test rig exists, and the next
    /// thing it learns will be harder to remove.
    /// </summary>
    public sealed class PressableControlPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;
        GameObject m_ButtonObject;
        PressableControl m_Button;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();

            m_ButtonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_ButtonObject.name = "Button";
            m_ButtonObject.transform.position = Vector3.zero;
            m_Button = m_ButtonObject.AddComponent<PressableControl>();
            m_Button.Called("Whatever somebody typed", reachMetres: 2.5f);
            m_Apron.Track(m_ButtonObject.transform);
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
        }

        [UnityTest]
        public IEnumerator PressingItFromCloseByDoesWhateverItIsWiredTo()
        {
            var pressed = 0;
            m_Button.Pressed.AddListener(() => pressed++);

            Assert.That(m_Button.PressFrom(new Vector3(0f, 0f, 1f)), Is.True);
            Assert.That(pressed, Is.EqualTo(1));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ItCannotBePressedFromAcrossTheApron()
        {
            var pressed = 0;
            m_Button.Pressed.AddListener(() => pressed++);

            Assert.That(m_Button.PressFrom(new Vector3(0f, 0f, 40f)), Is.False);
            Assert.That(pressed, Is.Zero,
                "a control anybody can press from anywhere can be pressed from inside a moving cart " +
                "on the far side of the apron");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ItSetsWhateverSpeedWasTypedIntoIt()
        {
            var vehicle = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 6f), Quaternion.identity);
            var driver = vehicle.gameObject.AddComponent<ScriptedDriver>();
            vehicle.IntentSource = driver;

            var setting = m_ButtonObject.AddComponent<SpeedSetting>();
            setting.Driver = driver;
            setting.SpeedMetresPerSecond = 7f;
            m_Button.Pressed.AddListener(setting.Apply);

            m_Button.PressFrom(new Vector3(0f, 0f, 1f));

            Assert.That(driver.TargetSpeed, Is.EqualTo(7f).Within(1e-3f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator NothingInTheGameKnowsWhatTheseNumbersMean()
        {
            var setting = m_ButtonObject.AddComponent<SpeedSetting>();

            // The component holds a speed and applies it. It has no opinion about whether seven
            // metres a second is fast, and no name for the ones that are.
            Assert.That(setting.SpeedMetresPerSecond, Is.Zero,
                "a speed setting that arrives with an opinion already in it is a scene's value " +
                "living in a runtime assembly");

            setting.SpeedMetresPerSecond = 7f;
            setting.Apply();

            Assert.That(setting.Driver, Is.Null, "and it does nothing at all until somebody wires it up");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ItSaysWhateverIsWrittenOnIt()
        {
            m_Button.Called("Anything at all", reachMetres: 3f);

            Assert.That(m_Button.Label, Is.EqualTo("Anything at all"));
            Assert.That(m_Button.ReachMetres, Is.EqualTo(3f).Within(1e-3f));

            yield return null;
        }
    }
}
