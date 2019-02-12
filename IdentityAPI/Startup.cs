using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityAPI.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using IdentityAPI.Helpers;
using IdentityAPI.Services;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;

namespace IdentityAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            this.IdentitySettings = new ConfigurationBuilder()
               .AddJsonFile(configuration["IdentitySettingsFile"].ToString(), optional: true, reloadOnChange: true)
               .Build();
        }

        public IConfiguration Configuration { get; }
        public IConfiguration IdentitySettings { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();

            services.AddDbContext<IdentityContext>(options =>
                options.UseSqlite(IdentitySettings["ConnectionStrings:DefaultConnection"])
            );

            // JWT Token Settings
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "roman015.com",
                    ValidAudience = "roman015.com",
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(IdentitySettings["TokenKey"]))
                };
            });

            // External Helpers/Servicecs
            services.AddScoped<ITotpHelper, TotpHelper>();

            // Services
            services.AddScoped<IAuthenticationService, AuthenticationService>();

            // Repositories
            services.AddScoped<IAccountRepository, AccountRepository>();

            // Config File
            services.AddSingleton<IConfiguration>(Configuration);

            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Formatting = Formatting.Indented;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // For Generating Swagger json
            services.AddSwaggerGen(config =>
            {
                config.SwaggerDoc("api", new Info { Title = "Authentication", Version = "v1" });

                config.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });

                config.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>()
                {
                    {"Bearer", new string[]{} }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
