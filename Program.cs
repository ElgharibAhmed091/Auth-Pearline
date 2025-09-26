using AuthAPI.Data;
using AuthAPI.Models;
using AuthAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ===== Controllers + JSON options (one call only) =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

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

builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.UserIdClaimType = System.Security.Claims.ClaimTypes.NameIdentifier;
});

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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty)
        ),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

// ===== Swagger =====
builder.Services.AddEndpointsApiExplorer();
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

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b =>
        b.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader());
});

var app = builder.Build();

// ===== Swagger UI =====
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pearline API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseStaticFiles();

// ===== Routing + CORS + Auth (ordered) =====
app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ===== Diagnostics: ????? ??????? ??? assembly ??? ????? =====
try
{
    var asm = System.Reflection.Assembly.GetExecutingAssembly();
    var buildTime = System.IO.File.GetLastWriteTimeUtc(asm.Location).ToString("o");
    Console.WriteLine($"[Startup] Assembly: {asm.GetName().Name}, BuildTimeUTC: {buildTime}");
}
catch (Exception ex)
{
    Console.WriteLine($"[Startup] Unable to read assembly info: {ex.Message}");
}

// ===== Error Logging Middleware (keep as-is) =====
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode >= 400)
    {
        string logMessage =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error {context.Response.StatusCode} - " +
            $"Method: {context.Request.Method}, Path: {context.Request.Path}{context.Request.QueryString}";

        Console.ForegroundColor = context.Response.StatusCode switch
        {
            401 => ConsoleColor.Yellow,
            403 => ConsoleColor.DarkYellow,
            404 => ConsoleColor.Red,
            500 => ConsoleColor.DarkRed,
            _ => ConsoleColor.Gray
        };
        Console.WriteLine(logMessage);
        Console.ResetColor();

        System.Diagnostics.Debug.WriteLine(logMessage);
    }
});

app.Run();
