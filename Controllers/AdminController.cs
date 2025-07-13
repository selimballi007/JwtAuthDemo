using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        [Authorize(Roles="Admin")]
        [HttpGet("dashboard")]
        public IActionResult GetAdminData()
        {
            return Ok(new
            {
                message = "Here is the Admin Dashboard Data",
                totalUsers = 124,
                activeSessions = 5
            });
        }
    }
}
