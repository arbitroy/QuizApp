using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuizApp;
using QuizApp.Api.Controllers;
using QuizApp.Data;
using QuizApp.Middleware;
using QuizApp.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// This line needed the added package
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    // Keep cookie as the default for the web app
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ClockSkew = TimeSpan.Zero
    };
});
// Add CORS policy for API clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin() 
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Configure authentication cookies to prevent back-button issues after logout
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "RequireAdministratorRole");
    options.Conventions.AuthorizeFolder("/User"); // Authorize the User folder
});

// Add API controllers
builder.Services.AddControllers();

// Add Swagger for API documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "QuizApp API", 
        Version = "v1",
        Description = "API for the QuizApp platform",
        Contact = new OpenApiContact
        {
            Name = "Quiz App Support",
            Email = "support@quizapp.com"
        }
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});


builder.Services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();


builder.Services.AddScoped<IDbConnectionHealthCheck, DbConnectionHealthCheck>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdministratorRole", policy => policy.RequireRole("Administrator"));
});

// Add response caching settings
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 0; // No limit
    options.UseCaseSensitivePaths = false;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // This line needed the added package
    app.UseMigrationsEndPoint();
    
    // Add Swagger UI in development
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuizApp API v1"));
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();

// Add cache control middleware for authenticated pages
app.Use(async (context, next) =>
{
    // Apply no-cache headers for authenticated users
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
    
    await next();
});

app.UseRouting();
app.UseSession();
app.UseCors("ApiCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
// Map API controllers
app.MapControllers();

// Add authentication check endpoint for client-side validation
app.MapGet("/api/auth/check", (HttpContext context) => {
    if (context.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }
    return Results.Ok();
}).RequireAuthorization();

// Create roles and admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Create the database and apply migrations
        context.Database.Migrate();
        
        // Now seed the data after tables are created
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.Run();