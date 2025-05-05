using Microsoft.AspNetCore.Mvc;

namespace Giacom.Cdr.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CallDetailsController : ControllerBase
    {
       
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            return Ok();
        }
    }
}