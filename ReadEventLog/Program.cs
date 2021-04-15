
using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace ReadEventLog
{
    class Program
    {
        private static void PrintEntry(EventLog log, EventLogEntry entry)
        {
            Console.WriteLine("Event Raised: |Log:{0}|Source:{1}|InstanceId:{2}|", log.Log, entry.Source, entry.InstanceId);
        }

        private class Listener : IDisposable
        {
            private string log;
            private EventLogWatcher watcher;

            public Listener(string logName, string query)
            {
                this.log = logName;
                var eventQuery = new EventLogQuery(logName, PathType.LogName, query);
                this.watcher = new EventLogWatcher(eventQuery);

                this.watcher.EventRecordWritten += Watcher_EventRecordWritten;
                this.watcher.Enabled = true;
            }

            ~Listener()
            {
                this.Dispose();
            }

            public void Dispose()
            {
                this.watcher.EventRecordWritten -= this.Watcher_EventRecordWritten;
            }

            private void Watcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
            {
                if (e.EventException != null)
                {
                    Console.Error.WriteLine("Failed to listen on event log {0}", e.EventException.Message);
                    // Try to re-subscribe etc or just bail out and notify the user.
                }
                var record = e.EventRecord;
                if (record != null)
                {
                    var timestamp = record.TimeCreated.ToString();
                    Console.Out.WriteLine("Event: {0} |Log:{1}|Provider:{2}|EventID:{3}|", timestamp, this.log, record.ProviderId?.ToString() ?? this.log, record.RecordId ?? record.Id);
                    Console.Out.WriteLine("    " + record.FormatDescription());
                }
            }
        }

        static void Main(string[] args)
        {
            {
                Console.Out.WriteLine("Available Logs:");
                var session = new EventLogSession();
                foreach (var log in session.GetLogNames())
                {
                    Console.Out.WriteLine("    " + log);
                }
                Console.Out.WriteLine();
            }

            if (!EventLog.SourceExists("TestEventLog"))
            {
                EventSourceCreationData eventSourceData = new EventSourceCreationData("TestEventLog", "TestEventLog");
                EventLog.CreateEventSource(eventSourceData);
            }

            using (var testEventLogListener = new Listener("TestEventLog", "*"))
            using (var applicationListener = new Listener("Application", "*"))
            using (var testEventLogger = new EventLog("TestEventLog", ".", "TestEventLog"))
            using (var timer = new System.Threading.Timer(
                _ => testEventLogger.WriteEntry("Test message", EventLogEntryType.Information),
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1)
            ))
            {
                while (Console.ReadLine().Trim().ToLower() != "exit")
                {
                    Console.WriteLine("  exit↵ to Exit");
                }
            }
        }        
    }
}
