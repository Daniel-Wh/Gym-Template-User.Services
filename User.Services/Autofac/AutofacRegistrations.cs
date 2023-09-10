using System;
using Autofac;

namespace User.Services.Autofac
{
    public class AutofacRegistrations
    {
        private readonly ContainerBuilder _builder;

        public AutofacRegistrations(ContainerBuilder builder)
        {
            _builder = builder;
        }

        public AutofacRegistrations RegisterInfrastructure()
        {
            _builder.RegisterModule(new InfrastructureAutofacModule());
            return this;
        }

        public AutofacRegistrations Register()
        {
            return RegisterInfrastructure();
        }
    }
}

