using AuthAPI.Data;
using AuthAPI.Models;
using AuthAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== Database =====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== Identity =====
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ===== JWT =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtBearer";
    options.DefaultChallengeScheme = "JwtBearer";
})
.AddJwtBearer("JwtBearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        ),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

// ===== Swagger =====
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Pearline API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please insert JWT token with Bearer prefix (Example: Bearer {token})",
        Name = "Authorization",
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
            Array.Empty<string>()
        }
    });
});

// ===== Email Service =====
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ===== Dev-only Seeder: ensure roles + SuperAdmin (uses config if present, else defaults) =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        // Only run the automatic creation in Development environment to avoid accidental creation in Production.
        if (app.Environment.IsDevelopment())
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var config = services.GetRequiredService<IConfiguration>();

            async Task SeedAsync()
            {
                // roles to ensure
                var roles = new[] { "User", "Admin", "SuperAdmin" };
                foreach (var r in roles)
                {
                    if (!await roleManager.RoleExistsAsync(r))
                    {
                        var rm = await roleManager.CreateAsync(new IdentityRole(r));
                        if (rm.Succeeded)
                            logger.LogInformation("Created role {Role}", r);
                        else
                            logger.LogWarning("Failed creating role {Role}: {Errors}", r, string.Join(", ", rm.Errors.Select(e => e.Description)));
                    }
                }

                // Attempt to read SuperAdmin credentials from configuration first
                var superEmailFromConfig = config["SuperAdmin:Email"];
                var superUserNameFromConfig = config["SuperAdmin:UserName"];
                var superPasswordFromConfig = config["SuperAdmin:Password"];

                // Fallback defaults for local development (change these as needed)
                var superEmail = !string.IsNullOrWhiteSpace(superEmailFromConfig) ? superEmailFromConfig : "superadmin@pearline.com";
                var superUserName = !string.IsNullOrWhiteSpace(superUserNameFromConfig) ? superUserNameFromConfig : superEmail;
                var superPassword = !string.IsNullOrWhiteSpace(superPasswordFromConfig) ? superPasswordFromConfig : "Super@123";

                var existing = await userManager.FindByEmailAsync(superEmail);
                if (existing == null)
                {
                    var super = new ApplicationUser
                    {
                        UserName = superUserName!,
                        Email = superEmail,
                        EmailConfirmed = true,
                        FirstName = "Super",
                        LastName = "Admin"
                    };

                    var createRes = await userManager.CreateAsync(super, superPassword);
                    if (createRes.Succeeded)
                    {
                        await userManager.AddToRoleAsync(super, "SuperAdmin");
                        logger.LogInformation("SuperAdmin created: {Email}", superEmail);
                    }
                    else
                    {
                        logger.LogError("Failed creating SuperAdmin: {Errors}", string.Join(", ", createRes.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    // Ensure user has the SuperAdmin role
                    var rolesOfExisting = await userManager.GetRolesAsync(existing);
                    if (!rolesOfExisting.Contains("SuperAdmin"))
                    {
                        var addRoleRes = await userManager.AddToRoleAsync(existing, "SuperAdmin");
                        if (addRoleRes.Succeeded)
                            logger.LogInformation("Added SuperAdmin role to existing user {Email}", superEmail);
                        else
                            logger.LogError("Failed to add SuperAdmin role to {Email}: {Errors}", superEmail, string.Join(", ", addRoleRes.Errors.Select(e => e.Description)));
                    }
                    else
                    {
                        logger.LogInformation("SuperAdmin already exists and has role: {Email}", superEmail);
                    }
                }
            }

            // Run seeding synchronously during startup
            SeedAsync().GetAwaiter().GetResult();
        }
        else
        {
            logger.LogInformation("Skipping dev-only SuperAdmin seeder because environment is not Development.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error running SuperAdmin seeder");
    }
}

// ===== Middleware =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
