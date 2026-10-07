using Microsoft.AspNetCore.Authentication.Cookies;
using SmartHomeApplication.Components;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is missing");

// Http client for handling users
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

//builder.Services.AddControllers();
builder.Services.AddControllersWithViews();

// Logging
builder.Services.AddLogging();
builder.Logging.SetMinimumLevel(LogLevel.Information); // This can be changed to LogLevel.Warning or Error if we determine that

// User login, auth and authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(settings =>
    {
        settings.Cookie.Name = "smart_home_auth";
        settings.LoginPath = "/login";
        settings.AccessDeniedPath = "/access-denied";
        settings.Cookie.HttpOnly = true;
        settings.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        settings.Cookie.SameSite = SameSiteMode.Lax;
        settings.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        settings.SlidingExpiration = true;
    });
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

// Same, for user login and stuff
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
