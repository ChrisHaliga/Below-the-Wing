using BelowTheWing.Wiring;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BelowTheWing.Crew
{
    [DisallowMultipleComponent]
    public sealed class CrewPromptView : MonoBehaviour
    {
        IOfferSomething[] m_Offers = System.Array.Empty<IOfferSomething>();

        public void Watch(params IOfferSomething[] offers)
            => m_Offers = offers ?? System.Array.Empty<IOfferSomething>();

        public IOfferSomething Showing
        {
            get
            {
                foreach (var offer in m_Offers)
                {
                    if (offer != null && offer.Prompt != CrewPrompt.None)
                    {
                        return offer;
                    }
                }

                return null;
            }
        }

        void OnGUI()
        {
            var showing = Showing;
            if (showing == null)
            {
                return;
            }

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18
            };

            style.normal.textColor = showing.Prompt == CrewPrompt.Offer ? Palette.Ink : Palette.Bad;

            const float width = 520f;
            const float height = 30f;
            var box = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.72f, width, height);

            GUI.Label(box, Wording(showing.Prompt, showing.Subject, CrewKeys.Drive), style);
        }

        public static string Wording(CrewPrompt prompt, string subject, Key drive)
            => prompt switch
            {
                CrewPrompt.Offer => $"Press {drive} to drive {subject}",
                CrewPrompt.NoAnswer => $"{subject} did not answer. Try again.",
                CrewPrompt.BeingDriven => $"{subject} is being driven",
                _ => ""
            };
    }
}
