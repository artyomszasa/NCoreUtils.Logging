using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Logging
{
    public static class ServiceProviderByteSequenceOutputExtensions
    {
        public sealed class CompositeByteSequenceOutputFactory
        {
            private IEnumerable<IByteSequenceOutputFactory>? Factories { get; }

            public CompositeByteSequenceOutputFactory(IEnumerable<IByteSequenceOutputFactory>? factories = default)
            {
                Factories = factories;
            }

            public bool TryCreateByteSequenceOutput(
                string uriOrName,
                [MaybeNullWhen(false)] out IByteSequenceOutput output)
            {
                if (Factories is null)
                {
                    output = default;
                    return false;
                }
                foreach (var factory in Factories)
                {
                    if (factory.TryCreate(uriOrName, out var o))
                    {
                        output = o;
                        return true;
                    }
                }
                output = default;
                return false;
            }
        }

        public static bool TryCreateByteSequenceOutput(
            this IServiceProvider serviceProvider,
            string uriOrName,
            [MaybeNullWhen(false)] out IByteSequenceOutput output)
            => ActivatorUtilities.CreateInstance<CompositeByteSequenceOutputFactory>(serviceProvider)
                .TryCreateByteSequenceOutput(uriOrName, out output);

        public static IByteSequenceOutput CreateByteSequenceOutput(
            this IServiceProvider serviceProvider,
            string uriOrName)
        {
            if (serviceProvider.TryCreateByteSequenceOutput(uriOrName, out var output))
            {
                return output;
            }
            return DefaultByteSequenceOutput.Create(uriOrName);
        }
    }
}