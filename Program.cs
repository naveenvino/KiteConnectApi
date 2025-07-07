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
using System.IO; // Required for Path.Combine

var builder = WebApplication.CreateBuilder(args);

// --- MODIFICATION: Set up an absolute path for the database ---
var contentRoot = builder.Environment.ContentRootPath;
var dbPath = Path.Combine(contentRoot, "KiteConnectApi.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"DataSource={dbPath}"));
// --- END OF MODIFICATION ---

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure strongly typed settings objects
builder.Services.Configure<NiftyOptionStrategyConfig>(builder.Configuration.GetSection("NiftyOptionStrategy"));
builder.Services.Configure<RiskParameters>(builder.Configuration.GetSection("RiskParameters"));

// Register KiteConnect client
builder.Services.AddSingleton(new Kite(builder.Configuration["Kite:ApiKey"]));

// Register repositories and services with dependency injection
bool useSimulated = builder.Configuration.GetValue<bool>("UseSimulatedServices");

if (useSimulated)
{
    builder.Services.AddScoped<IKiteConnectService, SimulatedKiteConnectService>();
    builder.Services.AddScoped<IPositionRepository, SimulatedPositionRepository>();
    builder.Services.AddScoped<IOrderRepository, SimulatedOrderRepository>();
}
else
{
    builder.Services.AddScoped<IKiteConnectService, KiteConnectService>();
    builder.Services.AddScoped<IPositionRepository, PositionRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
}

builder.Services.AddScoped<StrategyService>();
builder.Services.AddScoped<RiskManagementService>();
builder.Services.AddScoped<TechnicalAnalysisService>();
builder.Services.AddScoped<MarketDataService>();
builder.Services.AddScoped<INotificationService, EmailNotificationService>();

// Register hosted services for background tasks
builder.Services.AddHostedService<TradingStrategyMonitor>();
builder.Services.AddHostedService<OrderMonitoringService>();
builder.Services.AddHostedService<ExpiryDayMonitor>();


builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

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

// Configure JWT Authentication
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

// Configure CORS
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

// This will create and update the database every time the app starts.
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


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MarketDataHub>("/marketdatahub");

app.Run();
