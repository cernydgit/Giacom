using MediatR;
using System.Diagnostics;

namespace Giacom.Cdr.Application.Mediator
{
    /// <summary>
    /// A pipeline behavior for MediatR that logs the execution time and errors of requests.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class DiagnosticsPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
            where TRequest : notnull
    {
        private readonly ILogger<IMediator> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsPipelineBehavior{TRequest, TResponse}"/> class.
        /// </summary>
        /// <param name="logger">The logger used to log request information and errors.</param>
        public DiagnosticsPipelineBehavior(ILogger<IMediator> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Handles the MediatR request by logging its execution time and any errors that occur.
        /// </summary>
        /// <param name="request">The request being handled.</param>
        /// <param name="next">The next delegate in the pipeline.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The response from the next delegate in the pipeline.</returns>
        /// <exception cref="Exception">Throws any exception that occurs during request handling.</exception>
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting MediatR request: {Request}", typeof(TRequest).Name);
            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                return await next();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing MediatR request: {Request}", typeof(TRequest).Name);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation("Finished MediatR request: {Request} in {ElapsedMilliseconds} ms", typeof(TRequest).Name, stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }


}
