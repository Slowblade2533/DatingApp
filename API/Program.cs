using API.Data;
using API.Interfaces;
using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// เปิด Swagger เฉพาะตอน Dev
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen();
}
// =========================================================
// การตั้งค่า Forwarded Headers (สำหรับ IP & HTTPS)
// =========================================================
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // ให้อ่านค่า IP จริง (X-Forwarded-For) และ Protocol จริง (X-Forwarded-Proto เช่น https)
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // ⚠️ สำคัญมากสำหรับ Production:
    // ค่าเริ่มต้น ASP.NET จะเชื่อถือ Proxy ที่มาจาก Localhost (127.0.0.1) เท่านั้น
    // หากคุณรัน API ใน Docker, Kubernetes หรืออยู่หลัง Cloudflare / AWS ALB
    // คุณต้อง Clear ค่าข้างล่างนี้ทิ้ง เพื่อให้ระบบยอมรับ Header จาก Proxy ภายนอก
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// =========================================================
// การตั้งค่า Rate Limiting (ป้องกัน Brute Force / DoS)
// =========================================================
builder.Services.AddRateLimiter(options =>
{
    // ✅ เพิ่ม Global Rate Limit เป็น fallback
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>
    (context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
    // ถึงขั้นตอนนี้ context.Connection.RemoteIpAddress จะเป็น IP จริงของ User แล้ว
    // เพราะผ่าน ForwardedHeadersMiddleware มาแล้ว
    options.AddPolicy("LoginPolicy", context =>
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
        return RateLimitPartition.GetFixedWindowLimiter(remoteIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5, // อนุญาต 5 ครั้ง
            Window = TimeSpan.FromMinutes(1), // ต่อ 1 นาที (ต่อ 1 IP)
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0 // ถ้าเกินให้ตัดทิ้งทันที
        });
    });

    // ส่ง Status 429 กลับไปเมื่อถูก Block
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddControllers();
builder.Services.AddHealthChecks()
// Liveness: เช็คแค่ว่าแอพยังรันอยู่ไหม (ไม่ต่อ DB)
.AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
// Readiness: เช็ค DB (เอาไว้ให้ Load Balancer หรือ Docker/K8s เช็ค ไม่ใช่ให้ User เช็ค)
.AddDbContextCheck<AppDbContext>(tags: new[] { "ready" });
;
builder.Services.AddDbContextPool<AppDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var allowedOrigins = builder.Configuration
.GetSection("AllowOrigins")
.Get<string[]>()
?? ["https://yourdomain.com"];

if (builder.Environment.IsDevelopment())
{
    allowedOrigins = [..allowedOrigins, "http://localhost:4200", "https://localhost:4200"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .WithHeaders("Authorization", "Content-Type")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithExposedHeaders("Pagination");
    });
});
builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // ✅ ควรอ่านจาก Environment Variable หรือ Secret Manager
        // ❌ ถ้า appsettings.json ถูก commit ขึ้น Git = key หลุด
        var tokenKey = builder.Configuration["TokenKey"]
            ?? throw new InvalidOperationException("JWT TokenKey is not configured.");
        // ควรตรวจ minimum length ด้วย
        if (tokenKey.Length < 32)
            throw new InvalidOperationException("JWT TokenKey must be at least 32 characters.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey)),
            ValidateIssuer = true,
            ValidIssuer = "DatingApp-API",
            ValidateAudience = true,
            ValidAudience = "DatingApp-Client",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddOutputCache(options =>
{
    // ✅ แยก Cache ตาม User
    options.AddPolicy("Members", builder =>
        builder.Expire(TimeSpan.FromSeconds(30))
            .SetVaryByQuery("pageNumber", "pageSize")
            .SetVaryByHeader("Authorization"));
});

var app = builder.Build();

// =========================================================
// Middleware Pipeline (*** ลำดับบรรทัดสำคัญมาก ***)
// ✅ ลำดับที่ถูกต้อง
// 1. ForwardedHeaders
// 2. Security Headers
// 3. Exception Middleware
// 4. Routing
// 5. HSTS / Swagger
// 6. Rate Limiter
// 7. CORS
// 8. Output Cache    ← เพิ่ม
// 9. Authentication
// 10. Authorization
// 11. Map Controllers & Health Checks
// =========================================================

// 1. ต้องอยู่บนสุด เพื่อสลับ IP ปลอม (Proxy) เป็น IP จริง (User) ก่อนที่ Middleware อื่นจะทำงาน
app.UseForwardedHeaders();

// 2. Security Headers
app.Use(async (context, next) =>
{
    // ✅ เพิ่ม Headers ที่จำเป็น
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; frame-ancestors 'none';"
    );
    // ลบ Header ที่เปิดเผย Tech Stack
    context.Response.Headers.Remove("Server");
    context.Response.Headers.Remove("X-Powered-By");
    await next();
});

// 3. ดักจับ Error
app.UseMiddleware<ExceptionMiddleware>();

// 4. กำหนด Routing
app.UseRouting();

// 5. Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}
else
{
    app.UseHsts();
}

// 6. Rate Limiter ต้องอยู่หลัง Routing แต่ "ต้องอยู่ก่อน" Auth
// เพื่อไม่ให้เปลือง CPU ในการถอดรหัส Token หาก User คนนั้นโดน Block อยู่แล้ว
app.UseRateLimiter();

// 7. CORS
app.UseCors("ProductionPolicy");

// 8. Output Cache
app.UseOutputCache();

// 9. Authentication
app.UseAuthentication();

// 10. Authorization
app.UseAuthorization();

// 11. Map Controllers & Health Checks
app.MapControllers();

// ให้ Frontend (Angular) เรียกใช้แค่ตัวนี้พอ เพราะต้องการรู้แค่ว่า Server ยังไม่ตาย
app.MapHealthChecks("/api/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live"),
    AllowCachingResponses = true
});

// ให้ระบบ Infra (เช่น AWS ALB, Kubernetes) ใช้ตัวนี้ เพื่อเช็คก่อนโยน Traffic มาให้
// ✅ จำกัด access เฉพาะ internal network หรือใส่ Auth
app.MapHealthChecks("/api/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        // ไม่แสดง detail ถ้าเรียกจากภายนอก
        context.Response.ContentType = "application/json";
        var result = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy";
        await context.Response.WriteAsync($"{{\"status\":\"{result}\"}}");
    }
}).RequireAuthorization();

// seeding db for data test
/*
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    var context = services.GetRequiredService<AppDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasherService>();
    await context.Database.MigrateAsync();
    await Seed.SeedUsers(context, passwordHasher);
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occured during seeding");
}
*/
app.Run();
