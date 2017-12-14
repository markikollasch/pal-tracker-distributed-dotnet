using System;
using System.Net.Http;
using DatabaseSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pivotal.Discovery.Client;
using Steeltoe.Extensions.Configuration;
using Steeltoe.CircuitBreaker.Hystrix;
using Timesheets;
using Steeltoe.Security.Authentication.CloudFoundry;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using AuthDisabler;

namespace TimesheetsServer
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddCloudFoundry()
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDiscoveryClient(Configuration);
            services.AddHystrixMetricsStream(Configuration);
            services.AddCloudFoundryJwtAuthentication(Configuration);
            // Add framework services.
            services.AddMvc();

            services.AddSingleton<IDataSourceConfig, DataSourceConfig>();
            services.AddSingleton<IDatabaseTemplate, DatabaseTemplate>();
            services.AddSingleton<ITimeEntryDataGateway, TimeEntryDataGateway>();
            
            services.AddSingleton<IProjectClient>(sp =>
            {
                var handler = new DiscoveryHttpClientHandler(sp.GetService<IDiscoveryClient>());
                var httpClient = new HttpClient(handler, false)
                {
                    BaseAddress = new Uri(Configuration.GetValue<string>("REGISTRATION_SERVER_ENDPOINT"))
                };

                var contextAccessor = sp.GetService<IHttpContextAccessor>();
                var logger = sp.GetService<ILogger<ProjectClient>>();
                return new ProjectClient(httpClient, logger,
                    () => contextAccessor.HttpContext.Authentication.GetTokenAsync("access_token")
                );
            });

            if(Configuration.GetValue("DISABLE_AUTH", false))
            {
                services.AddSingleton<IAuthorizationHandler>(sp => new AllowAllClaimsAuthorizationHandler());
            }

            services.AddAuthorization(options =>
                options.AddPolicy("pal-dotnet", policy => policy.RequireClaim("scope", "uaa.resource")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseCloudFoundryJwtAuthentication();
            app.UseMvc();
            app.UseDiscoveryClient();
            app.UseHystrixMetricsStream();
            app.UseHystrixRequestContext();
        }
    }
}