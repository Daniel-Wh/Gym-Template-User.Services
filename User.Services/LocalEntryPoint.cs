using Autofac.Extensions.DependencyInjection;

namespace User.Services
{
    public class LocalEntryPoint
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder => { _ = webBuilder.UseStartup<Startup>(); })
                .ConfigureLogging(options => options.SetMinimumLevel(LogLevel.Debug));
        }
    }
}


