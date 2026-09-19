using System.Collections.Generic;
using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Menu
{
    [DisallowMultipleComponent]
    public sealed class MenuBackdrop : MonoBehaviour
    {
        [SerializeField, Tooltip("Where the camera stands to face the shut cart")]
        Transform m_CartShot;

        [SerializeField, Tooltip("Where the camera stands once the cart has opened")]
        Transform m_InsideShot;

        [SerializeField, Tooltip("Crew standing behind the cart, one per filled slot")]
        List<GameObject> m_LobbyCrew = new List<GameObject>();

        [SerializeField, Tooltip("The staged cart's doors, slid open for the lobby")]
        MenuCartDoors m_CartDoors;

        [SerializeField, Tooltip("How far above a crew member's feet their nameplate floats")]
        float m_PlateHeightMetres = 2.05f;

        [SerializeField, Tooltip("The belt loader the crew line up in front of")]
        Transform m_BeltLoader;

        public Transform CartShot => m_CartShot;

        public Transform InsideShot => m_InsideShot;

        public MenuCartDoors CartDoors
        {
            get
            {
                m_CartDoors = m_CartDoors != null ? m_CartDoors : GetComponent<MenuCartDoors>();
                m_CartDoors = m_CartDoors != null
                    ? m_CartDoors
                    : FindAnyObjectByType<MenuCartDoors>(FindObjectsInactive.Include);

                if (m_CartDoors == null)
                {
                    throw MisbuiltException.For(
                        this,
                        "has no cart doors to slide open, and none stand in the scene either. Run " +
                        "Below the Wing/Rebuild apron scene and prefabs");
                }

                return m_CartDoors;
            }
        }

        public int CrewCount => m_LobbyCrew.Count;

        public Transform BeltLoader => m_BeltLoader;

        public GameObject FigureFor(int crew)
        {
            if (crew < 0 || crew >= m_LobbyCrew.Count || m_LobbyCrew[crew] == null)
            {
                throw MisbuiltException.For(this, $"has no crew figure {crew}");
            }

            return m_LobbyCrew[crew];
        }

        public Transform StandingAt(MenuStation station)
        {
            var shot = station switch
            {
                MenuStation.Inside => m_InsideShot,
                _ => m_CartShot
            };

            if (shot == null)
            {
                throw MisbuiltException.For(
                    this,
                    $"has nowhere for the camera to stand for the {station} shot. Run " +
                    "Below the Wing/Rebuild apron scene and prefabs");
            }

            return shot;
        }

        public Vector3 PlateOver(int crew)
            => FigureFor(crew).transform.position + (Vector3.up * m_PlateHeightMetres);

        public void ShowThisManyCrew(int howMany)
        {
            for (var crew = 0; crew < m_LobbyCrew.Count; crew++)
            {
                if (m_LobbyCrew[crew] != null)
                {
                    m_LobbyCrew[crew].SetActive(crew < howMany);
                }
            }
        }
    }
}
