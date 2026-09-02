# Changelog

## 1.2.0.0 - 2026-09-02

Added in this release:

- configurable recurring date for door 1, defaulting to December 1
- calendar windows can continue into the following month or year while keeping earlier doors available

## 1.1.2.0 - 2026-08-31

Changed in this release:

- admin Save button remains the Jellyfin primary blue action
- Open advent calendar page button is now green
- Close all opened doors button is now red


## 1.1.1.0 - 2026-08-26

Added in this release:

- version bump to force a clearly newer test package
- no intended behavior change from `1.1.0.0`
- packaged specifically to help verify frontend asset refresh on another Jellyfin server

## 1.1.0.0 - 2026-08-26

Added in this release:

- series picker workflow with exact-name validation in the admin page
- season number input with default season `1`
- multiple season support with sequential door mapping across seasons
- missing episode numbers preserved in place instead of shifting later doors
- signed-out user message page instead of a raw unauthorized failure
- visible calendar title plus series and season labels
- per-user opened-door memory for signed-in Jellyfin users
- optional username allowlisting
- fullscreen playback preference in the admin page

## 1.0.0.0 - 2026-08-24

Initial unofficial release.

Included in this release:

- custom `/adventcalendar` route
- season-based advent calendar doors
- optional separate series backdrop source
- remembered opened doors with admin reset
- dual-confirm reset flow
- optional custom global missing-episode message
- themed door layout with backdrop-matched closed doors
- standalone route handling for direct access and subpath installs
- release zip and third-party repository manifest template
