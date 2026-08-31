namespace Jellyfin.Plugin.AdventCalendar;

public enum MissingEpisodeBehavior
{
    DisableDoor = 0,
    HideDoor = 1,
    UseNextAvailableEpisode = 2,
    ShowMessageOnly = 3
}
