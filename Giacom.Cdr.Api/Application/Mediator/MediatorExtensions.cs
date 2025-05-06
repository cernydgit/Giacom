using MediatR;
using System.Reflection;

namespace Giacom.Cdr.Application.Mediator
{
    public static class MediatorExtensions
    {
        public static IServiceCollection AddMediator(this IServiceCollection services,Assembly assembly)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DiagnosticsPipelineBehavior<,>));
            return services;
        }
    }
}
