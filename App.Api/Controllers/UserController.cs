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

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // get profile user 
        [HttpGet("profile/{userId}")]
        public async Task<IActionResult> GetProfileUser([FromRoute] Guid userId)
        {
            var query = new GetUserProfileQuery(userId);
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });

        }

        [HttpGet("")]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query)
        {

            var result = await _mediator.Send(query);

            return Ok(new
            {
                success = true,
                data = result,
                message = "Lấy danh sách người dùng thành công"
            });

        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserDetail(Guid id)
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

        // GET: api/users/{id}/permissions
        [HttpGet("{id}/permissions")]
        public async Task<IActionResult> GetUserPermissions(Guid id)
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


        // POST: api/users
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
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
        // PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
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


        // DELETE: api/users/{id}?hardDelete=false
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id, [FromQuery] bool hardDelete = false)
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

        // POST: api/users/{id}/restore
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreUser(Guid id)
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


        // POST: api/users/{id}/roles
        [HttpPost("{id}/roles")]
        public async Task<IActionResult> AssignRolesToUser(Guid id, [FromBody] AssignRolesDto dto)
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

        // DELETE: api/users/{userId}/roles/{roleId}
        [HttpDelete("{userId}/roles/{roleId}")]
        public async Task<IActionResult> RemoveRoleFromUser(Guid userId, Guid roleId)
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
