using UnityEngine;

namespace BelowTheWing.Settings
{
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        public int ReadInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);

        public float ReadFloat(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);

        public void Write(string key, int value) => PlayerPrefs.SetInt(key, value);

        public void Write(string key, float value) => PlayerPrefs.SetFloat(key, value);

        public void Save() => PlayerPrefs.Save();
    }
}
