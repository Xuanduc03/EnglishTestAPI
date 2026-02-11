using App.Application.Auth.Commands;
using App.Application.Auth.Queries;
using App.Application.DTOs;
using App.Application.Users.Commands;
using App.Application.Users.Queries;
using App.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UserController> _logger;


        public UserController(IMediator mediator, ILogger<UserController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // get profile user 
        [HttpGet("profile/{userId}")]
        public async Task<IActionResult> GetProfileUser([FromRoute] Guid userId)
        {
            try
            {
                var query = new GetUserProfileQuery(userId);
                var result = await _mediator.Send(query);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("")]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query )
        {
            try
            {
                var result = await _mediator.Send(query);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Lấy danh sách người dùng thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserDetail(Guid id)
        {   
            try
            {
                var query = new GetUserDetailQuery(id);
                var result = await _mediator.Send(query);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Lấy thông tin người dùng thành công"
                });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // GET: api/users/{id}/permissions
        [HttpGet("{id}/permissions")]
        public async Task<IActionResult> GetUserPermissions(Guid id)
        {
            try
            {
                var query = new GetUserPermissionsQuery(id);
                var result = await _mediator.Send(query);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Lấy quyền của người dùng thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        // POST: api/users
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var command = new CreateUserCommand(dto, currentUserId);
                var userId = await _mediator.Send(command);

                return CreatedAtAction(nameof(GetUserDetail), new { id = userId }, new
                {
                    success = true,
                    data = new { userId },
                    message = "Tạo người dùng thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        // PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var command = new UpdateUserCommand(id, dto, currentUserId);
                await _mediator.Send(command);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật người dùng thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =ex.Message
                });
            }
        }


        // DELETE: api/users/{id}?hardDelete=false
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id, [FromQuery] bool hardDelete = false)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var command = new DeleteUserCommand(id, currentUserId, hardDelete);
                await _mediator.Send(command);

                return Ok(new
                {
                    success = true,
                    message = hardDelete ? "Xóa vĩnh viễn người dùng thành công" : "Xóa người dùng thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // POST: api/users/{id}/restore
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreUser(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var command = new RestoreUserCommand(id, currentUserId);
                await _mediator.Send(command);

                return Ok(new
                {
                    success = true,
                    message = "Khôi phục người dùng thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        // POST: api/users/{id}/roles
        [HttpPost("{id}/roles")]
        public async Task<IActionResult> AssignRolesToUser(Guid id, [FromBody] AssignRolesDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var command = new AssignRolesToUserCommand(id, dto.RoleIds, currentUserId);
                await _mediator.Send(command);

                return Ok(new
                {
                    success = true,
                    message = "Gán vai trò cho người dùng thành công"
                });
            }
           
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // DELETE: api/users/{userId}/roles/{roleId}
        [HttpDelete("{userId}/roles/{roleId}")]
        public async Task<IActionResult> RemoveRoleFromUser(Guid userId, Guid roleId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var command = new RemoveRoleFromUserCommand(userId, roleId, currentUserId);
                await _mediator.Send(command);

                return Ok(new
                {
                    success = true,
                    message = "Gỡ vai trò khỏi người dùng thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }

            return userId;
        }

    }
}
