// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Linq;
using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;

using Mh.WebAPI.Core.Data;
using Mh.WebAPI.Core.Models;

namespace IdentityServer
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var connectionString = Configuration["Db:ConnectionString"];
            var version = Configuration["Db:Version"];

            // uncomment, if you want to add an MVC-based UI
            //services.AddControllersWithViews();
            services.AddControllers();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySql(connectionString, mySqlOptions =>
                    mySqlOptions.ServerVersion(version).MigrationsAssembly(migrationAssembly));
            });

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var builder = services.AddIdentityServer(options =>
            {
                var eventOptions = options.Events;
                eventOptions.RaiseErrorEvents = true;
                eventOptions.RaiseInformationEvents  = true;
                eventOptions.RaiseFailureEvents  = true;
                eventOptions.RaiseSuccessEvents  = true;

                // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                options.EmitStaticAudienceClaim = true;
            })
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                    {
                        builder.UseMySql(connectionString, mySqlOptions =>
                            mySqlOptions.ServerVersion(version).MigrationsAssembly(migrationAssembly));
                    };
                })
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                    {
                        builder.UseMySql(connectionString, mySqlOptions =>
                            mySqlOptions.ServerVersion(version).MigrationsAssembly(migrationAssembly));
                    };
                })
                .AddJwtBearerClientAuthentication();

            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();

            services.AddAuthentication()
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = "http://localhost:5000";
                    options.ClientId = "client";
                    options.ClientSecret = "secret";

                    options.RequireHttpsMetadata = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {

                    };
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            InitializeDatabase(app);

            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // uncomment if you want to add MVC
            //app.UseStaticFiles();
            //app.UseRouting();
            
            app.UseIdentityServer();

            // uncomment, if you want to add MVC
            //app.UseAuthorization();
            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapDefaultControllerRoute();
            //});
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                context.Database.Migrate();
                if (!context.Clients.Any())
                {
                    foreach (var client in Config.Clients)
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Config.IdentityResources)
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiScopes.Any())
                {
                    foreach (var resource in Config.ApiScopes)
                    {
                        context.ApiScopes.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}
