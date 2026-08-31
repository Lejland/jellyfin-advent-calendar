namespace Jellyfin.Plugin.AdventCalendar.Models;

public sealed class AdventCalendarStateDto
{
    public string Title { get; init; } = string.Empty;

    public string SeriesTitle { get; init; } = string.Empty;

    public string SeasonLabel { get; init; } = string.Empty;

    public string Language { get; init; } = string.Empty;

    public bool IsAuthenticated { get; init; }

    public bool HasAccess { get; init; }

    public bool AutoFullscreen { get; init; }

    public bool DebugUnlockAllDoors { get; init; }

    public int DoorCount { get; init; }

    public int UnlockedDoorCount { get; init; }

    public int OpenedDoorCount { get; init; }

    public string BackgroundImageUrl { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<AdventCalendarDoorDto> Doors { get; init; } = Array.Empty<AdventCalendarDoorDto>();
}
