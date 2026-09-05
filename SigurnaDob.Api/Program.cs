using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SigurnaDob.Api.Configuration;
using SigurnaDob.Api.Data;
using SigurnaDob.Api.Security;
using SigurnaDob.Api.Services;
using SigurnaDob.Api.Services.Ai;
using SigurnaDob.Api.Swagger;
using SigurnaDob.Shared.Constants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<SwaggerOperationFilter>();

    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT token nakon logina. Zalijepi samo accessToken iz /api/auth/login."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});
builder.Services.AddDbContext<SigurnaDobDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>()
                 ?? throw new InvalidOperationException("Nedostaje Jwt konfiguracija.");

if (jwtOptions.SigningKey.Length < 32)
    throw new InvalidOperationException(
        "Jwt:SigningKey mora imati najmanje 32 znaka.");

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddScoped<JwtTokenService>();
builder.Services.Configure<FileUploadOptions>(
    builder.Configuration.GetSection(FileUploadOptions.SectionName));
builder.Services.AddScoped<ResidentMediaStorageService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ChangeHistoryService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<RoomOccupancyService>();
builder.Services.AddScoped<CaregiverWorkloadService>();
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.AddScoped<AiInsightsService>();

var aiProvider = builder.Configuration.GetSection(AiOptions.SectionName).GetValue<string>("Provider") ?? "Mock";
if (string.Equals(aiProvider, "OpenAI", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddHttpClient<IAiService, OpenAiAiService>();
else
    builder.Services.AddScoped<IAiService, MockAiService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(
        AuthorizationPolicies.Staff,
        policy => policy.RequireRole(
            AppRoles.Admin,
            AppRoles.Coordinator,
            AppRoles.Caregiver));

    options.AddPolicy(
        AuthorizationPolicies.AdminOnly,
        policy => policy.RequireRole(AppRoles.Admin));

    options.AddPolicy(
        AuthorizationPolicies.CoordinatorOrAdmin,
        policy => policy.RequireRole(AppRoles.Admin, AppRoles.Coordinator));
});

var appOrigin = builder.Configuration["Cors:AppOrigin"]
                ?? "https://localhost:7096";

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorApp", policy =>
        policy.WithOrigins(appOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SigurnaDobDbContext>();
    await db.Database.MigrateAsync();
    await DemoDataSeeder.SeedAsync(db);
    await AppUserSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.IndexStream = () => File.OpenRead(
            Path.Combine(app.Environment.ContentRootPath, "Swagger", "index.html"));
    });
}

app.UseHttpsRedirection();
app.UseCors("BlazorApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
