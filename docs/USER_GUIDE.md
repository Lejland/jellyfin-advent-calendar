# Advent Calendar User Guide

Advent Calendar turns episodes from a Jellyfin TV series into calendar doors on the public `/adventcalendar` page. Signed-in users can open eligible doors and watch the matching episode.

## Install

### Plugin catalog

1. Open **Dashboard** -> **Plugins** -> **Repositories**.
2. Add this repository URL:

```text
https://raw.githubusercontent.com/Lejland/jellyfin-advent-calendar/main/artifacts/repository/manifest.json
```

3. Open the plugin catalog, find **Advent Calendar**, and select **Install**.
4. Restart Jellyfin if requested.

New releases appear in Jellyfin after the repository refreshes.

### Manual install

Download the ZIP from the [latest release](https://github.com/Lejland/jellyfin-advent-calendar/releases), extract it into your Jellyfin plugin folder, and restart Jellyfin. Keep these files together:

```text
Jellyfin.Plugin.AdventCalendar.dll
Jellyfin.Plugin.AdventCalendar.deps.json
```

## Configure

Open **Dashboard** -> **Plugins** -> **Advent Calendar**. Save after changing settings.

| Setting | Purpose |
| --- | --- |
| Page title | Heading shown on the calendar page. |
| Language | Calendar language code, such as `da-DK`. |
| Series | Jellyfin TV series used as the episode source. |
| Season numbers | Comma-separated sequence, for example `1,2`. Leave blank for season 1. |
| Doors | Number of doors to display. |
| Custom missing episode message | Message shown when no matching episode exists. Supports `{door}` and `{doorNumber}`. |
| Allowed users | Comma-separated Jellyfin usernames. Leave blank for all signed-in users. |
| Try to launch playback in fullscreen | Requests fullscreen when an episode begins. |
| Debug mode: unlock all doors | Opens every door for testing. Disable it for normal use. |

The green **Open advent calendar page** button opens the public page. The red **Close all opened doors** button resets opened-door state for every user for the current year.

## How Doors Work

- Door mapping follows actual episode numbers. A missing episode 9 leaves door 9 missing instead of shifting later episodes.
- With multiple seasons, the next season starts after the highest door used by the preceding season.
- Opened doors are stored separately for each signed-in Jellyfin user.
- Signed-out visitors see a friendly message. Opening a door requires signing in.

## Privacy And Access

The page can be viewed publicly, but door playback requires a signed-in Jellyfin user. The plugin stores opened-door state by normalized Jellyfin username in its plugin configuration. The reset action clears that state for all users for the current year.

## Troubleshooting

### Plugin not in the catalog

Confirm the repository URL, refresh it in Jellyfin, and check that the Jellyfin version matches the plugin ABI in the manifest. A newly published GitHub manifest can take a few minutes to refresh.

### `/adventcalendar` does not open

Confirm the plugin is enabled, restart Jellyfin after installation or update, and verify the configured series and seasons exist.

### A door is missing

Missing episode numbers intentionally remain in their original door position. Add a custom missing-episode message if you want the page to explain the gap.

## Updates

Check **Dashboard** -> **Plugins** for updates, install the new Advent Calendar version, and restart Jellyfin if requested.

## Project Note

This plugin was created 100% with AI. Source, release notes, and issue tracking are available on the [GitHub repository](https://github.com/Lejland/jellyfin-advent-calendar).
