using API.Data;
using API.Interfaces;
using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using ForwardedHeaderNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

const string CorsPolicy = "ProductionPolicy";
const string LoginRateLimitPolicy = "LoginPolicy";
const string MembersCachePolicy = "Members";
const string HealthCheckPolicy = "HealthCheckPolicy";

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;
var env = builder.Environment;

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

if (env.IsDevelopment())
{
    services.AddOpenApi();
}

ConfigureForwardedHeaders(services, config);

services.AddControllers();

var connectionString = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

services.AddDbContextPool<AppDbContext>(options => options.UseNpgsql(connectionString));

services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

var allowedOrigins = GetAllowedOrigins(config, env);
if (!env.IsDevelopment() && allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("AllowOrigins must contain at least one origin in production.");
}

services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .WithHeaders("Authorization", "Content-Type")
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithExposedHeaders("Pagination")
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = config.GetValue("RateLimit:Global:PermitLimit", 100),
                Window = TimeSpan.FromMinutes(config.GetValue("RateLimit:Global:WindowMinutes", 1)),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy(LoginRateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = config.GetValue("RateLimit:Login:PermitLimit", 5),
                Window = TimeSpan.FromMinutes(config.GetValue("RateLimit:Login:WindowMinutes", 1)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = WriteRateLimitResponseAsync;
});

services.AddOutputCache(options =>
{
    options.AddPolicy(MembersCachePolicy, policy => policy
    .Expire(TimeSpan.FromSeconds(30))
    .SetVaryByQuery("pageNumber", "pageSize"));
});

services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
services.AddScoped<ITokenService, TokenService>();
services.AddScoped<IMemberRepository, MemberRepository>();
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IAccountService, AccountService>();

var tokenKey = config["Jwt:TokenKey"] ?? config["TokenKey"]
    ?? throw new InvalidOperationException("JWT token key is not configured.");

var tokenKeyBytes = Encoding.UTF8.GetBytes(tokenKey);

if (tokenKeyBytes.Length < 64)
{
    throw new InvalidOperationException("JWT token key must be at least 64 bytes for HS512.");
}

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(tokenKeyBytes),
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"] ?? "DatingApp-API",
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audience"] ?? "DatingApp-Client",
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

services.AddAuthorization(options =>
{
    options.AddPolicy(HealthCheckPolicy, policy => policy.RequireAuthenticatedUser());
});

var app = builder.Build();

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    AddSecurityHeaders(context, app.Environment);
    await next();
});

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "API v1");
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().CacheOutput();
}

app.MapControllers();

app.MapHealthChecks("/api/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    AllowCachingResponses = false
});

app.MapHealthChecks("/api/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    AllowCachingResponses = false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy";
        await context.Response.WriteAsync($$"""{"status":"{{status}}"}""");
    }
}).RequireAuthorization(HealthCheckPolicy);

app.Run();

/***********/
static void ConfigureForwardedHeaders(IServiceCollection services, IConfiguration config)
{
    services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = config.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 1;

        var knownProxies = config.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
        var knownNetworks = config.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];

        if (knownProxies.Length > 0 || knownNetworks.Length > 0)
        {
            options.KnownProxies.Clear();
            options.KnownNetworks.Clear();
        }

        foreach (var proxy in knownProxies)
        {
            if (IPAddress.TryParse(proxy, out var address))
            {
                options.KnownProxies.Add(address);
            }
        }

        foreach (var network in knownNetworks)
        {
            var parts = network.Split('/', 2);
            if (parts.Length == 2
                && IPAddress.TryParse(parts[0], out var prefix)
                && int.TryParse(parts[1], out var prefixLength))
            {
                options.KnownNetworks.Add(new ForwardedHeaderNetwork(prefix, prefixLength));
            }
        }
    });
}

static string[] GetAllowedOrigins(IConfiguration config, IWebHostEnvironment env)
{
    var origins = config.GetSection("AllowOrigins").Get<string[]>() ?? [];

    if (env.IsDevelopment())
    {
        origins = [.. origins, "http://localhost:4200", "https://localhost:4200"];
    }

    return origins
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

static string GetClientIp(HttpContext context)
{
    var ip = context.Connection.RemoteIpAddress;
    if (ip is null)
    {
        return "unknown";
    }

    return ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4().ToString() : ip.ToString();
}

static void AddSecurityHeaders(HttpContext context, IWebHostEnvironment env)
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Permitted-Cross-Domain-Policies"] = "none";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    if (!env.IsDevelopment())
    {
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
    }
}

static async ValueTask WriteRateLimitResponseAsync(
    OnRejectedContext context,
    CancellationToken cancellationToken)
{
    var response = context.HttpContext.Response;
    response.StatusCode = StatusCodes.Status429TooManyRequests;
    response.ContentType = "application/problem+json";

    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
    {
        response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString();
    }

    await response.WriteAsJsonAsync(new
    {
        status = StatusCodes.Status429TooManyRequests,
        title = "Too many requests"
    }, cancellationToken);
}

/***********/