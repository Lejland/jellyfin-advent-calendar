# GitHub Release Template

## Title

`Advent Calendar 1.4.2.0`

## Release Notes

Advent Calendar is an unofficial Jellyfin plugin that turns a season into a date-based advent calendar page.

### Highlights

- Custom background uploads are stored safely on the Jellyfin server
- An uploaded background remains active in Movie Mode, even after opening doors
- Removing the image restores the automatic series or Movie Mode background

- Custom `/adventcalendar` page
- Per-user remembered opened doors
- Multiple season support without shifting later doors when episode numbers are missing
- Friendly signed-out visitor page
- Optional custom missing-episode message
- Admin reset flow
- Support for direct server access and subpath installs

### Compatibility

- Tested against Jellyfin `10.11.x`
- Current plugin target ABI in repository manifest: `10.11.0.0`

### Install

Manual install:

1. Download `jellyfin-plugin-advent-calendar_1.4.2.0.zip`
2. Extract into Jellyfin plugin folder:
   `.../plugins/AdventCalendar/`
3. Restart Jellyfin

Third-party repository install:

1. Add the hosted repository manifest URL to Jellyfin custom repositories
2. Install `Advent Calendar`
3. Restart Jellyfin if required

### Files

- `jellyfin-plugin-advent-calendar_1.4.2.0.zip`
- `manifest.json`

### Checksum

- MD5: generated when the release package is built
