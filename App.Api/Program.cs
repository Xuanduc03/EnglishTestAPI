using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using App.Application.Interfaces;
using FluentValidation;
using App.Infrastructure.Cloudinary;
using App.Application.Questions.Services;
using App.Application.Questions.Services.Interfaces;
using App.API.Middleware;
using App.Application.Services.Interface;
using App.Application.Services;
using App.Application.Validators;
using App.Application.ExamAttempts.Commands;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

builder.Services.Configure<CloudinaryOptions>(
    builder.Configuration.GetSection("Cloudinary"));

builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

builder.Services.AddScoped<IAppDbContext>(provider =>
    provider.GetRequiredService<AppDbContext>());

builder.Services.AddControllers();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(
                "http://localhost:5173",
                "https://english-test-fe-six.vercel.app"
                )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<StartExamCommand>();  // Assembly chứa commands/queries
    // cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()); // Nếu cùng assembly

    // Thêm ValidationBehavior vào pipeline (chạy trước handler)

    // Optional: thêm logging behavior nếu bạn có
    // cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập 'Bearer [space] token' để xác thực."
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

// Rate limiting cho chống brute-force (e.g., 5 requests/1 min per IP)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });


// Lấy assembly chứa các Handler của bạn
var appApplicationAssembly = typeof(App.Application.Auth.Commands.RegisterUserCommandHandler).Assembly;

// Đăng ký MediatR và tự động tìm tất cả Handler trong assembly đó

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(appApplicationAssembly));
builder.Services.AddAutoMapper(appApplicationAssembly);
builder.Services.AddValidatorsFromAssembly(appApplicationAssembly);

// service import excel question
builder.Services.AddValidatorsFromAssemblyContaining<StartExamCommandValidator>();
builder.Services.AddScoped<IUtilExcelService, UtilExcelService>();
builder.Services.AddScoped<IExcelQuestionParserService, ExcelQuestionParserService>();
builder.Services.AddScoped<IExcelZipParser, ExcelZipParserService>();
builder.Services.AddScoped<IExcelZipImportService, ExcelZipImportService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();
app.UseExceptionMiddleware();

// Quan trọng: Phải gọi UseAuthentication() TRƯỚC UseAuthorization()
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();

app.Run();