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

        private readonly Dictionary<int, TaskStartedEventArgs> msbuildStartEvents =
            new Dictionary<int, TaskStartedEventArgs>();

        public override void Initialize(IEventSource eventSource)
        {
            eventSource.BuildStarted += BuildStartedHandler;

            eventSource.ProjectStarted += ProjectStartedHandler;
            eventSource.ProjectFinished += ProjectFinishedHandler;

            eventSource.TargetStarted += TargetStartedHandler;
            eventSource.TargetFinished += TargetFinishedHandler;

            eventSource.TaskStarted += TaskStartedHandler;
            eventSource.TaskFinished += TaskFinishedHandler;

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
                ts = (args.Timestamp - firstObservedTime).TotalMicroseconds(),
                tid = args.BuildEventContext.ProjectInstanceId,
                pid = args.BuildEventContext.NodeId,
                args = new Dictionary<string, string> {{"targets", args.TargetNames}},
            };

            events.Add(e);

            if (msbuildStartEvents.TryGetValue(args.ParentProjectBuildEventContext.ProjectInstanceId,
                out var callingMsbuildTaskInvocation))
            {
                var fs = new TraceEvent
                {
                    cat = "p2p",
                    name = $"MSBuild \"{args.ProjectFile}\"",
                    ph = "s",
                    ts = (callingMsbuildTaskInvocation.Timestamp - firstObservedTime).TotalMicroseconds() + 1,
                    tid = args.ParentProjectBuildEventContext.ProjectInstanceId,
                    pid = args.ParentProjectBuildEventContext.NodeId,
                    args = new Dictionary<string, string> {{"targets", args.TargetNames}},
                    id = args.BuildEventContext.BuildRequestId.ToString(),
                };

                events.Add(fs);

                var ff = new TraceEvent
                {
                    cat = "p2p",
                    name = $"MSBuild \"{args.ProjectFile}\"",
                    ph = "f",
                    ts = (args.Timestamp - firstObservedTime).TotalMicroseconds() + 1,
                    tid = args.BuildEventContext.ProjectInstanceId,
                    pid = args.BuildEventContext.NodeId,
                    args = new Dictionary<string, string> {{"targets", args.TargetNames}},
                    id = args.BuildEventContext.BuildRequestId.ToString(),
                };

                events.Add(ff);
            }
        }

        private void ProjectFinishedHandler(object sender, ProjectFinishedEventArgs args)
        {
            var e = new TraceEvent
            {
                name = $"Project \"{args.ProjectFile}\" ({args.BuildEventContext.ProjectInstanceId})",
                ph = "E",
                ts = (args.Timestamp - firstObservedTime).TotalMicroseconds(),
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
                ts = (args.Timestamp - firstObservedTime).TotalMicroseconds(),
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
                ts = (args.Timestamp - firstObservedTime).TotalMicroseconds(),
                tid = args.BuildEventContext.ProjectInstanceId,
                pid = args.BuildEventContext.NodeId
            };

            events.Add(e);
        }

        private void TaskStartedHandler(object sender, TaskStartedEventArgs args)
        {
            if (args.TaskName.EndsWith("MSBuild"))
            {
                msbuildStartEvents[args.BuildEventContext.ProjectInstanceId] = args;

                var e = new TraceEvent
                {
                    name =
                        $"MSBuild (yielded) in project \"{args.ProjectFile}\" ({args.BuildEventContext.ProjectInstanceId})",
                    ph = "B",
                    ts = (args.Timestamp - firstObservedTime).TotalMicroseconds(),
                    tid = args.BuildEventContext.ProjectInstanceId,
                    pid = args.BuildEventContext.NodeId
                };

                events.Add(e);
            }
        }

        private void TaskFinishedHandler(object sender, TaskFinishedEventArgs args)
        {
            if (args.TaskName.EndsWith("MSBuild"))
            {
                var e = new TraceEvent
                {
                    name =
                        $"MSBuild (yielded) in project \"{args.ProjectFile}\" ({args.BuildEventContext.ProjectInstanceId})",
                    ph = "E",
                    ts = (args.Timestamp - firstObservedTime).TotalMicroseconds(),
                    tid = args.BuildEventContext.ProjectInstanceId,
                    pid = args.BuildEventContext.NodeId
                };

                events.Add(e);
            }
        }

    }

    static class TimeSpanExtensions
    {
        public static uint TotalMicroseconds(this TimeSpan ts)
        {
            return Convert.ToUInt32(ts.TotalMilliseconds * 1_000);
        }
    }
}
