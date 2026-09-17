using System.Collections.Generic;

namespace BelowTheWing.Settings
{
    public sealed class MemorySettingsStore : ISettingsStore
    {
        readonly Dictionary<string, int> m_Ints = new Dictionary<string, int>();
        readonly Dictionary<string, float> m_Floats = new Dictionary<string, float>();

        public int ReadInt(string key, int fallback) => m_Ints.TryGetValue(key, out var v) ? v : fallback;

        public float ReadFloat(string key, float fallback) => m_Floats.TryGetValue(key, out var v) ? v : fallback;

        public void Write(string key, int value) => m_Ints[key] = value;

        public void Write(string key, float value) => m_Floats[key] = value;

        public void Save()
        {
        }
    }
}
