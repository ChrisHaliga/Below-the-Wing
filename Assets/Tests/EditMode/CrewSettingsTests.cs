using BelowTheWing.Settings;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CrewSettingsTests
    {
        const string Sentinel = "tests.crewsettings.sentinel";

        MemorySettingsStore m_Store;

        [SetUp]
        public void SetUp()
        {
            m_Store = new MemorySettingsStore();
            CrewSettings.Use(m_Store);
        }

        [TearDown]
        public void TearDown()
        {
            CrewSettings.Use(new PlayerPrefsSettingsStore());
            PlayerPrefs.DeleteKey(Sentinel);
        }

        [Test]
        public void AFreshInstallStartsOnTheShippedDefaults()
        {
            Assert.That(CrewSettings.LookSensitivity, Is.EqualTo(CrewSettings.Defaults.LookSensitivity).Within(1e-4f));
            Assert.That(CrewSettings.InvertLookY, Is.EqualTo(CrewSettings.Defaults.InvertLookY));
            Assert.That(CrewSettings.MasterVolume, Is.EqualTo(CrewSettings.Defaults.MasterVolume).Within(1e-4f));
            Assert.That(CrewSettings.EffectsVolume, Is.EqualTo(CrewSettings.Defaults.EffectsVolume).Within(1e-4f));
            Assert.That(CrewSettings.FieldOfViewDegrees, Is.EqualTo(CrewSettings.Defaults.FieldOfViewDegrees).Within(1e-4f));
            Assert.That(CrewSettings.Mode, Is.EqualTo(CrewSettings.Defaults.Mode));
            Assert.That(CrewSettings.Monitor, Is.EqualTo(CrewSettings.Defaults.Monitor));
            Assert.That(CrewSettings.Resolution, Is.EqualTo(Vector2Int.zero),
                "zero means whatever the display is already using, so a fresh install does not " +
                "force a resolution on a machine it knows nothing about");
        }

        [Test]
        public void TheShippedDefaultsAreWrittenOnceAndResetReadsFromThatSameCopy()
        {
            CrewSettings.LookSensitivity = 2.4f;
            CrewSettings.MasterVolume = 0.1f;
            CrewSettings.InvertLookY = true;
            CrewSettings.FieldOfViewDegrees = 100f;

            CrewSettings.ResetToDefaults();
            CrewSettings.Reload();

            Assert.That(CrewSettings.LookSensitivity, Is.EqualTo(CrewSettings.Defaults.LookSensitivity).Within(1e-4f),
                "a fresh install and a reset must land on the same figure, or the two disagree the " +
                "day somebody edits one of them");
            Assert.That(CrewSettings.MasterVolume, Is.EqualTo(CrewSettings.Defaults.MasterVolume).Within(1e-4f));
            Assert.That(CrewSettings.InvertLookY, Is.EqualTo(CrewSettings.Defaults.InvertLookY));
            Assert.That(CrewSettings.FieldOfViewDegrees, Is.EqualTo(CrewSettings.Defaults.FieldOfViewDegrees).Within(1e-4f));
        }

        [Test]
        public void ASettingSurvivesTheGameBeingClosed()
        {
            CrewSettings.LookSensitivity = 1.8f;
            CrewSettings.InvertLookY = true;
            CrewSettings.FieldOfViewDegrees = 92f;

            CrewSettings.Reload();

            Assert.That(CrewSettings.LookSensitivity, Is.EqualTo(1.8f).Within(1e-4f));
            Assert.That(CrewSettings.InvertLookY, Is.True);
            Assert.That(CrewSettings.FieldOfViewDegrees, Is.EqualTo(92f).Within(1e-4f));
        }

        [Test]
        public void TheMachinesOwnSavedSettingsAreNeverTouchedByTheseTests()
        {
            PlayerPrefs.SetString(Sentinel, "still here");

            CrewSettings.LookSensitivity = 2.2f;
            CrewSettings.ResetToDefaults();
            CrewSettings.Reload();

            Assert.That(PlayerPrefs.GetString(Sentinel), Is.EqualTo("still here"),
                "the suite ran against the real PlayerPrefs once and wiped the owner's saved look " +
                "sensitivity, resolution and monitor on every run");
            Assert.That(PlayerPrefs.HasKey("settings.look.sensitivity"), Is.False,
                "and nothing this fixture does may leave a settings key in the real store");
        }

        [Test]
        public void ASensitivityIsHeldInsideTheRangeTheSliderOffers()
        {
            CrewSettings.LookSensitivity = 40f;
            Assert.That(CrewSettings.LookSensitivity, Is.EqualTo(CrewSettings.MostSensitive));

            CrewSettings.LookSensitivity = -3f;
            Assert.That(CrewSettings.LookSensitivity, Is.EqualTo(CrewSettings.LeastSensitive),
                "a stored zero would leave a player unable to turn their head at all");
        }

        [Test]
        public void AVolumeIsHeldBetweenSilentAndFull()
        {
            CrewSettings.MasterVolume = 3f;
            CrewSettings.EffectsVolume = -1f;

            Assert.That(CrewSettings.MasterVolume, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(CrewSettings.EffectsVolume, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void AFieldOfViewIsHeldInsideWhatACameraCanRender()
        {
            CrewSettings.FieldOfViewDegrees = 400f;
            Assert.That(CrewSettings.FieldOfViewDegrees, Is.EqualTo(CrewSettings.WidestFieldOfView));

            CrewSettings.FieldOfViewDegrees = 1f;
            Assert.That(CrewSettings.FieldOfViewDegrees, Is.EqualTo(CrewSettings.NarrowestFieldOfView));
        }

        [Test]
        public void AMonitorIndexNamingAScreenThatIsGoneIsNotTrusted()
        {
            CrewSettings.Monitor = -4;

            Assert.That(CrewSettings.Monitor, Is.EqualTo(0),
                "a stored index outlives the monitor it named, so someone unplugging a screen " +
                "between sessions must not leave the game pointing at a display that is not there");
        }

        [Test]
        public void AStoredValueOutsideTheRangeIsClampedOnTheWayIn()
        {
            m_Store.Write("settings.look.sensitivity", 50f);
            m_Store.Write("settings.look.fieldOfView", 5f);
            m_Store.Write("settings.display.monitor", -2);

            CrewSettings.Reload();

            Assert.That(CrewSettings.LookSensitivity, Is.EqualTo(CrewSettings.MostSensitive));
            Assert.That(CrewSettings.FieldOfViewDegrees, Is.EqualTo(CrewSettings.NarrowestFieldOfView));
            Assert.That(CrewSettings.Monitor, Is.EqualTo(0));
        }

        [Test]
        public void LookingAroundAsksForOneSignedMultiplierRatherThanTwoSettings()
        {
            CrewSettings.LookSensitivity = 2f;

            CrewSettings.InvertLookY = false;
            Assert.That(CrewSettings.LookYMultiplier, Is.EqualTo(2f).Within(1e-4f));

            CrewSettings.InvertLookY = true;
            Assert.That(CrewSettings.LookYMultiplier, Is.EqualTo(-2f).Within(1e-4f),
                "every place that reads this would otherwise have to remember to apply the flip, " +
                "and one that forgets inverts for some inputs and not others");
        }

        [Test]
        public void ChangingASettingTellsAnyOpenPanelToRepaint()
        {
            var told = 0;
            CrewSettings.Changed += Count;

            CrewSettings.LookSensitivity = 1.5f;
            CrewSettings.LookSensitivity = 1.5f;

            CrewSettings.Changed -= Count;

            Assert.That(told, Is.EqualTo(1), "setting the same value again is not a change");

            void Count() => told++;
        }
    }
}
