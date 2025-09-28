using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace naclib
{
    internal class EventLogWatcherHelper
    {
        private EventLogWatcher? watcher = null;
        private string logPath = "Microsoft-Windows-Hyper-V-VMMS-Admin";
        private string logQuery = "*[System[(EventID=13002 or EventID=18304)]]";

        public bool subscribeToEventLog()
        {
            try
            {
                EventLogQuery eventLogQuery = new EventLogQuery(logPath, PathType.LogName, logQuery);

                watcher = new EventLogWatcher(eventLogQuery);

                // Make the watcher listen to the EventRecordWritten
                // events.  When this event happens, the callback method
                // (EventLogEventRead) is called.
                watcher.EventRecordWritten +=
                    new EventHandler<EventRecordWrittenEventArgs>(
                        EventLogEventRead);

                // Activate the subscription
                watcher.Enabled = true;

                //for (int i = 0; i < 5; i++)
                //{
                //    // Wait for events to occur. 
                //    Thread.Sleep(10000);
                //}
                return true;
            }
            catch (EventLogReadingException)
            {
                if (watcher != null)
                {
                    // Stop listening to events
                    watcher.Enabled = false;
                    watcher.Dispose();
                }
                return false;
            }
        }

        private void EventLogEventRead(object? sender, EventRecordWrittenEventArgs e)
        {
            if (e.EventRecord != null)
            {
                // disable the watcher to prevent that the watcher is fired twice
                disableWatcher(watcher);
                string[] xPathRefs = new string[1];
                xPathRefs[0] = "Event/UserData/VmlEventLog/VmId";

                IEnumerable<String> xPathEnum = xPathRefs;

                EventLogPropertySelector logPropertyContext = new EventLogPropertySelector(xPathEnum);

                IList<object> logEventProps = ((EventLogRecord)e.EventRecord).GetPropertyValues(logPropertyContext);

                enableWatcher(watcher);
            }
        }

        private void enableWatcher(EventLogWatcher? watcher)
        {
            if (watcher != null)
            {
                watcher.Enabled = true;
            }
        }

        private void disableWatcher(EventLogWatcher? watcher)
        {
            if (watcher != null)
            {
                watcher.Enabled = false;
            }
        }
    }
}
