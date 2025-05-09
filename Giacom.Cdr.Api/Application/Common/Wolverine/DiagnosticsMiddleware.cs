using System.Diagnostics;
using Wolverine;

namespace Giacom.Cdr.Application.Common.Wolverine
{
    /// <summary>
    /// Middleware for logging the processing of messages, including timing and error handling.
    /// </summary>
    public class DiagnosticsMiddleware
    {
        private readonly ILogger<DiagnosticsMiddleware> logger;
        private readonly Stopwatch stopwatch = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsMiddleware"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used for logging information and errors.</param>
        public DiagnosticsMiddleware(ILogger<DiagnosticsMiddleware> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Called just before any handler executes. Logs the start of message processing.
        /// </summary>
        /// <param name="envelope">The envelope containing the message and metadata.</param>
        public void Before(Envelope envelope)
        {
            logger.LogInformation(
                "Starting processing message {Message} (envelope {EnvelopeId})",
                envelope.Message, envelope.Id);
            stopwatch.Restart();
        }

        /// <summary>
        /// Called if an exception is thrown during message handling. Logs the error details.
        /// </summary>
        /// <param name="exception">The exception that was thrown.</param>
        /// <param name="envelope">The envelope containing the message and metadata.</param>
        public void OnException(Exception exception, Envelope envelope)
        {
            stopwatch.Stop();
            logger.LogError(
                exception,
                "Error processing message {Message} (envelope {EnvelopeId}) after {ElapsedMilliseconds} ms",
                envelope.Message, envelope.Id, stopwatch.ElapsedMilliseconds
            );
        }

        /// <summary>
        /// Called in a finally block after handlers execute. Logs the completion of message processing.
        /// </summary>
        /// <param name="envelope">The envelope containing the message and metadata.</param>
        public void Finally(Envelope envelope)
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Processed message {Message} (envelope {EnvelopeId}) in {ElapsedMilliseconds} ms",
                envelope.Message, envelope.Id, stopwatch.ElapsedMilliseconds
            );
        }
    }
}

