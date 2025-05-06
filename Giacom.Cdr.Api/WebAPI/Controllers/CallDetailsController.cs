using Microsoft.AspNetCore.Mvc;
using MediatR;
using Giacom.Cdr.Application.Handlers;
using Giacom.Cdr.Application.DTOs;


namespace Giacom.Cdr.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CallDetailsController : ControllerBase
    {
        private const long MaxFileSize = 100L * 1024L * 1024L * 1024L; // 100GB

        private readonly ISender sender;

        public CallDetailsController(ISender sender)
        {
            this.sender = sender;
        }


        
        [HttpPost("upload")]
        [RequestSizeLimit(MaxFileSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("CSV file is required.");
            }
            var compressed = file.FileName != null && Path.GetExtension(file.FileName) == ".gz";
            using var stream = file.OpenReadStream();
            await sender.Send(new UploadCallDetailsRequest(stream, compressed));
            return Ok();
        }

        [HttpGet]
        public Task<IEnumerable<CallDetailDto>> GetAll([FromQuery] string? caller, [FromQuery] int? take)
        {
            return sender.Send(new QueryCallDetailsRequest(caller, take));
        }
    }
}