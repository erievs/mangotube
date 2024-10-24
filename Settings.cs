using System;
using Windows.Storage;

namespace ValleyTube
{
    public static class Settings
    {
        private static bool _isDash;
        private static bool _isSponserBlock;
        private static bool _isAutoplayEnabled;

        public static string _selectedQuality;

        private static bool _screenTimeOut;
        private static bool _doubleTapToSkip;
        private static bool _showSponserSkipMessage;
        private static bool _useFormatStreamForDownloads;

        private static string _invidiousInstance = "https://inv.nadeko.net";
        private static string _invidiousInstanceComments = "https://inv.nadeko.net";
        private static string _returnDislikeInstance = "https://returnyoutubedislikeapi.com";
        private static string _SponserBlockInstance = "https://sponsor.ajay.app";

        private static string accessToken = "";

        private static string _clientId = "";

        private static string _clientSecret = ""; // shhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh

        private static string _youTubeAPIKey = "";

        private static string _scope = "https://www.googleapis.com/auth/youtube.force-ssl";

        private static string _redirectUri = "http://localhost";

        // dumbass devs MAKE SURE to remove your own before release 

        public static string version = "Beta 1.3.0";

        public const int MaxHistorySize = 500;

        private static int _recommendVideoLimit = 25;

        private static int _howManySubbedVideosToFetch = 5;

        public static bool isDash
        {
            get { return _isDash; }
            set
            {
                _isDash = value;
                SaveSetting("isDash", value);
            }
        }

        public static bool isSponserBlock
        {
            get { return _isSponserBlock; }
            set
          {
                _isSponserBlock = value;
                SaveSetting("isSponserBlock", value);
            }  
        }

        public static string AccessToken
        {
            get { return accessToken; }
            set
            {
                accessToken = value;
                SaveSetting("AccessToken", value);
            }
        }

        public static string ClientId
        {
            get { return _clientId; }
            set
            {
                _clientId = value;
                SaveSetting("ClientId", value);
            }
        }

        public static string Scope
        {
            get { return _scope; }
            set
            {
                _scope = value;
                SaveSetting("Scope", value);
            }
        }

        public static string ClientSecret
        {
            get { return _clientSecret; }
            set
            {
                _clientSecret = value;
                SaveSetting("ClientSecret", value);
            }
        }

        public static string RedirectUri
        {
            get { return _redirectUri; }
            set
            {
                _redirectUri = value;
                SaveSetting("RedirectUri", value);
            }
        }

        public static bool showSponserSkipMessage
        {
            get { return _showSponserSkipMessage; }
            set
            {
                _showSponserSkipMessage = value;
                SaveSetting("showSponserSkipMessage", value);
            }
        }

        public static bool useFormatStreamForDownloads
        {
            get { return _useFormatStreamForDownloads; }
            set
            {
                _useFormatStreamForDownloads = value;
                SaveSetting("useFormatStreamForDownloads", value);
            }
        }

        public static bool doubleTapToSkip
        {
            get { return _doubleTapToSkip; }
            set
            {
                _doubleTapToSkip = value;
                SaveSetting("doubleTapToSkip", value);
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

        public static string YouTubeAPIKey
        {
            get { return _youTubeAPIKey; }
            set
            {
                _youTubeAPIKey = value;
                SaveSetting("YouTubeAPIKey", value);
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

        public static string SponserBlockInstance
        {
            get { return _SponserBlockInstance; }
            set
            {
                _SponserBlockInstance = value;
                SaveSetting("SponserBlockInstance", value);
            }
        }

        public static int RecommendVideoLimit
        {
            get { return _recommendVideoLimit; }
            set
            {
                _recommendVideoLimit = value;
                SaveSetting("RecommendVideoLimit", value);
            }
        }

        public static int HowManySubbedVideosToFetch
        {
            get { return _howManySubbedVideosToFetch; }
            set
            {
                _recommendVideoLimit = value;
                SaveSetting("HowManySubbedVideosToFetch", value);
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

            if (localSettings.Values.ContainsKey("isSponserBlock"))
            {
                _isSponserBlock = (bool)localSettings.Values["isSponserBlock"];
            }

            if (localSettings.Values.ContainsKey("useFormatStreamForDownloads"))
            {
                _useFormatStreamForDownloads = (bool)localSettings.Values["useFormatStreamForDownloads"];
            }

            if (localSettings.Values.ContainsKey("showSponserSkipMessage"))
            {
                _showSponserSkipMessage = (bool)localSettings.Values["showSponserSkipMessage"]; 
            } 

            if (localSettings.Values.ContainsKey("doubleTapToSkip"))
            {
                _doubleTapToSkip = (bool)localSettings.Values["doubleTapToSkip"]; 
            }
            
            if (localSettings.Values.ContainsKey("InvidiousInstance"))
            {
                _invidiousInstance = (string)localSettings.Values["InvidiousInstance"];
            }

            if (localSettings.Values.ContainsKey("SponserBlockInstance"))
            {
                _SponserBlockInstance = (string)localSettings.Values["SponserBlockInstance"];
            }

            if (localSettings.Values.ContainsKey("InvidiousInstanceComments"))
            {
                _invidiousInstanceComments = (string)localSettings.Values["InvidiousInstanceComments"];
            }

            if (localSettings.Values.ContainsKey("ReturnDislikeInstance"))
            {
                _returnDislikeInstance = (string)localSettings.Values["ReturnDislikeInstance"];
            }

            if (localSettings.Values.ContainsKey("RecommendVideoLimit"))
            {
                _recommendVideoLimit = (int)localSettings.Values["RecommendVideoLimit"];
            }

            if (localSettings.Values.ContainsKey("HowManySubbedVideosToFetch"))
            {
                _howManySubbedVideosToFetch = (int)localSettings.Values["HowManySubbedVideosToFetch"];
            }

            if (localSettings.Values.ContainsKey("YouTubeAPIKey"))
            {
                _youTubeAPIKey = (string)localSettings.Values["YouTubeAPIKey"];
            }

            if (localSettings.Values.ContainsKey("AccessToken"))
            {
                accessToken = (string)localSettings.Values["AccessToken"];
            }

            if (localSettings.Values.ContainsKey("ClientId"))
            {
                _clientId = (string)localSettings.Values["ClientId"];
            }

            if (localSettings.Values.ContainsKey("ClientSecret"))
            {
                _clientSecret = (string)localSettings.Values["ClientSecret"];
            }

            if (localSettings.Values.ContainsKey("RedirectUri"))
            {
                _redirectUri = (string)localSettings.Values["RedirectUri"];
            }

            if (localSettings.Values.ContainsKey("Scope"))
            {
                _scope = (string)localSettings.Values["Scope"];
            }

        }
    }
}

