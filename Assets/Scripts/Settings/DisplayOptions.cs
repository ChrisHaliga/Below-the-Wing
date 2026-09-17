using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Settings
{
    public static class DisplayOptions
    {
        public static List<DisplayInfo> Monitors()
        {
            var connected = new List<DisplayInfo>();
            Screen.GetDisplayLayout(connected);

            return connected;
        }

        public static bool CanApply => !Application.isEditor;

        public static List<Vector2Int> Resolutions()
        {
            var sizes = new List<Vector2Int>();

            foreach (var resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);

                if (!sizes.Contains(size))
                {
                    sizes.Add(size);
                }
            }

            sizes.Sort((a, b) => a.x != b.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));

            return sizes;
        }

        public static string Describe(int index, DisplayInfo info)
            => string.IsNullOrEmpty(info.name)
                ? $"Display {index + 1} ({info.width}x{info.height})"
                : $"{index + 1} - {info.name} ({info.width}x{info.height})";

        public static void Apply()
        {
            if (!CanApply)
            {
                return;
            }

            MoveToTheChosenMonitor();

            var size = CrewSettings.Resolution;

            Screen.SetResolution(
                size.x > 0 ? size.x : Screen.width,
                size.y > 0 ? size.y : Screen.height,
                CrewSettings.Mode);
        }

        static void MoveToTheChosenMonitor()
        {
            var monitors = Monitors();

            if (monitors.Count == 0)
            {
                return;
            }

            var target = monitors[Mathf.Clamp(CrewSettings.Monitor, 0, monitors.Count - 1)];

            if (!DisplayIdentity.Same(target, Screen.mainWindowDisplayInfo))
            {
                Screen.MoveMainWindowTo(target, Vector2Int.zero);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyOnBoot() => Apply();
    }
}
