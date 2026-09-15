using UnityEngine;

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
                    if (offer != null && !string.IsNullOrEmpty(offer.Message))
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

            style.normal.textColor = showing.Prompt == CrewPrompt.Refused
                ? new Color(1f, 0.6f, 0.45f)
                : Color.white;

            const float width = 520f;
            const float height = 30f;
            var box = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.72f, width, height);

            GUI.Label(box, showing.Message, style);
        }
    }
}
