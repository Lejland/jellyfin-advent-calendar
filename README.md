<p align="center">
  <img src="assets/advent-calendar-social.png" alt="Festive advent calendar doors with cinema-inspired lighting" width="860">
</p>

# Jellyfin Advent Calendar Plugin

An unofficial Jellyfin plugin that adds a custom `/adventcalendar` page and turns a TV series into a December advent calendar.

**Open a door each day and watch the matching episode directly in Jellyfin.**

## Documentation

- [User guide](docs/USER_GUIDE.md): installation, configuration, access, and troubleshooting
- [Release history](CHANGELOG.md): changes in each version

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

- Current release: `1.2.0.0`
- Tested against Jellyfin `10.11.11`


## Quick Configuration

Follow the [user guide](docs/USER_GUIDE.md) for the complete settings reference, including seasons, missing episodes, allowed users, fullscreen playback, and resets.

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
