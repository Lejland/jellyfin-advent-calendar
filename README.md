# Jellyfin Advent Calendar Plugin

An unofficial Jellyfin plugin that adds a custom `/adventcalendar` page and turns a TV series into a December advent calendar.

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

- Current release: `1.1.1.0`
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

- `artifacts/release/jellyfin-plugin-advent-calendar_1.1.1.0.zip`

## Deploy

Manual deploy flow used successfully on Wednesday, August 26, 2026:

1. Build the plugin
2. Copy these files to the Jellyfin plugin folder:
   - `Jellyfin.Plugin.AdventCalendar.dll`
   - `Jellyfin.Plugin.AdventCalendar.deps.json`
3. Server plugin path used:
   - `/var/lib/jellyfin/plugins/AdventCalendar/`
4. Fix ownership:
   - `chown jellyfin:jellyfin ...`
5. Restart Jellyfin:
   - `systemctl restart jellyfin`

Useful deployment commands:

```bash
scp bin/Release/net9.0/Jellyfin.Plugin.AdventCalendar.dll \
    bin/Release/net9.0/Jellyfin.Plugin.AdventCalendar.deps.json \
    root@10.45.8.164:/var/lib/jellyfin/plugins/AdventCalendar/
```

```bash
ssh root@10.45.8.164 \
  'chown jellyfin:jellyfin /var/lib/jellyfin/plugins/AdventCalendar/Jellyfin.Plugin.AdventCalendar.dll /var/lib/jellyfin/plugins/AdventCalendar/Jellyfin.Plugin.AdventCalendar.deps.json && systemctl restart jellyfin'
```

## Known Quirk

Jellyfin logs still show:

- `Loaded plugin: Advent Calendar 1.0.0.0`

But the server also logs the real loaded assembly version:

- `Loaded assembly Jellyfin.Plugin.AdventCalendar, Version=1.1.1.0`

This is because the server-side `meta.json` still has `version: 1.0.0.0`. The running DLL is the newer one.

## Files Worth Backing Up

- `PluginConfiguration.xml` on the Jellyfin server
- The whole project folder
- Current local backup:
  `project-saves/jellyfin-advent-calendar_2026-08-26_16-40-29_missing-episode-fix.zip`

## Next Likely Work

- Clean up plugin metadata so Jellyfin displays `1.1.1.0`
- Update server-side `meta.json` when deploying if Jellyfin still displays an older plugin version label
- Improve release packaging and admin documentation further if publishing publicly
