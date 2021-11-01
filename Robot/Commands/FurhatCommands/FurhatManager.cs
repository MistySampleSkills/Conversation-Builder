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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conversation.Common;
using MistyCharacter;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using SkillTools.Web;

namespace MistyCharacter
{
	public class FurhatManager
	{
		private string _ip;
		private const string ApiEndpoint = "/furhat";
		private WebMessenger _webMessenger = new WebMessenger();
		private SpeechManager _speechManager;

		private const string FurhatIp = "FurhatIp";

		protected IDictionary<string, object> _parameters { get; set; }
		protected IRobotMessenger _misty { get; set; }
		protected CharacterParameters _characterParameters { get; set; }

		public FurhatManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters, SpeechManager speechManager)
		{
			_misty = misty;
			_parameters = parameters;
			_characterParameters = characterParameters;
			//TODO process params and get furhat ip
			_ip = "10.0.0.100";
		}

		public Task<bool> Initialize()
		{
			if(!string.IsNullOrWhiteSpace(FurhatIp))
			{
				_ip = FurhatIp;
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}






		public async Task<bool> AttendClosest()
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/attend?user=CLOSEST";
				//TODO Moar!
				string data = Newtonsoft.Json.JsonConvert.SerializeObject(new
				{
					user = "CLOSEST"
				});

				WebMessengerData webMessengerData = await _webMessenger.PostRequest(endpoint, data, "application/json");
				_misty.SkillLogger.LogInfo($"Furhat 'AttendClosest' response: {webMessengerData?.Response}");
				return (webMessengerData?.HttpCode == 200);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'AttendClosest' threw exception", ex);
				return false;
			}
		}

		public async Task<bool> Voice(string voice)
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/voice?name={voice}";
				string data = Newtonsoft.Json.JsonConvert.SerializeObject(new
				{
					name = voice
				});

				WebMessengerData webMessengerData = await _webMessenger.PostRequest(endpoint, data, "application/json");
				_misty.SkillLogger.LogInfo($"Furhat 'Voice' response: {webMessengerData?.Response}");
				return (webMessengerData?.HttpCode == 200);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'Voice' threw exception", ex);
				return false;
			}
		}

		public async Task<bool> Face(string mask, string character = null, string model = null, string texture = null)
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/face?mask={mask}&character={character}&model={model}&texture={texture}";
				string data = Newtonsoft.Json.JsonConvert.SerializeObject(new
				{
					mask,
					character,
					model,
					texture
				});
				WebMessengerData webMessengerData = await _webMessenger.PostRequest(endpoint, data, "application/json");
				_misty.SkillLogger.LogInfo($"Furhat 'Face' response: {webMessengerData?.Response}");
				return (webMessengerData?.HttpCode == 200);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'Face' threw exception", ex);
				return false;
			}
		}

		public async Task<bool> Speak(string text)
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/say?text={text}";
				string data = Newtonsoft.Json.JsonConvert.SerializeObject(new
				{
					text
				});

				WebMessengerData webMessengerData = await _webMessenger.PostRequest(endpoint, data, "application/json");
				_misty.SkillLogger.LogInfo($"Furhat 'Speak' response: {webMessengerData?.Response}");
				return (webMessengerData?.HttpCode == 200);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'Speak' threw exception", ex);
				return false;
			}
		}

		public async Task<bool> StopSpeaking()
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/say/stop";
				string data = Newtonsoft.Json.JsonConvert.SerializeObject(new { });

				WebMessengerData webMessengerData = await _webMessenger.PostRequest(endpoint, data, "application/json");
				_misty.SkillLogger.LogInfo($"Furhat 'StopSpeaking' response: {webMessengerData?.Response}");
				return (webMessengerData?.HttpCode == 200);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'StopSpeaking' threw exception", ex);
				return false;
			}
		}

		public async Task<string> Listen(string language = "en-US")
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/listen";
				WebMessengerData webMessengerData = await _webMessenger.GetRequest(endpoint);
				_misty.SkillLogger.LogInfo($"Furhat 'Listen' response: {webMessengerData?.Response}");
				return (webMessengerData.Response);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'Listen' threw exception", ex);
				return null;
			}
		}

		public async Task<bool> StopListening()
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/listen/stop";
				string data = Newtonsoft.Json.JsonConvert.SerializeObject(new { });

				WebMessengerData webMessengerData = await _webMessenger.PostRequest(endpoint, data, "application/json");
				_misty.SkillLogger.LogInfo($"Furhat 'StopListening' response: {webMessengerData?.Response}");
				return (webMessengerData.HttpCode == 200);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'StopListening' threw exception", ex);
				return false;
			}
		}

		public async Task<bool> Gesture(string gesture)
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/gesture?name={gesture}";
				string data = Newtonsoft.Json.JsonConvert.SerializeObject(new
				{
					name = gesture
				});

				WebMessengerData webMessengerData = await _webMessenger.PostRequest(endpoint, data, "application/json");
				_misty.SkillLogger.LogInfo($"Furhat 'Gesture' response: {webMessengerData?.Response}");
				return (webMessengerData.HttpCode == 200);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'Gesture' threw exception", ex);
				return false;
			}
		}

		public async Task<bool> LED(int red, int green, int blue)
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/led?red={red}&green={green}&blue={blue}";
				string data = Newtonsoft.Json.JsonConvert.SerializeObject(new
				{
					red,
					green,
					blue
				});

				WebMessengerData webMessengerData = await _webMessenger.PostRequest(endpoint, data, "application/json");
				_misty.SkillLogger.LogInfo($"Furhat 'LED' response: {webMessengerData?.Response}");
				return (webMessengerData.HttpCode == 200);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'LED' threw exception", ex);
				return false;
			}
		}

		public async Task<bool> Audio(string url)
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/say?url={url}";
				string data = Newtonsoft.Json.JsonConvert.SerializeObject(new
				{
					url
				});

				WebMessengerData webMessengerData = await _webMessenger.PostRequest(endpoint, data, "application/json");
				_misty.SkillLogger.LogInfo($"Furhat 'Audio' response: {webMessengerData?.Response}");
				return (webMessengerData.HttpCode == 200);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'Audio' threw exception", ex);
				return false;
			}
		}

		public async Task<string> GetGestures()
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/gestures";
				WebMessengerData webMessengerData = await _webMessenger.GetRequestAsync(endpoint);
				_misty.SkillLogger.LogInfo($"Furhat 'GetGestures' response: {webMessengerData?.Response}");
				return (webMessengerData.Response);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'GetGestures' threw exception", ex);
				return null;
			}
		}

		public async Task<string> GetVoices()
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/voices";
				WebMessengerData webMessengerData = await _webMessenger.GetRequestAsync(endpoint);
				_misty.SkillLogger.LogInfo($"Furhat 'GetVoices' response: {webMessengerData?.Response}");
				return (webMessengerData.Response);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'GetVoices' threw exception", ex);
				return null;
			}
		}

		public async Task<string> GetUsers()
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/users";
				WebMessengerData webMessengerData = await _webMessenger.GetRequestAsync(endpoint);
				_misty.SkillLogger.LogInfo($"Furhat 'GetUsers' response: {webMessengerData?.Response}");
				return (webMessengerData.Response);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'GetUsers' threw exception", ex);
				return null;
			}
		}

		public async Task<bool> Test()
		{
			try
			{
				string endpoint = $"http://{_ip}{ApiEndpoint}/test";
				WebMessengerData webMessengerData = await _webMessenger.GetRequestAsync(endpoint);
				_misty.SkillLogger.LogInfo($"Furhat 'Test' response: {webMessengerData?.Response}");
				return (webMessengerData.HttpCode == 200);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Furhat 'Test' threw exception", ex);
				return false;
			}
		}

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{

			if (!_isDisposed)
			{
				if (disposing)
				{
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

