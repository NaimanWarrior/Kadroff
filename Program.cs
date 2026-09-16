using kadroff.Components;
using kadroff.Components.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Scoped);
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
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
    });

builder.Services.AddRadzenComponents();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
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
app.MapPost("/Account/PerformLogout", async (
    SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
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
            user.UserName!,
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
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
