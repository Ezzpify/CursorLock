﻿using System.Net;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace CursorLock
{
    class UpdateCheck
    {
        public static bool IsUpdateAvailable()
        {
            var reg = new Regex(@"^[0-9.]+$");

            string version = DownloadString(Const.VERSION_FILE);
            if (!string.IsNullOrWhiteSpace(version) && reg.IsMatch(version))
                return version != Application.ProductVersion;

            return false;
        }

        private static string DownloadString(string url)
        {
            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36");
                    return wc.DownloadString(url);
                }
                catch
                {
                    return string.Empty;
                }
            }
        }
    }
}
