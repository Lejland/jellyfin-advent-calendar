using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AdventCalendar.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AdventCalendar;

public sealed class AdventCalendarService
{
    private static readonly Regex GuidTokenRegex = new(@"[0-9a-fA-F]{32}|[0-9a-fA-F\-]{36}", RegexOptions.Compiled);
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<AdventCalendarService> _logger;

    public AdventCalendarService(ILibraryManager libraryManager, ILogger<AdventCalendarService> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public AdventCalendarStateDto BuildState(bool isAuthenticated, string? currentUsername, string pathBase)
    {
        var config = Plugin.Instance.Configuration;
        var effectiveMissingEpisodeBehavior = GetEffectiveMissingEpisodeBehavior(config);
        var safeDoorCount = Math.Clamp(config.DoorCount, 1, 31);
        var now = DateTime.Now;
        var unlockedDoorCount = GetUnlockedDoorCount(now, safeDoorCount, config.DebugUnlockAllDoors, config.FirstDoorMonth, config.FirstDoorDay);
        var calendarYear = GetCalendarYear(now, safeDoorCount, config.DebugUnlockAllDoors, config.FirstDoorMonth, config.FirstDoorDay);
        var openedDoors = GetOpenedDoorsForYear(config, calendarYear, currentUsername);
        var resolvedCalendar = ResolveConfiguredCalendar(config, effectiveMissingEpisodeBehavior, safeDoorCount, pathBase);
        if (config.MovieModeEnabled && TryGetLastOpenedMovieDoor(config, currentUsername, out var lastMovieDoor)
            && resolvedCalendar.EpisodesByDoor.TryGetValue(lastMovieDoor, out var lastMovie))
        {
            resolvedCalendar.BackgroundImageUrl = BuildItemBackdropUrl(pathBase, lastMovie);
        }
        else if (config.MovieModeEnabled && string.IsNullOrWhiteSpace(resolvedCalendar.BackgroundImageUrl))
        {
            resolvedCalendar.BackgroundImageUrl = BuildRelativeUrl(pathBase, "/adventcalendar/assets/movie-mystery-cinema.png");
        }

        var title = string.IsNullOrWhiteSpace(config.PageTitle)
            ? resolvedCalendar.SeriesTitle
            : config.PageTitle;

        if (!isAuthenticated)
        {
            return new AdventCalendarStateDto
            {
                Title = title,
                SeriesTitle = resolvedCalendar.SeriesTitle,
                SeasonLabel = resolvedCalendar.SeasonLabel,
                Language = config.Language,
                IsAuthenticated = false,
                HasAccess = false,
                AutoFullscreen = config.AutoFullscreen,
                DebugUnlockAllDoors = config.DebugUnlockAllDoors,
                MovieModeEnabled = config.MovieModeEnabled,
                DoorCount = safeDoorCount,
                UnlockedDoorCount = 0,
                OpenedDoorCount = 0,
                BackgroundImageUrl = resolvedCalendar.BackgroundImageUrl,
                Message = "Sign in to Jellyfin to open the advent calendar. Happy holidays."
            };
        }

        if (!HasAccess(config, currentUsername))
        {
            return new AdventCalendarStateDto
            {
                Title = title,
                SeriesTitle = resolvedCalendar.SeriesTitle,
                SeasonLabel = resolvedCalendar.SeasonLabel,
                Language = config.Language,
                IsAuthenticated = true,
                HasAccess = false,
                AutoFullscreen = config.AutoFullscreen,
                DebugUnlockAllDoors = config.DebugUnlockAllDoors,
                MovieModeEnabled = config.MovieModeEnabled,
                DoorCount = safeDoorCount,
                UnlockedDoorCount = 0,
                OpenedDoorCount = 0,
                BackgroundImageUrl = resolvedCalendar.BackgroundImageUrl,
                Message = "This Jellyfin user does not have access to the advent calendar."
            };
        }

        if (!resolvedCalendar.IsConfigured)
        {
            return new AdventCalendarStateDto
            {
                Title = title,
                SeriesTitle = resolvedCalendar.SeriesTitle,
                SeasonLabel = resolvedCalendar.SeasonLabel,
                Language = config.Language,
                IsAuthenticated = true,
                HasAccess = true,
                AutoFullscreen = config.AutoFullscreen,
                DebugUnlockAllDoors = config.DebugUnlockAllDoors,
                MovieModeEnabled = config.MovieModeEnabled,
                DoorCount = safeDoorCount,
                UnlockedDoorCount = unlockedDoorCount,
                OpenedDoorCount = openedDoors.Count,
                BackgroundImageUrl = resolvedCalendar.BackgroundImageUrl,
                Message = resolvedCalendar.Message
            };
        }

        return new AdventCalendarStateDto
        {
            Title = title,
            SeriesTitle = resolvedCalendar.SeriesTitle,
            SeasonLabel = resolvedCalendar.SeasonLabel,
            Language = config.Language,
            IsAuthenticated = true,
            HasAccess = true,
            AutoFullscreen = config.AutoFullscreen,
            DebugUnlockAllDoors = config.DebugUnlockAllDoors,
            DoorCount = safeDoorCount,
            UnlockedDoorCount = unlockedDoorCount,
            OpenedDoorCount = openedDoors.Count,
            BackgroundImageUrl = resolvedCalendar.BackgroundImageUrl,
            Message = config.DebugUnlockAllDoors ? "Debug mode is enabled. All doors are open." : string.Empty,
            Doors = BuildDoorShells(safeDoorCount, unlockedDoorCount, openedDoors, resolvedCalendar.EpisodesByDoor, pathBase)
        };
    }

    public AdventCalendarDoorDto ResolveDoor(string? currentUsername, string pathBase, int doorNumber)
    {
        var config = Plugin.Instance.Configuration;
        var effectiveMissingEpisodeBehavior = GetEffectiveMissingEpisodeBehavior(config);
        var safeDoorCount = Math.Clamp(config.DoorCount, 1, 31);
        var now = DateTime.Now;
        var unlockedDoorCount = GetUnlockedDoorCount(now, safeDoorCount, config.DebugUnlockAllDoors, config.FirstDoorMonth, config.FirstDoorDay);
        var calendarYear = GetCalendarYear(now, safeDoorCount, config.DebugUnlockAllDoors, config.FirstDoorMonth, config.FirstDoorDay);

        if (!HasAccess(config, currentUsername))
        {
            return CreateUnavailableDoor(doorNumber, false, "This Jellyfin user does not have access to the advent calendar.");
        }

        if (doorNumber < 1 || doorNumber > safeDoorCount)
        {
            return CreateUnavailableDoor(doorNumber, false, "That door does not exist.");
        }

        if (doorNumber > unlockedDoorCount)
        {
            return CreateUnavailableDoor(doorNumber, false, "This door is not open yet.");
        }

        var resolvedCalendar = ResolveConfiguredCalendar(config, effectiveMissingEpisodeBehavior, safeDoorCount, pathBase);
        if (!resolvedCalendar.IsConfigured)
        {
            return CreateUnavailableDoor(doorNumber, true, resolvedCalendar.Message);
        }

        if (!resolvedCalendar.EpisodesByDoor.TryGetValue(doorNumber, out var episode))
        {
            return CreateUnavailableDoor(doorNumber, true, BuildMissingEpisodeMessage(config, doorNumber));
        }

        MarkDoorAsOpened(config, calendarYear, currentUsername, doorNumber);
        if (config.MovieModeEnabled)
        {
            MarkLastOpenedMovieDoor(config, currentUsername, doorNumber);
        }

        return new AdventCalendarDoorDto
        {
            DoorNumber = doorNumber,
            IsUnlocked = true,
            IsOpened = true,
            IsAvailable = true,
            RequiresResolution = false,
            EpisodeId = episode.Id.ToString("N"),
            EpisodeTitle = episode.Name,
            SeasonNumber = episode.ParentIndexNumber,
            EpisodeNumber = episode.IndexNumber,
            PlaybackUrl = BuildPlaybackUrl(pathBase, episode),
            DetailsUrl = BuildDetailsUrl(pathBase, episode),
            ThumbnailUrl = BuildThumbnailUrl(pathBase, episode),
            BackdropUrl = BuildItemBackdropUrl(pathBase, episode)
        };
    }

    public void ResetOpenedDoors()
    {
        var config = Plugin.Instance.Configuration;
        var now = DateTime.Now;
        var safeDoorCount = Math.Clamp(config.DoorCount, 1, 31);
        config.OpenedDoorsYear = GetCalendarYear(now, safeDoorCount, config.DebugUnlockAllDoors, config.FirstDoorMonth, config.FirstDoorDay);
        config.OpenedDoors = Array.Empty<int>();
        config.OpenedDoorsByUserJson = string.Empty;
        Plugin.Instance.SaveConfiguration();
    }

    public IReadOnlyList<object> FindSeries(string? query)
    {
        var trimmedQuery = query?.Trim() ?? string.Empty;
        return GetAllSeries()
            .Where(item => string.IsNullOrWhiteSpace(trimmedQuery)
                || item.Name.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.SortName ?? item.Name)
            .Take(50)
            .Select(item => new
            {
                id = item.Id.ToString("N"),
                name = item.Name,
                productionYear = item.ProductionYear
            })
            .Cast<object>()
            .ToList();
    }

    public object? GetSeriesDetails(string seriesId)
    {
        if (!TryGetSeriesById(seriesId, out var series))
        {
            return null;
        }

        return new
        {
            id = series.Id.ToString("N"),
            name = series.Name,
            productionYear = series.ProductionYear
        };
    }

    public IReadOnlyList<object> GetSeriesSeasons(string seriesId)
    {
        if (!TryGetSeriesById(seriesId, out var series))
        {
            return Array.Empty<object>();
        }

        return GetSeasons(series)
            .Select(season => new
            {
                id = season.Id.ToString("N"),
                number = season.IndexNumber ?? 0,
                name = season.Name,
                episodeCount = GetEpisodes(season).Count
            })
            .Cast<object>()
            .ToList();
    }

    private IReadOnlyList<AdventCalendarDoorDto> BuildDoorShells(
        int doorCount,
        int unlockedDoorCount,
        ISet<int> openedDoors,
        IReadOnlyDictionary<int, BaseItem> episodesByDoor,
        string pathBase)
    {
        var effectiveMissingEpisodeBehavior = GetEffectiveMissingEpisodeBehavior(Plugin.Instance.Configuration);
        var doors = new List<AdventCalendarDoorDto>(doorCount);
        for (var doorNumber = 1; doorNumber <= doorCount; doorNumber++)
        {
            var isUnlocked = doorNumber <= unlockedDoorCount;
            var isOpened = openedDoors.Contains(doorNumber);
            var hasEpisode = episodesByDoor.TryGetValue(doorNumber, out var episode);
            var isAvailable = isUnlocked && (hasEpisode || ConfigAllowsMessageOnly(effectiveMissingEpisodeBehavior));

            doors.Add(new AdventCalendarDoorDto
            {
                DoorNumber = doorNumber,
                IsUnlocked = isUnlocked,
                IsOpened = isOpened && isUnlocked && hasEpisode,
                IsAvailable = isAvailable,
                EpisodeId = hasEpisode ? episode!.Id.ToString("N") : string.Empty,
                EpisodeTitle = hasEpisode && isOpened ? episode!.Name : $"Door {doorNumber}",
                SeasonNumber = hasEpisode ? episode!.ParentIndexNumber : null,
                EpisodeNumber = hasEpisode ? episode!.IndexNumber : null,
                PlaybackUrl = hasEpisode ? BuildPlaybackUrl(pathBase, episode!) : string.Empty,
                DetailsUrl = hasEpisode ? BuildDetailsUrl(pathBase, episode!) : string.Empty,
                ThumbnailUrl = hasEpisode && isOpened ? BuildThumbnailUrl(pathBase, episode!) : string.Empty,
                BackdropUrl = hasEpisode ? BuildItemBackdropUrl(pathBase, episode!) : string.Empty,
                Message = BuildDoorMessage(doorNumber, isUnlocked, isOpened, hasEpisode, Plugin.Instance.Configuration)
            });
        }

        return doors;
    }

    private ResolvedCalendar ResolveConfiguredCalendar(PluginConfiguration config, MissingEpisodeBehavior behavior, int doorCount, string pathBase)
    {
        if (config.MovieModeEnabled)
        {
            return ResolveMovieCalendar(config, doorCount, pathBase);
        }
        if (!TryResolveSeries(config, out var series))
        {
            return new ResolvedCalendar
            {
                IsConfigured = false,
                SeriesTitle = string.IsNullOrWhiteSpace(config.SeriesName) ? "Advent Calendar" : config.SeriesName,
                SeasonLabel = BuildSeasonLabel(config),
                Message = "Select a Jellyfin series and at least one season number in the plugin settings."
            };
        }

        var seasonNumbers = GetConfiguredSeasonNumbers(config);
        var seasons = GetSeasons(series);
        var selectedSeasons = new List<BaseItem>();

        foreach (var seasonNumber in seasonNumbers)
        {
            var matchingSeason = seasons.FirstOrDefault(item => (item.IndexNumber ?? 0) == seasonNumber);
            if (matchingSeason is null)
            {
                return new ResolvedCalendar
                {
                    IsConfigured = false,
                    SeriesTitle = series.Name,
                    SeasonLabel = BuildSeasonLabel(config),
                    BackgroundImageUrl = BuildItemBackdropUrl(pathBase, series),
                    Message = $"Season {seasonNumber} was not found for {series.Name}."
                };
            }

            selectedSeasons.Add(matchingSeason);
        }

        if (selectedSeasons.Count == 0)
        {
            return new ResolvedCalendar
            {
                IsConfigured = false,
                SeriesTitle = series.Name,
                SeasonLabel = BuildSeasonLabel(config),
                    BackgroundImageUrl = BuildItemBackdropUrl(pathBase, series),
                Message = "At least one season number is required."
            };
        }

        return new ResolvedCalendar
        {
            IsConfigured = true,
            SeriesTitle = series.Name,
            SeasonLabel = BuildSeasonLabel(seasonNumbers),
            BackgroundImageUrl = BuildItemBackdropUrl(pathBase, series),
            EpisodesByDoor = BuildEpisodeMap(behavior, selectedSeasons, doorCount)
        };
    }

    public IReadOnlyList<object> GetMovieLibraries()
    {
        return _libraryManager.GetVirtualFolders()
            .Where(folder => folder.CollectionType == CollectionTypeOptions.movies)
            .OrderBy(folder => folder.Name)
            .Select(folder => new { id = folder.ItemId.ToString(), name = folder.Name })
            .Cast<object>()
            .ToList();
    }

    public IReadOnlyList<string> GetMovieTags()
    {
        return GetAllMovies()
            .SelectMany(movie => movie.Tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag)
            .ToList();
    }

    public int ReshuffleMovies()
    {
        var config = Plugin.Instance.Configuration;
        var movies = GetConfiguredMovies(config).OrderBy(_ => Random.Shared.Next()).ToList();
        config.MovieDoorAssignmentsJson = JsonSerializer.Serialize(movies.Select(movie => movie.Id.ToString("N")).ToArray());
        Plugin.Instance.SaveConfiguration();
        return movies.Count;
    }

    private ResolvedCalendar ResolveMovieCalendar(PluginConfiguration config, int doorCount, string pathBase)
    {
        var ids = DeserializeMovieAssignments(config.MovieDoorAssignmentsJson);
        if (ids.Count == 0)
        {
            return new ResolvedCalendar { IsConfigured = false, SeriesTitle = "Movie Calendar", SeasonLabel = "Movie Mode", Message = "Save Movie Mode or use Reshuffle movies to assign the selected movies to doors." };
        }

        var movies = new Dictionary<int, BaseItem>();
        for (var index = 0; index < ids.Count && index < doorCount; index++)
        {
            if (TryGetItem(ids[index], out var movie) && string.Equals(movie.GetType().Name, "Movie", StringComparison.OrdinalIgnoreCase))
            {
                movies[index + 1] = movie;
            }
        }

        return new ResolvedCalendar { IsConfigured = true, SeriesTitle = "Movie Calendar", SeasonLabel = "Movie Mode", BackgroundImageUrl = string.Empty, EpisodesByDoor = movies };
    }

    private IReadOnlyList<BaseItem> GetConfiguredMovies(PluginConfiguration config)
    {
        var movies = GetAllMovies();
        if (string.Equals(config.MovieSourceType, "tag", StringComparison.OrdinalIgnoreCase))
        {
            return movies.Where(movie => movie.Tags?.Contains(config.MovieTag, StringComparer.OrdinalIgnoreCase) == true).ToList();
        }

        return TryParseGuid(config.MovieLibraryId, out var libraryId)
            ? movies.Where(movie => movie.GetAncestorIds().Contains(libraryId)).ToList()
            : [];
    }

    private IReadOnlyList<BaseItem> GetAllMovies()
    {
        return _libraryManager.GetItemList(new InternalItemsQuery { Recursive = true, IncludeItemTypes = [BaseItemKind.Movie] })
            .OrderBy(movie => movie.SortName ?? movie.Name)
            .ToList();
    }

    private static IReadOnlyList<string> DeserializeMovieAssignments(string? json)
    {
        try { return JsonSerializer.Deserialize<string[]>(json ?? string.Empty) ?? []; }
        catch { return []; }
    }

    private Dictionary<int, BaseItem> BuildEpisodeMap(MissingEpisodeBehavior behavior, IReadOnlyList<BaseItem> selectedSeasons, int doorCount)
    {
        var exactDoors = BuildExactEpisodeDoorMap(selectedSeasons, doorCount);
        if (behavior != MissingEpisodeBehavior.UseNextAvailableEpisode)
        {
            return exactDoors;
        }

        var map = new Dictionary<int, BaseItem>();
        for (var doorNumber = 1; doorNumber <= doorCount; doorNumber++)
        {
            if (exactDoors.TryGetValue(doorNumber, out var exactEpisode))
            {
                map[doorNumber] = exactEpisode;
                continue;
            }

            var nextEpisode = exactDoors
                .Where(item => item.Key >= doorNumber)
                .OrderBy(item => item.Key)
                .Select(item => item.Value)
                .FirstOrDefault()
                ?? exactDoors
                    .OrderBy(item => item.Key)
                    .Select(item => item.Value)
                    .LastOrDefault();

            if (nextEpisode is not null)
            {
                map[doorNumber] = nextEpisode;
            }
        }

        return map;
    }

    private Dictionary<int, BaseItem> BuildExactEpisodeDoorMap(IReadOnlyList<BaseItem> selectedSeasons, int doorCount)
    {
        var map = new Dictionary<int, BaseItem>();
        var doorOffset = 0;

        foreach (var season in selectedSeasons)
        {
            var episodes = GetEpisodes(season);
            var seasonDoorSpan = GetSeasonDoorSpan(episodes);

            foreach (var episode in episodes)
            {
                var episodeNumber = episode.IndexNumber;
                if (episodeNumber is null || episodeNumber <= 0)
                {
                    continue;
                }

                var absoluteDoorNumber = doorOffset + episodeNumber.Value;
                if (absoluteDoorNumber > doorCount)
                {
                    continue;
                }

                map[absoluteDoorNumber] = episode;
            }

            doorOffset += seasonDoorSpan;
            if (doorOffset >= doorCount)
            {
                break;
            }
        }

        return map;
    }

    private static int GetSeasonDoorSpan(IReadOnlyList<BaseItem> episodes)
    {
        var highestEpisodeNumber = episodes
            .Select(item => item.IndexNumber ?? 0)
            .DefaultIfEmpty(0)
            .Max();

        return Math.Max(highestEpisodeNumber, episodes.Count);
    }

    private static bool ConfigAllowsMessageOnly(MissingEpisodeBehavior behavior)
    {
        return behavior != MissingEpisodeBehavior.HideDoor;
    }

    private static MissingEpisodeBehavior GetEffectiveMissingEpisodeBehavior(PluginConfiguration config)
    {
        return string.IsNullOrWhiteSpace(config.MissingEpisodeMessage)
            ? MissingEpisodeBehavior.DisableDoor
            : MissingEpisodeBehavior.ShowMessageOnly;
    }

    private IReadOnlyList<BaseItem> GetAllSeries()
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            Recursive = true,
            IncludeItemTypes = [BaseItemKind.Series]
        })
        .OrderBy(item => item.SortName ?? item.Name)
        .ToList();
    }

    private IReadOnlyList<BaseItem> GetSeasons(BaseItem series)
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            Recursive = true,
            Parent = series,
            IncludeItemTypes = [BaseItemKind.Season]
        })
        .OrderBy(item => item.IndexNumber ?? int.MaxValue)
        .ThenBy(item => item.Name)
        .ToList();
    }

    private IReadOnlyList<BaseItem> GetEpisodes(BaseItem season)
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            Recursive = true,
            Parent = season,
            IncludeItemTypes = [BaseItemKind.Episode]
        })
        .OrderBy(item => item.IndexNumber ?? int.MaxValue)
        .ThenBy(item => item.Name)
        .ToList();
    }

    private bool TryResolveSeries(PluginConfiguration config, out BaseItem series)
    {
        if (TryGetSeriesById(config.SeriesId, out series))
        {
            return true;
        }

        if (TryGetItem(config.SeasonId, out var legacySeason))
        {
            foreach (var candidateSeries in GetAllSeries())
            {
                if (GetSeasons(candidateSeries).Any(season => season.Id == legacySeason.Id))
                {
                    series = candidateSeries;
                    return true;
                }
            }
        }

        series = null!;
        return false;
    }

    private bool TryGetSeriesById(string rawSeriesId, out BaseItem series)
    {
        series = null!;
        if (!TryGetItem(rawSeriesId, out var item))
        {
            return false;
        }

        if (string.Equals(item.GetType().Name, "Series", StringComparison.OrdinalIgnoreCase))
        {
            series = item;
            return true;
        }

        return false;
    }

    private bool TryGetItem(string rawItemId, out BaseItem item)
    {
        item = null!;
        if (!TryParseGuid(rawItemId, out var itemId))
        {
            return false;
        }

        item = _libraryManager.GetItemById(itemId)!;
        return item is not null;
    }

    private static bool TryParseGuid(string value, out Guid result)
    {
        result = Guid.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (Guid.TryParse(value.Trim(), out result))
        {
            return true;
        }

        var match = GuidTokenRegex.Match(value);
        return match.Success && Guid.TryParse(match.Value, out result);
    }

    private static int GetUnlockedDoorCount(
        DateTime now,
        int doorCount,
        bool debugUnlockAllDoors,
        int firstDoorMonth,
        int firstDoorDay)
    {
        if (debugUnlockAllDoors)
        {
            return doorCount;
        }

        var activeStart = GetMostRecentFirstDoorDate(now, firstDoorMonth, firstDoorDay);
        var activeEnd = activeStart.AddDays(doorCount - 1);

        if (now.Date < activeStart || now.Date > activeEnd)
        {
            return 0;
        }

        return (now.Date - activeStart).Days + 1;
    }

    private static int GetCalendarYear(
        DateTime now,
        int doorCount,
        bool debugUnlockAllDoors,
        int firstDoorMonth,
        int firstDoorDay)
    {
        if (debugUnlockAllDoors)
        {
            return now.Year;
        }

        var activeStart = GetMostRecentFirstDoorDate(now, firstDoorMonth, firstDoorDay);
        return now.Date <= activeStart.AddDays(doorCount - 1) ? activeStart.Year : now.Year;
    }

    private static DateTime GetMostRecentFirstDoorDate(DateTime now, int firstDoorMonth, int firstDoorDay)
    {
        var currentYearStart = CreateFirstDoorDate(now.Year, firstDoorMonth, firstDoorDay);
        return now.Date >= currentYearStart
            ? currentYearStart
            : CreateFirstDoorDate(now.Year - 1, firstDoorMonth, firstDoorDay);
    }

    private static DateTime CreateFirstDoorDate(int year, int month, int day)
    {
        var safeMonth = Math.Clamp(month, 1, 12);
        var safeDay = Math.Clamp(day, 1, DateTime.DaysInMonth(year, safeMonth));
        return new DateTime(year, safeMonth, safeDay);
    }

    private static bool HasAccess(PluginConfiguration config, string? currentUsername)
    {
        if (string.IsNullOrWhiteSpace(config.AllowedUsernames))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(currentUsername))
        {
            return false;
        }

        var allowedNames = config.AllowedUsernames
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return allowedNames.Contains(currentUsername, StringComparer.OrdinalIgnoreCase);
    }

    private static AdventCalendarDoorDto CreateUnavailableDoor(int doorNumber, bool isUnlocked, string message)
    {
        return new AdventCalendarDoorDto
        {
            DoorNumber = doorNumber,
            IsUnlocked = isUnlocked,
            IsOpened = false,
            IsAvailable = false,
            EpisodeTitle = $"Door {doorNumber}",
            Message = message
        };
    }

    private static string BuildDoorMessage(
        int doorNumber,
        bool isUnlocked,
        bool isOpened,
        bool hasEpisode,
        PluginConfiguration config)
    {
        if (!isUnlocked)
        {
            return "This door is not open yet.";
        }

        if (!hasEpisode)
        {
            return BuildMissingEpisodeMessage(config, doorNumber);
        }

        if (isOpened)
        {
            return "Opened";
        }

        return "Click to open this door.";
    }

    private static string BuildThumbnailUrl(string pathBase, BaseItem item)
    {
        return BuildRelativeUrl(pathBase, $"/Items/{item.Id:N}/Images/Primary/0");
    }

    private static string BuildItemBackdropUrl(string pathBase, BaseItem item)
    {
        return BuildRelativeUrl(pathBase, $"/Items/{item.Id:N}/Images/Backdrop/0");
    }

    private static string BuildPlaybackUrl(string pathBase, BaseItem item)
    {
        return BuildRelativeUrl(pathBase, $"/Videos/{item.Id:N}/stream?static=true");
    }

    private static string BuildDetailsUrl(string pathBase, BaseItem item)
    {
        return BuildRelativeUrl(pathBase, $"/web/#/details?id={item.Id:N}");
    }

    private static string BuildRelativeUrl(string pathBase, string path)
    {
        var prefix = string.IsNullOrWhiteSpace(pathBase) ? string.Empty : pathBase.TrimEnd('/');
        return $"{prefix}{path}";
    }

    private static HashSet<int> GetOpenedDoorsForYear(PluginConfiguration config, int year, string? currentUsername)
    {
        var usernameKey = NormalizeUsernameKey(currentUsername);
        if (!string.IsNullOrEmpty(config.OpenedDoorsByUserJson))
        {
            var state = DeserializeOpenedDoorsByUser(config.OpenedDoorsByUserJson);
            if (state.TryGetValue(usernameKey, out var userState) && userState.Year == year)
            {
                return userState.Doors
                    .Where(number => number > 0)
                    .ToHashSet();
            }

            return [];
        }

        if (config.OpenedDoorsYear != year)
        {
            return [];
        }

        return config.OpenedDoors
            .Where(number => number > 0)
            .ToHashSet();
    }

    private static void MarkDoorAsOpened(PluginConfiguration config, int year, string? currentUsername, int doorNumber)
    {
        var usernameKey = NormalizeUsernameKey(currentUsername);
        var allUserDoors = DeserializeOpenedDoorsByUser(config.OpenedDoorsByUserJson);
        HashSet<int> openedDoors;

        if (allUserDoors.TryGetValue(usernameKey, out var userState) && userState.Year == year)
        {
            openedDoors = userState.Doors.Where(number => number > 0).ToHashSet();
        }
        else if (string.IsNullOrWhiteSpace(config.OpenedDoorsByUserJson) && config.OpenedDoorsYear == year)
        {
            openedDoors = config.OpenedDoors.Where(number => number > 0).ToHashSet();
        }
        else
        {
            openedDoors = [];
        }

        if (!openedDoors.Add(doorNumber))
        {
            return;
        }

        allUserDoors[usernameKey] = new UserOpenedDoorsState
        {
            Year = year,
            Doors = openedDoors
                .OrderBy(number => number)
                .ToArray()
        };

        config.OpenedDoorsByUserJson = JsonSerializer.Serialize(allUserDoors);
        Plugin.Instance.SaveConfiguration();
    }

    private static bool TryGetLastOpenedMovieDoor(PluginConfiguration config, string? currentUsername, out int doorNumber)
    {
        doorNumber = 0;
        try
        {
            var state = JsonSerializer.Deserialize<Dictionary<string, int>>(config.LastOpenedMovieDoorByUserJson) ?? [];
            return state.TryGetValue(NormalizeUsernameKey(currentUsername), out doorNumber) && doorNumber > 0;
        }
        catch { return false; }
    }

    private static void MarkLastOpenedMovieDoor(PluginConfiguration config, string? currentUsername, int doorNumber)
    {
        Dictionary<string, int> state;
        try { state = JsonSerializer.Deserialize<Dictionary<string, int>>(config.LastOpenedMovieDoorByUserJson) ?? []; }
        catch { state = []; }
        state[NormalizeUsernameKey(currentUsername)] = doorNumber;
        config.LastOpenedMovieDoorByUserJson = JsonSerializer.Serialize(state);
        Plugin.Instance.SaveConfiguration();
    }

    private static string NormalizeUsernameKey(string? currentUsername)
    {
        return string.IsNullOrWhiteSpace(currentUsername)
            ? "__unknown__"
            : currentUsername.Trim().ToLowerInvariant();
    }

    private static Dictionary<string, UserOpenedDoorsState> DeserializeOpenedDoorsByUser(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, UserOpenedDoorsState>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, UserOpenedDoorsState>>(json)
                ?? new Dictionary<string, UserOpenedDoorsState>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, UserOpenedDoorsState>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string BuildMissingEpisodeMessage(PluginConfiguration config, int doorNumber)
    {
        if (!string.IsNullOrWhiteSpace(config.MissingEpisodeMessage))
        {
            return config.MissingEpisodeMessage
                .Replace("{door}", doorNumber.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                .Replace("{doorNumber}", doorNumber.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        }

        return config.MissingEpisodeBehavior switch
        {
            MissingEpisodeBehavior.HideDoor => string.Empty,
            MissingEpisodeBehavior.ShowMessageOnly => $"Door {doorNumber} has no configured episode yet.",
            MissingEpisodeBehavior.UseNextAvailableEpisode => $"Door {doorNumber} could not resolve an episode.",
            _ => $"Episode {doorNumber} is missing."
        };
    }

    private static IReadOnlyList<int> GetConfiguredSeasonNumbers(PluginConfiguration config)
    {
        var numbers = (config.SeasonNumbers ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : 0)
            .Where(number => number > 0)
            .Distinct()
            .ToList();

        if (numbers.Count > 0)
        {
            return numbers;
        }

        return [1];
    }

    private static string BuildSeasonLabel(PluginConfiguration config)
    {
        return BuildSeasonLabel(GetConfiguredSeasonNumbers(config));
    }

    private static string BuildSeasonLabel(IReadOnlyList<int> seasonNumbers)
    {
        if (seasonNumbers.Count == 0)
        {
            return "Season 1";
        }

        if (seasonNumbers.Count == 1)
        {
            return $"Season {seasonNumbers[0]}";
        }

        return "Seasons " + string.Join(", ", seasonNumbers);
    }

    private sealed class ResolvedCalendar
    {
        public bool IsConfigured { get; init; }

        public string SeriesTitle { get; init; } = "Advent Calendar";

        public string SeasonLabel { get; init; } = "Season 1";

        public string BackgroundImageUrl { get; set; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public IReadOnlyDictionary<int, BaseItem> EpisodesByDoor { get; init; } = new Dictionary<int, BaseItem>();
    }

    private sealed class UserOpenedDoorsState
    {
        public int Year { get; init; }

        public int[] Doors { get; init; } = Array.Empty<int>();
    }
}
