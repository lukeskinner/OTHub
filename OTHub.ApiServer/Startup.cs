﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using OTHub.Settings;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.Swagger;

namespace OTHub.APIServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            var settings = Configuration.Get<OTHubSettings>();

            settings.Validate();

            services.AddMemoryCache();

            List<string> origins = new List<string>();

#if DEBUG
            origins.Add("http://localhost:4200");
#endif

            origins.Add(OTHubSettings.Instance.WebServer.AccessControlAllowOrigin);
        

            services.AddSwaggerExamples();
            services.AddSwaggerExamplesFromAssemblyOf<OfferExample>();
            services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                    builder =>
                    {
                        builder.WithOrigins(origins.Distinct().ToArray()).AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetPreflightMaxAge(TimeSpan.FromDays(7));
                    });
            });

            //if (Program.IsTestNet)
            //{
            //    services.AddSignalR();
            //    //services.AddHostedService<DockerMonitorService>();
            //    //services.AddHostedService<JobCreatorService>();
            //    //services.AddHostedService<MarketTickerService>();
            //}

            services.AddMvc().AddJsonOptions(opt =>
            {
                opt.SerializerSettings.ContractResolver = new DefaultContractResolver { NamingStrategy = new DefaultNamingStrategy() };
            });

            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.ExampleFilters();
                c.SwaggerDoc(OTHubSettings.Instance.Blockchain.Network.ToString().ToLower(), new Info { Title = "OT Hub", Version = "1.0.0" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //app.UsePerformanceLog();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });


            app.UseSwagger(c =>
            {
                c.RouteTemplate = "docs/{documentName}/swagger.json";
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwaggerUI(c =>
            {
                c.DocumentTitle = "OT Hub - API Documentation";
                c.RoutePrefix = "docs";
                c.SwaggerEndpoint($"/docs/{OTHubSettings.Instance.Blockchain.Network.ToString().ToLower()}/swagger.json", $"OT Hub {OTHubSettings.Instance.Blockchain.Network.ToString()}");
            });

            app.UseCors(MyAllowSpecificOrigins);

            //if (Program.IsTestNet)
            //{
            //    app.UseSignalR(route => { route.MapHub<LogHub>("/signalr/testnet/log"); });
            //}

            app.UseMvc();
        }
    }
}