using UnityEngine;

namespace BelowTheWing.Apron
{
    [DisallowMultipleComponent]
    public sealed class WorldLabel : MonoBehaviour
    {
        const float LetterHeightMetres = 0.35f;

        const int RasterSize = 64;

        TextMesh m_Text;

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

            transform.rotation = viewer.transform.rotation;
        }
    }
}
