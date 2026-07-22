using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

using MySettings = SABotSupport.Properties.Settings;

namespace SABotSupport
{
    internal static class UpdateChecker
    {
        private const string VERSION_XML_URL = @"https://raw.githubusercontent.com/Setsu-BHMT/SABotSupport/master/version.xml";

        private static volatile bool isRunning = false;
        private static Version currentVersion = default;

        internal delegate void NewVersionAvailableEventHandler(object sender, EventArgs e);
        internal static event NewVersionAvailableEventHandler NewVersionAvailableEvent = delegate { };  //avoids null check when firing events

        /// <summary>
        /// Checks for updates immediately, and also schedules update checks to be ran every 24 hours.
        /// If CheckForUpdates is false in the application settings then it will do nothing except reschedule for the next day.
        /// </summary>
        internal static void ScheduleUpdates(string version)
        {
            if (isRunning)
                return;

            currentVersion = new(version);

            isRunning = true;
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    CheckForUpdates();
                    await Task.Delay(TimeSpan.FromDays(1)).ConfigureAwait(false);
                }
            });
        }

        internal static void CheckForUpdates()
        {
            if (!MySettings.Default.CheckForUpdates)
                return;

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var doc = XDocument.Load(VERSION_XML_URL);
                var app = doc.Element("SABotSupport");
                var currentVer = app.Element("currentVersion");
                var major = Int32.Parse(currentVer.Element("major").Value);
                var minor = Int32.Parse(currentVer.Element("minor").Value);
                var build = Int32.Parse(currentVer.Element("build").Value);
                var revision = Int32.Parse(currentVer.Element("revision").Value);
                Version serverVersion = new(major, minor, build, revision);

                if (serverVersion > currentVersion)
                {
                    NewVersionAvailableEvent(null, EventArgs.Empty);
                }
            }
            catch (Exception)
            {
#if !RELEASE
                throw;
#else
                //do nothing, just retry the next day
#endif
            }
        }

        internal static List<string> GetValidKeys()
        {
            List<string> output = new();

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var doc = XDocument.Load(VERSION_XML_URL);
                var app = doc.Element("SABotSupport");
                var validKeys = app.Element("validKeys");
                var keys = validKeys.Descendants("key");
                output.AddRange(keys.Select(x => x.Value));
            }
            catch (Exception)
            {
                //do nothing
            }

            return output;
        }
    }
}
