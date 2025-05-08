using System.Collections.Concurrent;
using System.Globalization;
using CsvHelper;

using Giacom.Cdr.Domain;
using Giacom.Cdr.Domain.Contracts.Repository;
using Giacom.Cdr.Domain.Entities;

namespace Giacom.Cdr.IntegrationTests
{
    public class FakeCallDetailRepository : ICallDetailRepository
    {
        private ConcurrentBag<CallDetail> callDetails = new ConcurrentBag<CallDetail>();

        public Task<IEnumerable<CallDetail>> GetByCallerAsync(long? caller, int? take, CancellationToken cancellationToken)
        {
            var result = callDetails
                .Where(callDetails => caller == null || callDetails.Caller == caller)
                .Take(take ?? callDetails.Count);
            return Task.FromResult(result);
        }

        public Task IngestAsync(Stream stream, string fileId)
        {
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            new CsvDataReader(csv).ToCallDetails().ForEach(callDetails.Add);
            return Task.CompletedTask;
        }
    }
}