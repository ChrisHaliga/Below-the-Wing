using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Settings
{
    public static class CrewSettings
    {
        public const float LeastSensitive = 0.25f;
        public const float MostSensitive = 3f;
        public const float NarrowestFieldOfView = 60f;
        public const float WidestFieldOfView = 110f;

        const string ModeKey = "settings.display.mode";
        const string MonitorKey = "settings.display.monitor";
        const string WidthKey = "settings.display.width";
        const string HeightKey = "settings.display.height";
        const string LookKey = "settings.look.sensitivity";
        const string InvertKey = "settings.look.invertY";
        const string MasterKey = "settings.audio.master";
        const string EffectsKey = "settings.audio.effects";
        const string FieldOfViewKey = "settings.look.fieldOfView";

        static bool s_Loaded;

        static FullScreenMode s_Mode = FullScreenMode.FullScreenWindow;
        static int s_Monitor;
        static int s_Width;
        static int s_Height;
        static float s_Look = 1f;
        static bool s_InvertY;
        static float s_Master = 1f;
        static float s_Effects = 1f;
        static float s_FieldOfView = 75f;

        public static event Action Changed;

        public static FullScreenMode Mode
        {
            get => Read(ref s_Mode);
            set => Set(ref s_Mode, value);
        }

        public static int Monitor
        {
            get => Read(ref s_Monitor);
            set => Set(ref s_Monitor, Mathf.Max(0, value));
        }

        public static Vector2Int Resolution
        {
            get
            {
                Load();

                return new Vector2Int(s_Width, s_Height);
            }
            set
            {
                Load();

                var width = Mathf.Max(0, value.x);
                var height = Mathf.Max(0, value.y);

                if (s_Width == width && s_Height == height)
                {
                    return;
                }

                s_Width = width;
                s_Height = height;
                Save();
            }
        }

        public static float LookSensitivity
        {
            get => Read(ref s_Look);
            set => Set(ref s_Look, ClampSensitivity(value));
        }

        public static bool InvertLookY
        {
            get => Read(ref s_InvertY);
            set => Set(ref s_InvertY, value);
        }

        public static float LookYMultiplier => InvertLookY ? -LookSensitivity : LookSensitivity;

        public static float MasterVolume
        {
            get => Read(ref s_Master);
            set => Set(ref s_Master, Mathf.Clamp01(value));
        }

        public static float EffectsVolume
        {
            get => Read(ref s_Effects);
            set => Set(ref s_Effects, Mathf.Clamp01(value));
        }

        public static float FieldOfViewDegrees
        {
            get => Read(ref s_FieldOfView);
            set => Set(ref s_FieldOfView, ClampFieldOfView(value));
        }

        public static float ClampSensitivity(float sensitivity)
            => Mathf.Clamp(sensitivity, LeastSensitive, MostSensitive);

        public static float ClampFieldOfView(float degrees)
            => Mathf.Clamp(degrees, NarrowestFieldOfView, WidestFieldOfView);

        public static void ResetToDefaults()
        {
            s_Loaded = true;
            s_Mode = FullScreenMode.FullScreenWindow;
            s_Monitor = 0;
            s_Width = 0;
            s_Height = 0;
            s_Look = 1f;
            s_InvertY = false;
            s_Master = 1f;
            s_Effects = 1f;
            s_FieldOfView = 75f;

            Save();
        }

        public static void Reload()
        {
            s_Loaded = false;
            Load();
        }

        public static void Load()
        {
            if (s_Loaded)
            {
                return;
            }

            s_Loaded = true;

            s_Mode = (FullScreenMode)PlayerPrefs.GetInt(ModeKey, (int)FullScreenMode.FullScreenWindow);
            s_Monitor = Mathf.Max(0, PlayerPrefs.GetInt(MonitorKey, 0));
            s_Width = Mathf.Max(0, PlayerPrefs.GetInt(WidthKey, 0));
            s_Height = Mathf.Max(0, PlayerPrefs.GetInt(HeightKey, 0));
            s_Look = ClampSensitivity(PlayerPrefs.GetFloat(LookKey, 1f));
            s_InvertY = PlayerPrefs.GetInt(InvertKey, 0) != 0;
            s_Master = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, 1f));
            s_Effects = Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey, 1f));
            s_FieldOfView = ClampFieldOfView(PlayerPrefs.GetFloat(FieldOfViewKey, 75f));
        }

        static T Read<T>(ref T field)
        {
            Load();

            return field;
        }

        static void Set<T>(ref T field, T value)
        {
            Load();

            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            Save();
        }

        static void Save()
        {
            PlayerPrefs.SetInt(ModeKey, (int)s_Mode);
            PlayerPrefs.SetInt(MonitorKey, s_Monitor);
            PlayerPrefs.SetInt(WidthKey, s_Width);
            PlayerPrefs.SetInt(HeightKey, s_Height);
            PlayerPrefs.SetFloat(LookKey, s_Look);
            PlayerPrefs.SetInt(InvertKey, s_InvertY ? 1 : 0);
            PlayerPrefs.SetFloat(MasterKey, s_Master);
            PlayerPrefs.SetFloat(EffectsKey, s_Effects);
            PlayerPrefs.SetFloat(FieldOfViewKey, s_FieldOfView);
            PlayerPrefs.Save();

            Changed?.Invoke();
        }
    }
}
