using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace User.Services.Services
{
	public class AWSCredentialsService
	{
		public readonly AWSCredentials credentials;
		public AWSCredentialsService()
		{
            var chain = new CredentialProfileStoreChain();
            
            if (chain.TryGetAWSCredentials("default", out credentials))
            {
               
            }
            else
            {
                credentials = new EnvironmentVariablesAWSCredentials();

            }
        }
	}
}

