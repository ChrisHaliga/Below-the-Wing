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

        public static readonly CrewDefaults Defaults = new CrewDefaults(
            mode: FullScreenMode.FullScreenWindow,
            monitor: 0,
            lookSensitivity: 1f,
            invertLookY: false,
            masterVolume: 1f,
            effectsVolume: 1f,
            fieldOfViewDegrees: 75f);

        const string ModeKey = "settings.display.mode";
        const string MonitorKey = "settings.display.monitor";
        const string WidthKey = "settings.display.width";
        const string HeightKey = "settings.display.height";
        const string LookKey = "settings.look.sensitivity";
        const string InvertKey = "settings.look.invertY";
        const string MasterKey = "settings.audio.master";
        const string EffectsKey = "settings.audio.effects";
        const string FieldOfViewKey = "settings.look.fieldOfView";

        static ISettingsStore s_Store = new PlayerPrefsSettingsStore();
        static bool s_Loaded;

        static FullScreenMode s_Mode;
        static int s_Monitor;
        static int s_Width;
        static int s_Height;
        static float s_Look;
        static bool s_InvertY;
        static float s_Master;
        static float s_Effects;
        static float s_FieldOfView;

        public static event Action Changed;

        public static void Use(ISettingsStore store)
        {
            s_Store = store ?? throw new ArgumentNullException(nameof(store));
            Reload();
        }

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
            TakeDefaults();
            s_Width = 0;
            s_Height = 0;

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

            s_Mode = (FullScreenMode)s_Store.ReadInt(ModeKey, (int)Defaults.Mode);
            s_Monitor = Mathf.Max(0, s_Store.ReadInt(MonitorKey, Defaults.Monitor));
            s_Width = Mathf.Max(0, s_Store.ReadInt(WidthKey, 0));
            s_Height = Mathf.Max(0, s_Store.ReadInt(HeightKey, 0));
            s_Look = ClampSensitivity(s_Store.ReadFloat(LookKey, Defaults.LookSensitivity));
            s_InvertY = s_Store.ReadInt(InvertKey, Defaults.InvertLookY ? 1 : 0) != 0;
            s_Master = Mathf.Clamp01(s_Store.ReadFloat(MasterKey, Defaults.MasterVolume));
            s_Effects = Mathf.Clamp01(s_Store.ReadFloat(EffectsKey, Defaults.EffectsVolume));
            s_FieldOfView = ClampFieldOfView(s_Store.ReadFloat(FieldOfViewKey, Defaults.FieldOfViewDegrees));
        }

        static void TakeDefaults()
        {
            s_Mode = Defaults.Mode;
            s_Monitor = Defaults.Monitor;
            s_Look = Defaults.LookSensitivity;
            s_InvertY = Defaults.InvertLookY;
            s_Master = Defaults.MasterVolume;
            s_Effects = Defaults.EffectsVolume;
            s_FieldOfView = Defaults.FieldOfViewDegrees;
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
            s_Store.Write(ModeKey, (int)s_Mode);
            s_Store.Write(MonitorKey, s_Monitor);
            s_Store.Write(WidthKey, s_Width);
            s_Store.Write(HeightKey, s_Height);
            s_Store.Write(LookKey, s_Look);
            s_Store.Write(InvertKey, s_InvertY ? 1 : 0);
            s_Store.Write(MasterKey, s_Master);
            s_Store.Write(EffectsKey, s_Effects);
            s_Store.Write(FieldOfViewKey, s_FieldOfView);
            s_Store.Save();

            Changed?.Invoke();
        }
    }
}
