using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SonqoAPI.Data;
using SonqoAPI.Models;
using SonqoAPI.Service;
using SonqoAPI.Service.IService;
using System.Threading.RateLimiting;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Base de Datos
builder.Services.AddDbContext<SonqoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SonqoDbConnection")));

// Inyección de Dependencias
builder.Services.AddScoped(typeof(SonqoAPI.Repositories.IRepositories.IGenericRepository<>), typeof(SonqoAPI.Repositories.GenericRepository<>));
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IDishService, DishService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<ICashRegisterService, CashRegisterService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSignalR();

// RA40: Valores por defecto coherentes para el JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "ClaveDeRespaldoSeguraParaDesarrollo2026*!";
var issuer = jwtSettings["Issuer"] ?? "SonqoAPI";
var audience = jwtSettings["Audience"] ?? "SonqoAdmin";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/orderHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configuración unificada de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendApps", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://192.168.1.16:4200",
                "http://localhost:5173",
                "http://192.168.1.16:5173"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configuración de Swagger (.NET 10)
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Pega aquí SOLO tu token.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

// Políticas de Autorización
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", policy => policy.RequireRole("SuperAdmin", "Admin"))
    .AddPolicy("RequireKitchen", policy => policy.RequireRole("SuperAdmin", "Admin", "Kitchen"))
    .AddPolicy("RequireStaff", policy => policy.RequireRole("SuperAdmin", "Admin", "Kitchen", "Waiter"));

// --- CONFIGURACIÓN DE RATE LIMITING (Blindaje por IP) ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("OrderLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconocida",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromSeconds(60),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseWebSockets();
app.UseRouting();
app.UseCors("AllowFrontendApps");

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// RA27 / RC09: Nuestro Middleware Global de Excepciones
app.UseMiddleware<SonqoAPI.Middlewares.ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<SonqoAPI.Hubs.OrderHub>("/orderHub");
app.MapControllers();

app.Run();