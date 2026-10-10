using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TraceLite.Api.Controllers
{
    [Route("api/health")]
    [ApiController]
    public class HealthController : ControllerBase
    {

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "Healthy",
                service = "TraceLite.Api",
                timestampUtc = DateTime.UtcNow
            });
        }
    }
}
