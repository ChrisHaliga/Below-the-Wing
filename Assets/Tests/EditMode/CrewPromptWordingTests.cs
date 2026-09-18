using BelowTheWing.Crew;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CrewPromptWordingTests
    {
        [Test]
        public void AnOfferNamesTheKeyThatIsActuallyBoundToDriving()
        {
            Assert.That(CrewPromptView.Wording(CrewPrompt.Offer, "Tug 1", Key.E), Is.EqualTo("Press E to drive Tug 1"));
            Assert.That(CrewPromptView.Wording(CrewPrompt.Offer, "Tug 1", Key.F), Is.EqualTo("Press F to drive Tug 1"),
                "the domain used to spell out Press E itself, so rebinding the key made the prompt lie");
        }

        [Test]
        public void ARefusalSaysWhyInTheViewNotTheDomain()
        {
            Assert.That(CrewPromptView.Wording(CrewPrompt.NoAnswer, "Tug 1", Key.E), Is.EqualTo("Tug 1 did not answer. Try again."));
            Assert.That(CrewPromptView.Wording(CrewPrompt.BeingDriven, "Tug 1", Key.E), Is.EqualTo("Tug 1 is being driven"));
        }

        [Test]
        public void NothingOnOfferSaysNothing()
        {
            Assert.That(CrewPromptView.Wording(CrewPrompt.None, "Tug 1", Key.E), Is.Empty);
        }

        [Test]
        public void TheDriveKeyTheInputReadsIsTheOneThePromptNames()
        {
            Assert.That(CrewKeys.Drive, Is.EqualTo(Key.E));
        }
    }
}
