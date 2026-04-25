// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Areas.Identity.Pages.Account;

public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserStore<ApplicationUser> _userStore;
    private readonly IUserEmailStore<ApplicationUser> _emailStore;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        ApplicationDbContext db,
        IWebHostEnvironment webHostEnvironment,
        ILogger<ExternalLoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _db = db;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ProviderDisplayName { get; set; }

    public string ReturnUrl { get; set; }

    [TempData]
    public string ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public IActionResult OnGet()
    {
        return RedirectToPage("./Login");
    }

    public IActionResult OnPost(string provider, string returnUrl = null)
    {
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Error loading external login information.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false,
            bypassTwoFactor: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in with {LoginProvider} provider.", info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = false });
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        var providerEmail = info.Principal.FindFirstValue(ClaimTypes.Email)
            ?? info.Principal.FindFirstValue("email");

        // Auto-link provider if local user with same email already exists.
        if (!string.IsNullOrWhiteSpace(providerEmail))
        {
            var existingUser = await _userManager.FindByEmailAsync(providerEmail);
            if (existingUser != null)
            {
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    await TrySyncExternalImageAsync(existingUser.Id, info);
                    var linkedSignInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);

                    if (linkedSignInResult.Succeeded)
                    {
                        _logger.LogInformation("Linked {LoginProvider} login to existing user {UserId}.", info.LoginProvider,
                            existingUser.Id);
                        return LocalRedirect(returnUrl);
                    }

                    if (linkedSignInResult.RequiresTwoFactor)
                    {
                        _logger.LogInformation("Linked {LoginProvider} login for user {UserId} requires two-factor authentication.", info.LoginProvider, existingUser.Id);
                        return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = false });
                    }
                }

                // If already linked in edge cases, allow sign-in continuation.
                if (addLoginResult.Errors.Any(e => e.Code == "LoginAlreadyAssociated"))
                {
                    await TrySyncExternalImageAsync(existingUser.Id, info);
                    var associatedSignInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);

                    if (associatedSignInResult.Succeeded)
                    {
                        return LocalRedirect(returnUrl);
                    }

                    if (associatedSignInResult.RequiresTwoFactor)
                    {
                        return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = false });
                    }
                }
            }
            else
            {
                var user = CreateUser();
                await _userStore.SetUserNameAsync(user, providerEmail, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, providerEmail, CancellationToken.None);
                user.EmailConfirmed = true;

                var createResult = await _userManager.CreateAsync(user);
                if (createResult.Succeeded)
                {
                    var addLoginResult = await _userManager.AddLoginAsync(user, info);
                    if (addLoginResult.Succeeded)
                    {
                        await TrySyncExternalImageAsync(user.Id, info);
                        var createdSignInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);

                        if (createdSignInResult.Succeeded)
                        {
                            _logger.LogInformation("Created local user and logged in with {LoginProvider}.",
                                info.LoginProvider);
                            return LocalRedirect(returnUrl);
                        }

                        if (createdSignInResult.RequiresTwoFactor)
                        {
                            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = false });
                        }
                    }

                    foreach (var error in addLoginResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
        }

        ReturnUrl = returnUrl;
        ProviderDisplayName = info.ProviderDisplayName;
        Input = new InputModel
        {
            Email = providerEmail
        };

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Error loading external login information during confirmation.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        if (!ModelState.IsValid)
        {
            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        var user = CreateUser();
        await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
        await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
        user.EmailConfirmed = true;

        var result = await _userManager.CreateAsync(user);
        if (result.Succeeded)
        {
            result = await _userManager.AddLoginAsync(user, info);
            if (result.Succeeded)
            {
                await TrySyncExternalImageAsync(user.Id, info);
                var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);

                if (signInResult.Succeeded)
                {
                    return LocalRedirect(returnUrl);
                }

                if (signInResult.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = false });
                }
            }
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        ProviderDisplayName = info.ProviderDisplayName;
        ReturnUrl = returnUrl;
        return Page();
    }

    private ApplicationUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<ApplicationUser>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.");
        }
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }

        return (IUserEmailStore<ApplicationUser>)_userStore;
    }

    private async Task TrySyncExternalImageAsync(string userId, ExternalLoginInfo info)
    {
        _logger.LogInformation("🔄 Starting external image sync for user {UserId} via {Provider}", userId, info.LoginProvider);
        
        var pictureUrl = await GetPictureUrlAsync(info);
        if (string.IsNullOrWhiteSpace(pictureUrl))
        {
            _logger.LogWarning("❌ No picture URL found in {Provider} claims for user {UserId}", info.LoginProvider, userId);
            return;
        }

        _logger.LogInformation("✅ Picture URL found: {PictureUrl}", pictureUrl);

        var user = await _db.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            _logger.LogError("❌ User {UserId} not found in database during image sync", userId);
            return;
        }

        var userData = await _db.UserData
            .AsTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        _logger.LogInformation("📋 UserData exists: {UserDataExists}, UserData.Image is null: {UserDataImageNull}", 
            userData != null, userData == null ? "N/A" : string.IsNullOrWhiteSpace(userData.Image));
        _logger.LogInformation("📋 ApplicationUser.ExternalImage is null: {ExternalImageNull}", 
            string.IsNullOrWhiteSpace(user.ExternalImage));

        var shouldSaveInUserData = userData != null && string.IsNullOrWhiteSpace(userData.Image);
        var shouldSaveInAspNetUsers = !shouldSaveInUserData && string.IsNullOrWhiteSpace(user.ExternalImage);

        if (!shouldSaveInUserData && !shouldSaveInAspNetUsers)
        {
            _logger.LogWarning("⚠️ Image already exists in both locations or UserData doesn't exist with null Image. Skipping sync.");
            return;
        }

        var targetLocation = shouldSaveInUserData ? "UserData.Image" : "ApplicationUser.ExternalImage";
        _logger.LogInformation("🎯 Target location: {TargetLocation}", targetLocation);

        var savedFile = await DownloadExternalImageAsync(pictureUrl, userId, info.LoginProvider);
        if (string.IsNullOrWhiteSpace(savedFile))
        {
            _logger.LogError("❌ Failed to download external image from {PictureUrl}", pictureUrl);
            return;
        }

        _logger.LogInformation("💾 Image downloaded successfully: {FileName}", savedFile);

        if (shouldSaveInUserData)
        {
            userData!.Image = savedFile;
            _logger.LogInformation("✏️ Assigned to UserData.Image: {FileName}", savedFile);
        }
        else
        {
            user.ExternalImage = savedFile;
            _logger.LogInformation("✏️ Assigned to ApplicationUser.ExternalImage: {FileName}", savedFile);
        }

        var saveResult = await _db.SaveChangesAsync();
        _logger.LogInformation("💾 Database save completed. Changes saved: {ChangeCount}", saveResult);
    }

    private async Task<string> GetPictureUrlAsync(ExternalLoginInfo info)
    {
        var claims = info.Principal;

        // Log all available claims to debug
        var allClaims = claims.Claims.ToList();
        _logger.LogInformation("📋 Total claims received: {ClaimCount}", allClaims.Count);
        foreach (var claim in allClaims)
        {
            _logger.LogInformation("  📌 Claim Type: {Type}, Value: {Value}", claim.Type, claim.Value);
        }

        // First, try to get picture from claims
        var pictureClaimValue = claims.FindFirstValue("picture");
        if (!string.IsNullOrWhiteSpace(pictureClaimValue))
        {
            _logger.LogInformation("📸 Found picture claim via 'picture': {Value}", pictureClaimValue);
            return pictureClaimValue;
        }

        var googlePictureValue = claims.FindFirstValue("urn:google:picture");
        if (!string.IsNullOrWhiteSpace(googlePictureValue))
        {
            _logger.LogInformation("📸 Found picture claim via 'urn:google:picture': {Value}", googlePictureValue);
            return googlePictureValue;
        }

        var avatarValue = claims.FindFirstValue("avatar_url");
        if (!string.IsNullOrWhiteSpace(avatarValue))
        {
            _logger.LogInformation("📸 Found picture claim via 'avatar_url': {Value}", avatarValue);
            return avatarValue;
        }

        var twitterProfileImageValue = claims.FindFirstValue("urn:twitter:profilepicture")
                                       ?? claims.FindFirstValue("profile_image_url_https")
                                       ?? claims.FindFirstValue("profile_image_url");
        if (!string.IsNullOrWhiteSpace(twitterProfileImageValue))
        {
            var normalizedTwitterImage = NormalizeTwitterProfileImageUrl(twitterProfileImageValue);
            _logger.LogInformation("📸 Found Twitter picture claim. Original: {OriginalValue}, Normalized: {NormalizedValue}",
                twitterProfileImageValue, normalizedTwitterImage);
            return normalizedTwitterImage;
        }

        _logger.LogWarning("⚠️ No picture in claims. Attempting to fetch from {Provider} API...", info.LoginProvider);

        // If no picture in claims, try to fetch from provider API using access token
        if (info.LoginProvider == "Google")
        {
            return await FetchGoogleProfilePictureAsync(info);
        }

        if (info.LoginProvider == "Facebook")
        {
            return await FetchFacebookProfilePictureAsync(info);
        }

        _logger.LogWarning("❌ Provider {Provider} API fetch not implemented", info.LoginProvider);
        return null;
    }

    private static string NormalizeTwitterProfileImageUrl(string imageUrl)
    {
        // Twitter/X profile image URLs often include size suffixes like _normal, _bigger, or _mini.
        // Removing them yields a higher-resolution image.
        return Regex.Replace(
            imageUrl,
            "_(normal|bigger|mini)(\\.[a-zA-Z0-9]+)(\\?.*)?$",
            "$2$3",
            RegexOptions.IgnoreCase);
    }

    private async Task<string> FetchGoogleProfilePictureAsync(ExternalLoginInfo info)
    {
        try
        {
            // Get access token from authentication properties
            var accessToken = info.AuthenticationProperties?.GetTokenValue("access_token");
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogWarning("❌ No access token found in Google authentication properties");
                return null;
            }

            _logger.LogInformation("🔑 Access token retrieved. Calling Google UserInfo API...");

            using var http = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("❌ Google UserInfo API returned {StatusCode}: {Reason}", response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("📨 Google UserInfo response: {Content}", content);

            // Parse JSON response to extract picture
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
            if (jsonDoc.RootElement.TryGetProperty("picture", out var pictureElement))
            {
                var pictureUrl = pictureElement.GetString();
                if (!string.IsNullOrWhiteSpace(pictureUrl))
                {
                    _logger.LogInformation("✅ Picture URL retrieved from Google API: {URL}", pictureUrl);
                    return pictureUrl;
                }
            }

            _logger.LogWarning("❌ 'picture' property not found in Google UserInfo response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error fetching Google profile picture: {Message}", ex.Message);
            return null;
        }
    }

    private async Task<string> FetchFacebookProfilePictureAsync(ExternalLoginInfo info)
    {
        try
        {
            // Get access token from authentication properties
            var accessToken = info.AuthenticationProperties?.GetTokenValue("access_token");
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogWarning("❌ No access token found in Facebook authentication properties");
                return null;
            }

            _logger.LogInformation("🔑 Access token retrieved. Calling Facebook Graph API...");

            using var http = new HttpClient();
            // Facebook Graph API endpoint to get user profile picture
            // redirect=false tells Facebook to return JSON with URL instead of redirecting to image
            var url = $"https://graph.facebook.com/me/picture?type=large&redirect=false&access_token={accessToken}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("❌ Facebook Graph API returned {StatusCode}: {Reason}", response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("📨 Facebook Graph API response: {Content}", content);

            // Parse JSON response to extract picture URL
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
            if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.TryGetProperty("url", out var urlElement))
                {
                    var pictureUrl = urlElement.GetString();
                    if (!string.IsNullOrWhiteSpace(pictureUrl))
                    {
                        _logger.LogInformation("✅ Picture URL retrieved from Facebook API: {URL}", pictureUrl);
                        return pictureUrl;
                    }
                }
            }

            _logger.LogWarning("❌ 'data.url' property not found in Facebook Graph API response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error fetching Facebook profile picture: {Message}", ex.Message);
            return null;
        }
    }

    private async Task<string> DownloadExternalImageAsync(string pictureUrl, string userId, string provider)
    {
        try
        {
            _logger.LogInformation("⬇️ Downloading image from URL: {URL}", pictureUrl);

            using var http = new HttpClient();
            var bytes = await http.GetByteArrayAsync(pictureUrl);
            
            if (bytes.Length == 0)
            {
                _logger.LogWarning("❌ Downloaded image is empty (0 bytes) from {URL}", pictureUrl);
                return null;
            }

            _logger.LogInformation("✅ Image downloaded: {ByteCount} bytes", bytes.Length);

            var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "users");
            if (!Directory.Exists(uploadFolder))
            {
                _logger.LogInformation("📁 Creating uploads folder: {Path}", uploadFolder);
                Directory.CreateDirectory(uploadFolder);
            }

            var providerSafe = provider.ToLowerInvariant().Replace(" ", "-");
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var fileName = $"ext-{providerSafe}-{userId}-{timestamp}.jpg";
            var fullPath = Path.Combine(uploadFolder, fileName);

            _logger.LogInformation("💾 Saving to: {FullPath}", fullPath);
            await System.IO.File.WriteAllBytesAsync(fullPath, bytes);
            
            _logger.LogInformation("✅ File saved successfully: {FileName}", fileName);
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error downloading/saving external image from {URL}. Exception: {Message}", pictureUrl, ex.Message);
            return null;
        }
    }
}
