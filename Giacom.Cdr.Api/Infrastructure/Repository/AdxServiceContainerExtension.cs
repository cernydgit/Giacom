using Giacom.Cdr.Application.Common;
using Giacom.Cdr.Domain.Contracts;
using Giacom.Cdr.Domain.Contracts.Repository;

namespace Giacom.Cdr.Infrastructure.Repository
{
    /// <summary>
    /// Provides extension methods for registering Azure Data Explorer (ADX) repository services in the dependency injection container.
    /// </summary>
    public static class AdxServiceContainerExtension
    {
        /// <summary>
        /// Adds the ADX class detail repository and its configuration to the service collection.
        /// </summary>
        /// <param name="services">The service collection to which the repository will be added.</param>
        /// <param name="configuration">The application configuration containing the ADX repository settings.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddAdxClassDetailRepository(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AdxCallDetailRepositoryOptions>(configuration.GetSection("AdxCallDetailRepository"));
            services.AddTransient<ICallDetailRepository, AdxClassDetailRepository>();
            services.AddScoped<IFactory<ICallDetailRepository>,Factory<ICallDetailRepository>>();
            return services;
        }
    }
}
