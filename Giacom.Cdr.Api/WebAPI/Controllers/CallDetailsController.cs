using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Giacom.Cdr.Application.Handlers;
using Giacom.Cdr.Domain.Entities;




namespace Giacom.Cdr.WebAPI.Controllers
{
    /// <summary>
    /// API controller for managing CDR
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CallDetailsController : ControllerBase
    {
        private const long MaxFileSize = 100L * 1024L * 1024L * 1024L; // 100GB

        private readonly IMessageBus sender;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallDetailsController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender for handling requests.</param>
        public CallDetailsController(IMessageBus sender)
        {
            this.sender = sender;
        }

        /// <summary>
        /// Uploads a CSV file containing call details.
        /// </summary>
        /// <param name="file">The CSV file to upload.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.</returns>
        [HttpPost("upload")]
        [RequestSizeLimit(MaxFileSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("CSV file is required.");
            }
            using var stream = file.OpenReadStream();
            await sender.InvokeAsync(new UploadCallDetailsRequest(stream, file.FileName));
            return Ok();
        }

        /// <summary>
        /// Retrieves call details filtered by caller ID.
        /// </summary>
        /// <param name="caller">The caller ID to filter the results by. If null, no filtering is applied.</param>
        /// <param name="take">The maximum number of records to return. If null, no limit is applied.</param>
        /// <returns>A collection of <see cref="CallDetail"/> objects representing the call details.</returns>
        [HttpGet("getByCaller")]
        public Task<IEnumerable<CallDetail>> GetByCaller([FromQuery] long? caller, [FromQuery] int? take)
        {
            return sender.InvokeAsync<IEnumerable<CallDetail>>(new QueryCallDetailsRequest(caller, take));
        }
    }
}