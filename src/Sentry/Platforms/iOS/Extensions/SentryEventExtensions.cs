using Sentry.Internal.Extensions;

namespace Sentry.iOS.Extensions;

internal static class SentryEventExtensions
{
    /*
     * These methods map between a SentryEvent and it's Cocoa counterpart by serializing as JSON into memory on one side,
     * then deserializing back to an object on the other side.  It is not expected to be performant, as this code is only
     * used when a BeforeSend option is set, and then only when an event is captured by the Cocoa SDK (which should be
     * relatively rare).
     *
     * This approach avoids having to write to/from methods for the entire object graph.  However, it's also important to
     * recognize that there's not necessarily a one-to-one mapping available on all objects (even through serialization)
     * between the two SDKs, so some optional details may be lost when roundtripping.  That's generally OK, as this is
     * still better than nothing.  If a specific part of the object graph becomes important to roundtrip, we can consider
     * updating the objects on either side.
     */

    public static SentryEvent ToSentryEvent(this CocoaSdk.SentryEvent sentryEvent, SentryCocoaOptions cocoaOptions)
    {
        using var stream = sentryEvent.ToJsonStream()!;
        stream.Seek(0, SeekOrigin.Begin);

        using var json = JsonDocument.Parse(stream);
        var exception = sentryEvent.Error == null ? null : new NSErrorException(sentryEvent.Error);

        return SentryEvent.FromJson(json.RootElement, exception);
    }

    // public static CocoaSdk.SentryEvent ToCocoaSentryEvent(this SentryEvent sentryEvent, SentryOptions options, SentryCocoaOptions cocoaOptions)
    // {
    //     // var envelope = Envelope.FromEvent(sentryEvent);
    //     //
    //     // using var stream = new MemoryStream();
    //     // envelope.Serialize(stream, options.DiagnosticLogger);
    //     // stream.Seek(0, SeekOrigin.Begin);
    //     //
    //     // using var data = NSData.FromStream(stream)!;
    //     // var cocoaEnvelope = CocoaSdk.PrivateSentrySdkOnly.EnvelopeWithData(data)!;
    //     //
    //     // // No way to get to a SentryEvent from here
    //     // throw new NotImplementedException();
    //
    //     var cocoaEvent = new CocoaSdk.SentryEvent(sentryEvent.Level.ToCocoaSentryLevel())
    //     {
    //         Breadcrumbs = sentryEvent.Breadcrumbs.NullIfEmpty()?.Select(b => b.ToCocoaBreadcrumb()).ToArray(),
    //         // Context = sentryEvent.Contexts.NullIfEmpty()?.ToNSDictionary(o => ??), // TODO
    //         // DebugMeta = new SentryDebugMeta[]
    //         // {
    //         //     // TODO
    //         // },
    //         Dist = sentryEvent.Distribution,
    //         Environment = sentryEvent.Environment,
    //         EventId = sentryEvent.EventId.ToCocoaSentryId(),
    //         // Exceptions = new SentryException[]
    //         // {
    //         //     // TODO
    //         // },
    //         Extra = sentryEvent.Extra.NullIfEmpty()?.ToNSObjectDictionary(),
    //         Fingerprint = sentryEvent.Fingerprint.NullIfEmpty()?.ToArray(),
    //         Logger = sentryEvent.Logger,
    //         Message = sentryEvent.Message?.ToCocoaSentryMessage(),
    //         Modules = sentryEvent.Modules.NullIfEmpty()?.ToNSStringDictionary(),
    //         Platform = sentryEvent.Platform ?? "",
    //         ReleaseName = sentryEvent.Release,
    //         // Request = null, // TODO
    //         Sdk = sentryEvent.Sdk.ToCocoaSdkDictionary(),
    //         ServerName = sentryEvent.ServerName,
    //         // Stacktrace = null, // TODO
    //         Tags = sentryEvent.Tags.NullIfEmpty()?.ToNSStringDictionary(),
    //         // Threads = new CocoaSdk.SentryThread[]
    //         // {
    //         //     // TODO
    //         // },
    //         Timestamp = sentryEvent.Timestamp.ToNSDate(),
    //         User = sentryEvent.User.ToCocoaUser()
    //     };
    //
    //     return cocoaEvent;
    // }
    //
    // private static CocoaSdk.SentryMessage? ToCocoaSentryMessage(this SentryMessage message)
    // {
    //     var formatted = message.Formatted ?? message.Message;
    //     if (formatted == null)
    //     {
    //         return null;
    //     }
    //
    //     var cocoaMessage = new CocoaSdk.SentryMessage(formatted);
    //     cocoaMessage.Message = message.Message;
    //     cocoaMessage.Params = message.Params?.Select(o => o.ToString() ?? "").ToArray();
    //     return cocoaMessage;
    // }
    //
    // private static NSDictionary<NSString, NSObject> ToCocoaSdkDictionary(this SdkVersion sdk)
    // {
    //     var d = new Dictionary<string, object>();
    //
    //     if (!string.IsNullOrEmpty(sdk.Name))
    //     {
    //         d.Add("name", (NSString)sdk.Name);
    //     }
    //
    //     if (!string.IsNullOrEmpty(sdk.Version))
    //     {
    //         d.Add("version", (NSString)sdk.Version);
    //     }
    //
    //     var integrations = NSArray.FromStrings(sdk.Integrations.ToList());
    //     if (integrations.Count > 0)
    //     {
    //         d.Add("integrations", integrations);
    //     }
    //
    //     var packages = NSArray.FromNSObjects(
    //         sdk.Packages.Select(p => new Dictionary<string, string>
    //         {
    //             ["name"] = p.Name,
    //             ["version"] = p.Version
    //         }.ToNSStringDictionary()).ToArray()
    //     );
    //     if (packages.Count > 0)
    //     {
    //         d.Add("packages", packages);
    //     }
    //
    //     return d.ToNSObjectDictionary();
    // }
}
