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
            eventSource.BuildStarted += BuildStartedHandler;

            eventSource.ProjectStarted += ProjectStartedHandler;
            eventSource.ProjectFinished += ProjectFinishedHandler;

            eventSource.TargetStarted += TargetStartedHandler;
            eventSource.TargetFinished += TargetFinishedHandler;
        }

        public override void Shutdown()
        {
            using (StreamWriter file = File.CreateText(@"msbuild_events.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, events);
            }
        }

        private void BuildStartedHandler(object sender, BuildStartedEventArgs args)
        {
            firstObservedTime = args.Timestamp;
        }

        private void ProjectStartedHandler(object sender, ProjectStartedEventArgs args)
        {
            var e = new TraceEvent
            {
                name = $"Project \"{args.ProjectFile}\" ({args.BuildEventContext.ProjectInstanceId})",
                ph = "B",
                ts = (uint) (args.Timestamp - firstObservedTime).TotalMilliseconds * 1000,
                tid = args.BuildEventContext.ProjectInstanceId,
                pid = args.BuildEventContext.NodeId,
                args = new Dictionary<string, string> { { "targets", args.TargetNames } },
            };

            events.Add(e);
        }

        private void ProjectFinishedHandler(object sender, ProjectFinishedEventArgs args)
        {
            var e = new TraceEvent
            {
                name = $"Project \"{args.ProjectFile}\" ({args.BuildEventContext.ProjectInstanceId})",
                ph = "E",
                ts = (uint) (args.Timestamp - firstObservedTime).TotalMilliseconds * 1000,
                tid = args.BuildEventContext.ProjectInstanceId,
                pid = args.BuildEventContext.NodeId
            };

            events.Add(e);
        }


        private void TargetStartedHandler(object sender, TargetStartedEventArgs args)
        {
            var e = new TraceEvent
            {
                name =
                    $"Target \"{args.TargetName}\" in project \"{args.ProjectFile}\" ({args.BuildEventContext.ProjectInstanceId})",
                ph = "B",
                ts = (uint) (args.Timestamp - firstObservedTime).TotalMilliseconds * 1000,
                tid = args.BuildEventContext.ProjectInstanceId,
                pid = args.BuildEventContext.NodeId
            };

            events.Add(e);
        }

        private void TargetFinishedHandler(object sender, TargetFinishedEventArgs args)
        {
            var e = new TraceEvent
            {
                name =
                    $"Target \"{args.TargetName}\" in project \"{args.ProjectFile}\" ({args.BuildEventContext.ProjectInstanceId})",
                ph = "E",
                ts = (uint) (args.Timestamp - firstObservedTime).TotalMilliseconds * 1000,
                tid = args.BuildEventContext.ProjectInstanceId,
                pid = args.BuildEventContext.NodeId
            };

            events.Add(e);
        }
    }
}
