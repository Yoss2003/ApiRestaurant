using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Service.IService;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RestaurantApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class AuditController(IAuditService auditService) : ControllerBase
    {
        private int GetCurrentRestaurantId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "RestaurantId");
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentLogs()
        {
            int restaurantId = GetCurrentRestaurantId();
            var logs = await auditService.GetRecentLogsAsync(restaurantId);
            return Ok(logs);
        }
    }
}