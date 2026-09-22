using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace PasskeyAuthAspNet.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    const string AttestationStateCookieName = "PasskeyAttestationState";
    const string AttestationStatePurpose = "Contoso.Passkeys.AttestationState.v1";

    private readonly UserManager<IdentityUser> _userManager;
    private readonly IPasskeyHandler<IdentityUser> _passkeyHandler;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public AccountController(
        IDataProtectionProvider dataProtectionProvider,
        IPasskeyHandler<IdentityUser> passkeyHandler,
        UserManager<IdentityUser> userManager)
    {
        _dataProtectionProvider = dataProtectionProvider;
        _passkeyHandler = passkeyHandler;
        _userManager = userManager;
    }

    [HttpPost("PasskeyCreationOptions")]
    public async Task<IActionResult> PasskeyCreationOptions()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);

        if (user is null)
        {
            return Unauthorized();
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var userName = await _userManager.GetUserNameAsync(user) ?? "User";

        var options = await _passkeyHandler.MakeCreationOptionsAsync(new()
        {
            Id = userId,
            Name = userName,
            DisplayName = userName
        }, HttpContext);

        var attestationState = options.AttestationState
            ?? throw new InvalidOperationException("No attestation state was created.");

        var protector =
            _dataProtectionProvider.CreateProtector(AttestationStatePurpose);

        HttpContext.Response.Cookies.Append(
            AttestationStateCookieName,
            protector.Protect(attestationState),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromMinutes(5)
            });

        return Content(options.CreationOptionsJson, contentType: "application/json");
    }

    [HttpPost("PasskeyRegistration")]
    public async Task<IActionResult> RegisterWithPasskey(PasskeyRegistrationRequest request)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        if (!HttpContext.Request.Cookies.TryGetValue(AttestationStateCookieName,
            out var protectedState))
        {
            return BadRequest("No passkey registration is underway");
        }

        HttpContext.Response.Cookies.Delete(AttestationStateCookieName);

        var protector =
            _dataProtectionProvider.CreateProtector(AttestationStatePurpose);
        string attestationState;

        try
        {
            attestationState = protector.Unprotect(protectedState);
        }
        catch (CryptographicException)
        {
            return BadRequest("The passkey registration state isn't valid");
        }

        var attestationResult = await _passkeyHandler.PerformAttestationAsync(new()
        {
            HttpContext = HttpContext,
            CredentialJson = request.CredentialJson,
            AttestationState = attestationState
        });

        if (!attestationResult.Succeeded)
        {
            return BadRequest($"Error: {attestationResult.Failure.Message}");
        }

        var userId = await _userManager.GetUserIdAsync(user);

        if (!string.Equals(attestationResult.UserEntity.Id, userId,
            StringComparison.Ordinal))
        {
            return BadRequest("The passkey isn't registered to the current user");
        }

        var addResult =
            await _userManager.AddOrUpdatePasskeyAsync(user, attestationResult.Passkey);

        if (!addResult.Succeeded)
        {
            return BadRequest("Failed to store passkey");
        }

        return Ok();
    }
}

public record PasskeyRegistrationRequest(string CredentialJson);