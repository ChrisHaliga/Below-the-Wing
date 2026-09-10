using UnityEngine;

namespace BelowTheWing.Apron
{
    /// <summary>
    /// Floating text naming the thing underneath it.
    ///
    /// Everything on the apron is a grey primitive at this stage, so a box is only recognisable as
    /// a baggage cart because it says so. The text turns to face whoever is looking at it, and has
    /// no collider: it is something to read, never something to drive into.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldLabel : MonoBehaviour
    {
        /// <summary>Height of the lettering in metres, so a name is readable from across the apron.</summary>
        const float LetterHeightMetres = 0.35f;

        /// <summary>
        /// Glyphs per metre used when the text is rasterised. Higher than the letter height needs,
        /// so the text stays crisp rather than blocky when a camera comes close.
        /// </summary>
        const int RasterSize = 64;

        TextMesh m_Text;

        /// <summary>What the label says.</summary>
        public string Text
        {
            get => m_Text != null ? m_Text.text : "";
            set
            {
                if (m_Text != null)
                {
                    m_Text.text = value;
                }
            }
        }

        /// <summary>
        /// Hangs a label above an object and returns it.
        /// </summary>
        /// <param name="target">What is being named. The label is parented to it and follows it.</param>
        /// <param name="text">What to write.</param>
        /// <param name="heightMetres">How far above the object's origin the text floats.</param>
        public static WorldLabel Attach(Transform target, string text, float heightMetres)
        {
            var holder = new GameObject($"{target.name} label");
            holder.transform.SetParent(target, worldPositionStays: false);
            holder.transform.localPosition = new Vector3(0f, heightMetres, 0f);

            var written = holder.AddComponent<TextMesh>();
            written.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            written.text = text;
            written.fontSize = RasterSize;
            written.characterSize = LetterHeightMetres * 2f / RasterSize * 10f;
            written.anchor = TextAnchor.LowerCenter;
            written.alignment = TextAlignment.Center;
            written.color = Color.white;

            holder.GetComponent<MeshRenderer>().sharedMaterial = written.font.material;

            var label = holder.AddComponent<WorldLabel>();
            label.m_Text = written;
            return label;
        }

        void LateUpdate()
        {
            var viewer = Camera.main;
            if (viewer == null)
            {
                return;
            }

            // Matching the camera's rotation rather than pointing at it. Lettering is drawn facing
            // the transform's forward, so text and camera have to face the same way for it to read
            // the right way round rather than mirrored.
            transform.rotation = viewer.transform.rotation;
        }
    }
}
