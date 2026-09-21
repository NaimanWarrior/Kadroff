using kadroff.Components;
using kadroff.Components.Data;
using kadroff.Components.Pages;
using kadroff.Components.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Scoped);
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
}).AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "temp";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "temp";
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

builder.Services.AddHostedService<ApplicationAutoAcceptWorker>();
builder.Services.AddScoped<CvService>();
builder.Services.AddScoped<CvWizardService>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<JobApplicationService>();
builder.Services.AddScoped<CvPdfController>();
builder.Services.AddRadzenComponents();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var seeder = services.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
    try {
    var dbContext = services.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = ["Admin", "Recruiter", "Candidate"];
        foreach (var role in roles)
        {
            if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
            }
        }
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync("xaziev2001@gmail.com");
        
            if (user != null && !await userManager.IsInRoleAsync(user, "Admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        
    } catch {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError("An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapPost("/Account/PerformLogout", async (
    SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
});
app.MapGet("/Account/PerformExternalLogin", (
    string provider,
    string? returnUrl,
    SignInManager<IdentityUser> signInManager) =>
{
    var redirectUrl = $"/Account/ExternalLoginCallback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";
    var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
    return Results.Challenge(properties, new[] { provider });
});
app.MapGet("/Account/ExternalLoginCallback", async (
    string? returnUrl,
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager) =>
{
    returnUrl ??= "/";

    var info = await signInManager.GetExternalLoginInfoAsync();
    if (info == null)
    {
        return Results.Redirect("/Account/Login?error=external_login_failed");
    }

    var result = await signInManager.ExternalLoginSignInAsync(
        info.LoginProvider,
        info.ProviderKey,
        isPersistent: true,
        bypassTwoFactor: true);

    if (result.Succeeded)
    {
        return Results.Redirect(returnUrl);
    }

    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
    if (string.IsNullOrEmpty(email))
    {
        return Results.Redirect("/Account/Login?error=email_not_provided");
    }

    var user = await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            var err = Uri.EscapeDataString(createResult.Errors.First().Description);
            return Results.Redirect($"/Account/Login?error={err}");
        }

        string targetRole = email.Equals("xaziev2001@gmail.com", StringComparison.OrdinalIgnoreCase)
            ? "Admin"
            : "Candidate";

        if (!await roleManager.RoleExistsAsync(targetRole))
        {
            await roleManager.CreateAsync(new IdentityRole(targetRole));
        }
        await userManager.AddToRoleAsync(user, targetRole);
    }

    var addLoginResult = await userManager.AddLoginAsync(user, info);
    if (addLoginResult.Succeeded)
    {
        await signInManager.SignInAsync(user, isPersistent: true);
        return Results.Redirect(returnUrl);
    }

    return Results.Redirect("/Account/Login?error=failed_to_link_account");
});
app.MapPost("/Account/PerformLogin", async (
    HttpContext context,
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager) =>
{
    var form = await context.Request.ReadFormAsync();
    string loginInput = form["loginInput"].ToString();
    string passwordInput = form["passwordInput"].ToString();

    var user = await userManager.FindByEmailAsync(loginInput)
            ?? await userManager.FindByNameAsync(loginInput);

    if (user != null)
    {
        var result = await signInManager.PasswordSignInAsync(
            user,
            passwordInput,
            isPersistent: true,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return Results.Redirect("/");
        }
    }

    return Results.Redirect("/Account/Login?error=invalid_credentials");
});
app.MapPost("/Account/PerformRegister", async (
    HttpContext context,
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    string email = form["email"].ToString();
    string password = form["password"].ToString();

    var user = new IdentityUser { UserName = email, Email = email };
    var result = await userManager.CreateAsync(user, password);

    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(user, "Candidate");

        await signInManager.SignInAsync(user, isPersistent: true);

        return Results.Redirect("/");
    }

    var error = Uri.EscapeDataString(result.Errors.First().Description);
    return Results.Redirect($"/Account/Register?error={error}");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
