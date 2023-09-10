using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SecretsManager.Extensions.Caching;
using AspNetRouteVersions;
using User.Services.Autofac;
using User.Services.Models;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace User.Services
{
    public class Startup
    {
        private static SecretCacheConfiguration secretsCacheConfig = new SecretCacheConfiguration
        {
            CacheItemTTL = 60000
        };
        private SecretsManagerCache cache = new SecretsManagerCache(secretsCacheConfig);
        private const string signingKey = "JWTSIGNINGKEY";

        public Startup(IWebHostEnvironment env)
        {
            if (env == null) return;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            WebHostEnvironment = env;

        }

        public static IConfiguration Configuration { get; private set; }

        public ILifetimeScope AutoFacContainer { get; private set; }

        public IWebHostEnvironment WebHostEnvironment { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            string key = Task.Run(async () => await cache.GetSecretString(signingKey)).Result;
            services.AddOptions();
            SigningKey convertedKey = JsonSerializer.Deserialize<SigningKey>(key);
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = "GTL-Auth", // need to change this and include it in app-config. Fine for np development for now
                        //ValidAudiences = new List<string> { "GTL-USER-SERVICES", "GTL-CLIENT-SERVICES", "GTL-AUTH-SERVICES"},
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
               .GetBytes(convertedKey.JWTSIGNINGKEY)),
                        ValidateIssuer = true,
                        ValidateAudience = false
                    };
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Default", new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build());
                options.AddPolicy("user", new AuthorizationPolicyBuilder()
                    .RequireRole("user")
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build());
                options.AddPolicy("owner", new AuthorizationPolicyBuilder()
                    .RequireRole("owner")
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build());
            });
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = "Standard Authorization header using the Bearer scheme (\"bearer {token}\") - Please add Bearer before the token to test"
                });

                options.OperationFilter<SecurityRequirementsOperationFilter>();
            });
            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());

            services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped(x =>
            {
                var actionCtx = x.GetRequiredService<IActionContextAccessor>().ActionContext;
                var factory = x.GetRequiredService<IUrlHelperFactory>();
                return factory.GetUrlHelper(actionCtx);
            });
            services.ConfigureRouteVersions(options =>
            {
                options.UseRoute = false;
                options.UseQuery = false;
                options.UseCustomHeader = false;
                options.UseAcceptHeader = false;

            });

            services.AddControllersWithViews();

        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            new AutofacRegistrations(builder).Register();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IHostApplicationLifetime applicationLifetime)
        {
            AutoFacContainer = app.ApplicationServices.GetAutofacRoot();

            var logger = AutoFacContainer.Resolve<ILogger<Startup>>();

            applicationLifetime.ApplicationStarted.Register(() =>
            {
                logger.LogInformation("Application startup");
            }
            );

            var loggerOptions = new LambdaLoggerOptions();
            loggerOptions.IncludeCategory = true;
            loggerOptions.IncludeException = true;
            loggerOptions.IncludeLogLevel = true;
            loggerOptions.IncludeNewline = true;

            loggerFactory.AddLambdaLogger(loggerOptions);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseMiddleware<LoggingPropertiesMiddleware>();

            app.UseHttpsRedirection().UseRouting().UseAuthentication().UseAuthorization().UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", async (context) => { await context.Response.WriteAsync("RUNNING"); });
            });
        }
    }
}

