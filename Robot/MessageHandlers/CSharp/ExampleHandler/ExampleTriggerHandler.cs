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
using System.Threading.Tasks;
using MistyCharacter;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Messengers;
using Conversation.Common;
using Conversation.Weather.OpenWeather;
using FunnyBone;

namespace ExampleHandlerSkill
{	
    /// <summary>
    /// Example trigger handler skill that can be called from conversation to interact with the code in the Conversation Libraries
    /// Skill is used in the Presentation example to tell jokes and get the weather.
    /// Trigger handler skills can be created to provide more capabilities to conversations.
    /// The conversation skill will start this skill and it should not be started separately by the user.
    /// </summary>
	internal class ExampleTriggerHandler : IMistySkill
	{
		private IRobotMessenger _misty;
		private Random _random = new Random();
		private WeatherManager _weatherManager;
		private ParameterManager _parameterManager = null;
		private FunnyBoneAPI _funnyBoneAPI;
		private TriggerToSend _triggerToSend;
		private IDictionary<string, object> _parameters;

		public INativeRobotSkill Skill { get; private set; } = 
			new NativeRobotSkill("Example Trigger Handler", "0e971056-d222-4b64-a289-7fd0c75683bf")
			{
				TimeoutInSeconds = -1
			};


		public void LoadRobotConnection(IRobotMessenger robotInterface)
		{
			_misty = robotInterface;
		}
		
		public async void OnStart(object sender, IDictionary<string, object> parameters)
        {
            //Trigger skills are started by the conversation manager and are started with the same parameters of the conversation skill	
            _parameters = parameters;

            //In case this wasn't shut down properly last time, make sure it's events are all newly registered
			_misty.UnregisterAllEvents(null);
			
            //Process the startup parameters
            //TODO Handle more of this in the libraries
			try
			{
				if (!parameters.TryGetValue("Parameters", out object innerParameterObject))
				{
					_misty.Speak($"Failed to start example handler skill, could not find required parameters.", true, "failed", null);
					await Task.Delay(3000);
					return;
				}

				IDictionary<string, object> innerParameters = Newtonsoft.Json.JsonConvert.DeserializeObject<IDictionary<string, object>>(Convert.ToString(innerParameterObject));
				_parameterManager = new ParameterManager(_misty, innerParameters, false);
				CharacterParameters characterParameters = await _parameterManager.Initialize();
				if(characterParameters == null || !string.IsNullOrWhiteSpace(characterParameters.InitializationError))
				{
					_misty.Speak($"Failed processing parameters in Example handler skill.  Skill may not work properly.", true, "failed", null);
				}
			}
			catch
			{
				_misty.Speak($"Failed to start Example Handler Skill, could not find required parameters.", true, "failed", null);
				await Task.Delay(3000);
				return;
			}

            //Get parameters for open weather from user defined character payload
            //TODO Update to use from skill payload, currently using character configuration payload
            // eg: {"OpenWeatherApiAuth": "xyz_your_auth_code", "OpenWeatherCountryCode" : "US", "OpenWeatherCity" :"Boulder" }
            var openWeatherApiAuth = _parameterManager.GetUserDefinedStringData("OpenWeatherApiAuth");
			var ipStackApiAuth = _parameterManager.GetUserDefinedStringData("IpStackApiAuth");
			var openWeatherCountryCode = _parameterManager.GetUserDefinedStringData("OpenWeatherCountryCode");
			var openWeatherCity = _parameterManager.GetUserDefinedStringData("OpenWeatherCity");
			_weatherManager = new WeatherManager(_misty, openWeatherApiAuth, ipStackApiAuth, openWeatherCountryCode, openWeatherCity);
            
            _funnyBoneAPI = new FunnyBoneAPI();
            
            /////////////////////////////////////////////////////////////////
            ///
            /// Adding Skill Messages with this skill id and the event names Joke and Weather
            ///   will cause them to be called if this skill is installed on the robot.
            ///   the conversation can choose to wait on an external event and these calls will send those events back when complete
            
            //Calls an endpoint to get a joke
			_misty.RegisterUserEvent("Joke", Joke, 0, true, null);

            //Requires proper setup in conversation builder UI
            _misty.RegisterUserEvent("Weather", WeatherCallback, 0, true, null);
            
            //Setup event so can send trigger after Misty completes whatever needs to be said
            _misty.RegisterTextToSpeechCompleteEvent(TextToSpeechCompleteCallback, 100, true, "TriggerHandlerTTSComplete", null);
		}

		private void TextToSpeechCompleteCallback(ITextToSpeechCompleteEvent textToSpeechCompleteEvent)
		{
			if(_triggerToSend != null && textToSpeechCompleteEvent.UttteranceId == _triggerToSend.UtteranceId)
			{
				SendTrigger(_triggerToSend.Trigger, _triggerToSend.TriggerFilter, _triggerToSend.Text);
				_triggerToSend = null;
			}
		}

		private async void Joke(IUserEvent e)
		{
			SingleJokeFormat singleJokeFormat = await _funnyBoneAPI.GetDeveloperJoke();
			_triggerToSend = new TriggerToSend("ExternalEvent", "joke", "Joke");
			if (singleJokeFormat.Error || !singleJokeFormat.Safe)
			{
				_misty.Speak("Sorry, I had trouble getting a joke.", true, "joke", null);
			}
			else
			{
				_misty.Speak(singleJokeFormat.Joke, true, "joke", null);
			}
		}

		private void WeatherCallback(IUserEvent e)
		{
			string weather = _weatherManager.GetWeatherString();
			_triggerToSend = new TriggerToSend("ExternalEvent", "weather", "WeatherSpoken");
			_misty.Speak(weather, true, "weather", null);
		}
		
		public void OnPause(object sender, IDictionary<string, object> parameters)
		{
			OnCancel(sender, parameters);
		}
		
		public void OnResume(object sender, IDictionary<string, object> parameters)
		{
			OnStart(sender, parameters);
		}

		public void OnCancel(object sender, IDictionary<string, object> parameters)
		{
			_misty.UnregisterAllEvents(null);
			Dispose();
		}
		
		public void OnTimeout(object sender, IDictionary<string, object> parameters)
		{
			OnCancel(sender, parameters);
		}
		
		private void SendTrigger(string trigger, string triggerFilter, string text)
		{
            IDictionary<string, object> data = new Dictionary<string, object>
            {
                { "Trigger", trigger },
                { "TriggerFilter", triggerFilter },
                { "Text", text }
            };

            _misty.TriggerEvent("ExternalEvent", Skill.UniqueId.ToString(), data, new List<string> { "8be20a90-1150-44ac-a756-ebe4de30689e" }, null);
		}

		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_weatherManager?.Dispose();
                }

				_isDisposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}

		#endregion
	}
}
