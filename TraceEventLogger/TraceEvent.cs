using System.Collections.Generic;

namespace TraceEventLogger
{
        /// <summary>
        /// </summary>
    public class TraceEvent
    {
        /// <summary>
        /// The name of the event, as displayed in Trace Viewer
        /// </summary>
        public string name;

        /// <summary>
        /// The event categories. This is a comma separated list of categories for the event. The categories can be used to hide events in the Trace Viewer UI.
        /// </summary>
        public string cat;

        /// <summary>
        /// The event type. This is a single character which changes depending on the type of event being output. The valid values are listed in the table below. We will discuss each phase type below.
        /// </summary>
        // TODO: make this an enum?
        public string ph;

        /// <summary>
        /// The tracing clock timestamp of the event. The timestamps are provided at microsecond granularity.
        /// </summary>
        public uint ts;

        /// <summary>
        /// Optional. The thread clock timestamp of the event. The timestamps are provided at microsecond granularity.
        /// </summary>
        public uint tts;

        /// <summary>
        /// The process ID for the process that output this event.
        /// </summary>
        public string pid;

        /// <summary>
        /// The thread ID for the process that output this event.
        /// </summary>
        public int tid;

        /// <summary>
        /// Any arguments provided for the event. Some of the event types have required argument fields, otherwise, you can put any information you wish in here. The arguments are displayed in Trace Viewer when you view an event in the analysis section.
        /// </summary>
        public Dictionary<string, string> args;

        public string id;
    }
}
