namespace BelowTheWing.Settings
{
    public interface ISettingsStore
    {
        int ReadInt(string key, int fallback);

        float ReadFloat(string key, float fallback);

        void Write(string key, int value);

        void Write(string key, float value);

        void Save();
    }
}
