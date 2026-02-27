using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace App.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception on {Method} {Path}: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message, errors) = ex switch
            {
                // 400 - Validation lỗi (System.ComponentModel.DataAnnotations)
                ValidationException validationEx
                    => (HttpStatusCode.BadRequest,
                        validationEx.Message,
                        (object?)null),

                // 400 - FluentValidation (nếu dùng)
                // ValidationException fluentEx when fluentEx.Errors != null
                //     => (HttpStatusCode.BadRequest,
                //         "Dữ liệu không hợp lệ",
                //         fluentEx.Errors.Select(e => e.ErrorMessage).ToList()),

                // 400 - Argument không hợp lệ
                ArgumentException argEx
                    => (HttpStatusCode.BadRequest,
                        argEx.Message,
                        (object?)null),

                // 400 - InvalidOperation (business rule violation)
                InvalidOperationException invEx
                    => (HttpStatusCode.BadRequest,
                        invEx.Message,
                        (object?)null),

                // 404 - Không tìm thấy
                KeyNotFoundException keyEx
                    => (HttpStatusCode.NotFound,
                        keyEx.Message,
                        (object?)null),

                // 401 - Chưa xác thực
                UnauthorizedAccessException unAuthEx
                    => (HttpStatusCode.Unauthorized,
                         unAuthEx.Message,
                        (object?)null),

                // 403 - Không có quyền
                // ForbiddenException forbidEx  ← custom exception nếu cần
                //     => (HttpStatusCode.Forbidden,
                //         forbidEx.Message,
                //         (object?)null),

                // 500 - Mặc định
                _ => (HttpStatusCode.InternalServerError,
                      _env.IsDevelopment()
                          ? ex.Message          // Dev: trả full message
                          : "Lỗi hệ thống, vui lòng thử lại sau",  // Prod: ẩn detail
                      (object?)null)
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                success = false,
                message,
                errors,
                // Chỉ trả stack trace khi dev
                detail = _env.IsDevelopment() ? ex.StackTrace : null
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            await context.Response.WriteAsync(json);
        }
    }

    // ── Extension method để đăng ký gọn ──────────────────────────
    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionMiddleware(
            this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionMiddleware>();
        }
    }
}