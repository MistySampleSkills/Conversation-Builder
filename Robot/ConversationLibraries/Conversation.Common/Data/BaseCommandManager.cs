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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MistyRobotics.SDK.Messengers;

namespace Conversation.Common
{
	public abstract class BaseCommandManager : ICommandManager
	{
		protected IRobotMessenger Misty;
		protected IList<IBaseCommand> Commands = new List<IBaseCommand>();
		protected IDictionary<string, object> Parameters;
		protected CharacterParameters CharacterParameters;
		protected IMistyState MistyState;
		protected IList<ICommandAuthorization> Authorizations;

		public BaseCommandManager(IRobotMessenger misty, IDictionary<string, object> parameters)
		{
			Misty = misty;
			Parameters = parameters;
		}

		public bool TryGetReplacements(out IList<string> replacements)
		{
			if (Commands != null && Commands.Count() > 0)
			{
				replacements = Commands.Select(x => x.Name).ToList();
				return true;
			}
			replacements = new List<string>();
			return false;

		}
		public virtual async Task<bool> Initialize(CharacterParameters characterParameters, IMistyState mistyState, IList<ICommandAuthorization> listOfAuthorizations)
		{
			MistyState = mistyState;
			CharacterParameters = characterParameters;
			Authorizations = listOfAuthorizations;
			return true;
		}

		public bool TryGetAuth(string authName, out ICommandAuthorization command)
		{
			command = Authorizations.FirstOrDefault(x => string.Compare(x.Name, authName, true) == 0);
			return command != null;
		}

		public bool TryGetCommand(string commandName, out IBaseCommand command)
		{
			command = Commands.FirstOrDefault(x => string.Compare(x.Name, commandName, true) == 0);
			return command != null;
		}

		public bool TryGetDescription(string commandName, out string lastResponse)
		{
			IBaseCommand command = Commands.FirstOrDefault(x => string.Compare(x.Name, commandName, true) == 0);
			lastResponse = command.Description;
			return command != null;
		}

		public bool TryGetLastResponse(string commandName, out string lastResponse)
		{
			IBaseCommand command = Commands.FirstOrDefault(x => string.Compare(x.Name, commandName, true) == 0);
			lastResponse = command.ResponseString;
			return command != null;
		}

		public bool TryGetLastAction(string commandName, out string lastAction)
		{
			IBaseCommand command = Commands.FirstOrDefault(x => string.Compare(x.Name, commandName, true) == 0);
			lastAction = command.ResponseAction;
			return command != null;
		}
	}	
}
