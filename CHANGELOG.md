# Changelog

## 1.4.5.0 - 2026-09-04

Fixed:

- keep the configured Series Mode backdrop visible after opening an episode door; Movie Mode retains its latest-opened-movie backdrop behavior

## 1.4.4.0 - 2026-09-03

Added and fixed:

- add a dedicated Advent Calendar browser-tab icon
- preserve Movie Mode assignments when saving unrelated settings
- support Jellyfin 12 current authorization format; published separately as a beta-tested GitHub prerelease


## 1.4.3.0 - 2026-09-03

Security and documentation:

- restrict custom background, reset, reshuffle, and content-selection endpoints to Jellyfin administrators
- allowlist the custom background filename before serving it
- add a public security policy and document Movie Mode and custom backgrounds


## 1.4.2.0 - 2026-09-02

Fixed:

- store uploaded custom backgrounds as server files instead of plugin configuration data
- restore automatic backgrounds immediately when the custom background is removed
- keep an uploaded custom background visible after Movie Mode doors are opened

## 1.4.1.0 - 2026-09-02

Fixed:

- repaired the admin configuration page after custom background support

## 1.4.0.0 - 2026-09-02

Added:

- shared custom calendar background upload and removal
- Movie Mode can be saved without a configured series or season

## 1.3.3.0 - 2026-09-02

Fixed in this release:

- resetting opened doors also clears each users remembered Movie Mode backdrop

## 1.3.2.0 - 2026-09-02

Fixed in this release:

- after opening a movie, remaining closed doors now sync as a seamless continuation of the selected movie backdrop

## 1.3.1.0 - 2026-09-02

Fixed in this release:

- each unopened Movie Mode door now displays the complete mystery-cinema artwork instead of a cropped page fragment

## 1.3.0.0 - 2026-09-02

Added in this test release:

- Movie Mode with Jellyfin movie library or tag selection
- persisted random movie-to-door assignments and manual reshuffle
- mystery-cinema artwork, movie primary images, and per-user movie backdrops

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
