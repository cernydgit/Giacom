using MediatR;
using System.Diagnostics;

namespace Giacom.Cdr.Application.Mediator
{
    public class DiagnosticsPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
            where TRequest : notnull
    {
        private readonly ILogger<IMediator> logger;

        public DiagnosticsPipelineBehavior(ILogger<IMediator> logger)
        {
            this.logger = logger;
        }

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
