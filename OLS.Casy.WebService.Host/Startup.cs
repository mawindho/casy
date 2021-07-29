using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OLS.Casy.Calculation.Volume;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Encryption;
using OLS.Casy.IO.SQLite.Standard;
using OLS.Casy.WebService.Host.Handlers;
using OLS.Casy.WebService.Host.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace OLS.Casy.WebService.Host
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
            }).AddNewtonsoftJson().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddAuthentication("BasicAuthentication")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "OLS CASY Remote Access Api",
                    Version = "v1",
                    Description = "OLS CASY Remote Access Api"
                });

                c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    In = ParameterLocation.Header,
                    Description = "Basic Authorization header."
                });
                c.OperationFilter<AuthOperationFilter>();

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
                c.CustomSchemaIds(r => r.FullName);
            });

            services.AddScoped(_ => new CasyContext(null, null, Configuration));
            services.AddSingleton(new MeasureResultDataCalculationService(new VolumeCalculationProvider()));
            services.AddSingleton<IEncryptionProvider>(new EncryptionProvider());
            //services.AddSingleton<LicenseService>();
            services.AddSingleton<UserService>();
            services.AddHostedService<CasyDetectionService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            //loggerFactory.AddDebug();

            app.UseCors(x => x
               .AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "OLS CASY Remote Access API v1");
                c.DefaultModelsExpandDepth(-1);
            });

            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
