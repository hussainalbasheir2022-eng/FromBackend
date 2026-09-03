namespace FlutterPlatform.Application.Interfaces;

public record PublishIntent(string Channel, bool Mandatory, string? ReleaseNotes, string ApplicationId, string Version);

public static class PublishIntentStore
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, PublishIntent> Intents = new();

    public static void Set(Guid buildId, PublishIntent intent) => Intents[buildId] = intent;

    public static bool TryTake(Guid buildId, out PublishIntent? intent) => Intents.TryRemove(buildId, out intent);
}
