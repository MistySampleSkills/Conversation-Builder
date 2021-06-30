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

namespace MistyCharacter
{
	public class CommandResult
	{
		public bool Success { get; set; }
	}
	
	public class AwaitingSync
	{
		public IList<Robot> Robots { get; set; } = new List<Robot>();
		public string SyncName { get; set; }
		public bool IncludeSelf { get; set; }
		public bool AwaitAck { get; set; }
	}

	public class AwaitingEvent
	{
		public IList<Robot> Robots { get; set; } = new List<Robot>();
		public string Trigger { get; set; }
		public string TriggerFilter { get; set; }
		public string Text { get; set; }
		public bool IncludeSelf { get; set; }
		public bool AwaitAck { get; set; }
	}

	public class AnimationManager : BaseManager, IAnimationManager
	{
		public event EventHandler<DateTime> CompletedAnimationScript;
		public event EventHandler<DateTime> StartedAnimationScript;
		public event EventHandler<DateTime> AnimationScriptActionsComplete;
		public event EventHandler<DateTime> RepeatingAnimationScript;

		public event EventHandler<TriggerData> SyncEvent;

		private bool _userTextLayerVisible;
		private bool _webLayerVisible;
		private bool _videoLayerVisible;
		private bool _userImageLayerVisible;

		private bool _animationsCanceled;
		private bool _runningAnimation;
		private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
		private IList<string> _completionCommands = new List<string>();
		private IList<string> _startupCommands = new List<string>();
		private ISpeechManager _speechManager;
		private ILocomotionManager _locomotionManager;
		private IArmManager _armManager;
		private IHeadManager _headManager;
		private AnimationRequest _currentAnimation;
		private Interaction _currentInteraction;
		private bool _repeatScript;
		private WebMessenger _webMessenger;

		//TODO This should prolly go to loco manager
		private string _lastWaypoint;
		private string _goingToWaypoint;
		private IList<string> _actionsFromWaypoint = new List<string>();

		private AwaitingSync _awaitingSyncToSend;
		private AwaitingEvent _awaitingEventToSend;

		private int _waitingTimeoutMs;
		private string _waitingEvent;
		private bool _awaitAny;
		private object _waitingLock = new object();
		private TaskCompletionSource<bool> _receivedSyncEvent;
		private TaskCompletionSource<bool> _receivedSpeechCompletionEvent;

		//TODO move to arm and head managers or replace with script only?
		private double _rightArmDegrees;
		private double _leftArmDegrees;
		private double _headPitchDegrees;
		private double _headRollDegrees;
		private double _headYawDegrees;

		private bool _responsiveState = true; //by default, let other bots call this bot if it knows the IP

		public AnimationManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters, ISpeechManager speechManager, ILocomotionManager locomotionManager, IArmManager armManager, IHeadManager headManager)
		: base(misty, parameters, characterParameters)
		{
			_speechManager = speechManager;
			_locomotionManager = locomotionManager;
			_headManager = headManager;
			_armManager = armManager;
			_webMessenger = new WebMessenger();

			_speechManager.StoppedSpeaking += _speechManager_StoppedSpeaking;
			_speechManager.UserDataAnimationScript += _speechManager_UserDataAnimationScript;

			_armManager.LeftArmActuatorEvent += _armManager_LeftArmActuatorEvent;
			_armManager.RightArmActuatorEvent += _armManager_RightArmActuatorEvent;
			_headManager.HeadPitchActuatorEvent += _headManager_HeadPitchActuatorEvent;
			_headManager.HeadYawActuatorEvent += _headManager_HeadYawActuatorEvent;
			_headManager.HeadRollActuatorEvent += _headManager_HeadRollActuatorEvent;
		}

		private void _headManager_HeadRollActuatorEvent(object sender, IActuatorEvent e)
		{
			_headRollDegrees = e.ActuatorValue;
		}

		private void _headManager_HeadYawActuatorEvent(object sender, IActuatorEvent e)
		{
			_headYawDegrees = e.ActuatorValue;
		}

		private void _headManager_HeadPitchActuatorEvent(object sender, IActuatorEvent e)
		{
			_headPitchDegrees = e.ActuatorValue;
		}

		private void _armManager_RightArmActuatorEvent(object sender, IActuatorEvent e)
		{
			_rightArmDegrees = e.ActuatorValue;
		}

		private void _armManager_LeftArmActuatorEvent(object sender, IActuatorEvent e)
		{
			_leftArmDegrees = e.ActuatorValue;
		}

		private async void _speechManager_UserDataAnimationScript(object sender, string script)
		{
			await StopRunningAnimationScripts();
			_ = RunAnimationScript(script, false, _currentAnimation, _currentInteraction);
		}

		private async void _speechManager_StoppedSpeaking(object sender, IAudioPlayCompleteEvent e)
		{
			if(_awaitingSyncToSend != null)
			{
				await SendSyncEvent(_awaitingSyncToSend.Robots, _awaitingSyncToSend.SyncName, _awaitingSyncToSend.IncludeSelf, _awaitingSyncToSend.AwaitAck);
				_awaitingSyncToSend = null;
			}

			if (_awaitingEventToSend != null)
			{
				await SendCrossRobotEvent(_awaitingEventToSend.Robots, _awaitingEventToSend.Trigger, _awaitingEventToSend.TriggerFilter, _awaitingEventToSend.Text, _awaitingEventToSend.IncludeSelf, _awaitingEventToSend.AwaitAck);
				_awaitingEventToSend = null;
			}

			_receivedSpeechCompletionEvent?.TrySetResult(true);
		}

		public override Task<bool> Initialize()
		{
			_ = Robot.SetImageDisplaySettingsAsync(null, new ImageSettings
			{
				Visible = true,
				PlaceOnTop = true
			});

			_ = Robot.SetTextDisplaySettingsAsync("AnimationText", new TextSettings
			{
				Wrap = true,
				Visible = true,
				Weight = 25,
				Size = 30,
				HorizontalAlignment = ImageHorizontalAlignment.Center,
				VerticalAlignment = ImageVerticalAlignment.Bottom,
				Red = 255,
				Green = 255,
				Blue = 255,
				PlaceOnTop = true,
				FontFamily = "Courier New",
				Height = 50
			});
			_userTextLayerVisible = true;
			
			return Task.FromResult(true);
		}

		public async Task SendCrossRobotEvent(IList<Robot> robots, string trigger, string triggerFilter, string text, bool includeSelf = false, bool awaitAck = false)
		{
			foreach (Robot robot in robots)
			{
				if (!includeSelf && !robot.AllowCrossRobotCommunication)
				{
					continue;
				}

				bool callSelf = false;
				string[] robotIps = robot.IP.Split(",");
				foreach (string robotIp in robotIps)
				{
					//Need to handle when bot says it isn't gonna handle cross robot stuff
					bool isSelf = robotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", "") == CharacterParameters.RobotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", "");
					if (isSelf && !includeSelf)
					{
						continue;
					}

					if (!isSelf && !robot.AllowCrossRobotCommunication)
					{
						continue;
					}

					callSelf = isSelf && includeSelf;
					/*
					if (includeSelf && !robot.AllowCrossRobotCommunication)
					{
						if (robotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", "") != CharacterParameters.RobotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", ""))
						{
							continue;
						}
					}

					if (!string.IsNullOrWhiteSpace(CharacterParameters.RobotIp) && !includeSelf)
					{
						if (robotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", "") == CharacterParameters.RobotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", ""))
						{
							continue;
						}
					}*/

					IDictionary<string, object> payload = new Dictionary<string, object>();
					payload.Add("Trigger", trigger);
					payload.Add("Filter", triggerFilter);
					payload.Add("Text", text);

					if (callSelf)
					{
						_ = await Robot.TriggerEventAsync("SyncEvent", CharacterParameters.RobotIp ?? "ConversationBuilder", payload, null);
					}
					else
					{
						UserEvent userEvent = new UserEvent("ExternalEvent", "ConversationBuilder", EventOriginator.Skill, payload, -1);
						string data = Newtonsoft.Json.JsonConvert.SerializeObject(userEvent);
						string endpoint = $"http://{robotIp}/api/skills/event";
						if (awaitAck)
						{
							await _webMessenger.PostRequest(endpoint, data, "application/json");
						}
						else
						{
							_ = _webMessenger.PostRequest(endpoint, data, "application/json");
						}
					}
				}
			}
		}

		public async Task SendSyncEvent(IList<Robot> robots, string syncName, bool includeSelf = false, bool awaitAck = false)
		{
			foreach (Robot robot in robots)
			{
				if (!includeSelf && !robot.AllowCrossRobotCommunication)
				{
					continue;
				}

				bool callSelf = false;
				string[] robotIps = robot.IP.Split(",");
				foreach (string robotIp in robotIps)
				{
					bool isSelf = robotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", "") == CharacterParameters.RobotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", "");
					if(isSelf && !includeSelf)
					{
						continue;
					}

					if(!isSelf && !robot.AllowCrossRobotCommunication)
					{
						continue;
					}

					callSelf = isSelf && includeSelf;

				/*	if (includeSelf && !robot.AllowCrossRobotCommunication)
					{
						if (robotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", "") != CharacterParameters.RobotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", ""))
						{
							continue;
						}
					}

					if (!string.IsNullOrWhiteSpace(CharacterParameters.RobotIp) && !includeSelf)
					{
						if (isSelf)
						{
							continue;
						}
					}*/

					IDictionary<string, object> payload = new Dictionary<string, object>();
					payload.Add("Trigger", "SyncEvent");
					payload.Add("TriggerFilter", syncName.Trim());
					
					if(callSelf)
					{
						_ = await Robot.TriggerEventAsync("SyncEvent", CharacterParameters.RobotIp ?? "ConversationBuilder", payload, null);
					}
					else
					{
						UserEvent userEvent = new UserEvent("SyncEvent", "ConversationBuilder", EventOriginator.Skill, payload, -1);
						string data = Newtonsoft.Json.JsonConvert.SerializeObject(userEvent);

						string endpoint = $"http://{robotIp}/api/skills/event";
						if (awaitAck)
						{
							await _webMessenger.PostRequest(endpoint, data, "application/json");
						}
						else
						{
							_ = _webMessenger.PostRequest(endpoint, data, "application/json");
						}
					}
				}
			}
		}
		
		public async Task SendCrossRobotCommand(IList<Robot> robots, string command, bool includeSelf = false, bool awaitAck = false)
		{
			foreach (Robot robot in robots)
			{
				if (!includeSelf && !robot.AllowCrossRobotCommunication)
				{
					continue;
				}

				bool callSelf = false;
				string[] robotIps = robot.IP.Split(",");
				foreach(string robotIp in robotIps)
				{
					bool isSelf = robotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", "") == CharacterParameters.RobotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", "");
					if (isSelf && !includeSelf)
					{
						continue;
					}

					if (!isSelf && !robot.AllowCrossRobotCommunication)
					{
						continue;
					}

					callSelf = isSelf && includeSelf;

					/*if (includeSelf && !robot.AllowCrossRobotCommunication)
					{
						if (robotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", "") != CharacterParameters.RobotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", ""))
						{
							continue;
						}
					}

					if (!string.IsNullOrWhiteSpace(CharacterParameters.RobotIp) && !includeSelf)
					{
						if (robotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", "") == CharacterParameters.RobotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", ""))
						{
							continue;
						}
					}*/
					
					IDictionary<string, object> payload = new Dictionary<string, object>();
					payload.Add("Command", command);

					if (callSelf)
					{
						_ = await Robot.TriggerEventAsync("SyncEvent", CharacterParameters.RobotIp ?? "ConversationBuilder", payload, null);
					}
					else
					{
						UserEvent userEvent = new UserEvent("CrossRobotCommand", "ConversationBuilder", EventOriginator.Skill, payload, -1);
						string data = Newtonsoft.Json.JsonConvert.SerializeObject(userEvent);
						string endpoint = $"http://{robotIp.Trim().Replace("https://", "").Replace("http://", "").Replace("/", "")}/api/skills/event";
						if (awaitAck)
						{
							await _webMessenger.PostRequest(endpoint, data, "application/json");
						}
						else
						{
							_ = _webMessenger.PostRequest(endpoint, data, "application/json");
						}
					}
				}
			}
		}

		public async Task StopRunningAnimationScripts()
		{
			_semaphoreSlim.Wait();
			try
			{
				_animationsCanceled = true;
				foreach (string command in _completionCommands)
				{
					await ProcessCommand(command, true, 0);
				}
				_completionCommands.Clear();
				_runningAnimation = false;
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Failed stopping animation script.", ex);
			}
			finally
			{
				_semaphoreSlim.Release();
			}
		}

		public async Task<bool> RunAnimationScript(string animationScript, bool repeatScript, AnimationRequest currentAnimation, Interaction currentInteraction, bool stopOnFailedCommand = false)
		{
			try
			{
				if(_runningAnimation)
				{
					return false;
				}
				_runningAnimation = true;
				if (!string.IsNullOrWhiteSpace(animationScript))
				{
					StartedAnimationScript?.Invoke(this, DateTime.Now);
					_repeatScript = repeatScript;
					_currentAnimation = currentAnimation;
					_currentInteraction = currentInteraction;
					_animationsCanceled = false;
					animationScript = animationScript.Trim().Replace(Environment.NewLine, "");
					string[] commands = animationScript.Split(";");
					foreach (string command in commands)
					{
						//preprocess
						//let individual commands fail
						try
						{
							string[] commandData = command.Split(":", 2);

							if (commandData.Length > 0 && !string.IsNullOrWhiteSpace(commandData[0]))
							{
								if (command.Contains("#"))
								{
									//Contains 'finally'/cleanup cmds, only run once
									_completionCommands.Add(command.Replace("#", "").Trim());
								}

								if (command.Contains("*"))
								{
									//Contains script startup cmds, only run once
									_startupCommands.Add(command.Replace("*", "").Trim());
								}
							}
						}
						catch (Exception ex)
						{
							Robot.SkillLogger.Log($"Failed preprocessing animation script command. Ignoring {command}", ex);
						}
					}

					//do the * commands immediately, and only once, even if they are spread throughout script
					foreach(string startCommand in _startupCommands)
					{
						CommandResult commandResult = await ProcessCommand(startCommand, true, 0);
						if (!commandResult.Success && stopOnFailedCommand)
						{
							_runningAnimation = false;
							return false;
						}
					}
					
					int loopCount = 0;
					do
					{
						loopCount++;

						if ((loopCount > 1 && !_repeatScript) || _animationsCanceled)
						{
							return false;
						}
						else if (loopCount > 1 && _repeatScript)
						{
							RepeatingAnimationScript?.Invoke(this, DateTime.Now);
						}

						foreach (string runCommand in commands)
						{
							try
							{
								if (_animationsCanceled)
								{
									_semaphoreSlim.Wait();
									try
									{
										_animationsCanceled = false;
									}
									catch (Exception ex)
									{
										Robot.SkillLogger.Log($"Failed stopping animation script.", ex);
									}
									finally
									{
										_semaphoreSlim.Release();
									}
									return false;
								}
								else
								{
									if(!string.IsNullOrWhiteSpace(_waitingEvent) || _awaitAny)
									{
										await WaitOnSyncEvent();
									}
									
									CommandResult commandResult = await ProcessCommand(runCommand, false, loopCount);
									if (!commandResult.Success && stopOnFailedCommand)
									{
										_runningAnimation = false;
										return false;
									}

								}
							}
							catch (Exception ex)
							{
								Robot.SkillLogger.Log($"Failed processing animation script command. Ignoring {runCommand}", ex);
							}
						}
						AnimationScriptActionsComplete?.Invoke(this, DateTime.Now);
					}
					while (_repeatScript && !_animationsCanceled && _runningAnimation);
					
					CompletedAnimationScript?.Invoke(this, DateTime.Now);
					return true;
				}
				return false;
			}
			catch (Exception ex)
			{
				//let it all go as this is their code
				Robot.SkillLogger.Log($"Failed running animation script. {animationScript}", ex);
				return false;
			}
			finally
			{
				_runningAnimation = false;
			}
		}

		private async Task<bool> WaitOnSyncEvent()
		{
			bool response = false;
			_receivedSyncEvent = null;
			_receivedSyncEvent = new TaskCompletionSource<bool>();
			try
			{
				if (_receivedSyncEvent.Task == await Task.WhenAny(_receivedSyncEvent.Task, Task.Delay(_waitingTimeoutMs)))
				{
					response = _receivedSyncEvent.Task.Result;
				}
				else
				{
					Robot.SkillLogger.LogInfo("Timeout waiting for sync event.");
				}
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.LogError("Failed waiting for sync event", ex);
			}

			_receivedSyncEvent = null;
			return response;
		}

		private async Task<bool> WaitOnSpeechCompletionEvent(int timeoutMs)
		{
			bool response = false;
			_receivedSpeechCompletionEvent = null;
			_receivedSpeechCompletionEvent = new TaskCompletionSource<bool>();
			try
			{
				if (_receivedSpeechCompletionEvent.Task == await Task.WhenAny(_receivedSpeechCompletionEvent.Task, Task.Delay(timeoutMs)))
				{
					response = _receivedSpeechCompletionEvent.Task.Result;
				}
				else
				{
					Robot.SkillLogger.LogInfo("Timeout waiting for speech completion event.");
				}
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.LogError("Failed waiting for speech completion event", ex);
			}

			_receivedSpeechCompletionEvent = null;
			return response;
		}

		private double? GetNullableObject(string [] array, int index)
		{
			if (array == null || array.Length < index || array.ElementAtOrDefault(index) == null)
			{
				return null;
			}
			return Convert.ToDouble(array[index]);
		}

		private async Task<CommandResult> ProcessCommand(string command, bool guaranteedCommand, int loop)
		{
			// ; delimted :0)
			try
			{
				if(string.IsNullOrWhiteSpace(command))
				{
					return new CommandResult { Success = false };
				}

				lock (_waitingLock)
				{
					_waitingEvent = null;
					_awaitAny = false;
					_waitingTimeoutMs = 1;
				}

				string[] commandData = command.Split(":", 2); //find just the first one!
				string action = "EMPTY";

				if (commandData.Length > 0)
				{
					if(commandData[0].Contains("#") || (commandData[0].Contains("*")) && !guaranteedCommand)
					{
						//these get called separately, ignrore in script
						return new CommandResult { Success = true };
					}

					action = commandData[0].Trim();
					action = action.Replace("#", "").Trim();
					action = action.Replace("*", "").Trim();

					//check loop limiter
					if (action.Contains("{") && action.Contains("}"))
					{
						//get the numero of loops
						
						int indexOpen = action.IndexOf("{");
						int indexClose = action.IndexOf("}");

						if (indexClose - 1 <= indexOpen)
						{
							return new CommandResult { Success = false };
						}

						string loopString = action.Substring(indexOpen + 1, (indexClose - 1) - indexOpen);
						
						int loopLimit = Convert.ToInt32(loopString);
						
						if (loopLimit < loop)
						{
							return new CommandResult { Success = true };
						}

						//remove looping details
						if(indexOpen == 0)
						{
							action = action.Substring((indexClose + 1), (action.Length - 1 - indexClose));
						}
						else
						{
							action = action.Substring(0, indexOpen) + action.Substring((indexClose + 1), (action.Length - 1 - indexClose));
						}
					}

					bool sendToRobots = false;
					bool awaitAck = false;
					bool includeSelf = true;

					IList<Robot> allRobots = CharacterParameters.Robots;
					IList<Robot> externalRobots = new List<Robot>();

					if (action.Contains("$$%"))
					{
						sendToRobots = true;
						awaitAck = true;
						includeSelf = true;
					}
					else if (action.Contains("$%"))
					{
						sendToRobots = true;
						includeSelf = true;
					}
					else if (action.Contains("$$"))
					{
						includeSelf = false;
						sendToRobots = true;
						awaitAck = true;
					}
					else if (action.Contains("$"))
					{
						includeSelf = false;
						sendToRobots = true;
					}
					
					action = action.Replace("$", "").Trim();
					action = action.Replace("%", "").Trim();

					//look for override robot list
					if (action.Contains("[") && action.Contains("]"))
					{
						//get the numero of loops

						int indexOpen = action.IndexOf("]");
						int indexClose = action.IndexOf("[");

						if (indexClose - 1 <= indexOpen)
						{
							return new CommandResult { Success = false };
						}

						string robotOverrideListString = action.Substring(indexOpen + 1, (indexClose - 1) - indexOpen);

						string [] robotNames = robotOverrideListString.Split(",");
						foreach(string robotName in robotNames)
						{
							//Look it up and add to the list
							if(!externalRobots.Any(x => x.RobotName.Trim().ToLower() == robotName.Trim().ToLower()))
							{
								Robot selectedBot = allRobots.FirstOrDefault(x => x.RobotName.Trim().ToLower() == robotName.Trim().ToLower());
								externalRobots.Add(selectedBot);
							}
						}

						//remove override data from command
						action = action.Substring(0, indexOpen) + action.Substring(indexClose + 1, action.Length - indexClose);
					}
					else
					{
						externalRobots = allRobots;
					}
					
					if(includeSelf)
					{
						//split the rest based on action, hacky wacky for now
						//deal with inconsistencies for arms and head, so all scripting is ms
						switch (action.ToUpper())
						{
							case "ARMS":
								//ARMS:leftDegrees,rightDegrees,timeMs;
								string[] data = commandData[1].Split(",");
								_ = Robot.MoveArmsAsync(Convert.ToDouble(data[0]), Convert.ToDouble(data[1]), null, null, Convert.ToDouble(data[2]) / 1000, AngularUnit.Degrees);
								break;
							case "ARMS-OFFSET":
								//ARMS:leftDegrees,rightDegrees,timeMs;
								string[] data1 = commandData[1].Split(",");
								_ = Robot.MoveArmsAsync(Convert.ToDouble(data1[0]) + _leftArmDegrees, Convert.ToDouble(data1[1]) + _rightArmDegrees, null, null, Convert.ToDouble(data1[2]) / 1000, AngularUnit.Degrees);
								break;
							case "ARMS-OFFSET-V":
								//ARMS:leftDegrees,rightDegrees,timeMs;
								string[] datav1 = commandData[1].Split(",");
								_ = Robot.MoveArmsAsync(Convert.ToDouble(datav1[0]) + _leftArmDegrees, Convert.ToDouble(datav1[1]) + _rightArmDegrees, null, Convert.ToDouble(datav1[2]), null, AngularUnit.Degrees);
								break;
							case "ARM-V":
								//ARMS-V:leftDegrees,rightDegrees,velocity;
								string[] armVData = commandData[1].Split(",");
								RobotArm selectedVArm = armVData[0].ToLower().StartsWith("r") ? RobotArm.Right : RobotArm.Left;
								_ = Robot.MoveArmAsync(Convert.ToDouble(armVData[1]), selectedVArm, Convert.ToDouble(armVData[2]), null, AngularUnit.Degrees);
								break;
							case "ARM":
								//ARM:left/right,degrees,timeMs;
								string[] armData = commandData[1].Split(",");
								RobotArm selectedArm = armData[0].ToLower().StartsWith("r") ? RobotArm.Right : RobotArm.Left;
								_ = Robot.MoveArmAsync(Convert.ToDouble(armData[1]), selectedArm, null, Convert.ToDouble(armData[2]) / 1000, AngularUnit.Degrees);
								break;
							case "ARM-OFFSET":
								//ARM:left/right,degrees,timeMs;
								string[] armOData = commandData[1].Split(",");
								RobotArm selectedArm2 = armOData[0].ToLower().StartsWith("r") ? RobotArm.Right : RobotArm.Left;
								double armDegrees = armOData[0].ToLower().StartsWith("r") ? _rightArmDegrees : _leftArmDegrees;
								_ = Robot.MoveArmAsync(Convert.ToDouble(armOData[1]) + armDegrees, selectedArm2, null, Convert.ToDouble(armOData[2]) / 1000, AngularUnit.Degrees);
								break;
							case "ARM-OFFSET-V":
								//ARM:left/right,degrees,timeMs;
								string[] armOvData = commandData[1].Split(",");
								RobotArm selectedArm3 = armOvData[0].ToLower().StartsWith("r") ? RobotArm.Right : RobotArm.Left;
								double armDegrees2 = armOvData[0].ToLower().StartsWith("r") ? _rightArmDegrees : _leftArmDegrees;
								_ = Robot.MoveArmAsync(Convert.ToDouble(armOvData[1]) + armDegrees2, selectedArm3, Convert.ToDouble(armOvData[2]), null, AngularUnit.Degrees);
								break;
							case "HEAD":
								//HEAD:pitch,roll,yaw,timeMs;
								string[] headData = commandData[1].Split(",");
								_ = Robot.MoveHeadAsync(GetNullableObject(headData, 0), GetNullableObject(headData, 1), GetNullableObject(headData, 2), null, Convert.ToDouble(headData[3]) / 1000, AngularUnit.Degrees);
								break;
							case "HEAD-OFFSET":
								//HEAD:pitch,roll,yaw,velocity;
								string[] headOData = commandData[1].Split(",");
								_ = Robot.MoveHeadAsync(GetNullableObject(headOData, 0) == null ? null : GetNullableObject(headOData, 0) + _headPitchDegrees,
										GetNullableObject(headOData, 1) == null ? null : GetNullableObject(headOData, 1) + _headRollDegrees,
										GetNullableObject(headOData, 2) == null ? null : GetNullableObject(headOData, 2) + _headYawDegrees,
										null, Convert.ToDouble(headOData[3]) / 1000, AngularUnit.Degrees);
								break;
							case "HEAD-OFFSET-V":
								//HEAD:pitch,roll,yaw,velocity;
								string[] headOvData = commandData[1].Split(",");
								_ = Robot.MoveHeadAsync(GetNullableObject(headOvData, 0) == null ? null : GetNullableObject(headOvData, 0)+ _headPitchDegrees,
										GetNullableObject(headOvData, 1) == null ? null : GetNullableObject(headOvData, 1) + _headRollDegrees,
										GetNullableObject(headOvData, 2) == null ? null : GetNullableObject(headOvData, 2) + _headYawDegrees,
										Convert.ToDouble(headOvData[3]), null, AngularUnit.Degrees);
								break;
							case "HEAD-V":
								//HEAD:pitch,roll,yaw,velocity;
								string[] headVData = commandData[1].Split(",");
								_ = Robot.MoveHeadAsync(GetNullableObject(headVData, 0), GetNullableObject(headVData, 1), GetNullableObject(headVData, 2), Convert.ToDouble(headVData[3]), null, AngularUnit.Degrees);
								break;
							case "PAUSE":
								//PAUSE:timeMs;
								await Task.Delay(Convert.ToInt32(commandData[1]));
								break;
							case "VOLUME":
								//VOLUME:newVolume;
								_ = await Robot.SetDefaultVolumeAsync(Convert.ToInt32(commandData[1]));
								break;

							case "DEBUG":
								//DEBUG: User websocket message to send if skill is debug level;
								_ = await Robot.SendDebugMessageAsync(Convert.ToString(commandData[1]));
								break;
							case "PUBLISH":
								//PUBLISH: User websocket message to send;
								_ = await Robot.PublishMessageAsync(Convert.ToString(commandData[1]));
								break;
							case "LIGHT":
								//LIGHT:true/false;
								string lightValue = Convert.ToString(commandData[1]);
								lightValue = lightValue.ToLower();
								if (lightValue.StartsWith("t") || lightValue == "on")
								{
									_ = await Robot.SetFlashlightAsync(true);
								}
								else
								{
									_ = await Robot.SetFlashlightAsync(false);
								}
								break;
							case "PICTURE":
								//PICTURE:image-name,true/false(display on screen),width,height;

								//TODO Fit on screen!!
								string[] pictureData = commandData[1].Split(",");
								double? width = null;
								double? height = null;
								if (pictureData.Length >= 3)
								{
									width = Convert.ToDouble(pictureData[2].Trim());
								}
								if (pictureData.Length >= 4)
								{
									height = Convert.ToDouble(pictureData[3].Trim());
								}
								_ = await Robot.TakePictureAsync(Convert.ToString(pictureData[0].Trim()), false, Convert.ToBoolean(pictureData[1]), true, width, height);
								break;
							case "SERIAL":
								//SERIAL:write to the serial stream;
								_ = await Robot.WriteToSerialStreamAsync(Convert.ToString(commandData[1]));
								break;
							case "STOP":
								//STOP;
								// TODO Needs to stop entire locomotion recipe/ script, or separate command?
								await _locomotionManager.HandleLocomotionAction(new LocomotionAction
								{
									Action = LocomotionCommand.Stop
								});
								break;
							case "HALT":
								//HALT;
								await _locomotionManager.HandleLocomotionAction(new LocomotionAction
								{
									Action = LocomotionCommand.Halt
								});
								break;
							case "RESET-LAYERS":
								//RESET-LAYERS;								
								ClearAnimationDisplayLayers();
								await Robot.SetImageDisplaySettingsAsync(null, new ImageSettings
								{
									Visible = true,
									PlaceOnTop = true
								});
								break;
							case "RESET-EYES":
								//RESET-EYES;			
								ClearAnimationDisplayLayers();
								_ = Robot.SetBlinkSettingsAsync(true, null, null, null, null, null);
								await Robot.SetImageDisplaySettingsAsync(null, new ImageSettings
								{
									Visible = true,
									PlaceOnTop = true
								});
								break;
							case "IMAGE":
								//IMAGE:imageName;
								if (!_userImageLayerVisible)
								{
									await Robot.SetImageDisplaySettingsAsync(null, new ImageSettings
									{
										Visible = true,
										PlaceOnTop = true
									});
									_userImageLayerVisible = true;
								}
								//_ = Robot.DisplayImageAsync(commandData[1], "UserImageLayer", false);
								_ = Robot.DisplayImageAsync(Convert.ToString(commandData[1]), null, false);
								break;
							case "CLEAR-IMAGE":
								//CLEAR-IMAGE;
								_userImageLayerVisible = false;
								_ = Robot.SetImageDisplaySettingsAsync(null, new ImageSettings
								{
									Visible = false
								});
								break;
							
							case "FOLLOW-FACE":
								//FOLLOW-FACE;
								HeadLocation _currentHeadRequest = new HeadLocation(-40, -2, -45, 10, 2, 45, 0.5, null);
								_currentHeadRequest.FollowFace = true;
								_currentHeadRequest.FollowObject = "";
								_headManager.HandleHeadAction(_currentHeadRequest);
								break;
							case "STOP-FOLLOW":
								//STOP-FOLLOW;
								HeadLocation _stopFollowHeadRequest = new HeadLocation(-40, -2, -45, 10, 2, 45, 0.5, null);
								_stopFollowHeadRequest.FollowFace = false;
								_stopFollowHeadRequest.FollowObject = "";
								_headManager.HandleHeadAction(_stopFollowHeadRequest);
								break;
							case "FOLLOW-OBJECT":
								//FOLLOW-OBJECT:objectName;
								HeadLocation _objectHeadRequest = new HeadLocation(-40, -2, -45, 10, 2, 45, 0.5, null);
								_objectHeadRequest.FollowObject = Convert.ToString(commandData[1]);
								_objectHeadRequest.FollowFace = false;
								_headManager.HandleHeadAction(_objectHeadRequest);
								break;
							case "IMAGE-URL":
								//IMAGE-URL:URL;
								/*if (!_userImageLayerVisible)
								{
									await Robot.SetImageDisplaySettingsAsync("UserImageLayer", new ImageSettings
									{
										Visible = true,
										PlaceOnTop = true
									});
									_userImageLayerVisible = true;
								}*/
								//_ = Robot.DisplayImageAsync(commandData[1], "UserImageLayer", true);
								_ = Robot.DisplayImageAsync(commandData[1], null, true);
								break;
							case "TEXT":
								//TEXT:text to display;
								//or
								//TEXT:text to display,size,weight,height,r,g,b,wrap,font;
								string[] textData = commandData[1].Split(",");
								int? size = null;
								int? weight = null;
								int? textHeight = null;
								byte? red = null;
								byte? green = null;
								byte? blue = null;
								bool? wrap = null;
								string font = null;

								if (textData.Length >= 2)
								{
									size = Convert.ToInt32(textData[1].Trim());
								}
								if (textData.Length >= 3)
								{
									weight = Convert.ToInt32(textData[2].Trim());
								}
								if (textData.Length >= 4)
								{
									textHeight = Convert.ToInt32(textData[3].Trim());
								}
								if (textData.Length >= 5)
								{
									red = Convert.ToByte(textData[4].Trim());
								}
								if (textData.Length >= 6)
								{
									green = Convert.ToByte(textData[5].Trim());
								}
								if (textData.Length >= 7)
								{
									blue = Convert.ToByte(textData[6].Trim());
								}
								if (textData.Length >= 8)
								{
									wrap = Convert.ToBoolean(textData[7].Trim());
								}
								if (textData.Length >= 9)
								{
									font = Convert.ToString(textData[8].Trim());
								}


								if (!_userTextLayerVisible)
								{
									await Robot.SetTextDisplaySettingsAsync("AnimationText", new TextSettings
									{
										Wrap = wrap ?? true,
										Visible = true,
										Weight = weight ?? 25,
										Size = size ?? 30,
										HorizontalAlignment = ImageHorizontalAlignment.Center,
										VerticalAlignment = ImageVerticalAlignment.Bottom,
										Red = red ?? 255,
										Green = green ??255,
										Blue = blue ?? 255,
										PlaceOnTop = true,
										FontFamily = font ?? "Courier New",
										Height = textHeight ?? 50
									});
									_userTextLayerVisible = true;
								}
								_ = Robot.DisplayTextAsync(Convert.ToString(commandData[1]), "AnimationText");
								break;
							case "CLEAR-TEXT":
								//CLEAR-TEXT;
								_userTextLayerVisible = false;

								_ = Robot.SetTextDisplaySettingsAsync("AnimationText", new TextSettings
								{
									Deleted = true
								});
								break;

							case "AUDIO":
								//AUDIO:audio-file-name.wav;
								_ = Robot.PlayAudioAsync(Convert.ToString(commandData[1]), null);
								break;
							case "VIDEO":
								//VIDEO:videoName.mp4;
								if (!_videoLayerVisible)
								{
									await Robot.SetVideoDisplaySettingsAsync("VideoLayer", new VideoSettings
									{
										Visible = true,
										PlaceOnTop = true
									});
									_videoLayerVisible = true;
								}
								_ = Robot.DisplayVideoAsync(Convert.ToString(commandData[1]), "VideoLayer", false);
								break;
							case "VIDEO-URL":
								//VIDEO-URL:http://www.site.com/videoName.mpeg;

								if (!_videoLayerVisible)
								{
									await Robot.SetVideoDisplaySettingsAsync("VideoLayer", new VideoSettings
									{
										Visible = true,
										PlaceOnTop = true
									});
									_videoLayerVisible = true;
								}
								_ = Robot.DisplayVideoAsync(commandData[1], "VideoLayer", true);
								break;
							case "CLEAR-VIDEO":
								//CLEAR-VIDEO;
								_videoLayerVisible = false;
								_ = Robot.SetVideoDisplaySettingsAsync("VideoLayer", new VideoSettings
								{
									Deleted = true
								});
								break;
							case "WEB":
								//WEB:http://site-name.com;
								if (!_webLayerVisible)
								{
									await Robot.SetWebViewDisplaySettingsAsync("WebLayer", new WebViewSettings
									{
										Visible = true,
										PlaceOnTop = true
									});
									_webLayerVisible = true;
								}

								_ = Robot.DisplayWebViewAsync(commandData[1], "WebLayer");
								break;
							case "CLEAR-WEB":
								//CLEAR-WEB;
								_webLayerVisible = false;
								_ = Robot.SetWebViewDisplaySettingsAsync("WebLayer", new WebViewSettings
								{
									Deleted = true
								});
								break;
							case "LED":
								//LED:red,green,blue;
								string[] ledData = commandData[1].Split(",");
								_ = Robot.ChangeLEDAsync(Convert.ToUInt32(ledData[0]), Convert.ToUInt32(ledData[1]), Convert.ToUInt32(ledData[2]));
								break;

							case "LED-PATTERN":
								//LED-PATTERN:red1,green1,blue1,red2,green2,blue2,durationMs,blink/breathe/transit;
								LEDTransition transType = LEDTransition.None;
								string[] transitLedData = commandData[1].Split(",");
								string transitionType = Convert.ToString(transitLedData[7]);
								transitionType = transitionType.ToLower();
								if (transitionType.StartsWith("br"))
								{
									transType = LEDTransition.Breathe;
								}
								else if (transitionType.StartsWith("bl"))
								{
									transType = LEDTransition.Blink;
								}
								else if (transitionType.StartsWith("tr"))
								{
									transType = LEDTransition.TransitOnce;
								}

								if (transType != LEDTransition.None)
								{
									_ = Robot.TransitionLEDAsync(Convert.ToUInt32(transitLedData[0]), Convert.ToUInt32(transitLedData[1]), Convert.ToUInt32(transitLedData[2]),
										Convert.ToUInt32(transitLedData[3]), Convert.ToUInt32(transitLedData[4]), Convert.ToUInt32(transitLedData[5]),
										transType, Convert.ToUInt32(transitLedData[6]));
								}
								break;

							case "SPEAK":
								//SPEAK:What to say;
								if (_speechManager.TryToPersonalizeData(commandData[1], _currentAnimation, _currentInteraction, out string newText))
								{
									_currentAnimation.Speak = newText;
								}
								else
								{
									_currentAnimation.Speak = commandData[1];
								}

								_currentInteraction.StartListening = false;
								_speechManager.Speak(_currentAnimation, _currentInteraction);
								break;

							case "SPEAK-AND-WAIT":
								//SPEAK-AND-WAIT:What to say, timeoutMs;
								string[] sawData = commandData[1].Split(",");
								if (_speechManager.TryToPersonalizeData(sawData[0], _currentAnimation, _currentInteraction, out string newspeakText))
								{
									_currentAnimation.Speak = newspeakText;
								}
								else
								{
									_currentAnimation.Speak = sawData[0];
								}

								_currentInteraction.StartListening = false;
								_speechManager.Speak(_currentAnimation, _currentInteraction);
								await WaitOnSpeechCompletionEvent(Convert.ToInt32(sawData[1]));
								break;

							case "SPEAK-AND-SYNC":
								//SPEAK-AND-SYNC:What to say - without commas for now,SyncName;
								//TODO Timeout??

								string[] sasData = commandData[1].Split(",");
								if (_speechManager.TryToPersonalizeData(sasData[0], _currentAnimation, _currentInteraction, out string newText2))
								{
									_currentAnimation.Speak = newText2;
								}
								else
								{
									_currentAnimation.Speak = sasData[0];
								}
								_currentInteraction.StartListening = false;
								
								
								_speechManager.Speak(_currentAnimation, _currentInteraction);
								//await Task.Delay(100);
								_awaitingSyncToSend = new AwaitingSync
								{
									Robots = externalRobots,
									SyncName = sasData[1].Trim(),
									AwaitAck = awaitAck,
									IncludeSelf = includeSelf
								};
								break;
							case "SPEAK-AND-EVENT":
								//SPEAK-AND-EVENT:What to say,trigger,triggerFilter,text;
								string[] saeData = commandData[1].Split(",");
								if (_speechManager.TryToPersonalizeData(saeData[0], _currentAnimation, _currentInteraction, out string newText3))
								{
									_currentAnimation.Speak = newText3;
								}
								else
								{
									_currentAnimation.Speak = saeData[0];
								}
								_currentInteraction.StartListening = false;
								
								_speechManager.Speak(_currentAnimation, _currentInteraction);
								//await Task.Delay(100);
								_awaitingEventToSend = new AwaitingEvent
								{
									Robots = externalRobots,
									Trigger = saeData[1].Trim(),
									TriggerFilter = saeData[2].Trim(),
									Text = saeData[3],
									AwaitAck = awaitAck,
									IncludeSelf = includeSelf
								};
								break;
							case "SPEAK-AND-LISTEN":
								//SPEAK-AND-LISTEN:What to say;
								string[] salData = commandData[1].Split(",");
								if (_speechManager.TryToPersonalizeData(salData[0], _currentAnimation, _currentInteraction, out string newText4))
								{
									_currentAnimation.Speak = newText4;
								}
								else
								{
									_currentAnimation.Speak = salData[0];
								}
								_currentInteraction.StartListening = true;
								_speechManager.Speak(_currentAnimation, _currentInteraction);
								break;
							case "START-LISTEN":
								//START-LISTEN;
								switch (CharacterParameters.SpeechRecognitionService)
								{
									case "GoogleOnboard":
										_ = Robot.CaptureSpeechGoogleAsync(false, (int)(_currentInteraction.ListenTimeout * 1000), (int)(_currentInteraction.SilenceTimeout * 1000), CharacterParameters.GoogleSpeechParameters.SubscriptionKey, CharacterParameters.GoogleSpeechParameters.SpokenLanguage);
										break;
									case "AzureOnboard":
										_ = Robot.CaptureSpeechAzureAsync(false, (int)(_currentInteraction.ListenTimeout * 1000), (int)(_currentInteraction.SilenceTimeout * 1000), CharacterParameters.AzureSpeechParameters.SubscriptionKey, CharacterParameters.AzureSpeechParameters.Region, CharacterParameters.AzureSpeechParameters.SpokenLanguage);
										break;
									default:
										_ = Robot.CaptureSpeechAsync(false, true, (int)(_currentInteraction.ListenTimeout * 1000), (int)(_currentInteraction.SilenceTimeout * 1000), null);
										break;
								}
								break;
							case "AllOW-KEYPHRASE":
								//AllOW-KEYPHRASE;
								_currentInteraction.AllowKeyPhraseRecognition = true;
								_ = await _speechManager.UpdateKeyPhraseRecognition(_currentInteraction, false);
								break;
							case "CANCEL-KEYPHRASE":
								//CANCEL-KEYPHRASE;
								_currentInteraction.AllowKeyPhraseRecognition = false;
								_ = await _speechManager.UpdateKeyPhraseRecognition(_currentInteraction, true);
								break;

							case "DRIVE":
								//DRIVE:distanceMeters,timeMs,true/false(reverse);
								string[] driveData = commandData[1].Split(",");
								_ = _locomotionManager.HandleLocomotionAction(new LocomotionAction
								{
									Action = LocomotionCommand.Drive,
									DistanceMeters = Convert.ToDouble(driveData[0]),
									TimeMs = Convert.ToInt32(driveData[1]),
									Reverse = Convert.ToBoolean(driveData[2].Trim())
								});
								break;

							case "HEADING":
								//HEADING:heading,distanceMeters,timeMs,true/false(reverse);
								string[] headingData = commandData[1].Split(",");
								_ = _locomotionManager.HandleLocomotionAction(new LocomotionAction
								{
									Action = LocomotionCommand.Heading,
									Heading = Convert.ToDouble(headingData[0]),
									DistanceMeters = Convert.ToDouble(headingData[1]),
									TimeMs = Convert.ToInt32(headingData[2]),
									Reverse = Convert.ToBoolean(headingData[3].Trim())
								});
								break;

							case "TURN":
								//TURN:degrees,timeMs,left/right;
								string[] turnData = commandData[1].Split(",");
								string direction = Convert.ToString(turnData[2]);
								direction = direction.ToLower().Trim();
								_ = _locomotionManager.HandleLocomotionAction(new LocomotionAction
								{
									Action = LocomotionCommand.Turn,
									Degrees = direction.StartsWith("r") ? -Math.Abs(Convert.ToDouble(turnData[0])) : Math.Abs(Convert.ToDouble(turnData[0])),
									TimeMs = Convert.ToInt32(turnData[1].Trim()),
									Reverse = direction.StartsWith("r") ? true : false
								});
								break;

							case "TURN-HEADING":
								//TURN-HEADING:heading,timeMs,right/left;
								string[] turnHData = commandData[1].Split(",");
								_ = _locomotionManager.HandleLocomotionAction(new LocomotionAction
								{
									Action = LocomotionCommand.TurnHeading,
									Heading = Math.Abs(Convert.ToDouble(turnHData[0])),
									TimeMs = Convert.ToInt32(turnHData[1].Trim()),
									Reverse = turnHData[2].Trim().ToLower().StartsWith("r") ? true : false
								});
								break;

							case "ARC":
								//ARC:heading,radius,timeMs,true/false(reverse);
								string[] arcData = commandData[1].Split(",");
								_ = _locomotionManager.HandleLocomotionAction(new LocomotionAction
								{
									Action = LocomotionCommand.Arc,
									Heading = Convert.ToDouble(arcData[0]),
									Radius = Convert.ToInt32(arcData[1]),
									TimeMs = Convert.ToInt32(arcData[2]),
									Reverse = Convert.ToBoolean(arcData[3].Trim())
								});
								break;

							case "RESPONSIVE-STATE":
								//RESPONSIVE-STATE:true/on/false/off;
								string[] responsiveStateData = commandData[1].Split(",");
								string responsiveState = responsiveStateData[0];
								responsiveState = responsiveState.ToLower();
								if (responsiveState.StartsWith("t") || responsiveState == "on")
								{
									_responsiveState = true;
								}
								else
								{
									_responsiveState = false;
								}
								break;

							case "SET-LOCATION":
								//SET-LOCATION:bookcase;
								string[] locData = commandData[1].Split(",");
								_lastWaypoint = locData[0].Trim();
								_goingToWaypoint = "";
								_actionsFromWaypoint = new List<string>();
								break;

							case "HAZARDS-OFF":
								_ = Robot.UpdateHazardSettingsAsync(new MistyRobotics.Common.Data.HazardSettings { DisableTimeOfFlights = true });
								break;
							case "HAZARDS-ON":
								_ = Robot.UpdateHazardSettingsAsync(new MistyRobotics.Common.Data.HazardSettings { RevertToDefault = true });
								break;
							case "START-SKILL":
								//START-SKILL: skillId,?? and the params in robot config if cross skill
								string[] startSkillData = commandData[1].Split(",");
								IDictionary<string, object> parameters = new Dictionary<string, object>();
								foreach (KeyValuePair<string, object> item in Parameters)
								{
									parameters.TryAdd(item.Key, item.Value);
								}

								//TODO also add in whatever they give ya or info from bots/skills

								_ = Robot.RunSkillAsync(startSkillData[0], parameters);
								break;
							case "STOP-SKILL":
								string[] stopSkillData = commandData[1].Split(",");
								_ = Robot.CancelRunningSkillAsync(stopSkillData[0]);
								break;

							case "AWAIT-ANY":
								//AWAIT-ANY:10000/-1;
								string[] awaitAnySyncEvent = commandData[1].Split(",");
								lock (_waitingLock)
								{
									_waitingEvent = "Any";
									_waitingTimeoutMs = Convert.ToInt32(awaitAnySyncEvent[0]);
									_awaitAny = true;
								}
								break;

							case "AWAIT-SYNC":
								//AWAIT-SYNC:syncName1,10000/-1;
								string[] awaitSyncEvent = commandData[1].Split(",");
								lock (_waitingLock)
								{
									_waitingEvent = awaitSyncEvent[0].Trim();
									_awaitAny = false;
									_waitingTimeoutMs = Convert.ToInt32(awaitSyncEvent[1]);
								}
								break;

							//TODO
							case "RETURN":
								//RETURN; //return to last known waypoint
								break;
							//case "WANDER":
								//WANDER:leftAreaMeters,upAreaMeters,rightAreaMeters,downAreaMeters,velocity;
								//break;
							case "GOTO-WAYPOINT":
								//WAYPOINT:waypoint-name,velocity?;
								break;
							default:
								Robot.SkillLogger.Log($"Unknown command in animation script. {action?.ToUpper()}");
								return new CommandResult { Success = false };
						}
					}

					if(action == "SYNC")
					{
						string[] syncData = commandData[1].Split(",");
						await SendSyncEvent(externalRobots, syncData[0], includeSelf, awaitAck);
					}
					else if (action == "EVENT")
					{
						string[] triggerData = commandData[1].Split(",");
						await SendCrossRobotEvent(externalRobots, triggerData[0], triggerData[1], triggerData[2], includeSelf, awaitAck);
					}
					else if (sendToRobots)
					{
						string actionToSend = action.ToUpper() + ":" + commandData[1] + ";";
						//as of now, already sent to self if it was supposed to...
						await SendCrossRobotCommand(externalRobots, actionToSend, false, awaitAck);
					}
					
					return new CommandResult { Success = true };
				}
				else
				{
					Robot.SkillLogger.Log("Missing command in animation script.");
					return new CommandResult { Success = false };
				}
			}
			catch (Exception ex)
			{
				//let it all go as this is their code
				Robot.SkillLogger.Log($"Failed processing animation script. {command}", ex);
				return new CommandResult { Success = false };
			}
		}

		//This command needs to come in from another bot or skill
		//Need to handle when bot says it isn't gonna handle cross robot stuff
		public bool HandleSyncEvent(IUserEvent userEvent)
		{
			//TODO Deny cross robot communication per robot

			string name = "";
			IDictionary<string, object> payload = new Dictionary<string, object>();
			if (_awaitAny || userEvent.TryGetPayload(out payload))
			{
				//get the name if it exists
				if(payload != null && payload.TryGetValue("TriggerFilter", out object theName))
				{
					name = Convert.ToString(theName);
				}
			}
		
			//always be open to sync events for now
			lock (_waitingLock)
			{
				if(_awaitAny || _waitingEvent == name)
				{
					_waitingEvent = null;
					_awaitAny = false;
					return _receivedSyncEvent?.TrySetResult(true) ?? false;
				}
			}

			//Send sync event into conversation system in case interaction is waiting for it too...
			SyncEvent?.Invoke(this, new TriggerData("", name, Triggers.SyncEvent));
			
			return false;
		}

		public async Task HandleExternalCommand(IUserEvent userEvent)
		{
			//TODO Deny cross robot communication per robot

			//only allow external commands if robot is in responsive state
			if (_responsiveState)
			{
				string command = "";
				IDictionary<string, object> payload = new Dictionary<string, object>();
				if (_awaitAny || userEvent.TryGetPayload(out payload))
				{
					//get the name if it exists
					if (payload != null && payload.TryGetValue("Command", out object theName))
					{
						command = Convert.ToString(theName);
					}
				}

				await ProcessCommand(command, false, 0);
			}
		}

		private void ClearAnimationDisplayLayers()
		{
			_userTextLayerVisible = false;
			_ = Robot.SetTextDisplaySettingsAsync("AnimationText", new TextSettings
			{
				Deleted = true
			});

			_videoLayerVisible = false;
			_ = Robot.SetVideoDisplaySettingsAsync("VideoLayer", new VideoSettings
			{
				Deleted = true
			});
			_userImageLayerVisible = false;
			_ = Robot.SetImageDisplaySettingsAsync("UserImageLayer", new ImageSettings
			{
				Deleted = true
			});
			_webLayerVisible = false;
			_ = Robot.SetWebViewDisplaySettingsAsync("WebLayer", new WebViewSettings
			{
				Deleted = true
			});
		}

		private bool _isDisposed = false;

		protected void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					ClearAnimationDisplayLayers();
					_ = StopRunningAnimationScripts();
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
 