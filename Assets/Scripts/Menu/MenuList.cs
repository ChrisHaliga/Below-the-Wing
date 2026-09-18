using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BelowTheWing.Menu
{
    public sealed class MenuList
    {
        const int ItemSize = 27;
        const int ItemHeight = 46;

        readonly List<Row> m_Rows = new List<Row>();

        int m_On = -1;

        public MenuList()
        {
            Root = new VisualElement();
            Root.style.marginTop = 8;
        }

        public VisualElement Root { get; }

        public int Count => m_Rows.Count;

        sealed class Row
        {
            public VisualElement Element;
            public VisualElement Marker;
            public Label Text;
            public Action Chosen;
            public bool Available = true;
        }

        public void Add(string text, Action chosen)
        {
            var row = new Row { Chosen = chosen };
            var mine = m_Rows.Count;

            row.Element = new VisualElement();
            row.Element.style.flexDirection = FlexDirection.Row;
            row.Element.style.alignItems = Align.Center;
            row.Element.style.height = ItemHeight;

            row.Marker = new VisualElement();
            row.Marker.style.width = 3;
            row.Marker.style.height = 20;
            row.Marker.style.backgroundColor = Color.clear;
            row.Marker.style.marginRight = 16;

            row.Text = MenuLook.Display(text.ToUpperInvariant(), ItemSize);
            row.Text.style.color = MenuLook.InkSoft;

            row.Element.Add(row.Marker);
            row.Element.Add(row.Text);

            row.Element.RegisterCallback<MouseEnterEvent>(_ => On(mine));
            row.Element.RegisterCallback<MouseDownEvent>(_ => Choose(mine));

            m_Rows.Add(row);
            Root.Add(row.Element);

            MenuLook.SettleIn(row.Element, 0.12f + (mine * 0.05f));

            if (m_On < 0)
            {
                On(0);
            }
        }

        public void Available(int index, bool available)
        {
            if (index < 0 || index >= m_Rows.Count)
            {
                return;
            }

            m_Rows[index].Available = available;
            Paint();
        }

        public void Move(int by)
        {
            if (m_Rows.Count == 0)
            {
                return;
            }

            var next = m_On;

            for (var step = 0; step < m_Rows.Count; step++)
            {
                next = (next + by + m_Rows.Count) % m_Rows.Count;

                if (m_Rows[next].Available)
                {
                    break;
                }
            }

            On(next);
        }

        public void ChooseWhatIsOn() => Choose(m_On);

        void On(int index)
        {
            if (index < 0 || index >= m_Rows.Count)
            {
                return;
            }

            m_On = index;
            Paint();
        }

        void Choose(int index)
        {
            if (index < 0 || index >= m_Rows.Count || !m_Rows[index].Available)
            {
                return;
            }

            On(index);
            m_Rows[index].Chosen?.Invoke();
        }

        void Paint()
        {
            for (var row = 0; row < m_Rows.Count; row++)
            {
                var on = row == m_On;
                var available = m_Rows[row].Available;

                m_Rows[row].Marker.style.backgroundColor =
                    on && available ? MenuLook.HiVis : Color.clear;

                m_Rows[row].Text.style.color =
                    !available ? MenuLook.InkFaint
                    : on ? MenuLook.Ink
                    : MenuLook.InkSoft;

                m_Rows[row].Element.style.translate =
                    new Translate(on && available ? 8 : 0, 0);
            }
        }
    }
}
