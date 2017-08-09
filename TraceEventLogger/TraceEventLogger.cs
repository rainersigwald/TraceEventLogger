using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;

namespace TraceEventLogger
{
    public class TraceEventLogger : Logger
    {
        private DateTime firstObservedTime;

        private readonly List<TraceEvent> events = new List<TraceEvent>();

        public override void Initialize(IEventSource eventSource)
        {
            eventSource.ProjectStarted += ProjectStartedHandler;
            eventSource.ProjectFinished += ProjectFinishedHandler;
        }

        public override void Shutdown()
        {
            using (StreamWriter file = File.CreateText(@"msbuild_events.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, events);
            }
        }

        private void ProjectStartedHandler(object sender, ProjectStartedEventArgs args)
        {
            if (firstObservedTime == null)
            {
                firstObservedTime = args.Timestamp;
            }

            var e = new TraceEvent();
            e.name = $"Project \"{args.ProjectFile}\" ({args.ProjectId}) started";
            e.ph = "B";
            e.ts = (uint) (args.Timestamp - firstObservedTime).TotalMilliseconds;
            e.tid = args.ThreadId;

            events.Add(e);
        }

        private void ProjectFinishedHandler(object sender, ProjectFinishedEventArgs args)
        {
            var e = new TraceEvent();
            e.name = $"Project \"{args.ProjectFile}\" finished";
            e.ph = "E";
            e.ts = (uint) (args.Timestamp - firstObservedTime).TotalMilliseconds;
            e.tid = args.ThreadId;

            events.Add(e);

        }
    }
}
