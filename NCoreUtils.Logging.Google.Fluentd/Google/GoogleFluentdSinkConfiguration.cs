using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Logging.Google.Internal;

namespace NCoreUtils.Logging.Google
{
    public class GoogleFluentdSinkConfiguration : IGoogleFluentdSinkConfiguration
    {
        private string? _logName;

        private string _projectId = string.Empty;

        private string _service = string.Empty;

        public string ProjectId
        {
            get => _projectId;
            set
            {
                _projectId = value;
                _logName = default;
            }
        }

        public string Service
        {
            get => _service;
            set
            {
                _service = value;
                _logName = default;
            }
        }

        public string? ServiceVersion { get; set; }

        public string LogName
        {
            get
            {
                _logName ??= Fmt.LogName(ProjectId, Service);
                return _logName;
            }
        }

        public string Output { get; set; } = DefaultByteSequenceOutput.StdOut;

        public CategoryHandling CategoryHandling { get; set; }

        public EventIdHandling EventIdHandling { get; set; }

        public TraceHandling TraceHandling { get; set; }
    }
}