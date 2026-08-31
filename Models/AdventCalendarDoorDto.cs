namespace Jellyfin.Plugin.AdventCalendar.Models;

public sealed class AdventCalendarDoorDto
{
    public int DoorNumber { get; init; }

    public bool IsUnlocked { get; init; }

    public bool IsOpened { get; init; }

    public bool IsAvailable { get; init; }

    public bool RequiresResolution { get; init; } = true;

    public string EpisodeId { get; init; } = string.Empty;

    public string EpisodeTitle { get; init; } = string.Empty;

    public int? SeasonNumber { get; init; }

    public int? EpisodeNumber { get; init; }

    public string PlaybackUrl { get; init; } = string.Empty;

    public string DetailsUrl { get; init; } = string.Empty;

    public string ThumbnailUrl { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
