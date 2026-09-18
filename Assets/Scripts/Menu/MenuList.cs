using System.Collections.Generic;
using System;
using BelowTheWing.Wiring;
using UnityEngine.UIElements;
using UnityEngine;

namespace BelowTheWing.Menu
{
    public sealed class MenuList
    {
        const int ItemSize = 27;
        const int ItemHeight = 46;
        const int ButtonSize = 20;
        const int ButtonWidth = 190;
        const int ButtonGap = 10;
        const int SelectedEdge = 2;

        readonly List<Row> m_Rows = new List<Row>();

        int m_On = -1;

        readonly bool m_AcrossTheScreen;

        public MenuList() : this(acrossTheScreen: false)
        {
        }

        public MenuList(bool acrossTheScreen)
        {
            m_AcrossTheScreen = acrossTheScreen;

            Root = new VisualElement();
            Root.style.marginTop = 8;

            if (acrossTheScreen)
            {
                Root.style.flexDirection = FlexDirection.Row;
                Root.style.alignItems = Align.Center;
            }
        }

        public VisualElement Root { get; }

        public int Count => m_Rows.Count;

        public bool AcrossTheScreen => m_AcrossTheScreen;

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
            row.Marker.style.height = m_AcrossTheScreen ? 3 : 20;
            row.Marker.style.width = m_AcrossTheScreen ? 0 : 3;
            row.Marker.style.backgroundColor = Color.clear;
            row.Marker.style.marginRight = m_AcrossTheScreen ? 0 : 16;

            row.Text = MenuLook.Display(text.ToUpperInvariant(), m_AcrossTheScreen ? ButtonSize : ItemSize);
            row.Text.style.color = Palette.InkSoft;

            if (m_AcrossTheScreen)
            {
                row.Element.style.justifyContent = Justify.Center;
                row.Element.style.minWidth = ButtonWidth;
                row.Element.style.marginLeft = ButtonGap;
                row.Element.style.marginRight = ButtonGap;
                row.Element.style.paddingLeft = 22;
                row.Element.style.paddingRight = 22;
                row.Element.style.backgroundColor = Palette.ButtonFill;

                MenuLook.Edges(row.Element, Palette.InkFaint, SelectedEdge);
            }

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

        public void Rename(int index, string text)
        {
            if (index < 0 || index >= m_Rows.Count)
            {
                return;
            }

            m_Rows[index].Text.text = text.ToUpperInvariant();
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
                    on && available ? Palette.HiVis : Color.clear;

                m_Rows[row].Text.style.color =
                    !available ? Palette.InkFaint
                    : on ? Palette.Ink
                    : Palette.InkSoft;

                m_Rows[row].Element.style.translate = m_AcrossTheScreen
                    ? new Translate(0, 0)
                    : new Translate(on && available ? 8 : 0, 0);

                if (m_AcrossTheScreen)
                {
                    MenuLook.Edges(
                        m_Rows[row].Element,
                        !available ? Palette.InkFaint : on ? Palette.HiVis : Palette.InkSoft,
                        SelectedEdge);
                }
            }
        }
    }
}
