﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValleyTube
{
    class Utils
    {
        public static string ConvertUnixTimestampToRelativeTime(long unixTimestamp)
        {
         
            DateTimeOffset dateTimeOffset = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset dateTime = dateTimeOffset.AddSeconds(unixTimestamp);
            DateTime dateTimeUtc = dateTime.UtcDateTime;

            TimeSpan timeDifference = DateTime.UtcNow - dateTimeUtc;

            if (timeDifference.TotalSeconds < 60)
            {
                return string.Format("{0} seconds ago", (int)timeDifference.TotalSeconds);
            }
            else if (timeDifference.TotalMinutes < 60)
            {
                return string.Format("{0} minutes ago", (int)timeDifference.TotalMinutes);
            }
            else if (timeDifference.TotalHours < 24)
            {
                return string.Format("{0} hours ago", (int)timeDifference.TotalHours);
            }
            else if (timeDifference.TotalDays < 30)
            {
                return string.Format("{0} days ago", (int)timeDifference.TotalDays);
            }
            else if (timeDifference.TotalDays < 365)
            {
                return string.Format("{0} months ago", (int)(timeDifference.TotalDays / 30));
            }
            else
            {
                return string.Format("{0} years ago", (int)(timeDifference.TotalDays / 365));
            }
        }

        internal static string ConvertUnixTimestampToRelativeTime(string published)
        {
            throw new NotImplementedException();
        }
    }
}
