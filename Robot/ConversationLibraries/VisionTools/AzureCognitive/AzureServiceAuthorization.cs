namespace VisionTools.AzureCognitive
{
	/// <summary>
	/// Azure Service Subscription authorization information
	/// </summary>
	public sealed class AzureServiceAuthorization
	{
		public string SubscriptionKey { get; set; }
		public string Region { get; set; }
		public string Endpoint { get; set; }
	}
}