using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.iOS.Extensions;

internal static class BreadcrumbExtensions
{
    public static Breadcrumb ToBreadcrumb(this CocoaSdk.SentryBreadcrumb breadcrumb, IDiagnosticLogger? logger) =>
        new(
            breadcrumb.Timestamp?.ToDateTimeOffset(),
            breadcrumb.Message,
            breadcrumb.Type,
            breadcrumb.Data?.ToStringDictionary(logger).NullIfEmpty(),
            breadcrumb.Category,
            breadcrumb.Level.ToBreadcrumbLevel());

    public static CocoaSdk.SentryBreadcrumb ToCocoaBreadcrumb(this Breadcrumb breadcrumb) =>
        new()
        {
            Timestamp = breadcrumb.Timestamp.ToNSDate(),
            Message = breadcrumb.Message,
            Type = breadcrumb.Type,
            Data = breadcrumb.Data?.NullIfEmpty()?.ToNSObjectDictionary(),
            Category = breadcrumb.Category ?? "",
            Level = breadcrumb.Level.ToCocoaSentryLevel()
        };
}
