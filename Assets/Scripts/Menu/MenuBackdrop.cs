using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Menu
{
    [DisallowMultipleComponent]
    public sealed class MenuBackdrop : MonoBehaviour
    {
        [SerializeField, Tooltip("Where the camera stands for the title card")]
        Transform m_TitleShot;

        [SerializeField, Tooltip("Where the camera stands for every other panel")]
        Transform m_PanelShot;

        [SerializeField, Tooltip("Where the camera stands for the lobby")]
        Transform m_LobbyShot;

        [SerializeField, Tooltip("Crew standing at the cart, one per filled slot")]
        List<GameObject> m_LobbyCrew = new List<GameObject>();

        public Transform TitleShot => m_TitleShot;

        public Transform PanelShot => m_PanelShot;

        public Transform LobbyShot => m_LobbyShot;

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
