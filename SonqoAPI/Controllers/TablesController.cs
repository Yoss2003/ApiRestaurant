using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Service.IService;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireStaff")]
    public class TablesController(ITableService tableService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAllTables()
        {
            var tables = await tableService.GetAllActiveTablesAsync();
            return Ok(tables);
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> CreateTable([FromBody] TableCreateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Number))
                return BadRequest("El número o nombre de la mesa es obligatorio.");

            var newTable = await tableService.CreateTableAsync(dto);
            return Ok(new { Message = "Mesa creada exitosamente.", Table = newTable });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> UpdateTable(int id, [FromBody] TableCreateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Number))
                return BadRequest("El número o nombre de la mesa es obligatorio.");

            var updatedTable = await tableService.UpdateTableAsync(id, dto);

            if (updatedTable == null)
                return NotFound("La mesa no existe o fue eliminada.");

            return Ok(new { Message = "Mesa actualizada exitosamente.", Table = updatedTable });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> DeleteTable(int id)
        {
            try
            {
                var success = await tableService.DeleteTableAsync(id);
                if (!success)
                    return NotFound("La mesa no existe o ya fue eliminada.");

                return Ok(new { Message = "Mesa eliminada correctamente." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}