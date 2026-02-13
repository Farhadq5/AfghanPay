using AfghanPay.API.Data;
using AfghanPay.API.Hubs;
using AfghanPay.API.Services;
using AfghanPay.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using AfghanPay.Hubs;
using AfghanPay.Services.Interfaces;
using AfghanPay.Services;

var builder = WebApplication.CreateBuilder(args);


//database connection 
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString);

    // showing sql queries in development mode
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddSignalR();



// JWT Authentication Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

// Validate JWT settings
if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
{
    throw new InvalidOperationException("JWT settings are not properly configured in appsettings.json");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute tolerance
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            // Try to get token from cookie first
            var accessToken = context.Request.Cookies["jwt"];
            logger.LogInformation($"Checking JWT cookie: {(accessToken != null ? "Found" : "Not Found")}");

            // Fallback to Authorization header
            if (string.IsNullOrEmpty(accessToken) &&
            context.HttpContext.Request.Path.StartsWithSegments("/cashoutHub"))
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    accessToken = authHeader.Substring("Bearer ".Length).Trim();
                    logger.LogInformation("JWT found in Authorization header");
                }
            }

            // Fallback to query string (for SignalR)
            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    logger.LogInformation("JWT found in query string");
                }
            }

            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
                logger.LogInformation("Token set for validation");
            }
            else
            {
                logger.LogWarning("No JWT token found in request");
            }

            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError($"Authentication failed: {context.Exception.GetType().Name} - {context.Exception.Message}");

            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Headers.Add("Token-Expired", "true");
                logger.LogWarning("Token has expired");
            }

            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userName = context.Principal?.Identity?.Name ?? "Unknown";
            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            logger.LogInformation($"Token validated successfully for user: {userName} (ID: {userId})");
            return Task.CompletedTask;
        },

        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning($"Authentication challenge triggered. Path: {context.Request.Path}");
            return Task.CompletedTask;
        }
    };
});

// Add authorization
builder.Services.AddAuthorization();



//dependecy injections
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICashOutService, CashOutService>();
builder.Services.AddSingleton<IUserIdProvider, SubUserIdProvider>();
builder.Services.AddScoped<IAdminEventsBrodcaster, AdminEventsBrodcaster>();

// cors configuration (allow front end apps to access api)

var mvcOrigins = builder.Configuration.GetSection("Cors:MvcOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCors", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("https://localhost:7177")     // Allow any frontend (Flutter, Blazor)
              .AllowAnyMethod()      // Allow GET, POST, PUT, DELETE, etc.
              .AllowAnyHeader()    // Allow any headers
              .SetIsOriginAllowed(origin => true) // Allow all origins (for development)
              .AllowCredentials();
        }
        else
        {
            policy.WithOrigins("https://localhost:7177")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();

        }

    });

});



// conntrollers and API explorer
builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep property names as-is
});
builder.Services.AddEndpointsApiExplorer();

// swagger configuration(Api documentation)

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AfghanPay API",
        Version = "v1",
        Description = "API documentation for AfghanPay (MVP) application with P2P, Cash-in and Cash-out",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "AfghanPay Team",
            Email = "Farhad@gmail.com"
        }
    });

    //add JWT authentication to swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token. Example: Bearer eyJhbGc..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

// logging configuration

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

//buil the app

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

//middleware pipeline configuration

//devolopment specific middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AfghanPay API v1");
        options.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });

    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error"); //production error handling
    app.UseHsts();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment() || mvcOrigins.Length > 0)
{
    app.UseCors("ApiCors");
}

app.UseAuthentication(); // check jwt token

app.UseAuthorization(); // checl user permissions/roles

app.MapHub<CashoutHub>("/cashoutHub");
app.MapHub<NotificationHub>("/notifyHub");
app.MapHub<AdminHub>("/adminHub");

app.MapControllers();

// database initialization and automatic migrations

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();

            // Apply any pending migrations
            if (context.Database.GetPendingMigrations().Any())
            {
                Console.WriteLine("Applying pending migrations...");
                context.Database.Migrate();
                Console.WriteLine("Migrations applied successfully!");
            }
            else
            {
                Console.WriteLine("Database is up to date.");
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}
else
{
    Console.WriteLine("Running in Production environment. Applying database migrations...");
}



app.Run();
