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
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy danh sách vai trò"
                });
            }
        }

        // GET: api/roles/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleDetail(Guid id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role detail {RoleId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = ex
                });
            }
        }

        // POST: api/roles
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex
                });
            }
        }

        // PUT: api/roles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleDto dto)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex
                });
            }
        }

        // DELETE: api/roles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex
                });
            }
        }

        // POST: api/roles/{id}/permissions
        [HttpPost("{id}/permissions")]
        public async Task<IActionResult> AssignPermissionsToRole(Guid id, [FromBody] AssignPermissionsDto dto)
        {
            try
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
           
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi gán quyền cho vai trò"
                });
            }
        }

        // DELETE: api/roles/{roleId}/permissions/{permissionId}
        [HttpDelete("{roleId}/permissions/{permissionId}")]
        public async Task<IActionResult> RemovePermissionFromRole(Guid roleId, Guid permissionId)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("select-role")]
        public async Task<IActionResult> GetSelectRoleQuery()
        {
            try
            {
                var query = new GetRolesForSelectQuery();
                var result = await _mediator.Send(query);
                return Ok(new
                {
                    success = true,
                    data = result,
                });
            }catch(Exception ex)
            {
                throw new Exception(ex.Message);
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