/**********************************************************************
	Copyright 2021 Misty Robotics
	Licensed under the Apache License, Version 2.0 (the "License");
	you may not use this file except in compliance with the License.
	You may obtain a copy of the License at
		http://www.apache.org/licenses/LICENSE-2.0
	Unless required by applicable law or agreed to in writing, software
	distributed under the License is distributed on an "AS IS" BASIS,
	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	See the License for the specific language governing permissions and
	limitations under the License.
	**WARRANTY DISCLAIMER.**
	* General. TO THE MAXIMUM EXTENT PERMITTED BY APPLICABLE LAW, MISTY
	ROBOTICS PROVIDES THIS SAMPLE SOFTWARE "AS-IS" AND DISCLAIMS ALL
	WARRANTIES AND CONDITIONS, WHETHER EXPRESS, IMPLIED, OR STATUTORY,
	INCLUDING THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
	PURPOSE, TITLE, QUIET ENJOYMENT, ACCURACY, AND NON-INFRINGEMENT OF
	THIRD-PARTY RIGHTS. MISTY ROBOTICS DOES NOT GUARANTEE ANY SPECIFIC
	RESULTS FROM THE USE OF THIS SAMPLE SOFTWARE. MISTY ROBOTICS MAKES NO
	WARRANTY THAT THIS SAMPLE SOFTWARE WILL BE UNINTERRUPTED, FREE OF VIRUSES
	OR OTHER HARMFUL CODE, TIMELY, SECURE, OR ERROR-FREE.
	* Use at Your Own Risk. YOU USE THIS SAMPLE SOFTWARE AND THE PRODUCT AT
	YOUR OWN DISCRETION AND RISK. YOU WILL BE SOLELY RESPONSIBLE FOR (AND MISTY
	ROBOTICS DISCLAIMS) ANY AND ALL LOSS, LIABILITY, OR DAMAGES, INCLUDING TO
	ANY HOME, PERSONAL ITEMS, PRODUCT, OTHER PERIPHERALS CONNECTED TO THE PRODUCT,
	COMPUTER, AND MOBILE DEVICE, RESULTING FROM YOUR USE OF THIS SAMPLE SOFTWARE
	OR PRODUCT.
	Please refer to the Misty Robotics End User License Agreement for further
	information and full details:
		https://www.mistyrobotics.com/legal/end-user-license-agreement/
**********************************************************************/

using System.Net.Http;
using Conversation.Weather.OpenWeather.IpStack;
using Conversation.Weather.OpenWeather.IpStack.Models;
using Newtonsoft.Json;

namespace Conversation.Weather.OpenWeather
{
	public class OpenWeatherService : IWeatherService
	{
		private string _ipStackApiKey;
		private string _openWeatherApiKey;

		private static string _addressCity;
		private static string _addressCountry;

		public OpenWeatherService(string openWeatherApiAuth, string ipStackApiAuth)
		{
			_ipStackApiKey = ipStackApiAuth;
			_openWeatherApiKey = openWeatherApiAuth;
		}
				
		public CurrentWeather GetCurrentWeather(string city, string countryCode)
		{
			try
			{
				if(string.IsNullOrWhiteSpace(_openWeatherApiKey))
				{
					return null;
				}

				if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(countryCode))
				{
					//Get override info
					_addressCity = city;
					_addressCountry = countryCode;
				}
				else if (string.IsNullOrWhiteSpace(_ipStackApiKey) || string.IsNullOrWhiteSpace(_openWeatherApiKey))
				{
					return null;
				}

				if (string.IsNullOrWhiteSpace(_addressCity) || string.IsNullOrWhiteSpace(_addressCountry))
				{
					IpStackClient client = new IpStackClient(_ipStackApiKey, false);
					IpAddressDetails addressDetails = client.GetRequesterIpAddressDetails();
					
					if (string.IsNullOrWhiteSpace(addressDetails.city) || string.IsNullOrWhiteSpace(addressDetails.country_code))
					{
						return null;
					}

					_addressCity = addressDetails.city;
					_addressCountry = addressDetails.country_code;
				}
				
				if(string.IsNullOrEmpty(_addressCity) || string.IsNullOrEmpty(_addressCountry))
				{
					return null;
				}
				
				// api.openweathermap.org/data/2.5/weather?q={city name},{state code}&appid={your api key}&units=imperial
				string url = $"http://api.openweathermap.org/data/2.5/weather?q={_addressCity},{_addressCountry}&appid={_openWeatherApiKey}&units=imperial";

				HttpClient httpClient = new HttpClient();
				string responseData = httpClient.GetStringAsync(url).Result;

				WeatherResponse response = JsonConvert.DeserializeObject<WeatherResponse>(responseData);

				return new CurrentWeather
				{
					Source = "Open Weather Service",
					Identifier = response.weather[0].id,
					Description = response.weather[0].description,
					Overall = response.weather[0].main,
					Temperature = response.main.temp,
					FeelsLike = response.main.feels_like,
					DayHigh = response.main.temp_max,
					DayLow = response.main.temp_min,
					Pressure = response.main.pressure,
					Humidity = response.main.humidity,
					Visibility = response.visibility,
					WindSpeed = response.wind.speed,
					WindDegrees = response.wind.deg,
					WindGusts = response.wind.gust,
					CloudPercent = response.clouds.all,
					Country = response.sys.country,
					Location = response.name,
				};
			}
			catch
			{
				return null;
			}
			
		}
	}
}
 