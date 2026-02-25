using App.Application.Categories.Commands;
using App.Application.Categories.Queries;
using App.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("api/categories")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetList([FromQuery] GetCategoriesQuery query)
        {

            var result = await _mediator.Send(query);
            return Ok(new
            {
                success = true,
                data = result
            });

        }


        // GET: api/categories/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {

            var query = new GetCategoryDetailQuery(id);
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });

        }


        // POST: api/categories
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {

            var command = new CreateCategoryCommand(dto);
            var id = await _mediator.Send(command);
            return Ok(new { success = true, data = new { id }, message = "Tạo thành công" });

        }

        // PUT: api/categories/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto)
        {

            var command = new UpdateCategoryCommand(id, dto);
            await _mediator.Send(command);
            return Ok(new { success = true, message = "Cập nhật thành công" });

        }

        // DELETE: api/categories/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {

            var command = new DeleteCategoryCommand(id);
            await _mediator.Send(command);
            return Ok(new { success = true, message = "Xóa thành công" });

        }

        [HttpGet("code-type")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCodeType([FromQuery] string codeType, Guid parentId)
        {

            if (string.IsNullOrWhiteSpace(codeType))
                return BadRequest("CodeType không được để trống");

            var query = new GetCategoriesByCodeTypeQuery(codeType, parentId);
            var result = await _mediator.Send(query);
            return Ok(new
            {
                success = true,
                data = result,
            });

        }

        // get select category 
        [HttpGet("select")]
        public async Task<IActionResult> GetCategorySelect([FromQuery] string? codeType)
        {

            var query = new GetCategorySelectQuery(codeType);
            var result = await _mediator.Send(query);
            return Ok(new
            {
                success = true,
                data = result,
            });

        }

    }
}
