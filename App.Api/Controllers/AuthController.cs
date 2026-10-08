using App.Application.Auth.Commands;
using App.Application.Auth.Queries;
using App.Application.DTOs;
using App.Application.Services.Interface;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public AuthController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (dto == null)
                return BadRequest("Dữ liệu gửi lên không hợp lệ.");

            var id = await _mediator.Send(new RegisterUserCommand(dto));

            return Ok(new
            {
                success = true,
                message = "Đăng ký tài khoản thành công!",
                userId = id
            });

        }


        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
        {

            if (string.IsNullOrEmpty(command.Email) || string.IsNullOrEmpty(command.Password))
                return BadRequest("Email và mật khẩu không được để trống.");

            var result = await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                message = "Đăng nhập thành công.",
                data = result
            });

        }


        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command)
        {

            var result = await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                message = "Đăng nhập thành công.",
                data = result
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
        {
            if (string.IsNullOrEmpty(command.Email))
                return BadRequest(new { message = "Email không được để trống" });


            var result = await _mediator.Send(command);
            return Ok(new
            {
                message = "Email khôi phục mật khẩu đã được gửi thành công!",
                token = result
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {

            var result = await _mediator.Send(command);
            return Ok(new
            {
                success = true,
                result = result
            });

        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {

            // Lấy userId từ token
            if (_currentUser.UserId is not Guid userId)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Token không hợp lệ"
                });
            }

            var command = new LogoutUserCommand(request.RefreshToken, userId);
            var result = await _mediator.Send(command);

            if (result)
            {
                return Ok(new
                {
                    success = true,
                    message = "Đăng xuất thành công"
                });
            }

            return BadRequest(new
            {
                success = false,
                message = "Không thể đăng xuất"
            });

        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefeshToken([FromBody] RefreshTokenCommand request, CancellationToken cancellation)
        {
            if (string.IsNullOrEmpty(request.refreshToken))
            {
                throw new Exception("Refresh token không được để trống");
            }

            var result = await _mediator.Send(
                new RefreshTokenCommand(request.refreshToken), cancellation);

            return Ok(result);
        }

    }
}
