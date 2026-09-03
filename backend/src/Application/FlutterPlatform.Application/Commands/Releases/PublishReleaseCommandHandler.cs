using MediatR;
using FlutterPlatform.Application.Interfaces;
using FlutterPlatform.Domain.Entities;
using FlutterPlatform.Domain.Interfaces;

namespace FlutterPlatform.Application.Commands.Releases;

public class PublishReleaseCommandHandler : IRequestHandler<PublishReleaseCommand, PublishReleaseResult>
{
    private readonly IRepository<Release> _releases;
    private readonly ISignalRNotifier _notifier;

    public PublishReleaseCommandHandler(IRepository<Release> releases, ISignalRNotifier notifier)
    {
        _releases = releases;
        _notifier = notifier;
    }

    public async Task<PublishReleaseResult> Handle(PublishReleaseCommand request, CancellationToken ct)
    {
        var release = await _releases.GetByIdAsync(request.ReleaseId, ct);
        if (release == null) return new PublishReleaseResult(false, "Release not found");
        if (release.Status == ReleaseStatus.Published)
            return new PublishReleaseResult(false, "Already published");

        release.Status = ReleaseStatus.Published;
        release.Channel = request.Channel;
        release.IsMandatory = request.Mandatory;
        release.ReleaseNotes = request.ReleaseNotes;
        release.PublishedAt = DateTime.UtcNow;
        await _releases.UpdateAsync(release, ct);

        await _notifier.NotifyReleasePublished(release.Id, release.ApplicationId, release.Version, request.Mandatory);

        return new PublishReleaseResult(true, null);
    }
}
