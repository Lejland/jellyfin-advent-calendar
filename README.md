# Jellyfin Advent Calendar Plugin

An unofficial Jellyfin plugin that adds a custom `/adventcalendar` page and turns a TV series into a December advent calendar.

## Project Note

This plugin was created 100% with AI.


## What It Does

- Adds a public page at `/adventcalendar`
- Lets admins choose a Jellyfin series and one or more season numbers
- Maps doors to episodes across one or multiple seasons
- Keeps missing episode numbers in place instead of shifting later doors
- Remembers opened doors per signed-in Jellyfin user
- Supports a global missing-episode message
- Supports optional username allowlisting
- Shows a friendly message when a visitor is not signed in
- Includes an admin reset to clear remembered opened doors for all users

## Current Status

- Current release: `1.1.2.0`
- Tested against Jellyfin `10.11.11`


## Admin Setup

Open the plugin settings in Jellyfin and configure:

1. `Page title`
2. `Language`
3. `Series`
4. `Season numbers`
5. `Doors`
6. `Custom missing episode message`
7. `Allowed users`
8. `Try to launch playback in fullscreen`
9. `Debug mode: unlock all doors`

Notes:

- If `Season numbers` is blank, the plugin uses season `1`
- `Season numbers` can be a comma-separated list such as `1,2`
- Multiple seasons are chained in order
- Missing episode numbers stay missing at their original door position
- The admin page contains a quick button to open `/adventcalendar`

## Important Behavior

### Door Mapping

- Season mapping is based on actual episode numbers, not just episode count
- If season 1 has episodes `1-8` and `10-12`, door `9` stays missing
- If season 1 spans 12 doors, then season 2 starts at door `13`

### Opened Doors

- Opened doors are remembered per Jellyfin username
- User A opening door `5` does not open door `5` for User B
- Reset clears remembered opened doors for all users for the current year

### Access Control

- If `Allowed users` is empty, every signed-in user can use the calendar
- If `Allowed users` has names, only those usernames can use it
- If a visitor is not signed in, the page shows a friendly holiday message instead of a raw auth error

### Missing Episode Behavior

- If `Custom missing episode message` is empty, missing doors are disabled
- If `Custom missing episode message` has a value, that message is shown on missing doors
- Supported placeholders are `{door}` and `{doorNumber}`

## Planned Feature

A future release is planned to let administrators choose the date when door 1 becomes available. The default will remain December 1.

## Updating The Project

See [the release update guide](docs/UPDATING_RELEASES.md) for the complete build, packaging, GitHub release, and Jellyfin update workflow.

## Project Layout

- `PluginConfiguration.cs`
  Stores plugin settings and opened-door persistence
- `AdventCalendarService.cs`
  Main logic for access checks, episode mapping, opened doors, and state building
- `Controllers/AdventCalendarController.cs`
  Exposes the calendar page, assets, state, door endpoints, and admin helper endpoints
- `Configuration/configPage.html`
  Jellyfin admin settings page
- `Web/adventcalendar.html`
  Frontend page shell
- `Web/adventcalendar.js`
  Frontend rendering, token handling, door interactions, and video playback
- `Web/adventcalendar.css`
  Frontend layout and styling
- `CHANGELOG.md`
  Release history
- `artifacts/release/`
  Build output zip files for distribution
- `artifacts/repository/manifest.json`
  Template for a third-party Jellyfin plugin repository

## Build

Local build command used successfully:

```bash
/tmp/dotnet-advent-build/dotnet9/dotnet build -c Release
```

Output:

- `bin/Release/net9.0/Jellyfin.Plugin.AdventCalendar.dll`
- `bin/Release/net9.0/Jellyfin.Plugin.AdventCalendar.deps.json`

Release zip currently created:

- `artifacts/release/jellyfin-plugin-advent-calendar_1.1.2.0.zip`

## Install And Updates

Install through Jellyfin instead of copying plugin files manually:

1. Open **Dashboard**, then **Plugins**, then **Repositories**.
2. Add this repository URL:

```text
https://raw.githubusercontent.com/Lejland/jellyfin-advent-calendar/main/artifacts/repository/manifest.json
```

3. Open the plugin catalog, install **Advent Calendar**, then restart Jellyfin if requested.

When a new release is published, Jellyfin will show it under plugin updates. Install the update and restart Jellyfin if requested.

## License

This project is licensed under the [MIT License](LICENSE).
