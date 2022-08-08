namespace SlugEnt.ResourceHealthChecker
{
	public enum EnumHealthCheckerType
	{
		Database = 0,
		FileSystem = 1,
		RabbitMQ = 2,
		Redis = 3,
		ExternalAPI = 4,
		Dummy = 254,
	}
}
