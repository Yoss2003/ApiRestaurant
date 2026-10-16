using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Service.IService;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireStaff")]
    public class TablesController(ITableService tableService) : ControllerBase
    {
        private int GetCurrentRestaurantId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "RestaurantId");
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTables()
        {
            int restaurantId = GetCurrentRestaurantId();
            var tables = await tableService.GetAllActiveTablesAsync(restaurantId);
            return Ok(tables);
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> CreateTable([FromBody] TableCreateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Number))
                return BadRequest("El número o nombre de la mesa es obligatorio.");

            int restaurantId = GetCurrentRestaurantId();
            var newTable = await tableService.CreateTableAsync(dto, restaurantId);
            return Ok(new { Message = "Mesa creada exitosamente.", Table = newTable });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> UpdateTable(int id, [FromBody] TableCreateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Number))
                return BadRequest("El número o nombre de la mesa es obligatorio.");

            int restaurantId = GetCurrentRestaurantId();
            var updatedTable = await tableService.UpdateTableAsync(id, dto, restaurantId);
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
                int restaurantId = GetCurrentRestaurantId();
                var success = await tableService.DeleteTableAsync(id, restaurantId);
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