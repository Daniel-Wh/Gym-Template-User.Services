using System;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using System.Text;
using System.Text.Json;
using Autofac.Extensions.DependencyInjection;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace User.Services
{
    public class LambdaEntryPoint
    {

        static LambdaEntryPoint()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        }

        public async Task<Stream> FunctionHandlerAsync(Stream stream, ILambdaContext context)
        {
            string body;

            using (var sr = new StreamReader(stream))
            {
                body = await sr.ReadToEndAsync();
            }

            object resp;
            var version = GetRequestVersion(body);

            if (version == "2.0")
            {
                var apiGwRequest = JsonSerializer.Deserialize<APIGatewayHttpApiV2ProxyRequest>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                resp = await LambdaEntryPointHttpApiGwv2.Instance.FunctionHandlerAsync(apiGwRequest,
                    context);
            }
            else
            {
                var apiGwRequest = JsonSerializer.Deserialize<APIGatewayProxyRequest>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                resp = await LambdaEntryPointApiGwv1.Instance.FunctionHandlerAsync(apiGwRequest,
                    context);
            }

            var ms = new MemoryStream();

            await using (var sw = new StreamWriter(ms, Encoding.UTF8, 1024, true))
            {
                await JsonSerializer.SerializeAsync(ms, resp);
            }

            ms.Position = 0;

            return ms;
        }

        private string GetRequestVersion(string reqBody)
        {
            var version = "1.0";

            using (var jsonDoc = JsonDocument.Parse(reqBody))
            {
                var requestVersion = jsonDoc.RootElement.EnumerateObject().Where(p => p.Name == "version")
                    .Select(p => p.Value.ToString());

                if (requestVersion.Any() && requestVersion.First() == "2.0")
                {
                    version = "2.0";
                }
            }

            return version;
        }
    }

    /// <summary>
    ///     This class extends from APIGatewayProxyFunction which contains the method FunctionHandlerAsync which is the
    ///     actual Lambda function entry point. The Lambda handler field should be set to
    ///     auth.services::auth.services.LambdaEntryPoint::FunctionHandlerAsync.
    /// </summary>
    public class LambdaEntryPointApiGwv1 :

        // When using an ELB's Application Load Balancer as the event source change
        // the base class to Amazon.Lambda.AspNetCoreServer.ApplicationLoadBalancerFunction
        APIGatewayProxyFunction
    {
        private static readonly Lazy<LambdaEntryPointApiGwv1> lazyInstance = new(() => new LambdaEntryPointApiGwv1());

        public static LambdaEntryPointApiGwv1 Instance => lazyInstance.Value;

        public Task<APIGatewayProxyResponse> FunctionHandlerAsync(
            APIGatewayProxyRequest request,
            ILambdaContext lambdaContext)
        {
            return ActualFunctionHandlerAsync(request, lambdaContext);
        }

        public Task<APIGatewayProxyResponse> ActualFunctionHandlerAsync(APIGatewayProxyRequest request,
            ILambdaContext lambdaContext)
        {
            return base.FunctionHandlerAsync(request, lambdaContext);
        }

        /// <summary>
        ///     The builder has configuration, logging and Amazon API Gateway already configured. The startup class
        ///     needs to be configured in this method using the UseStartup() method.
        /// </summary>
        /// <param name="builder"></param>
        protected override void Init(IHostBuilder builder)
        {
            builder
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webHostbuilder =>
                {
                    webHostbuilder.ConfigureLogging(logging =>
                    {
                        // logging.AddAWSProvider();
                        logging.SetMinimumLevel(LogLevel.Information);
                    })
                        .UseStartup<Startup>();
                });
        }
    }

    public class LambdaEntryPointHttpApiGwv2 :

        // When using an ELB's Application Load Balancer as the event source change
        // the base class to Amazon.Lambda.AspNetCoreServer.ApplicationLoadBalancerFunction
        APIGatewayHttpApiV2ProxyFunction
    {
        private static readonly Lazy<LambdaEntryPointHttpApiGwv2> lazyInstance =
            new(() => new LambdaEntryPointHttpApiGwv2());

        public static LambdaEntryPointHttpApiGwv2 Instance => lazyInstance.Value;

        public Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandlerAsync(
            APIGatewayHttpApiV2ProxyRequest request,
            ILambdaContext lambdaContext)
        {
            return ActualFunctionHandlerAsync(request, lambdaContext);
        }

        public Task<APIGatewayHttpApiV2ProxyResponse> ActualFunctionHandlerAsync(
            APIGatewayHttpApiV2ProxyRequest request,
            ILambdaContext lambdaContext)
        {
            return base.FunctionHandlerAsync(request, lambdaContext);
        }

        /// <summary>
        ///     The builder has configuration, logging and Amazon API Gateway already configured. The startup class
        ///     needs to be configured in this method using the UseStartup() method.
        /// </summary>
        /// <param name="builder"></param>
        protected override void Init(IHostBuilder builder)
        {
            builder
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webHostbuilder =>
                {
                    webHostbuilder.ConfigureLogging(logging =>
                    {
                        // logging.AddAWSProvider();
                        logging.SetMinimumLevel(LogLevel.Information);
                    })
                        .UseStartup<Startup>();
                });
        }
    }
}
