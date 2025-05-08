using System.Reflection;
using Mapster;
using Serilog;
using MediatR;

using Giacom.Cdr.Domain;
using Giacom.Cdr.Application;
using Giacom.Cdr.Application.Common;
using Giacom.Cdr.Infrastructure.Repository;



namespace Giacom.Cdr.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            RegisterServices(builder);

            var app = builder.Build();

            Configure(app);

            app.Run();
        }

        private static void RegisterServices(WebApplicationBuilder builder)
        {
            builder.Services.AddSerilog((services, lc) => lc.ReadFrom.Configuration(builder.Configuration));
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DiagnosticsPipelineBehavior<,>));
            builder.Services.Configure<CallDetailsOptions>(options => builder.Configuration.Bind("CallDetailOptions", options));
            builder.Services.AddAdxClassDetailRepository(builder.Configuration);
        }

        private static void Configure(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapControllers();

            // Mapster configuration
            TypeAdapterConfig.GlobalSettings.MapModels();
        }
    }
}
