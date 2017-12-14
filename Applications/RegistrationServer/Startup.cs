﻿using Accounts;
using DatabaseSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pivotal.Discovery.Client;
using Steeltoe.Extensions.Configuration;
using Projects;
using Users;
using Steeltoe.Security.Authentication.CloudFoundry;
using Microsoft.AspNetCore.Authorization;
using AuthDisabler;

namespace RegistrationServer
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
            services.AddCloudFoundryJwtAuthentication(Configuration);
            // Add framework services.
            services.AddMvc();

            services.AddSingleton<IDataSourceConfig, DataSourceConfig>();
            services.AddSingleton<IDatabaseTemplate, DatabaseTemplate>();
            services.AddSingleton<IAccountDataGateway, AccountDataGateway>();
            services.AddSingleton<IProjectDataGateway, ProjectDataGateway>();
            services.AddSingleton<IUserDataGateway, UserDataGateway>();
            services.AddSingleton<IRegistrationService, RegistrationService>();

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
        }
    }
}