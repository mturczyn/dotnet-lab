using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PasskeyAuthAspNet.DataAccess;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PasskeyAuthAppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddScoped(_ => DataProtectionProvider.Create("passkeywebapi"));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<PasskeyAuthAppDbContext>()
    .AddSignInManager();

builder.Services.Configure<IdentityPasskeyOptions>(options =>
{
    options.ServerDomain = "localhost";
    options.AuthenticatorTimeout = TimeSpan.FromMinutes(5);
    options.ChallengeSize = 64;
    options.VerifyAttestationStatement = async context =>
    {
        Console.WriteLine("Vaidating attestation statement");
        return true;
    };
    options.ValidateOrigin = async context =>
    {
        Console.WriteLine("Validating origin");
        return true;
    };
});

const string CorsPolicyName = "FrontEnd";

// For frontend dev.
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            // Adding explicit origin works, when fetch has "credentials: 'include'" option.
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// It breaks preflight requests to HTTP endpoitns.
// In case HTTPS needed, check any preflight request such as
/*
 curl -i -X OPTIONS `
  http://localhost:5025/accountv2/register-passwordless `
  -H "Origin: http://localhost:5173" `
  -H "Access-Control-Request-Method: POST" `
  -H "Access-Control-Request-Headers: content-type"
 */
//app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseCors(CorsPolicyName);

app.MapControllers();

app.Run();
