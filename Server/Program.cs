using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using gameroombookingsys;
using gameroombookingsys.Interfaces;
using Gameroombookingsys.Repository;
using Gameroombookingsys.Services;
using gameroombookingsys.IRepository;
using gameroombookingsys.Repository;
using gameroombookingsys.Service;
using gameroombookingsys.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using gameroombookingsys.IService;
using Npgsql.EntityFrameworkCore.PostgreSQL;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var allowFrontEndCors = "AllowFrontend";
var apiTitle = "Game Room Booking API"; 
var apiVersion = "v1";  
var frontEndUrl = "http://localhost:5173";

// Setup Database Context (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repository & service registration
builder.Services.AddScoped<IPlayersRepository, PlayersRepository>();
builder.Services.AddScoped<IPlayersService, PlayersService>();
builder.Services.AddScoped<IRoomBookingsRepository, RoomBookingsRepository>();
builder.Services.AddScoped<IRoomBookingsService, RoomBookingsService>();
builder.Services.AddScoped<IDevicesRepository, DevicesRepository>();
builder.Services.AddScoped<IDevicesService, DevicesService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<KeycloakHelper>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// Enable Controllers & Endpoints
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// OpenAPI Configuration
builder.Services.AddSwaggerGen(options =>
{
    // Enable the annotations
    options.EnableAnnotations();

    options.SwaggerDoc(apiVersion, new OpenApiInfo
    {
        Title = apiTitle,
        Version = apiVersion,
        Description = "API documentation for managing game room bookings.",
        Contact = new OpenApiContact
        {
            Name = "Support Team",
            Email = "support@gameroombooking.com",
            Url = new Uri("https://gameroombooking.com/support")
        }
    });
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(allowFrontEndCors, policy =>
    {
        policy.WithOrigins(frontEndUrl)
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

var secret = "gameroombookingsys_gameroombookingsys";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
{
    KeyId = "1"
};

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
        };

        // Uncomment to add logging for JWT events
        //options.Events = new JwtBearerEvents
        //{
        //    OnAuthenticationFailed = context =>
        //    {
        //        var logger = context.HttpContext.RequestServices
        //            .GetRequiredService<ILoggerFactory>()
        //            .CreateLogger("JwtBearer");
        //        logger.LogError(context.Exception, "JWT Authentication failed!");
        //        return Task.CompletedTask;
        //    },
        //    OnTokenValidated = context =>
        //    {
        //        var logger = context.HttpContext.RequestServices
        //            .GetRequiredService<ILoggerFactory>()
        //            .CreateLogger("JwtBearer");
        //        logger.LogInformation("Token validated. Claims: {Claims}",
        //            string.Join(", ", context.Principal.Claims.Select(c => $"{c.Type}={c.Value}")));
        //        return Task.CompletedTask;
        //    }
        //};
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Apply automatic database creation in Development for PostgreSQL
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true
        && app.Environment.IsDevelopment())
    {
        db.Database.EnsureCreated();
    }
}

// Apply Middleware
app.UseCors(allowFrontEndCors);

// Enable OpenAPI UI. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint($"/swagger/{apiVersion}/swagger.json", apiTitle);
    });
}

// Avoid HTTPS redirection in development/containers where HTTPS isn't configured
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();