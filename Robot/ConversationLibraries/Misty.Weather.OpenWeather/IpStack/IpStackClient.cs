using System.Net.Http;
using Conversation.Weather.OpenWeather.IpStack.Models;
using Newtonsoft.Json;

namespace Conversation.Weather.OpenWeather.IpStack
{
	public class IpStackClient
	{
		private const string BaseUri = "http://api.ipstack.com/";

		private readonly string _accessKey;
		private readonly bool _https;

		public IpStackClient(string accessKey, bool https)
		{
			_accessKey = accessKey;
			_https = https;
		}

		public IpAddressDetails GetRequesterIpAddressDetails()
		{
			HttpClient client = new HttpClient();

			string url = BaseUri + "check?access_key=" + _accessKey;
			string response = client.GetStringAsync(url).Result;

			return JsonConvert.DeserializeObject<IpAddressDetails>(response);
		}
	}
}