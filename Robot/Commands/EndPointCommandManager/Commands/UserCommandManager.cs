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

using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Conversation.Common;
using MistyRobotics.SDK.Messengers;

namespace CommandManager
{

	//public class SceneCommandAuthorization : CommandAuthorization
	//{
	//	public SceneCommandAuthorization(string subscriptionKey, string region, string endpoint)
	//	{
	//		SubscriptionKey = subscriptionKey;
	//		Region = region;
	//		Endpoint = endpoint;
	//	}

	//	public string Name { get; set; } = "SceneCommand";
	//	public string SubscriptionKey { get; set; }
	//	public string Region { get; set; }
	//	public string Endpoint { get; set; }
	//}

	public class UserCommandManager : BaseCommandManager
	{

		public UserCommandManager(IRobotMessenger misty, IDictionary<string, object> parameters) 
			: base(misty, parameters) {}

		public override async Task<bool> Initialize(CharacterParameters characterParameters, IMistyState mistyState, IList<ICommandAuthorization> listOfAuthorizations)
		{
			CharacterParameters = characterParameters;
			MistyState = mistyState;
			Authorizations = listOfAuthorizations;

			Commands.Add(new DevJokeCommand());
			Commands.Add(new ChuckJokeCommand());
			
			Commands.Add(new DescribeSceneCommand(Misty, Authorizations.FirstOrDefault(x => x.Name.ToUpper().Trim() == "DESCRIBE-SCENE")));
			Commands.Add(new SendEmailCommand(Misty, Authorizations.FirstOrDefault(x => x.Name.ToUpper().Trim() == "SEND-EMAIL")));
			Commands.Add(new WolframCommand(Misty, Authorizations.FirstOrDefault(x => x.Name.ToUpper().Trim() == "WOLFRAM")));
			//Commands.Add(new SendTwilioCommand(Misty, Authorizations.FirstOrDefault(x => x.Name.ToUpper().Trim() == "SEND-TWILIO")));			
			return true;
		}

	}
	

}
