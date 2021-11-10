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

using System;
using System.Collections.Generic;
using System.Threading;
using MistyRobotics.SDK.Messengers;

namespace Conversation.Weather.OpenWeather
{
	/// <summary>
	/// Write your own to have Misty say the weather differently
	/// </summary>
	public class WeatherManager
	{
		private IRobotMessenger _misty;
		private Timer _weatherUpdate;
		private Random _random = new Random();
		private CurrentWeather _currentWeather = new CurrentWeather();
		private OpenWeatherService _weatherService;
		private int _weatherEndpointCallsThisRun;
		private string _ipStackApiAuth;
		private string _openWeatherApiAuth;
		private string _openWeatherCountryCode;
		private string _openWeatherCity;

		public WeatherManager(IRobotMessenger misty, string openWeatherApiAuth, string ipStackApiAuth, string openWeatherCountryCode, string openWeatherCity)
		{
			_misty = misty;
			_ipStackApiAuth = ipStackApiAuth;
			_openWeatherApiAuth = openWeatherApiAuth;
			_openWeatherCountryCode = openWeatherCountryCode;
			_openWeatherCity = openWeatherCity;

			_weatherService = new OpenWeatherService(_openWeatherApiAuth, _ipStackApiAuth);
			_weatherUpdate = new Timer(UpdateWeather, null, 1000, 5000 * 60 * 5); //update weather info every 5 minutes
		}

		public string GetWeatherString()
		{
			//Now add weather options
			if (_currentWeather?.Overall != null)
			{
				IList<string> weatherOptions = new List<string>();
				switch (_currentWeather.Overall.ToLower())
				{
					case "thunderstorm":
						weatherOptions.Add("There is going to be a thunderstorm today! Stay safe!");
						break;
					case "drizzle":
						weatherOptions.Add("Looks like a slight drizzle, stay dry out there.");
						break;
					case "rain":
						weatherOptions.Add("Looks like rain, stay dry out there.");
						break;
					case "snow":
						weatherOptions.Add("Forecast indicates snow, stay warm!");
						break;
					case "clear":
						weatherOptions.Add("Looks nice and clear, enjoy the nice weather and stay hydrated.");
						break;
					case "clouds":
						switch (_currentWeather.Description)
						{
							case "overcast clouds":  //85-100%
								weatherOptions.Add("Looks like a very cloudy day, today.");
								break;
							case "few clouds": //11-25%
								weatherOptions.Add("The forecast predicts a slightly cloudy day.");
								break;
							case "scattered clouds":  //25-50%
								weatherOptions.Add("The forecast predicts scattered clouds today.");
								break;
							case "broken clouds":  //51-84%
								weatherOptions.Add("Looks like some broken cloud covering, today.");
								break;
							default:
								weatherOptions.Add("Looks like a cloudy day, today.");
								break;
						}
						break;
					case "mist":
						weatherOptions.Add("The weather is misty today. Drive safe!");
						break;

					case "dust":
						weatherOptions.Add("Weather report says to expect dust in the air.  Be careful!");
						break;
					case "sand":
						weatherOptions.Add("Weather report says to expect sand in the air.  Be careful!");
						break;
					case "ash":
						weatherOptions.Add("Weather report says to expect ash in the air.  Be careful!");
						break;
					case "squall":
						weatherOptions.Add("Weather report says to expect a squall!");
						break;
					case "tornado":
						weatherOptions.Add("Oh, looks like there may be a tornado in the area, be careful!");
						break;
					case "smoke":
					case "haze":
					case "fog":
					default:
						weatherOptions.Add("Weather report says to expect " + _currentWeather.Description);
						break;
				}
					return GetRandomPhraseOption(weatherOptions);
			}
			return "";
		}

		private string GetRandomPhraseOption(IList<string> options)
		{
			if (options != null && options.Count > 0)
			{
				Random random = new Random();
				return options[random.Next(0, options.Count)];
			}
			return "";
		}

		private void UpdateWeather(object timerData)
		{
			try
			{
				if (_weatherService != null)
				{
					_weatherEndpointCallsThisRun += 1;
					_misty.SkillLogger.Log($"Calling weather endpoint.  Running call number {_weatherEndpointCallsThisRun}");
					_currentWeather = _weatherService.GetCurrentWeather(_openWeatherCity, _openWeatherCountryCode);

					if (_currentWeather == null)
					{
						if (string.IsNullOrWhiteSpace(_openWeatherCity) || string.IsNullOrWhiteSpace(_openWeatherCountryCode))
						{
							_misty.SkillLogger.Log("Could not find weather data. Try passing in OpenWeatherCity and OpenWeatherCountryCode values.");
						}
						else
						{
							_misty.SkillLogger.Log($"Could not find weather data. Is {_openWeatherCity}, {_openWeatherCountryCode} a valid OpenWeather location?");
						}
						
						//will try again in X minutes
					}
					else
					{
						if (string.IsNullOrWhiteSpace(_openWeatherCity) || string.IsNullOrWhiteSpace(_openWeatherCountryCode))
						{
							_misty.SkillLogger.Log($"Current temperature is {_currentWeather.Temperature} degrees.");
						}
						else
						{
							_misty.SkillLogger.Log($"Current temperature returned for {_openWeatherCity}, {_openWeatherCountryCode} is {_currentWeather.Temperature} degrees.");							
						}
					}
				}
			}
			catch(Exception ex)
			{
				_weatherUpdate?.Dispose();
				_misty.SkillLogger.Log("Exception in UpdateWeather endpoint access.", ex);
			}
		}

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_weatherUpdate?.Dispose();
				}

				_isDisposed = true;
			}
		}
		
		public void Dispose()
		{
			Dispose(true);
		}
	}
}
 