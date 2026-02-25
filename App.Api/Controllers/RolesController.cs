using App.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using App.Application.Users.Commands;
using App.Application.Users.Queries;

namespace App.API.Controllers
{
    [ApiController]
    [Route("api/roles")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IMediator mediator, ILogger<RolesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // GET: api/roles?includePermissions=true
        [HttpGet("")]
        public async Task<IActionResult> GetRoles([FromQuery] bool includePermissions = false)
        {

            var query = new GetRolesQuery(includePermissions);
            var result = await _mediator.Send(query);

            return Ok(new
            {
                success = true,
                data = result,
                message = "Lấy danh sách vai trò thành công"
            });

        }

        // GET: api/roles/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleDetail(Guid id)
        {

            var query = new GetRoleDetailQuery(id);
            var result = await _mediator.Send(query);

            return Ok(new
            {
                success = true,
                data = result,
                message = "Lấy thông tin vai trò thành công"
            });

        }

        // POST: api/roles
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {

            var currentUserId = GetCurrentUserId();
            var command = new CreateRoleCommand(dto, currentUserId);
            var roleId = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetRoleDetail), new { id = roleId }, new
            {
                success = true,
                data = new { roleId },
                message = "Tạo vai trò thành công"
            });

        }

        // PUT: api/roles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleDto dto)
        {

            var currentUserId = GetCurrentUserId();
            var command = new UpdateRoleCommand(id, dto, currentUserId);
            await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                message = "Cập nhật vai trò thành công"
            });
        }

        // DELETE: api/roles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {

            var currentUserId = GetCurrentUserId();
            var command = new DeleteRoleCommand(id, currentUserId);
            await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                message = "Xóa vai trò thành công"
            });

        }

        // POST: api/roles/{id}/permissions
        [HttpPost("{id}/permissions")]
        public async Task<IActionResult> AssignPermissionsToRole(Guid id, [FromBody] AssignPermissionsDto dto)
        {

            var currentUserId = GetCurrentUserId();
            var command = new AssignPermissionsToRoleCommand(id, dto.PermissionIds, currentUserId);
            await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                message = "Gán quyền cho vai trò thành công"
            });

        }

        // DELETE: api/roles/{roleId}/permissions/{permissionId}
        [HttpDelete("{roleId}/permissions/{permissionId}")]
        public async Task<IActionResult> RemovePermissionFromRole(Guid roleId, Guid permissionId)
        {

            var currentUserId = GetCurrentUserId();
            var command = new RemovePermissionFromRoleCommand(roleId, permissionId, currentUserId);
            await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                message = "Gỡ quyền khỏi vai trò thành công"
            });

        }

        [HttpGet("select-role")]
        public async Task<IActionResult> GetSelectRoleQuery()
        {

            var query = new GetRolesForSelectQuery();
            var result = await _mediator.Send(query);
            return Ok(new
            {
                success = true,
                data = result,
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