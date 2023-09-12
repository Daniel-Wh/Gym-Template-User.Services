using System;
using EfficientDynamoDb;
using EfficientDynamoDb.Configs;
using EfficientDynamoDb.Credentials.AWSSDK;

namespace User.Services.Services
{
	public class DynamoDbClientService
	{
		public DynamoDbContext _client;
		public DynamoDbClientService(AWSCredentialsService creds)
		{
			var config = new DynamoDbContextConfig(RegionEndpoint.USEast1, new AWSCredentialsProvider(creds.credentials));
			_client = new DynamoDbContext(config);
		}
	}
}

