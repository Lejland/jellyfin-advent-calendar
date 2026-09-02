using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Jellyfin.Plugin.AdventCalendar.Models;
using System.Net.Mime;

namespace Jellyfin.Plugin.AdventCalendar.Controllers;

[ApiController]
public sealed class AdventCalendarController : ControllerBase
{
    private readonly AdventCalendarService _service;

    public AdventCalendarController(AdventCalendarService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [HttpGet("/adventcalendar")]
    [Produces("text/html")]
    public IActionResult GetCalendarPage()
    {
        return Content(RenderEmbeddedResource("Web.adventcalendar.html"), "text/html");
    }

    [AllowAnonymous]
    [HttpGet("/adventcalendar/assets/adventcalendar.css")]
    [Produces("text/css")]
    public IActionResult GetCalendarStyles()
    {
        return Content(RenderEmbeddedResource("Web.adventcalendar.css"), "text/css");
    }

    [AllowAnonymous]
    [HttpGet("/adventcalendar/assets/custom-background")]
    public IActionResult GetCustomBackground()
    {
        var name = Plugin.Instance.Configuration.CustomBackgroundFileName;
        var path = Path.Combine(Plugin.Instance.DataFolderPath, name);
        return string.IsNullOrWhiteSpace(name) || !System.IO.File.Exists(path) ? NotFound() : PhysicalFile(path, "image/*");
    }

    [Authorize]
    [HttpPost("/adventcalendar/admin/background")]
    public async Task<IActionResult> UploadCustomBackground(IFormFile file)
    {
        if (file.Length == 0 || file.Length > 2 * 1024 * 1024 || !new[] { "image/png", "image/jpeg", "image/webp" }.Contains(file.ContentType)) return BadRequest();
        var extension = file.ContentType == "image/png" ? ".png" : file.ContentType == "image/webp" ? ".webp" : ".jpg";
        Directory.CreateDirectory(Plugin.Instance.DataFolderPath);
        var name = "custom-calendar-background" + extension;
        await using var stream = System.IO.File.Create(Path.Combine(Plugin.Instance.DataFolderPath, name));
        await file.CopyToAsync(stream);
        Plugin.Instance.Configuration.CustomBackgroundFileName = name;
        Plugin.Instance.Configuration.CustomBackgroundImageData = string.Empty;
        Plugin.Instance.SaveConfiguration();
        return Ok(new { success = true });
    }

    [Authorize]
    [HttpPost("/adventcalendar/admin/background/remove")]
    public IActionResult RemoveCustomBackground()
    {
        Plugin.Instance.Configuration.CustomBackgroundFileName = string.Empty;
        Plugin.Instance.Configuration.CustomBackgroundImageData = string.Empty;
        Plugin.Instance.SaveConfiguration();
        return Ok(new { success = true });
    }

    [AllowAnonymous]
    [HttpGet("/adventcalendar/assets/movie-mystery-cinema.png")]
    public IActionResult GetMovieMysteryArtwork()
    {
        var assembly = typeof(Plugin).Assembly;
        var stream = assembly.GetManifestResourceStream("Jellyfin.Plugin.AdventCalendar.assets.movie-mystery-cinema.png")
            ?? throw new InvalidOperationException("Movie mystery artwork was not found.");
        return File(stream, "image/png");
    }

    [AllowAnonymous]
    [HttpGet("/adventcalendar/assets/adventcalendar.js")]
    [Produces("application/javascript")]
    public IActionResult GetCalendarScript()
    {
        return Content(RenderEmbeddedResource("Web.adventcalendar.js"), "application/javascript");
    }

    [AllowAnonymous]
    [HttpGet("/adventcalendar/state")]
    public ActionResult<AdventCalendarStateDto> GetState()
    {
        return Ok(_service.BuildState(IsAuthenticated(), GetCurrentUsername(), Request.PathBase.Value ?? string.Empty));
    }

    [Authorize]
    [HttpGet("/adventcalendar/door/{doorNumber:int}")]
    public ActionResult<AdventCalendarDoorDto> GetDoor(int doorNumber)
    {
        return Ok(_service.ResolveDoor(GetCurrentUsername(), Request.PathBase.Value ?? string.Empty, doorNumber));
    }

    [Authorize]
    [HttpPost("/adventcalendar/admin/reset")]
    [Produces(MediaTypeNames.Application.Json)]
    public IActionResult ResetOpenedDoors()
    {
        _service.ResetOpenedDoors();
        return Ok(new { success = true });
    }

    [Authorize]
    [HttpPost("/adventcalendar/admin/movies/reshuffle")]
    public IActionResult ReshuffleMovies()
    {
        return Ok(new { count = _service.ReshuffleMovies() });
    }

    [Authorize]
    [HttpGet("/adventcalendar/admin/movies/libraries")]
    public IActionResult GetMovieLibraries()
    {
        return Ok(_service.GetMovieLibraries());
    }

    [Authorize]
    [HttpGet("/adventcalendar/admin/movies/tags")]
    public IActionResult GetMovieTags()
    {
        return Ok(_service.GetMovieTags());
    }

    [Authorize]
    [HttpGet("/adventcalendar/admin/series")]
    [Produces(MediaTypeNames.Application.Json)]
    public IActionResult SearchSeries([FromQuery] string? query)
    {
        return Ok(_service.FindSeries(query));
    }

    [Authorize]
    [HttpGet("/adventcalendar/admin/series/{seriesId}")]
    [Produces(MediaTypeNames.Application.Json)]
    public IActionResult GetSeries(string seriesId)
    {
        var series = _service.GetSeriesDetails(seriesId);
        return series is null ? NotFound() : Ok(series);
    }

    [Authorize]
    [HttpGet("/adventcalendar/admin/series/{seriesId}/seasons")]
    [Produces(MediaTypeNames.Application.Json)]
    public IActionResult GetSeriesSeasons(string seriesId)
    {
        return Ok(_service.GetSeriesSeasons(seriesId));
    }

    private bool IsAuthenticated()
    {
        return User.Identity?.IsAuthenticated == true;
    }

    private string? GetCurrentUsername()
    {
        return User.Identity?.Name
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("name");
    }

    private string RenderEmbeddedResource(string resourceName)
    {
        var assembly = typeof(Plugin).Assembly;
        var fullResourceName = $"{typeof(Plugin).Namespace}.{resourceName}";
        using var stream = assembly.GetManifestResourceStream(fullResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{fullResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd()
            .Replace("__ADVENT_BASE_PATH__", $"{Request.PathBase}/adventcalendar", StringComparison.Ordinal)
            .Replace("__PATH_BASE__", Request.PathBase.Value ?? string.Empty, StringComparison.Ordinal)
            .Replace("__PLUGIN_ID__", Plugin.Instance.Id.ToString(), StringComparison.Ordinal);
    }
}
