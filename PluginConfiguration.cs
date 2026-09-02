using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AdventCalendar;

public class PluginConfiguration : BasePluginConfiguration
{
    public string Language { get; set; } = "da-DK";

    public string PageTitle { get; set; } = "Julekalender";

    public string SeriesId { get; set; } = string.Empty;

    public string SeriesName { get; set; } = string.Empty;

    public string SeasonId { get; set; } = string.Empty;

    public string SeasonNumbers { get; set; } = "1";

    public int DoorCount { get; set; } = 24;

    public int FirstDoorMonth { get; set; } = 12;

    public int FirstDoorDay { get; set; } = 1;

    public MissingEpisodeBehavior MissingEpisodeBehavior { get; set; } = MissingEpisodeBehavior.DisableDoor;

    public string MissingEpisodeMessage { get; set; } = string.Empty;

    public string AllowedUsernames { get; set; } = string.Empty;

    public bool AutoFullscreen { get; set; } = true;

    public bool DebugUnlockAllDoors { get; set; }

    public int OpenedDoorsYear { get; set; }

    public int[] OpenedDoors { get; set; } = Array.Empty<int>();

    public string OpenedDoorsByUserJson { get; set; } = string.Empty;
}
