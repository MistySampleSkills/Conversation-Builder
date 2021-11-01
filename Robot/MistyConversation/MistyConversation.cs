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
using System.Threading.Tasks;
using Conversation.Common;
using MistyCharacter;
using MistyManager;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using TimeManager;

namespace MistyConversation
{
	/// <summary>
	/// Skill used to start up the conversation capabilities
	/// </summary>
	internal class MistyConversationSkill : IMistySkill
	{
		public INativeRobotSkill Skill { get; set; }
		private IRobotMessenger _misty;
		private ConversationManager _conversationManager;
		
		public MistyConversationSkill()
		{
			Skill = new NativeRobotSkill("Misty Conversation", "8be20a90-1150-44ac-a756-ebe4de30689e")
			{
				TimeoutInSeconds = -1,
				AllowedCleanupTimeInMs = 10000,
				StartupRules = new List<NativeStartupRule> { NativeStartupRule.Manual }
			};
		}

		public void LoadRobotConnection(IRobotMessenger robotInterface)
		{
			_misty = robotInterface;
		}

		public async void OnStart(object sender, IDictionary<string, object> parameters)
		{
			try
			{
				_conversationManager = new ConversationManager(_misty, parameters, new MistyCharacter.ManagerConfiguration());
				if (!await _conversationManager.Initialize(new BasicMisty(_misty, parameters, new ManagerConfiguration())))
				{
					_misty.SkillLogger.Log($"Failed to initialize conversation manager.");
					_misty.SkillCompleted();
					return;
				}

				await _conversationManager.StartConversation();

			}
			catch (OperationCanceledException)
			{
				_misty.SkillLogger.Log($"Skill cancelled by user.");
				return;
			}
			catch (Exception ex)
			{
				_misty.DisplayText($"Failed Initialization", "Text", null);
				_misty.SkillLogger.Log($"Failed to initialize the skill.", ex);
				_misty.Speak("Sorry, I am unable to initialize the skill. You need to restart the skill or the robot.", true, null, null);
				await Task.Delay(5000);
				_misty.SkillCompleted();
			}
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
			_conversationManager?.Dispose();

			_misty.Speak("Misty conversation skill cancelling.", true, null, null);
			_misty.SetBlinkSettings(true, null, null, null, null, null, null);
			_misty.Halt(new List<MotorMask> { MotorMask.RightArm, MotorMask.LeftArm}, null);
		}

		public void OnTimeout(object sender, IDictionary<string, object> parameters)
		{
			OnCancel(sender, parameters);
		}
		
		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_conversationManager?.Dispose();
					_misty.UnregisterAllEvents(null);
					_misty.SkillCompleted();
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