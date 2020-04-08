using System.Collections.Generic;
using System.Text;

namespace NCoreUtils.AspNetCore
{
    public class GoogleAspNetCoreLoggingConfiguration : IGoogleAspNetCoreLoggingConfiguration
    {
        IReadOnlyDictionary<string, string>? IGoogleAspNetCoreLoggingConfiguration.ResourceLabels => ResourceLabels;

        public string? ProjectId { get; set; }

        public string? ServiceName { get; set; }

        public string? ServiceVersion { get; set; }

        public string? ResourceType { get; set; }

        public Dictionary<string, string> ResourceLabels { get; } = new Dictionary<string, string>();

        public CategoryHandling CategoryHandling { get; set; }

        public EventIdHandling EventIdHandling { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append('[');
            var first = true;
            if (!string.IsNullOrEmpty(ProjectId))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(", ");
                }
                builder.Append(nameof(ProjectId)).Append(" = ").Append(ProjectId);
            }
            if (!string.IsNullOrEmpty(ServiceName))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(", ");
                }
                builder.Append(nameof(ServiceName)).Append(" = ").Append(ServiceName);
            }
            if (!string.IsNullOrEmpty(ServiceVersion))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(", ");
                }
                builder.Append(nameof(ServiceVersion)).Append(" = ").Append(ServiceVersion);
            }
            if (!string.IsNullOrEmpty(ResourceType))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(", ");
                }
                builder.Append(nameof(ResourceType)).Append(" = ").Append(ResourceType);
            }
            if (!first)
            {
                builder.Append(", ").Append(nameof(CategoryHandling)).Append(" = ").Append(CategoryHandling.ToString());
            }
            builder.Append(", ").Append(nameof(EventIdHandling)).Append(" = ").Append(EventIdHandling.ToString());
            builder.Append(']');
            return builder.ToString();
        }
    }
}