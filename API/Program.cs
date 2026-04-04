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
builder.Services.AddCors();
builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var tokenKey = builder.Configuration["TokenKey"]
            ?? throw new Exception("Token key not found - Program.cs");

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
    options.AddPolicy("Members", builder =>
        builder.Expire(TimeSpan.FromSeconds(30))
            .SetVaryByQuery("pageNumber", "pageSize"));
});

var app = builder.Build();

// =========================================================
// Middleware Pipeline (*** ลำดับบรรทัดสำคัญมาก ***)
// =========================================================

// 1. ต้องอยู่บนสุด เพื่อสลับ IP ปลอม (Proxy) เป็น IP จริง (User) ก่อนที่ Middleware อื่นจะทำงาน
app.UseForwardedHeaders();

// 2. Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// 3. ดักจับ Error
app.UseMiddleware<ExceptionMiddleware>();

// 3. กำหนด Routing
app.UseRouting();

// 3.1 Configure the HTTP request pipeline
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

// 4. Rate Limiter ต้องอยู่หลัง Routing แต่ "ต้องอยู่ก่อน" Auth
// เพื่อไม่ให้เปลือง CPU ในการถอดรหัส Token หาก User คนนั้นโดน Block อยู่แล้ว
app.UseRateLimiter();

// 5. CORS
app.UseCors(x => x.AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Pagination")
    .WithOrigins("http://localhost:4200", "https://localhost:4200"));

// 6. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 7. Map Controllers & Health Checks
app.MapControllers();

// ให้ Frontend (Angular) เรียกใช้แค่ตัวนี้พอ เพราะต้องการรู้แค่ว่า Server ยังไม่ตาย
app.MapHealthChecks("/api/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live"),
    AllowCachingResponses = true
});

// ให้ระบบ Infra (เช่น AWS ALB, Kubernetes) ใช้ตัวนี้ เพื่อเช็คก่อนโยน Traffic มาให้
app.MapHealthChecks("/api/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

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
