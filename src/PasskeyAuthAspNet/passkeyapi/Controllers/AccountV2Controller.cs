using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace PasskeyAuthAspNet.Controllers;

public record RegisterRequest(string Username, string Password);

public record LoginRequest(string Username, string Password);

public record RegisterPasswordlessRequest(string Username);

[ApiController]
[Route("[controller]")]
public class AccountV2Controller : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public AccountV2Controller(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // ----- Password-based accounts -----

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        var user = new IdentityUser
        {
            UserName = request.Username,
            Email = request.Username
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _signInManager.SignInAsync(
            user,
            isPersistent: false);

        return Ok();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.Username,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: false);

        return result.Succeeded
            ? Ok()
            : Unauthorized();
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok();
    }

    // ----- Passwordless account creation -----

    [HttpPost("register-passwordless")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterPasswordless(
        [FromBody] RegisterPasswordlessRequest request)
    {
        var user = new IdentityUser
        {
            UserName = request.Username,
            Email = request.Username
        };

        var result = await _userManager.CreateAsync(user);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _signInManager.SignInAsync(
            user,
            isPersistent: false);

        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        return Ok(new
        {
            username = User.Identity.Name
        });
    }

    // ----- Passkey registration (attestation) -----

    [HttpPost("passkey-options")]
    [Authorize]
    public async Task<IActionResult> PasskeyOptions()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
            return NotFound();

        var userId = await _userManager.GetUserIdAsync(user);
        var userName =
            await _userManager.GetUserNameAsync(user) ?? "User";

        var optionsJson =
            await _signInManager.MakePasskeyCreationOptionsAsync(
                new PasskeyUserEntity
                {
                    Id = userId,
                    Name = userName,
                    DisplayName = userName
                });

        return Content(
            optionsJson,
            "application/json");
    }

    [HttpPost("passkey-register")]
    [Authorize]
    public async Task<IActionResult> PasskeyRegister(
        [FromBody] JsonElement credentialJsonRaw)
    {
        var credentialJson = credentialJsonRaw.GetRawText();

        var user = await _userManager.GetUserAsync(User);

        if (user is null)
            return NotFound();

        PasskeyAttestationResult attestationResult;

        try
        {
            attestationResult =
                await _signInManager.PerformPasskeyAttestationAsync(
                    credentialJson);
        }
        catch (InvalidOperationException ex)
        {
            // No matching ceremony state cookie.
            return BadRequest(ex.Message);
        }

        if (!attestationResult.Succeeded)
        {
            return BadRequest(
                attestationResult.Failure?.Message
                ?? "Attestation failed");
        }

        var addResult =
            await _userManager.AddOrUpdatePasskeyAsync(
                user,
                attestationResult.Passkey);

        return addResult.Succeeded
            ? Ok()
            : BadRequest("Failed to store passkey");
    }

    // ----- Passkey sign-in (assertion) -----

    [HttpPost("passkey-request-options")]
    [AllowAnonymous]
    public async Task<IActionResult> PasskeyRequestOptions(
        [FromQuery] string? username)
    {
        // A null user enables username-less / conditional UI sign-in.
        var user = string.IsNullOrEmpty(username)
            ? null
            : await _userManager.FindByNameAsync(username);

        var optionsJson =
            await _signInManager.MakePasskeyRequestOptionsAsync(user);

        return Content(
            optionsJson,
            "application/json");
    }

    [HttpPost("passkey-signin")]
    [AllowAnonymous]
    public async Task<IActionResult> PasskeySignIn(
        [FromBody] string credentialJson)
    {
        try
        {
            var result =
                await _signInManager.PasskeySignInAsync(
                    credentialJson);

            return result.Succeeded
                ? Ok()
                : Unauthorized();
        }
        catch (InvalidOperationException)
        {
            // No matching ceremony state cookie.
            return Unauthorized();
        }
    }
}
