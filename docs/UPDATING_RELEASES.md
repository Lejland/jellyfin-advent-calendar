# Updating And Publishing Releases

This guide describes the manual release workflow for the Jellyfin Advent Calendar plugin.

## Prerequisites

- GitHub CLI is authenticated: `gh auth status`
- .NET SDK is available at `/tmp/dotnet-advent-build/dotnet9/dotnet`
- You have made and tested the source changes locally

## 1. Choose A New Version

Use a higher four-part version for every published plugin update, for example `1.1.2.0` to `1.1.3.0`.

Update all three version fields in `Jellyfin.Plugin.AdventCalendar.csproj`:

```xml
<Version>1.1.3.0</Version>
<AssemblyVersion>1.1.3.0</AssemblyVersion>
<FileVersion>1.1.3.0</FileVersion>
```

Add a dated entry at the top of `CHANGELOG.md` that clearly describes the change.

## 2. Build The Plugin

From the repository root, run:

```bash
/tmp/dotnet-advent-build/dotnet9/dotnet build -c Release
```

The build must finish with zero errors. The two files needed for the release are:

```text
bin/Release/net9.0/Jellyfin.Plugin.AdventCalendar.dll
bin/Release/net9.0/Jellyfin.Plugin.AdventCalendar.deps.json
```

Both files must come from the same build. Do not reuse an older `.deps.json` with a newer DLL.

## 3. Create And Validate The Release ZIP

Replace `VERSION` with the new version number:

```bash
zip -j artifacts/release/jellyfin-plugin-advent-calendar_VERSION.zip \
    bin/Release/net9.0/Jellyfin.Plugin.AdventCalendar.dll \
    bin/Release/net9.0/Jellyfin.Plugin.AdventCalendar.deps.json
```

Validate the archive and record its MD5 checksum:

```bash
unzip -t artifacts/release/jellyfin-plugin-advent-calendar_VERSION.zip
md5sum artifacts/release/jellyfin-plugin-advent-calendar_VERSION.zip
```

The ZIP should contain exactly these two files:

```text
Jellyfin.Plugin.AdventCalendar.dll
Jellyfin.Plugin.AdventCalendar.deps.json
```

## 4. Update Repository Metadata

In `artifacts/repository/manifest.json`, add a new object at the top of the `versions` array. Keep all older version entries below it so existing installations can continue to resolve their installed version.

Set:

- `version` to the new version
- `sourceUrl` to `https://github.com/Lejland/jellyfin-advent-calendar/releases/download/vVERSION/jellyfin-plugin-advent-calendar_VERSION.zip`
- `checksum` to the MD5 value from the previous step
- `timestamp` to the current UTC time in ISO 8601 format

Update `artifacts/release/GITHUB_RELEASE_TEMPLATE.md` with the version, ZIP name, checksum, and release highlights. Update `README.md` when its current-release value or user documentation changes.

## 5. Commit And Push The Source

The release ZIP is intentionally ignored by Git. Commit only source, documentation, changelog, and manifest changes:

```bash
git status
git add CHANGELOG.md Jellyfin.Plugin.AdventCalendar.csproj README.md \
    artifacts/repository/manifest.json artifacts/release/GITHUB_RELEASE_TEMPLATE.md \
    Configuration/ Web/ Controllers/ Models/
git commit -m "Describe the release"
git push
```

Review `git status` before committing. Do not add `bin/`, `obj/`, `artifacts/publish/`, temporary folders, local backups, or release ZIPs.

## 6. Publish The GitHub Release

Create a GitHub Release whose tag matches the version:

```bash
gh release create vVERSION \
    artifacts/release/jellyfin-plugin-advent-calendar_VERSION.zip \
    --title "Advent Calendar VERSION" \
    --notes-file artifacts/release/GITHUB_RELEASE_TEMPLATE.md
```

Verify it:

```bash
gh release view vVERSION --repo Lejland/jellyfin-advent-calendar
```

## 7. Verify The Public Manifest

The Jellyfin repository URL is:

```text
https://raw.githubusercontent.com/Lejland/jellyfin-advent-calendar/main/artifacts/repository/manifest.json
```

Open it in a browser or fetch it with `curl`. Confirm the first version entry has the new version, release URL, and MD5 checksum.

GitHub's raw-content cache can take a few minutes to refresh. If a test Jellyfin server still sees the old manifest immediately after a release, refresh the repository later. As a temporary cache-bypass measure, append a query string such as `?v=VERSION` to the repository URL.

## 8. Update Jellyfin

In Jellyfin, go to **Dashboard** -> **Plugins** -> **Repositories**, refresh the repository, then install the update from the plugin catalog or updates page. Restart Jellyfin if requested.
