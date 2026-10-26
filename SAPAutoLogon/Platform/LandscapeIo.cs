using System;
using System.IO;
using System.Net;
using System.Text;

namespace SAPAutoLogon
{
    /// <summary>
    /// Reads landscape documents from a local path or an http(s) URL.
    /// Read only. Exceptions propagate to the caller (LandscapeLoader handles them).
    /// </summary>
    public static class LandscapeIo
    {
        private const int TimeoutMs = 5000;

        public static string ReadText(string pathOrUrl)
        {
            if (pathOrUrl == null) throw new ArgumentNullException("pathOrUrl");

            if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return ReadUrl(pathOrUrl);
            }

            return File.ReadAllText(pathOrUrl);
        }

        private static string ReadUrl(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = TimeoutMs;
            request.ReadWriteTimeout = TimeoutMs;
            request.UseDefaultCredentials = true;

            using (WebResponse response = request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
