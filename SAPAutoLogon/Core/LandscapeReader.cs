using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SAPAutoLogon
{
    /// <summary>
    /// Parses SAP Logon landscape XML documents (SAPUILandscape.xml and friends)
    /// into <see cref="SapSystem"/> objects.
    /// </summary>
    public static class LandscapeReader
    {
        private static readonly Regex DirectServer = new Regex(@"^(.+):32(\d\d)$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Reads and merges the given documents. Messageservers, Routers and
        /// Services are matched by uuid across all documents. Returns only
        /// SAPGUI services, sorted by name (case-insensitive, stable).
        /// </summary>
        /// <exception cref="System.Xml.XmlException">A document is not valid XML.</exception>
        public static List<SapSystem> Read(IEnumerable<string> xmlDocuments)
        {
            Dictionary<string, XElement> messageServers = new Dictionary<string, XElement>();
            Dictionary<string, XElement> routers = new Dictionary<string, XElement>();
            List<XElement> services = new List<XElement>();
            Dictionary<string, int> serviceIndex = new Dictionary<string, int>();

            foreach (string xml in xmlDocuments)
            {
                XDocument doc = XDocument.Parse(xml);
                foreach (XElement e in doc.Descendants("Messageserver"))
                {
                    string uuid = Attr(e, "uuid");
                    if (uuid.Length > 0) messageServers[uuid] = e;
                }
                foreach (XElement e in doc.Descendants("Router"))
                {
                    string uuid = Attr(e, "uuid");
                    if (uuid.Length > 0) routers[uuid] = e;
                }
                foreach (XElement e in doc.Descendants("Service"))
                {
                    if (Attr(e, "type") != "SAPGUI") continue;
                    string uuid = Attr(e, "uuid");
                    int index;
                    if (uuid.Length > 0 && serviceIndex.TryGetValue(uuid, out index))
                    {
                        services[index] = e; // later document wins, keeps earlier position
                    }
                    else
                    {
                        if (uuid.Length > 0) serviceIndex[uuid] = services.Count;
                        services.Add(e);
                    }
                }
            }

            List<SapSystem> result = new List<SapSystem>();
            foreach (XElement svc in services)
            {
                result.Add(Build(svc, messageServers, routers));
            }

            // OrderBy is stable, so equal names keep document order.
            return result.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static SapSystem Build(XElement svc, Dictionary<string, XElement> messageServers, Dictionary<string, XElement> routers)
        {
            SapSystem s = new SapSystem();
            s.Uuid = Attr(svc, "uuid");
            s.Name = Attr(svc, "name");
            s.SystemId = Attr(svc, "systemid");
            s.Client = Attr(svc, "client");
            s.Language = Attr(svc, "language");
            s.User = Attr(svc, "user");
            s.SncName = Attr(svc, "sncname");

            string sncop = Attr(svc, "sncop");
            s.SncType = sncop == "-1" ? "" : sncop;

            string server = Attr(svc, "server");
            string routerId = Attr(svc, "routerid");

            XElement ms;
            string msid = Attr(svc, "msid");
            if (msid.Length > 0 && messageServers.TryGetValue(msid, out ms))
            {
                s.IsGroup = true;
                s.MessageServer = Attr(ms, "host");
                s.MessageServerPort = Attr(ms, "port");
                s.Group = server;
                string msName = Attr(ms, "name");
                if (msName.Length > 0) s.SystemId = msName;
                if (routerId.Length == 0) routerId = Attr(ms, "routerid");
            }
            else
            {
                Match m = DirectServer.Match(server);
                if (m.Success)
                {
                    s.ApplicationServer = m.Groups[1].Value;
                    s.SystemNumber = m.Groups[2].Value;
                }
                else
                {
                    s.ApplicationServer = server;
                }
            }

            XElement router;
            if (routerId.Length > 0 && routers.TryGetValue(routerId, out router))
            {
                s.SapRouterString = Attr(router, "router");
            }

            return s;
        }

        private static string Attr(XElement e, string name)
        {
            XAttribute a = e.Attribute(name);
            return a == null ? "" : a.Value;
        }
    }
}
