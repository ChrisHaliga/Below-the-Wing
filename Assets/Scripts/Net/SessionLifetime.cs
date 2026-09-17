using System;
using BelowTheWing.Wiring;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Net
{
    [DisallowMultipleComponent]
    public sealed class SessionLifetime : MonoBehaviour
    {
        public const float WaitsForTheServiceSeconds = 2f;

        NetworkManager m_Netcode;
        ILeaveTheService m_Service;

        bool m_AskedTheServiceToLeave;
        bool m_ServiceHasBeenLeft;
        float m_AskedAt;

        public void Ends(NetworkManager netcode, ILeaveTheService service)
        {
            m_Netcode = netcode != null
                ? netcode
                : throw MisbuiltException.For(this, "was given no NetworkManager to shut down on quit");
            m_Service = service;
        }

        public bool CloseBeforeQuitting()
        {
            StopListening();

            if (!StillLeavingTheService())
            {
                return true;
            }

            if (m_AskedTheServiceToLeave)
            {
                return m_ServiceHasBeenLeft
                       || Time.realtimeSinceStartup - m_AskedAt >= WaitsForTheServiceSeconds;
            }

            m_AskedTheServiceToLeave = true;
            m_AskedAt = Time.realtimeSinceStartup;

            LeaveThenQuit();

            return false;
        }

        async void LeaveThenQuit()
        {
            try
            {
                await m_Service.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Leaving the service failed on the way out: {e.Message}. Quitting anyway, " +
                    "because nothing may hold a quit open.", this);
            }
            finally
            {
                m_ServiceHasBeenLeft = true;
                Application.Quit();
            }
        }

        bool StillLeavingTheService() => m_Service != null && m_Service.InOne;

        void StopListening()
        {
            if (m_Netcode != null && m_Netcode.IsListening)
            {
                m_Netcode.Shutdown();
            }
        }

        void Awake() => Application.wantsToQuit += CloseBeforeQuitting;

        void OnDestroy()
        {
            Application.wantsToQuit -= CloseBeforeQuitting;

            StopListening();
        }
    }
}
