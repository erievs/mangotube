using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValleyTube
{
    public static class Settings
    {
        private static bool _isDash = false;

        public const int MaxHistorySize = 52;

        public static bool isDash
        {
            get { return _isDash; }
            set { _isDash = value; }
        }

        static Settings()
        {
            _isDash = false;
        }
    }
}
