using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Settings
{
    public static class DisplayOptions
    {
        static readonly List<DisplayInfo> Connected = new List<DisplayInfo>();

        public static IReadOnlyList<DisplayInfo> Monitors
        {
            get
            {
                Connected.Clear();
                Screen.GetDisplayLayout(Connected);

                return Connected;
            }
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
            var monitors = Monitors;

            if (monitors.Count == 0)
            {
                return;
            }

            var target = monitors[Mathf.Clamp(CrewSettings.Monitor, 0, monitors.Count - 1)];

            if (!TheSameScreen(target, Screen.mainWindowDisplayInfo))
            {
                Screen.MoveMainWindowTo(target, Vector2Int.zero);
            }
        }

        static bool TheSameScreen(DisplayInfo a, DisplayInfo b)
            => a.name == b.name && a.width == b.width && a.height == b.height;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyOnBoot()
        {
            CrewSettings.Load();
            Apply();
        }
    }
}
