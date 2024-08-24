using System;
using Windows.Storage;

namespace ValleyTube
{
    public static class Settings
    {
        private static bool _isDash;
        private static bool _isAutoplayEnabled;
        private static string _selectedQuality; // New field for video quality

        public const int MaxHistorySize = 50;

        public static string InvidiousInstance = "https://invidious.nerdvpn.de";
        public static string InvidiousInstanceComments = "https://inv.nadeko.net";

        public static bool isDash
        {
            get { return _isDash; }
            set
            {
                _isDash = value;
                SaveSetting("isDash", value);
            }
        }

        public static bool IsAutoplayEnabled
        {
            get { return _isAutoplayEnabled; }
            set
            {
                _isAutoplayEnabled = value;
                SaveSetting("IsAutoplayEnabled", value);
            }
        }


        public static string SelectedQuality
        {
            get { return _selectedQuality; }
            set
            {
                _selectedQuality = value;
                SaveSetting("SelectedQuality", value);
            }
        }

        static Settings()
        {
            _isDash = false;
            _isAutoplayEnabled = true;
            _selectedQuality = "480"; 
            LoadSettings();
        }

        private static void SaveSetting(string key, object value)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[key] = value;
        }

        public static void LoadSettings()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("isDash"))
            {
                _isDash = (bool)localSettings.Values["isDash"];
            }

            if (localSettings.Values.ContainsKey("IsAutoplayEnabled"))
            {
                _isAutoplayEnabled = (bool)localSettings.Values["IsAutoplayEnabled"];
            }

            if (localSettings.Values.ContainsKey("SelectedQuality"))
            {
                _selectedQuality = (string)localSettings.Values["SelectedQuality"];
            }
        }
    }
}
