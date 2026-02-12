using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using NodaTime;
using Octokit;
using Humans.Domain.Enums;
using Humans.Infrastructure.Configuration;
using Humans.Infrastructure.Data;
using Humans.Web.Extensions;
using Humans.Web.Models;
using MemberApplication = Humans.Domain.Entities.Application;

namespace Humans.Web.Controllers;

[Authorize]
[Route("[controller]")]
[Route("Governance")]
public class ApplicationController : Controller
{
    private readonly HumansDbContext _dbContext;
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IClock _clock;
    private readonly ILogger<ApplicationController> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IMemoryCache _cache;
    private readonly GitHubSettings _gitHubSettings;

    private const string StatutesCacheKey = "StatutesContent";
    private static readonly Regex LanguageFilePattern = new(
        @"^(?<name>[A-Za-z0-9_-]+?)(?:-(?<lang>[A-Za-z]{2}))?\.md$",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    public ApplicationController(
        HumansDbContext dbContext,
        UserManager<Domain.Entities.User> userManager,
        IClock clock,
        ILogger<ApplicationController> logger,
        IStringLocalizer<SharedResource> localizer,
        IMemoryCache cache,
        IOptions<GitHubSettings> gitHubSettings)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _clock = clock;
        _logger = logger;
        _localizer = localizer;
        _cache = cache;
        _gitHubSettings = gitHubSettings.Value;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var applications = await _dbContext.Applications
            .Where(a => a.UserId == user.Id)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        // Can submit new if no pending/under review applications
        var hasPendingApplication = applications.Any(a =>
            a.Status == ApplicationStatus.Submitted ||
            a.Status == ApplicationStatus.UnderReview);

        var statutesContent = await GetStatutesContentAsync();

        var viewModel = new ApplicationIndexViewModel
        {
            Applications = applications.Select(a => new ApplicationSummaryViewModel
            {
                Id = a.Id,
                Status = a.Status.ToString(),
                SubmittedAt = a.SubmittedAt.ToDateTimeUtc(),
                ResolvedAt = a.ResolvedAt?.ToDateTimeUtc(),
                StatusBadgeClass = a.Status.GetBadgeClass()
            }).ToList(),
            CanSubmitNew = !hasPendingApplication,
            StatutesContent = statutesContent
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        // Check if user already has a pending application
        var hasPending = await _dbContext.Applications
            .AnyAsync(a => a.UserId == user.Id &&
                (a.Status == ApplicationStatus.Submitted || a.Status == ApplicationStatus.UnderReview));

        if (hasPending)
        {
            TempData["ErrorMessage"] = _localizer["Application_AlreadyPending"].Value;
            return RedirectToAction(nameof(Index));
        }

        return View(new ApplicationCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApplicationCreateViewModel model)
    {
        if (!model.ConfirmAccuracy)
        {
            ModelState.AddModelError(nameof(model.ConfirmAccuracy), _localizer["Application_ConfirmAccuracy"].Value);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        // Double-check no pending application
        var hasPending = await _dbContext.Applications
            .AnyAsync(a => a.UserId == user.Id &&
                (a.Status == ApplicationStatus.Submitted || a.Status == ApplicationStatus.UnderReview));

        if (hasPending)
        {
            TempData["ErrorMessage"] = _localizer["Application_AlreadyPending"].Value;
            return RedirectToAction(nameof(Index));
        }

        var now = _clock.GetCurrentInstant();

        var application = new MemberApplication
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Motivation = model.Motivation,
            AdditionalInfo = model.AdditionalInfo,
            Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
            SubmittedAt = now,
            UpdatedAt = now
        };

        _dbContext.Applications.Add(application);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} submitted application {ApplicationId}", user.Id, application.Id);

        TempData["SuccessMessage"] = _localizer["Application_Submitted"].Value;
        return RedirectToAction(nameof(Details), new { id = application.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var application = await _dbContext.Applications
            .Include(a => a.ReviewedByUser)
            .Include(a => a.StateHistory)
                .ThenInclude(h => h.ChangedByUser)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

        if (application == null)
        {
            return NotFound();
        }

        var viewModel = new ApplicationDetailViewModel
        {
            Id = application.Id,
            Status = application.Status.ToString(),
            Motivation = application.Motivation,
            AdditionalInfo = application.AdditionalInfo,
            SubmittedAt = application.SubmittedAt.ToDateTimeUtc(),
            ReviewStartedAt = application.ReviewStartedAt?.ToDateTimeUtc(),
            ResolvedAt = application.ResolvedAt?.ToDateTimeUtc(),
            ReviewerName = application.ReviewedByUser?.DisplayName,
            ReviewNotes = application.ReviewNotes,
            CanWithdraw = application.Status == ApplicationStatus.Submitted ||
                          application.Status == ApplicationStatus.UnderReview,
            History = application.StateHistory
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new ApplicationHistoryViewModel
                {
                    Status = h.Status.ToString(),
                    ChangedAt = h.ChangedAt.ToDateTimeUtc(),
                    ChangedBy = h.ChangedByUser?.DisplayName ?? "System",
                    Notes = h.Notes
                }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var application = await _dbContext.Applications
            .Include(a => a.StateHistory)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

        if (application == null)
        {
            return NotFound();
        }

        if (application.Status != ApplicationStatus.Submitted &&
            application.Status != ApplicationStatus.UnderReview)
        {
            TempData["ErrorMessage"] = _localizer["Application_CannotWithdraw"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        application.Withdraw(_clock);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} withdrew application {ApplicationId}", user.Id, application.Id);

        TempData["SuccessMessage"] = _localizer["Application_Withdrawn"].Value;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Fetches statutes markdown content from GitHub, cached for 1 hour.
    /// </summary>
    private async Task<Dictionary<string, string>> GetStatutesContentAsync()
    {
        if (_cache.TryGetValue(StatutesCacheKey, out Dictionary<string, string>? cached) && cached != null)
            return cached;

        var content = new Dictionary<string, string>(StringComparer.Ordinal);

        try
        {
            var client = new GitHubClient(new ProductHeaderValue("NobodiesHumans"));
            if (!string.IsNullOrEmpty(_gitHubSettings.AccessToken))
            {
                client.Credentials = new Credentials(_gitHubSettings.AccessToken);
            }

            var files = await client.Repository.Content.GetAllContents(
                _gitHubSettings.Owner,
                _gitHubSettings.Repository,
                "Estatutos");

            foreach (var file in files.Where(f => f.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
            {
                var match = LanguageFilePattern.Match(file.Name);
                if (!match.Success) continue;

                var lang = match.Groups["lang"].Success ? match.Groups["lang"].Value : "es";

                // Fetch full content (GetAllContents for a directory only returns metadata)
                var fileContent = await client.Repository.Content.GetAllContents(
                    _gitHubSettings.Owner,
                    _gitHubSettings.Repository,
                    file.Path);

                if (fileContent.Count > 0 && fileContent[0].Content != null)
                {
                    content[lang] = fileContent[0].Content;
                }
            }

            _cache.Set(StatutesCacheKey, content, TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch statutes from GitHub");
        }

        return content;
    }
}
