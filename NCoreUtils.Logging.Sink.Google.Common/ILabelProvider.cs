using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging.Google
{
    public interface ILabelProvider
    {
        void UpdateLabels(string category, EventId eventId, LogLevel logLevel, in WebContext context, IDictionary<string, string> labels);
    }
}