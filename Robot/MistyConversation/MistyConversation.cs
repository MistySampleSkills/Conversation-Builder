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
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;

namespace MistyConversation
{
	/// <summary>
	/// Skill used to start up the conversation capabilities
	/// </summary>
	internal class MistyConversationSkill : IMistySkill
	{
		public INativeRobotSkill Skill { get; set; }
		private IRobotMessenger Misty;
		private CharacterManagerLoader _characterManagerLoader;
		
		public MistyConversationSkill()
		{
			Skill = new NativeRobotSkill("Misty Conversation", "8be20a90-1150-44ac-a756-ebe4de30689e")
			{
				TimeoutInSeconds = -1,
				AllowedCleanupTimeInMs = 5000,
				StartupRules = new List<NativeStartupRule> { NativeStartupRule.Manual }
			};
		}

		public void LoadRobotConnection(IRobotMessenger robotInterface)
		{
			Misty = robotInterface;
		}

		public async void OnStart(object sender, IDictionary<string, object> parameters)
		{
			try
			{
                //Unregister events in case didn't shut down cleanly last time
				Misty.UnregisterAllEvents(null);

				//Revert display to defaults before starting...
				await Misty.SetDisplaySettingsAsync(true);
				Misty.SetBlinkSettings(true, null, null, null, null, null, null);

				await Misty.SetTextDisplaySettingsAsync("Text", new TextSettings
				{
					Wrap = true,
					Visible = true,
					Weight = 15,
					Size = 20,
					HorizontalAlignment = ImageHorizontalAlignment.Center,
					VerticalAlignment = ImageVerticalAlignment.Bottom,
					Red = 255,
					Green = 255,
					Blue = 255,
					PlaceOnTop = true,
					FontFamily = "Courier New",
					Height = 45
				});
				
				Misty.DisplayText("Hello...", "Text", null);
				Misty.ChangeLED(0, 255, 255, null);
				Misty.MoveArms(65.0, 65.0, 20, 20, null, AngularUnit.Degrees, null);

				await Task.Delay(2000);
				_characterManagerLoader = await CharacterManagerLoader.InitializeCharacter(parameters, Misty);
				if (_characterManagerLoader == null)
				{
					Misty.Speak("Sorry, I am having trouble initializing and starting this skill.", true, null, null);
					await Task.Delay(5000);
					return;
				}

				Misty.DisplayText("Almost there...", "Text", null);
				await Task.Delay(2000);
				try
				{
					bool audioEnabled = false;
					bool cameraEnabled = false;
					IRobotCommandResponse audioResults = null;
					IRobotCommandResponse cameraResults = null;
					cameraEnabled = (await Misty.CameraServiceEnabledAsync()).Data;
					audioEnabled = (await Misty.AudioServiceEnabledAsync()).Data;
					if (!cameraEnabled)
					{
						cameraResults = await Misty.EnableCameraServiceAsync();
						cameraEnabled = cameraResults.Status == ResponseStatus.Success;
					}
					if (!audioEnabled)
					{
						audioResults = await Misty.EnableAudioServiceAsync();
						audioEnabled = audioResults.Status == ResponseStatus.Success;
					}

					if ((!cameraEnabled || !audioEnabled))
					{
						Misty.Speak("Sorry, I am having trouble initializing the audio or camera services.  I will try to start the conversation, but you may have to restart Misty.", true, null, null);
						Misty.DisplayText($"Failed Audio/Camera Test", "Text", null);
						Misty.SkillLogger.Log($"Failed to get enabled response from audio or camera.");
						await Task.Delay(4000);
                        //Warned them, but try anyway in case they are just slow to come up
					}
				}
				catch (Exception ex)
				{
                    Misty.Speak("Sorry, I am having trouble initializing the audio or camera services.  I will try to start the conversation, but you may have to restart Misty.", true, null, null);
                    Misty.DisplayText($"Failed Audio/Camera Test", "Text", null);
					Misty.SkillLogger.Log("Enable services threw an exception. 820 services not loaded properly.", ex);
					await Task.Delay(4000);
                    //Warned them, but try anyway in case they are just slow to come up
                }

                Misty.DisplayText($"Waking Misty...", "Text", null);
                await Task.Delay(2000);

                string startupConversation = CharacterManagerLoader.CharacterParameters.ConversationGroup.StartupConversation;
				ConversationData conversationData = CharacterManagerLoader.CharacterParameters.ConversationGroup.Conversations.FirstOrDefault(x => x.Id == startupConversation);
				if (conversationData == null)
				{
					Misty.SkillLogger.Log($"Could not locate the starting conversation.");
					Misty.DisplayText($"Failed to start conversation.", "SpokeText", null);
					return;
				}				

				Misty.DisplayText($"Starting {conversationData.Name}", "Text", null);
				await Task.Delay(3000);
				Misty.SetTextDisplaySettings("Text", new TextSettings
				{
					Deleted = true
				}, null);

				//Go!
				await _characterManagerLoader.StartConversation();
			}
			catch (OperationCanceledException)
			{
				Misty.SkillLogger.Log($"Skill cancelled by user.");
				return;
			}
			catch (Exception ex)
			{
				Misty.DisplayText($"Failed Initialization", "Text", null);
				Misty.SkillLogger.Log($"Failed to initialize the skill.", ex);
				Misty.Speak("Sorry, I am unable to initialize the skill. You need to restart the skill or the robot.", true, null, null);
				await Task.Delay(2000);
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

		public async void OnCancel(object sender, IDictionary<string, object> parameters)
		{
			Misty.Speak("Misty conversation skill cancelling.", true, null, null);
			await Misty.SetBlinkSettingsAsync(true, null, null, null, null, null);
			Misty.Halt(new List<MotorMask> { MotorMask.RightArm, MotorMask.LeftArm}, null);
			_characterManagerLoader?.Dispose();
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
					_characterManagerLoader?.Dispose();
					Misty.UnregisterAllEvents(null);
					Misty.SkillCompleted();
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