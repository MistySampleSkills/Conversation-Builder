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
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using SkillTools.Web;

namespace MistyInteraction
{
	public abstract class ConversationCommand
	{
		ExecuteCommand command = Code;
		public ConversationCommand(string name, string script)
		{
			Name = name;
			Script = script;
		}

		public string Name { get; set; }
		public string Script { get; set; }
		public delegate object ExecuteCommand(IDictionary<string, object> parameters);
		public static object Code(IDictionary<string, object> parameters)
		{
			return null;
		}
	}

	public class TestCommand : ConversationCommand
	{
		public TestCommand(string name, string script, Delegate command)
		: base(name, script)
		{

		}

		public new static object Code(IDictionary<string, object> parameters)
		{
			return null;
		}

	}

	public class CommandManager : BaseManager, IDisposable
	{
		private IList<ConversationCommand> _userCommands;
		private IList<ConversationCommand> _builtInCommands;

		public CommandManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters)
		: base(misty, parameters, characterParameters)
		{

		}

		public async Task<bool> Initialize(IList<ConversationCommand> commands)
		{
			_userCommands = commands;
			AddBuiltIncommands();
			return true;
		}

		private void AddBuiltIncommands()
		{
			_builtInCommands.Add(new ConversationCommand)
		}

		private bool _isDisposed = false;

		protected void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing) { }

				_isDisposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
	}
}
 