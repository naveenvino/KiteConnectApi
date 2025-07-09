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
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<NiftyOptionStrategyConfig>(builder.Configuration.GetSection("NiftyOptionStrategy"));
builder.Services.Configure<RiskParameters>(builder.Configuration.GetSection("RiskParameters"));

builder.Services.AddSingleton(new Kite(Environment.GetEnvironmentVariable("KiteConnect__ApiKey"), Environment.GetEnvironmentVariable("KiteConnect__ApiSecret")));

bool useSimulated = builder.Configuration.GetValue<bool>("UseSimulatedServices");

// --- FIX: Ensure simulated services use simulated repositories ---
if (useSimulated)
{
    // Use the simulated service for placing trades and simulated repositories for data persistence.
    builder.Services.AddScoped<IKiteConnectService, SimulatedKiteConnectService>();
    builder.Services.AddScoped<IPositionRepository, SimulatedPositionRepository>();
    builder.Services.AddScoped<IOrderRepository, SimulatedOrderRepository>();
}
else
{
    // Use the real KiteConnectService and real repositories for live trading.
    builder.Services.AddScoped<KiteConnectService>(); // Register the concrete implementation
    builder.Services.AddScoped<IKiteConnectService, KiteConnectPolicyService>(provider =>
        new KiteConnectPolicyService(provider.GetRequiredService<KiteConnectService>(), provider.GetRequiredService<ILogger<KiteConnectPolicyService>>()));
    builder.Services.AddScoped<IPositionRepository, PositionRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IStrategyConfigRepository, StrategyConfigRepository>();
builder.Services.AddScoped<StrategyManagerService>();
builder.Services.AddScoped<PortfolioAllocationService>();
}
// --- END OF FIX ---


builder.Services.AddScoped<StrategyService>();
builder.Services.AddScoped<RiskManagementService>();
builder.Services.AddScoped<TechnicalAnalysisService>();
builder.Services.AddScoped<MarketDataService>();
builder.Services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
builder.Services.AddScoped<IScreenerCriteriaRepository, ScreenerCriteriaRepository>();
builder.Services.AddScoped<EmailNotificationService>();
builder.Services.AddScoped<SmsNotificationService>();
builder.Services.AddScoped<TelegramNotificationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<MarketScreenerService>();
builder.Services.AddScoped<ExternalDataService>();

builder.Services.AddHostedService<TradingStrategyMonitor>();
builder.Services.AddHostedService<OrderMonitoringService>();
builder.Services.AddHostedService<ExpiryDayMonitor>();

builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
});
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
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "KiteConnectApi v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Added to serve static files
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
