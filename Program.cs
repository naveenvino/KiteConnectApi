using KiteConnectApi.Data;
using KiteConnectApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using KiteConnectApi.Hubs;
using Serilog;
using KiteConnectApi.Repositories;
using KiteConnect;
using KiteConnectApi.Models.Trading;
using Microsoft.OpenApi.Models;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

var contentRoot = builder.Environment.ContentRootPath;
var dbPath = Path.Combine(contentRoot, "KiteConnectApi.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"DataSource={dbPath}"));

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<NiftyOptionStrategyConfig>(builder.Configuration.GetSection("NiftyOptionStrategy"));
builder.Services.Configure<RiskParameters>(builder.Configuration.GetSection("RiskParameters"));

builder.Services.AddSingleton(new Kite(builder.Configuration["Kite:ApiKey"]));

bool useSimulated = builder.Configuration.GetValue<bool>("UseSimulatedServices");

// --- FIX: Always use the real repositories for database persistence ---
if (useSimulated)
{
    // Use the simulated service for placing trades, but it will save to the real DB.
    builder.Services.AddScoped<IKiteConnectService, SimulatedKiteConnectService>();
}
else
{
    builder.Services.AddScoped<IKiteConnectService, KiteConnectService>();
}

// Repositories are always scoped to the request and use the database.
builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
// --- END OF FIX ---


builder.Services.AddScoped<StrategyService>();
builder.Services.AddScoped<RiskManagementService>();
builder.Services.AddScoped<TechnicalAnalysisService>();
builder.Services.AddScoped<MarketDataService>();
builder.Services.AddScoped<INotificationService, EmailNotificationService>();

builder.Services.AddHostedService<TradingStrategyMonitor>();
builder.Services.AddHostedService<OrderMonitoringService>();
builder.Services.AddHostedService<ExpiryDayMonitor>();

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

var jwtEnabled = builder.Configuration.GetValue<bool>("Jwt:Enabled");

if (jwtEnabled)
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "KiteConnectApi", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }
        });
    });

    var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]!);
    builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
}
else
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "KiteConnectApi", Version = "v1" });
    });
}


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
        });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");

if (jwtEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();
app.MapHub<MarketDataHub>("/marketdatahub");

app.Run();
