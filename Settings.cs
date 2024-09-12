﻿using System;
using Windows.Storage;

namespace ValleyTube
{
    public static class Settings
    {
        private static bool _isDash;
        private static bool _isAutoplayEnabled;
        public static string _selectedQuality;
        private static bool _screenTimeOut;
        private static string _invidiousInstance = "https://invidious.privacyredirect.com";
        private static string _invidiousInstanceComments = "https://inv.nadeko.net";
        private static string _returnDislikeInstance = "https://returnyoutubedislikeapi.com";

        public const int MaxHistorySize = 200;

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

        public static bool ScreenTimeOut
        {
            get { return _screenTimeOut; }
            set
            {
                _screenTimeOut = value;
                SaveSetting("IsScreenTimeoutEnabled", value);
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

        public static string InvidiousInstance
        {
            get { return _invidiousInstance; }
            set
            {
                _invidiousInstance = value;
                SaveSetting("InvidiousInstance", value);
            }
        }

        public static string InvidiousInstanceComments
        {
            get { return _invidiousInstanceComments; }
            set
            {
                _invidiousInstanceComments = value;
                SaveSetting("InvidiousInstanceComments", value);
            }
        }

        public static string ReturnDislikeInstance
        {
            get { return _returnDislikeInstance; }
            set
            {
                _returnDislikeInstance = value;
                SaveSetting("ReturnDislikeInstance", value);
            }
        }

        public static void SaveSetting(string key, object value)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[key] = value;
        }

        public static void ClearSettings()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            localSettings.Values.Clear();
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

            if (localSettings.Values.ContainsKey("IsScreenTimeoutEnabled"))
            {
                _screenTimeOut = (bool)localSettings.Values["IsScreenTimeoutEnabled"];
            }

            if (localSettings.Values.ContainsKey("SelectedQuality"))
            {
                _selectedQuality = (string)localSettings.Values["SelectedQuality"];
            }

            if (localSettings.Values.ContainsKey("InvidiousInstance"))
            {
                _invidiousInstance = (string)localSettings.Values["InvidiousInstance"];
            }

            if (localSettings.Values.ContainsKey("InvidiousInstanceComments"))
            {
                _invidiousInstanceComments = (string)localSettings.Values["InvidiousInstanceComments"];
            }

            if (localSettings.Values.ContainsKey("ReturnDislikeInstance"))
            {
                _returnDislikeInstance = (string)localSettings.Values["ReturnDislikeInstance"];
            }

        }
    }
}
