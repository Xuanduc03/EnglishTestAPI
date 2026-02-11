using App.Application.Users.Queries;
using App.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [ApiController]
    [Route("api/permission")]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(IMediator mediator, ILogger<PermissionsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // GET: api/permissions?module=User
        [HttpGet]
        [Authorize(Policy = "ViewPermissions")]
        public async Task<IActionResult> GetPermissions([FromQuery] string? module = null)
        {
            try
            {
                var query = new GetPermissionsQuery(module);
                var result = await _mediator.Send(query);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Lấy danh sách quyền thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy danh sách quyền"
                });
            }
        }
    }
}