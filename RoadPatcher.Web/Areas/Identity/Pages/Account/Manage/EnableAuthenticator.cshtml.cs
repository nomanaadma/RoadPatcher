// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class EnableAuthenticatorModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<EnableAuthenticatorModel> _logger;
    private readonly UrlEncoder _urlEncoder;

    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    public EnableAuthenticatorModel(
        UserManager<ApplicationUser> userManager,
        ILogger<EnableAuthenticatorModel> logger,
        UrlEncoder urlEncoder)
    {
        _userManager = userManager;
        _logger = logger;
        _urlEncoder = urlEncoder;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string SharedKey { get; set; }
    public string AuthenticatorUri { get; set; }
    public string QrCodeImageUrl { get; set; }
    public IList<string> RecoveryCodes { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(7, ErrorMessage = "{0} must be at least {2} and at max {1} characters.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Verification code")]
        public string Code { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadSharedKeyAndQrCodeUriAsync(user);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            await LoadSharedKeyAndQrCodeUriAsync(user);
            return Page();
        }

        var verificationCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

        if (!is2faTokenValid)
        {
            ModelState.AddModelError("Input.Code", "Verification code is invalid.");
            await LoadSharedKeyAndQrCodeUriAsync(user);
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        _logger.LogInformation("User with ID '{UserId}' has enabled 2FA with an authenticator app.", await _userManager.GetUserIdAsync(user));

        RecoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)).ToList();
        StatusMessage = "Your authenticator app has been verified.";

        await LoadSharedKeyAndQrCodeUriAsync(user);
        return Page();
    }

    private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
    {
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        SharedKey = FormatKey(unformattedKey);
        var email = await _userManager.GetEmailAsync(user) ?? await _userManager.GetUserNameAsync(user) ?? "user";
        AuthenticatorUri = GenerateQrCodeUri(email, unformattedKey);
        QrCodeImageUrl = GenerateQrCodeImageUrl(AuthenticatorUri);
    }

    private string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }

        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string email, string unformattedKey)
    {
        return string.Format(
            AuthenticatorUriFormat,
            _urlEncoder.Encode("RoadPatcher"),
            _urlEncoder.Encode(email),
            unformattedKey);
    }

    private string GenerateQrCodeImageUrl(string authenticatorUri)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        using var pngQrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = pngQrCode.GetGraphic(20);

        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
    }
}
