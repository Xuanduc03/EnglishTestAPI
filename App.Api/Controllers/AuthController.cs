using App.Application.Auth.Commands;
using App.Application.Auth.Queries;
using App.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi trong quá trình đăng ký. Vui lòng thử lại sau.",
                    detail = ex.Message
                });
            }
            
        }


        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(401, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);

                return Ok(new
                {
                    success = true,
                    message = "Đăng nhập thành công.",
                    data = result
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
        {
            if (string.IsNullOrEmpty(command.Email))
                return BadRequest(new { message = "Email không được để trống" });

            try
            {
                var result = await _mediator.Send(command);
                return Ok(new
                {
                    message = "Email khôi phục mật khẩu đã được gửi thành công!",
                    token = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }



        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return Ok(new
                {
                    success = true,
                    result = result
                });
            }catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                // Lấy userId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi đăng xuất"
                });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefeshToken([FromBody]RefreshTokenCommand request, CancellationToken cancellation)
        {
            if (string.IsNullOrEmpty(request.refreshToken))
            {
                throw new Exception("Refresh token không được để trống");
            }
            try
            {
                var result = await _mediator.Send(
                    new RefreshTokenCommand(request.refreshToken), cancellation);

                return Ok(result);
            }catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex
                });
            }
        }


    }
}
