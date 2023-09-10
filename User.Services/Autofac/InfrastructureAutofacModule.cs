using System;
using Autofac;

namespace User.Services.Autofac
{
	public class InfrastructureAutofacModule: Module
	{
        protected override void Load(ContainerBuilder builder)
        {
            var loggerFactory = new LoggerFactory();
            builder.RegisterInstance(loggerFactory).As<ILoggerFactory>().SingleInstance();
            builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();
        }
    }
}

