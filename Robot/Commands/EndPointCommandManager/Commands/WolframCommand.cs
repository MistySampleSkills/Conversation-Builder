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
using System.Web;
using Conversation.Common;
using MistyRobotics.SDK.Messengers;
using SkillTools.Web;

namespace CommandManager
{
    public class WolframCommand : IBaseCommand
	{
		private IDictionary<string, object> _parameters = new Dictionary<string, object>();
		public string Name { get; } = "WOLFRAM";
		public string Description { get; } = "Calls wolfram alpha with the spoken text.";
		public string ResponseString { get; private set; }
		private IRobotMessenger _misty;
		private string _appId;
		private string _endpoint;
		private WebMessenger _webMessenger = new WebMessenger();

		public WolframCommand(IRobotMessenger misty, ICommandAuthorization commandAuth)
		{
			try
			{
				_misty = misty;
				_endpoint = commandAuth.AuthFields["Endpoint"];
				_appId = commandAuth.AuthFields["AppId"];
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogError("Exception while trying to create Wolfram Service.", ex);
			}
		}
		
		public async Task<string> ExecuteAsync(string[] parameters)
		{
			try
			{
				//"https://api.wolframalpha.com/v1/result?i=" + parameters[0] + "&appid=" + _appId

				string encodedQuestion = HttpUtility.UrlEncode(parameters[0]);

				WebMessengerData response = await _webMessenger.PostRequest(_endpoint + encodedQuestion + "&appid=" + _appId, null, "application/json");

				//TODO Sign up and process wolfram response
				string wolframResponse = response.Response;

				if (response.HttpCode != 200 || wolframResponse == "No short answer available" || wolframResponse == "Wolfram|Alpha did not understand your input")
				{
					wolframResponse = wolframResponse.Replace("Wolfram|Alpha", "Misty");
					wolframResponse = wolframResponse.Replace("Wolfram Alpha", "Misty");
				}
				else
				{
					wolframResponse = "Hmmm. I am having trouble figuring that one out.";
				}

				ResponseAction = $"SPEAK-AND-WAIT:\"{wolframResponse}\", 60000;";
				return ResponseString;
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogError("Failed to process wolfram request.", ex);
				return ResponseAction = "Failed to process wolfram request.";
			}
		}

		public string ResponseAction { get; private set; }

		public TriggerData CompletionTrigger { get; private set; } = new TriggerData("", "", "");
	}
}
