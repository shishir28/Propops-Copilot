using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PropOpsCopilot.Api.Configuration;
using PropOpsCopilot.Application;
using PropOpsCopilot.Infrastructure;
using PropOpsCopilot.Infrastructure.Identity;
using PropOpsCopilot.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection(PortalJwtOptions.SectionName).Get<PortalJwtOptions>()
    ?? throw new InvalidOperationException("JWT settings are missing.");
var aiServiceOptions = builder.Configuration.GetSection(AiServiceOptions.SectionName).Get<AiServiceOptions>()
    ?? throw new InvalidOperationException("AI service settings are missing.");

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);
builder.Services.Configure<AiServiceOptions>(builder.Configuration.GetSection(AiServiceOptions.SectionName));
builder.Services.AddHttpClient(
    "propops-ai",
    client =>
    {
        client.BaseAddress = new Uri(aiServiceOptions.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(60);
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "frontend",
        policy =>
        {
            policy.WithOrigins(
                    "http://localhost:4200",
                    "http://127.0.0.1:4200",
                    "http://localhost:4315",
                    "http://127.0.0.1:4315",
                    "http://host.docker.internal:4200",
                    "http://host.docker.internal:4315")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        JwtBearerDefaults.AuthenticationScheme,
        new()
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Provide the bearer token returned by /api/auth/login."
        });

    options.AddSecurityRequirement(
        new()
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = JwtBearerDefaults.AuthenticationScheme
                    }
                },
                Array.Empty<string>()
            }
        });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<PropOpsDataSeeder>();
    await seeder.SeedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet(
    "/health",
    () => Results.Ok(new { status = "healthy", service = "propops-api" }));

app.Run();
