using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NuciAPI.Middleware.ExceptionHandling;
using NuciAPI.Middleware.Logging;
using NuciAPI.Middleware.Security;

namespace GptActionsOrchestrator
{
    public sealed class Startup(IConfiguration configuration)
    {
        public IConfiguration Configuration => configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services
                .AddNuciApiScannerProtection()
                .AddConfigurations(Configuration)
                .AddCustomServices();
        }

        public void Configure(IApplicationBuilder application, IWebHostEnvironment environment)
        {
            application.UseNuciApiExceptionHandling();
            application.UseNuciApiScannerProtection();
            application.UseNuciApiRequestLogging();

            if (environment.IsDevelopment())
            {
                application.UseDeveloperExceptionPage();
            }

            application.UseHttpsRedirection();
            application.UseDefaultFiles();
            application.UseStaticFiles();
            application.UseRouting();
            application.UseAuthorization();

            application.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
