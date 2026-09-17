using BelowTheWing.Settings;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CrewSettingsTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();
            CrewSettings.Reload();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
            CrewSettings.Reload();
        }

        [Test]
        public void AFreshInstallStartsOnTheShippedDefaults()
        {
            Assert.That(CrewSettings.LookSensitivity, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(CrewSettings.InvertLookY, Is.False);
            Assert.That(CrewSettings.MasterVolume, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(CrewSettings.EffectsVolume, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(CrewSettings.FieldOfViewDegrees, Is.EqualTo(75f).Within(1e-4f));
            Assert.That(CrewSettings.Mode, Is.EqualTo(FullScreenMode.FullScreenWindow));
            Assert.That(CrewSettings.Monitor, Is.EqualTo(0));
            Assert.That(CrewSettings.Resolution, Is.EqualTo(Vector2Int.zero),
                "zero means whatever the display is already using, so a fresh install does not " +
                "force a resolution on a machine it knows nothing about");
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

        [Test]
        public void EverythingCanBePutBackTheWayItShipped()
        {
            CrewSettings.LookSensitivity = 2.4f;
            CrewSettings.MasterVolume = 0.1f;
            CrewSettings.InvertLookY = true;

            CrewSettings.ResetToDefaults();

            Assert.That(CrewSettings.LookSensitivity, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(CrewSettings.MasterVolume, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(CrewSettings.InvertLookY, Is.False);
        }
    }
}
