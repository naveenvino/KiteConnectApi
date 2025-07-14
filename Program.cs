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
using Microsoft.AspNetCore.Mvc;
using Hangfire;
using Hangfire.SqlServer;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;
using FluentValidation.AspNetCore;
using FluentValidation;
using OpenTelemetry.Trace;
using Orleans.Hosting;
using Orleans.Configuration;
using KiteConnectApi; // Added for MapsterConfig
using KiteConnectApi.ML;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<NiftyOptionStrategyConfig>(builder.Configuration.GetSection("NiftyOptionStrategy"));
builder.Services.Configure<RiskParameters>(builder.Configuration.GetSection("RiskParameters"));



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
    builder.Services.AddHttpClient<IKiteConnectService, KiteConnectService>()
    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(new[]
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    }))
    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30)
    ));
    builder.Services.AddScoped<IPositionRepository, PositionRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IStrategyConfigRepository, StrategyConfigRepository>();
builder.Services.AddScoped<IStrategyRepository, StrategyRepository>();
builder.Services.AddScoped<IStrategyConfigRepository, StrategyConfigRepository>();
builder.Services.AddScoped<IStrategyRepository, StrategyRepository>();
builder.Services.AddScoped<StrategyManagerService>();
builder.Services.AddScoped<PortfolioAllocationService>();
}
// --- END OF FIX ---


builder.Services.AddScoped<INiftyOptionStrategyConfigRepository, NiftyOptionStrategyConfigRepository>();
builder.Services.AddScoped<IManualTradingViewAlertRepository, ManualTradingViewAlertRepository>();
builder.Services.AddScoped<ITradingStrategyService, TradingStrategyService>();
builder.Services.AddScoped<BacktestingService>();
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
builder.Services.AddScoped<ITradeExecutionService, TradeExecutionService>();
builder.Services.AddSingleton<PricePredictionService>();
builder.Services.AddSingleton<VaultService>();


builder.Services.AddSingleton<StackExchange.Redis.ConnectionMultiplexer>(sp =>
{
    var configuration = StackExchange.Redis.ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("RedisConnection")!);
    return StackExchange.Redis.ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddScoped(sp => sp.GetRequiredService<StackExchange.Redis.ConnectionMultiplexer>().GetDatabase());
builder.Services.AddScoped<ICacheService, RedisCacheService>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();


// Configure Mapster
MapsterConfig.RegisterMappings();

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("PubSubStore");
});

builder.Services.AddHostedService<TradingStrategyMonitor>();
builder.Services.AddHostedService<OrderMonitoringService>();
builder.Services.AddHostedService<ExpiryDayMonitor>();

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection")));

builder.Services.AddHangfireServer();

builder.Services.AddControllers();

builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddConsoleExporter()
        .AddJaegerExporter(o =>
        {
            o.AgentHost = "localhost";
            o.AgentPort = 6831;
        }));
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
            Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}",

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
        // --- FIX: Add logic to accept token from query string for SignalR ---
        x.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/marketdatahub")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
        // --- END OF FIX ---
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
            .WithOrigins("http://localhost:4200")
            .AllowCredentials()
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