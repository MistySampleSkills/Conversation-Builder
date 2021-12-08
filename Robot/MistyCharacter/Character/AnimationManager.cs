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
using CommandManager;
using Conversation.Common;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using Newtonsoft.Json;
using SkillTools.Web;
using SpeechTools;
using TimeManager;

namespace MistyCharacter
{
	public static class StringCommandExtensions
	{
		public static string[] SmartSplit(this string str, string separator)
		{
			//First item can sometimes be a string, look for quotes
			int firstComma = str.IndexOf(",");
			if (firstComma == -1 || (str.IndexOf("\"") != -1 && str.IndexOf("\"") < firstComma))
			{
				//take the quotes text as first item
				List<string> builtSplit = new List<string>();
				string[] newSplit = str.Split("\"", StringSplitOptions.RemoveEmptyEntries);
				if (newSplit.Count() >= 1)
				{
					builtSplit.Add(newSplit[0]);
				}
				if (newSplit.Count() >= 2)
				{
					builtSplit.AddRange(newSplit[1].Split(separator, StringSplitOptions.RemoveEmptyEntries));
				}
				if(builtSplit.Count() >= 1)
				{
					return builtSplit.ToArray();
				}
			}

			return str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
		}

	}

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
		public event EventHandler<LocomotionAction> StartedLocomotionAction;
		public event EventHandler<LocomotionAction> CompletedLocomotionAction;
		public event EventHandler<LocomotionAction> LocomotionFailed;
		public event EventHandler<LocomotionAction> LocomotionStopped;

		public event EventHandler<LocomotionAction> ReachedDestination;
		public event EventHandler<LocomotionAction> PassingWaypoint;
		public event EventHandler<LocomotionAction> TryingNewRoute;
		public event EventHandler<IIMUEvent> IMUEvent;

		public LocomotionState CurrentLocomotionState { get; private set; } = new LocomotionState();
		
		public event EventHandler<DateTime> CompletedAnimationScript;
		public event EventHandler<DateTime> StartedAnimationScript;
		public event EventHandler<DateTime> AnimationScriptActionsComplete;
		public event EventHandler<DateTime> RepeatingAnimationScript;

		public event EventHandler<TriggerData> SyncEvent;

		public event EventHandler<KeyValuePair<AnimationRequest, Interaction>> TriggerAnimation;

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
		private ITimeManager _timeManager;
		private IHeadManager _headManager;
		private AnimationRequest _currentAnimation;
		private Interaction _currentInteraction;
		private bool _repeatScript;
		private WebMessenger _webMessenger;

		private TaskCompletionSource<bool> _receivedSyncEvent;
		private TaskCompletionSource<bool> _interactionCancellationEvent;
		private TaskCompletionSource<bool> _receivedSpeechCompletionEvent;

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
		
		private bool _responsiveState = true; //by default, let other bots call this bot if it knows the IP

		private IMistyState _mistyState;
		private ICommandManager _commandManager;

		//TODO Cleanup of event vs commands since passing in anyway

		public event EventHandler<KeyValuePair<string, TriggerData>> AddTrigger;
		//public event EventHandler<KeyValuePair<string, TriggerData>> RegisterEvent;
		public event EventHandler<string> RemoveTrigger;
		public event EventHandler<TriggerData> ManualTrigger;
		public ConversationData _currentConversationData;

		public AnimationManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters, ISpeechManager speechManager, IMistyState mistyState, ITimeManager timeManager = null, IHeadManager headManager = null, ICommandManager commandManager = null)
		: base(misty, parameters, characterParameters)
		{
			_speechManager = speechManager;
			_headManager = headManager ?? new HeadManager(Robot, Parameters, CharacterParameters);
			_timeManager = timeManager ?? new EnglishTimeManager(Robot, Parameters, CharacterParameters);
			_commandManager = commandManager;
		
			_webMessenger = new WebMessenger();
			CurrentLocomotionState.LocomotionStatus = LocomotionStatus.Unknown;

			_speechManager.StoppedSpeaking += _speechManager_StoppedSpeaking;
			//_speechManager.UserDataAnimationScript += _speechManager_UserDataAnimationScript;
			_mistyState = mistyState;
		}

		public void SpeechResponseHandler(object sender, TriggerData data)
		{
			//_speechIntent = data;
		}

		public async void HandleStartedProcessingVoice(object sender, IVoiceRecordEvent voiceEvent)
		{
			//TODO?
		}

		public async void HandleStartedListening(object sender, DateTime time)
		{
			//TODO?
		}

		//private async void _speechManager_UserDataAnimationScript(object sender, string script)
		//{
		//	_headManager.StopMovement();
		//	await StopRunningAnimationScripts();
		//	await RunAnimationScript(script, false, _currentAnimation, _currentInteraction, _currentConversationData);
		//}

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
				PlaceOnTop = false
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
			//_semaphoreSlim.Wait();
			try
			{
				foreach (string command in _completionCommands)
				{
					await ProcessCommand(command, true, 0);
				}
				_animationsCanceled = true;

				await WaitOnCompletionEvent(3000);

				_completionCommands.Clear();
				_runningAnimation = false;
				_animationsCanceled = false;

			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Failed stopping animation script.", ex);
			}
			finally
			{
				//_semaphoreSlim.Release();
			}
		}

		private async Task<bool> WaitOnCompletionEvent(int timeout)
		{
			if (!_runningAnimation)
			{
				return false;
			}
			if(timeout < 100)
			{
				timeout = 100;
			}

			bool response = false;
			_interactionCancellationEvent = null;
			_interactionCancellationEvent = new TaskCompletionSource<bool>();
			try
			{
				//wait a max of 5 seconds before starting next interaction. this can happen if people do long running actions and don't delay at the end
				if (_interactionCancellationEvent.Task == await Task.WhenAny(_interactionCancellationEvent.Task, Task.Delay(timeout)))
				{
					response = _interactionCancellationEvent.Task.Result;
				}
				else
				{
					Robot.SkillLogger.LogInfo("Timeout waiting for interaction cancellation.");
				}
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.LogError("Failed waiting for interaction cancellation.", ex);
			}

			_interactionCancellationEvent = null;
			return response;
		}

		public async Task<bool> RunAnimationScript(string animationScript, bool repeatScript, AnimationRequest currentAnimation, Interaction currentInteraction, ConversationData currentConversationData, bool stopOnFailedCommand = false)
		{
			try
			{
				//if(_runningAnimation)
				//{
				//	return false;
				//}
				_runningAnimation = true;
				_currentConversationData = currentConversationData;
				if (!string.IsNullOrWhiteSpace(animationScript))
				{
					_animationsCanceled = false;
					StartedAnimationScript?.Invoke(this, DateTime.Now);
					_repeatScript = repeatScript;
					_currentAnimation = currentAnimation;
					_currentInteraction = currentInteraction;
					animationScript = animationScript.Trim().Replace(Environment.NewLine, "").Replace("’", "'").Replace("“", "\"").Replace("”", "\"");
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
						if (_animationsCanceled)
						{
							_interactionCancellationEvent?.TrySetResult(true);
							return false;
						}
						else if (loopCount > 1 && !_repeatScript)
						{
							_interactionCancellationEvent?.TrySetResult(true);
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
									try
									{
										_interactionCancellationEvent?.TrySetResult(true);
										return false;
									}
									catch (Exception ex)
									{
										Robot.SkillLogger.Log($"Failed stopping animation script.", ex);
									}
									finally
									{
									}
									return false;
								}
								else
								{
									if (_animationsCanceled)
									{
										_interactionCancellationEvent?.TrySetResult(true);
										return false;
									}

									if (!string.IsNullOrWhiteSpace(_waitingEvent) || _awaitAny)
									{
										await WaitOnSyncEvent();
									}

									if (_animationsCanceled)
									{
										_interactionCancellationEvent?.TrySetResult(true);
										return false;
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

					_runningAnimation = false;
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
				//_runningAnimation = false;
			}
		}

		private async Task<bool> WaitOnSyncEvent()
		{
			if(!_runningAnimation)
			{
				return false;
			}
			bool response = false;
			_receivedSyncEvent = null;
			_receivedSyncEvent = new TaskCompletionSource<bool>();
			_interactionCancellationEvent = new TaskCompletionSource<bool>();
			try
			{
				if (_receivedSyncEvent.Task == await Task.WhenAny(_receivedSyncEvent.Task, _interactionCancellationEvent?.Task, Task.Delay(_waitingTimeoutMs)))
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
			if (!_runningAnimation)
			{
				return false;
			}
			bool response = false;
			_receivedSpeechCompletionEvent = null;
			_receivedSpeechCompletionEvent = new TaskCompletionSource<bool>();
			_interactionCancellationEvent = new TaskCompletionSource<bool>();
			try
			{
				if (_receivedSpeechCompletionEvent.Task == await Task.WhenAny(_receivedSpeechCompletionEvent.Task, Task.Delay(timeoutMs), _interactionCancellationEvent?.Task))
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
			if (array == null || array.Length < index || array.ElementAtOrDefault(index) == null || array.ElementAtOrDefault(index).Trim() == "null")
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
								Robot selectedBot = allRobots.FirstOrDefault(x => string.Compare(x.RobotName, robotName, true) == 0);
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
						//TODO Replace with CommandManager classes

						//split the rest based on action, hacky wacky for now
						//TODO deal with inconsistencies for arms and head, so all scripting is ms
						switch (action.ToUpper())
						{
							case "ARMS":
								//ARMS:leftDegrees,rightDegrees,timeMs;
								if(!CharacterParameters.IgnoreArmCommands)
								{
									string[] data = commandData[1].Split(",");
									_ = Robot.MoveArmsAsync(Convert.ToDouble(data[0]), Convert.ToDouble(data[1]), null, null, Convert.ToDouble(data[2]) / 1000, AngularUnit.Degrees);
								}
								else
								{
									_ = Robot.HaltAsync(new List<MotorMask>{ MotorMask.RightArm, MotorMask.LeftArm });
								}
								break;
							case "ARMS-OFFSET":
								//ARMS:leftDegrees,rightDegrees,timeMs;
								if (!CharacterParameters.IgnoreArmCommands)
								{
									string[] data1 = commandData[1].Split(",");

									_ = Robot.MoveArmsAsync(Convert.ToDouble(data1[0]) + _mistyState.GetCharacterState().LeftArmActuatorEvent.ActuatorValue, Convert.ToDouble(data1[1]) + _mistyState.GetCharacterState().RightArmActuatorEvent.ActuatorValue, null, null, Convert.ToDouble(data1[2]) / 1000, AngularUnit.Degrees);
								}
								else
								{
									_ = Robot.HaltAsync(new List<MotorMask> { MotorMask.RightArm, MotorMask.LeftArm });
								}
								break;
							case "ARMS-OFFSET-V":
								//ARMS:leftDegrees,rightDegrees,timeMs;
								if (!CharacterParameters.IgnoreArmCommands)
								{
									string[] datav1 = commandData[1].Split(",");
									_ = Robot.MoveArmsAsync(Convert.ToDouble(datav1[0]) + _mistyState.GetCharacterState().LeftArmActuatorEvent.ActuatorValue, Convert.ToDouble(datav1[1]) + _mistyState.GetCharacterState().RightArmActuatorEvent.ActuatorValue, null, Convert.ToDouble(datav1[2]), null, AngularUnit.Degrees);
								}
								else
								{
									_ = Robot.HaltAsync(new List<MotorMask> { MotorMask.RightArm, MotorMask.LeftArm });
								}
								break;
							case "ARM-V":
								//ARMS-V:leftDegrees,rightDegrees,velocity;
								if (!CharacterParameters.IgnoreArmCommands)
								{
									string[] armVData = commandData[1].Split(",");
									RobotArm selectedVArm = armVData[0].ToLower().StartsWith("r") ? RobotArm.Right : RobotArm.Left;
									_ = Robot.MoveArmAsync(Convert.ToDouble(armVData[1]), selectedVArm, Convert.ToDouble(armVData[2]), null, AngularUnit.Degrees);
								}
								else
								{
									_ = Robot.HaltAsync(new List<MotorMask> { MotorMask.RightArm, MotorMask.LeftArm });
								}
								break;
							case "ARM":
								//ARM:left/right,degrees,timeMs;
								if (!CharacterParameters.IgnoreArmCommands)
								{
									string[] armData = commandData[1].Split(",");
									RobotArm selectedArm = armData[0].ToLower().StartsWith("r") ? RobotArm.Right : RobotArm.Left;
									_ = Robot.MoveArmAsync(Convert.ToDouble(armData[1]), selectedArm, null, Convert.ToDouble(armData[2]) / 1000, AngularUnit.Degrees);
								}
								else
								{
									_ = Robot.HaltAsync(new List<MotorMask> { MotorMask.RightArm, MotorMask.LeftArm });
								}
								break;
							case "ARM-OFFSET":
								//ARM:left/right,degrees,timeMs;
								if (!CharacterParameters.IgnoreArmCommands)
								{
									string[] armOData = commandData[1].Split(",");
									RobotArm selectedArm2 = armOData[0].ToLower().StartsWith("r") ? RobotArm.Right : RobotArm.Left;
									double armDegrees = armOData[0].ToLower().StartsWith("r") ? _mistyState.GetCharacterState().RightArmActuatorEvent.ActuatorValue : _mistyState.GetCharacterState().LeftArmActuatorEvent.ActuatorValue;
									_ = Robot.MoveArmAsync(Convert.ToDouble(armOData[1]) + armDegrees, selectedArm2, null, Convert.ToDouble(armOData[2]) / 1000, AngularUnit.Degrees);
								}
								else
								{
									_ = Robot.HaltAsync(new List<MotorMask> { MotorMask.RightArm, MotorMask.LeftArm });
								}
								break;
							case "ARM-OFFSET-V":
								//ARM:left/right,degrees,timeMs;
								if (!CharacterParameters.IgnoreArmCommands)
								{
									string[] armOvData = commandData[1].Split(",");
									RobotArm selectedArm3 = armOvData[0].ToLower().StartsWith("r") ? RobotArm.Right : RobotArm.Left;
									double armDegrees2 = armOvData[0].ToLower().StartsWith("r") ? _mistyState.GetCharacterState().RightArmActuatorEvent.ActuatorValue : _mistyState.GetCharacterState().LeftArmActuatorEvent.ActuatorValue;
									_ = Robot.MoveArmAsync(Convert.ToDouble(armOvData[1]) + armDegrees2, selectedArm3, Convert.ToDouble(armOvData[2]), null, AngularUnit.Degrees);
								}
								else
								{
									_ = Robot.HaltAsync(new List<MotorMask> { MotorMask.RightArm, MotorMask.LeftArm });
								}
								break;
							case "HEAD":
								//HEAD:pitch,roll,yaw,timeMs;
								if (!CharacterParameters.IgnoreHeadCommands)
								{
									_headManager.StopMovement();
									string[] headData = commandData[1].Split(",");
									_ = Robot.MoveHeadAsync(GetNullableObject(headData, 0), GetNullableObject(headData, 1), GetNullableObject(headData, 2), null, Convert.ToDouble(headData[3]) / 1000, AngularUnit.Degrees);
									
								}
								else
								{
									_ = Robot.HaltAsync(new List<MotorMask> { MotorMask.HeadPitch, MotorMask.HeadRoll, MotorMask.HeadYaw });
								}
								break;
							case "HEAD-OFFSET":
								//HEAD:pitch,roll,yaw,velocity;
								if (!CharacterParameters.IgnoreHeadCommands)
								{
									_headManager.StopMovement();
									string[] headOData = commandData[1].Split(",");
									_ = Robot.MoveHeadAsync(GetNullableObject(headOData, 0) == null ? null : GetNullableObject(headOData, 0) + _mistyState.GetCharacterState().HeadPitchActuatorEvent.ActuatorValue,
											GetNullableObject(headOData, 1) == null ? null : GetNullableObject(headOData, 1) + _mistyState.GetCharacterState().HeadRollActuatorEvent.ActuatorValue,
											GetNullableObject(headOData, 2) == null ? null : GetNullableObject(headOData, 2) + _mistyState.GetCharacterState().HeadYawActuatorEvent.ActuatorValue,
											null, Convert.ToDouble(headOData[3]) / 1000, AngularUnit.Degrees);
								}
								else
								{
									_ = Robot.HaltAsync(new List<MotorMask> { MotorMask.HeadPitch, MotorMask.HeadRoll, MotorMask.HeadYaw });
								}
								break;
							case "HEAD-OFFSET-V":
								//HEAD:pitch,roll,yaw,velocity;
								if (!CharacterParameters.IgnoreHeadCommands)
								{
									_headManager.StopMovement();
									string[] headOvData = commandData[1].Split(",");
									_ = Robot.MoveHeadAsync(GetNullableObject(headOvData, 0) == null ? null : GetNullableObject(headOvData, 0)+ _mistyState.GetCharacterState().HeadPitchActuatorEvent.ActuatorValue,
											GetNullableObject(headOvData, 1) == null ? null : GetNullableObject(headOvData, 1) + _mistyState.GetCharacterState().HeadRollActuatorEvent.ActuatorValue,
											GetNullableObject(headOvData, 2) == null ? null : GetNullableObject(headOvData, 2) + _mistyState.GetCharacterState().HeadYawActuatorEvent.ActuatorValue,
											Convert.ToDouble(headOvData[3]), null, AngularUnit.Degrees);
								}
								else
								{
									_ = Robot.HaltAsync(new List<MotorMask> { MotorMask.HeadPitch, MotorMask.HeadRoll, MotorMask.HeadYaw });
								}
								break;
							case "HEAD-V":
								//HEAD:pitch,roll,yaw,velocity;
								if (!CharacterParameters.IgnoreHeadCommands)
								{
									_headManager.StopMovement();
									string[] headVData = commandData[1].Split(",");
									_ = Robot.MoveHeadAsync(GetNullableObject(headVData, 0), GetNullableObject(headVData, 1), GetNullableObject(headVData, 2), Convert.ToDouble(headVData[3]), null, AngularUnit.Degrees);
								}
								else
								{
									_ = Robot.HaltAsync(new List<MotorMask> { MotorMask.HeadPitch, MotorMask.HeadRoll, MotorMask.HeadYaw });
								}
								break;
							case "PAUSE":
								//PAUSE:timeMs;
								await Task.Delay(Convert.ToInt32(commandData[1]));

								//await WaitOnCompletionEvent(Convert.ToInt32(commandData[1]));

								break;
							case "VOLUME":
								//VOLUME:newVolume;
								_speechManager.Volume = Convert.ToInt32(commandData[1]);
								break;
							case "VOLUME-OFFSET":
								//VOLUME:volumeOffset;
								_speechManager.Volume = Convert.ToInt32(commandData[1]) + _speechManager.Volume;
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
								string[] pictureData = commandData[1].SmartSplit(",");
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
								await HandleLocomotionAction(new LocomotionAction
								{
									Action = LocomotionCommand.Stop
								});
								break;
							case "HALT":
								//HALT;
								await HandleLocomotionAction(new LocomotionAction
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
										//PlaceOnTop = true
										PlaceOnTop = false //TODOmake a param
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
							case "END-SKILL":
								await StopRunningAnimationScripts();
								Robot.SkillCompleted();
								break;
							case "FOLLOW-FACE":
								//FOLLOW-FACE;
								HeadLocation _currentHeadRequest = new HeadLocation(-40, -2, -45, 10, 2, 45, 0.5, null);
								_currentHeadRequest.FollowFace = true;
								_currentHeadRequest.FollowObject = "";

								//_mistyState.RegisterEvent(Triggers.ObjectSeen);
								_mistyState.RegisterEvent(Triggers.FaceRecognized);
								_headManager.HandleHeadAction(_currentHeadRequest);
								break;
							case "FOLLOW-VOICE":
								_headManager.StopMovement();
								FollowVoice();
								break;
							case "FOLLOW-NOISE":
								_headManager.StopMovement();
								FollowNoise();
								break;
							case "STOP-AUDIO-FOLLOW":
								_headManager.StopMovement();
								StopFollowAudio();
								break;
							case "STOP-FOLLOW":
								//STOP-FOLLOW;
								//HeadLocation _stopFollowHeadRequest = new HeadLocation(-40, -2, -45, 10, 2, 45, 0.5, null);
								//_stopFollowHeadRequest.FollowFace = false;
								//_stopFollowHeadRequest.FollowObject = "";
								_headManager.StopMovement();//.HandleHeadAction(_stopFollowHeadRequest);
								break;
							case "FOLLOW-OBJECT":
								//FOLLOW-OBJECT:objectName;
								HeadLocation _objectHeadRequest = new HeadLocation(-40, -2, -45, 10, 2, 45, 0.5, null);
								_objectHeadRequest.FollowObject = Convert.ToString(commandData[1]);
								_objectHeadRequest.FollowFace = false;
								_mistyState.RegisterEvent(Triggers.ObjectSeen);
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
								string[] textData = commandData[1].SmartSplit(",");
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
								_mistyState.GetCharacterState().DisplayedScreenText = Convert.ToString(commandData[1]);
								break;
							case "CLEAR-TEXT":
								//CLEAR-TEXT;
								_userTextLayerVisible = false;

								_ = Robot.SetTextDisplaySettingsAsync("AnimationText", new TextSettings
								{
									Deleted = true
								});

								//Clear user data too, but don't delete
								_ = Robot.SetTextDisplaySettingsAsync("UserDataText", new TextSettings
								{
									Visible = false
								});
								_mistyState.GetCharacterState().DisplayedScreenText = "";
								break;
							case "UI-TEXT":
								SendUIEvent("UI-TEXT", Convert.ToString(commandData[1]));
								break;
							case "UI-IMAGE":
								SendUIEvent("UI-IMAGE", Convert.ToString(commandData[1]));
								break;
							case "UI-WEB":
								SendUIEvent("UI-WEB", Convert.ToString(commandData[1]));
								break;
							case "UI-AUDIO":
								SendUIEvent("UI-AUDIO", Convert.ToString(commandData[1]));
								break;
							case "UI-SPEECH":
								SendUIEvent("UI-SPEECH", Convert.ToString(commandData[1]));
								break;
							case "UI-LED":
								//UI-LED:red,green,blue;
								string[] uiLedData = commandData[1].Split(",");
								System.Drawing.Color systemColor = System.Drawing.Color.FromArgb(Convert.ToInt32(uiLedData[0]), Convert.ToInt32(uiLedData[1]), Convert.ToInt32(uiLedData[2]));
								string hex = systemColor.R.ToString("X2") + systemColor.G.ToString("X2") + systemColor.B.ToString("X2");
								SendUIEvent("UI-LED", hex);
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
								_currentAnimation.SpeakFileName = "";
								await _speechManager.Speak(_currentAnimation, _currentInteraction, false);
								break;

							case "SPEAK-AND-WAIT":
								//SPEAK-AND-WAIT:What to say, timeoutMs;
								string[] sawData = commandData[1].SmartSplit(",");
								if (_speechManager.TryToPersonalizeData(sawData[0], _currentAnimation, _currentInteraction, out string newspeakText))
								{
									_currentAnimation.Speak = newspeakText;
								}
								else
								{
									_currentAnimation.Speak = sawData[0];
								}

								_currentInteraction.StartListening = false;
								_currentAnimation.SpeakFileName = "";
								_ = _speechManager.Speak(_currentAnimation, _currentInteraction, false);
								await WaitOnSpeechCompletionEvent(Convert.ToInt32(sawData[1]));
								break;

							case "SPEAK-AND-SYNC":
								//SPEAK-AND-SYNC:What to say - without commas for now,SyncName;
								//TODO Timeout??

								string[] sasData = commandData[1].SmartSplit(",");
								if (_speechManager.TryToPersonalizeData(sasData[0], _currentAnimation, _currentInteraction, out string newText2))
								{
									_currentAnimation.Speak = newText2;
								}
								else
								{
									_currentAnimation.Speak = sasData[0];
								}
								_currentAnimation.SpeakFileName = "";
								_currentInteraction.StartListening = false;


								await _speechManager.Speak(_currentAnimation, _currentInteraction, false);
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
								string[] saeData = commandData[1].SmartSplit(",");
								if (_speechManager.TryToPersonalizeData(saeData[0], _currentAnimation, _currentInteraction, out string newText3))
								{
									_currentAnimation.Speak = newText3;
								}
								else
								{
									_currentAnimation.Speak = saeData[0];
								}
								_currentInteraction.StartListening = false;
								_currentAnimation.SpeakFileName = "";

								await _speechManager.Speak(_currentAnimation, _currentInteraction, false);
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
								_currentAnimation.SpeakFileName = "";
								await _speechManager.Speak(_currentAnimation, _currentInteraction, false);
								break;
							case "START-LISTEN":
								//START-LISTEN;
								switch (CharacterParameters.SpeechRecognitionService.ToLower().Trim())
								{
									case "googleonboard":
										_ = Robot.CaptureSpeechGoogleAsync(false, (int)(_currentInteraction.ListenTimeout * 1000), (int)(_currentInteraction.SilenceTimeout * 1000), CharacterParameters.GoogleSpeechRecognitionParameters.SubscriptionKey, CharacterParameters.GoogleSpeechRecognitionParameters.SpokenLanguage);
										break;
									case "azureonboard":
										_ = Robot.CaptureSpeechAzureAsync(false, (int)(_currentInteraction.ListenTimeout * 1000), (int)(_currentInteraction.SilenceTimeout * 1000), CharacterParameters.AzureSpeechRecognitionParameters.SubscriptionKey, CharacterParameters.AzureSpeechRecognitionParameters.Region, CharacterParameters.AzureSpeechRecognitionParameters.SpokenLanguage);
										break;
									case "vosk":
										_ = Robot.CaptureSpeechVoskAsync(false, (int)(_currentInteraction.ListenTimeout * 1000), (int)(_currentInteraction.SilenceTimeout * 1000));
										break;
									case "deepspeech":
										_ = Robot.CaptureSpeechDeepSpeechAsync(false, (int)(_currentInteraction.ListenTimeout * 1000), (int)(_currentInteraction.SilenceTimeout * 1000));
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
								_ = HandleLocomotionAction(new LocomotionAction
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
								_ = HandleLocomotionAction(new LocomotionAction
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
								_ = HandleLocomotionAction(new LocomotionAction
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
								_ = HandleLocomotionAction(new LocomotionAction
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
								_ = HandleLocomotionAction(new LocomotionAction
								{
									Action = LocomotionCommand.Arc,
									Heading = Convert.ToDouble(arcData[0]),
									Radius = Convert.ToInt32(arcData[1]),
									TimeMs = Convert.ToInt32(arcData[2]),
									Reverse = Convert.ToBoolean(arcData[3].Trim())
								});
								break;
							case "RECORD":
								_ = Robot.StartRecordingAudioAsync(commandData[1]);
								break;
							case "STOP-RECORDING":
								_ = Robot.StopRecordingAudioAsync();
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
								
							case "TRIGGER":
								//TRIGGER:trigger,triggerFilter,text;
								string[] triggerEventData = commandData[1].Split(",");
								string text = "";
								if (triggerEventData.Length > 2)
								{
									text = triggerEventData[2];
								}
								ManualTrigger?.Invoke(this, new TriggerData(text, triggerEventData[1], triggerEventData[0]));
								break;
							case "GOTO-ACTION":
								//GOTO-ACTION:Action;
								string[] gotoData = commandData[1].Split(",");

								Guid guid;
								if(!Guid.TryParse(gotoData[0], out guid))
								{
									//it's the name, map it to id...
									
									Interaction interaction = _currentConversationData.Interactions.FirstOrDefault(x => string.Compare(x.Name, gotoData[0], true) == 0);
									guid = Guid.Parse(interaction.Id);
								}

								TriggerData newTriggerData0 = new TriggerData(guid.ToString(), Triggers.Manual, Triggers.Manual) { OverrideInteraction = guid.ToString() };
								newTriggerData0.KeepAlive = false;
								newTriggerData0.OverrideInteraction = guid.ToString();
								AddTrigger?.Invoke(this, new KeyValuePair<string, TriggerData>(gotoData[0], newTriggerData0));
								//_mistyState.RegisterEvent(Triggers.Manual);
								await Task.Delay(200);
								ManualTrigger?.Invoke(this, newTriggerData0);
								break;

								//NEW FROM IM - TODO MORE TESTING!!!!
							case "T-ACTION":
								//T-ACTION:Name,OverrideAction,Trigger, TriggerFilter;
								string[] triggerData2 = commandData[1].Split(",");
								string filter2 = "";
								if (triggerData2.Length == 4)
								{
									filter2 = triggerData2[3];
								}
								//?? Part of add trigger?  RegisterEvent(triggerData2[2]);
								_mistyState.RegisterEvent(triggerData2[2]);
								TriggerData newTriggerData = new TriggerData(null, filter2, triggerData2[2]);
								newTriggerData.KeepAlive = false;
								newTriggerData.OverrideInteraction = triggerData2[1];
								AddTrigger?.Invoke(this, new KeyValuePair<string, TriggerData>(triggerData2[0], newTriggerData));								
								break;
							case "T-ON":
								//T-ON:Name,Trigger,TriggerFilter;
								string[] triggerData = commandData[1].Split(",");
								string filter = "";
								if (triggerData.Length == 3)
								{
									filter = triggerData[2];
								}
								//?? Part of add trigger?  RegisterEvent(triggerData[1]);
								_mistyState.RegisterEvent(triggerData[1]);
								AddTrigger?.Invoke(this, new KeyValuePair<string, TriggerData>(triggerData[0], new TriggerData(null, filter, triggerData[1])));
								break;
							case "T-ACTION!":
								//T-ACTION!:Name,OverrideAction,Trigger, TriggerFilter;
								string[] triggerDataA = commandData[1].Split(",");
								string filterA = "";
								if (triggerDataA.Length == 4)
								{
									filterA = triggerDataA[3];
								}
								_mistyState.RegisterEvent(triggerDataA[2]);
								TriggerData newTriggerDataA = new TriggerData(null, filterA, triggerDataA[2]);
								newTriggerDataA.KeepAlive = true;
								newTriggerDataA.OverrideInteraction = triggerDataA[1];
								AddTrigger?.Invoke(this, new KeyValuePair<string, TriggerData>(triggerDataA[0], newTriggerDataA));
								break;
							case "T-ON!":
								//T-ON!:Name,Trigger,TriggerFilter;
								string[] triggerData4 = commandData[1].Split(",");
								string filter4 = "";
								if (triggerData4.Length == 3)
								{
									filter4 = triggerData4[2];
								}
								_mistyState.RegisterEvent(triggerData4[1]);
								TriggerData newTriggerData4 = new TriggerData(null, filter4, triggerData4[1]);
								newTriggerData4.KeepAlive = true;
								AddTrigger?.Invoke(this, new KeyValuePair<string, TriggerData>(triggerData4[0], newTriggerData4));
								break;
							case "T-OFF":
								//T-OFF:Name;
								RemoveTrigger?.Invoke(this, commandData[1]);
								break;

							case "SET-AUDIO-TRIM":
								_speechManager.SetAudioTrim(Convert.ToInt32(commandData[1]));
								break;
							case "SET-MAX-SILENCE":
								//command expects seconds
								_speechManager.SetMaxSilence(Convert.ToDouble(commandData[1]) / 1000.0);
								break;
							case "SET-MAX-LISTEN":
								//command expects seconds
								_speechManager.SetMaxListen(Convert.ToDouble(commandData[1]) / 1000.0);
								break;
							case "SET-SPEECH-PITCH":
								_speechManager.SetPitch(commandData[1]);
								break;
							case "SET-VOICE":
								_speechManager.SetVoice(commandData[1]);
								break;
							case "SET-SPEECH-STYLE":
								_speechManager.SetSpeakingStyle(commandData[1]);
								break;
							case "SET-LANGUAGE":
								_speechManager.SetLanguage(commandData[1]);
								break;
							case "SET-SPEECH-RATE":
								_speechManager.SetSpeechRate(Convert.ToDouble(commandData[1]));
								break;
							case "ANIMATE":								
								AnimationRequest animationRequest = _currentConversationData.Animations.FirstOrDefault(x => string.Compare(x.Name, commandData[1], true) == 0 || string.Compare(x.Id, commandData[1], true) == 0);
								if(animationRequest != null)
								{
									TriggerAnimation?.Invoke(this, new KeyValuePair<AnimationRequest, Interaction>(animationRequest, new Interaction(_currentInteraction)));
								}								
								break;
							case "TIMED-TRIGGER":
								//TIMED-TRIGGER:timeMs,trigger,triggerFilter,text;
								string[] timedTriggerEventData = commandData[1].Split(",");
								string timedText = "";
								if (timedTriggerEventData.Length > 3)
								{
									timedText = timedTriggerEventData[3];
								}

								_ = Task.Run(async () =>
								{
									//Use timer instead?
									int delay = Convert.ToInt32(timedTriggerEventData[0]);
									if(delay > 0)
									{
										await Task.Delay(delay);
									}
									
									ManualTrigger?.Invoke(this, new TriggerData(timedText, timedTriggerEventData[2], timedTriggerEventData[1]));
								});
								break;
							
							case "AWAIT-EVENT":
								//AWAIT-EVENT:EventName1,10000/-1;
								//TODO Not quite right
								string[] awaitSyncEventData = commandData[1].Split(",");
								lock (_waitingLock)
								{
									_waitingEvent = awaitSyncEventData[0].Trim();
									Robot.RegisterUserEvent(_waitingEvent, UserEventCallback, 0, false, null);
									_waitingTimeoutMs = Convert.ToInt32(awaitSyncEventData[1]);
								}
								break;

							default:

								if(_commandManager.TryGetCommand(action, out IBaseCommand userCommand))
								{
									Robot.SkillLogger.Log($"Running user command. {action?.ToUpper()}");
									string[] userParams;
									if(commandData.Length > 1)
									{
										userParams = commandData[1].SmartSplit(",");
									}
									else
									{
										userParams = new string[0];
									}
									
									await userCommand.ExecuteAsync(userParams);
									if(!string.IsNullOrWhiteSpace(userCommand.ResponseAction))
									{
										_headManager.StopMovement();
										await StopRunningAnimationScripts();
										await RunAnimationScript(userCommand.ResponseAction, false, new AnimationRequest(_currentAnimation), new Interaction(_currentInteraction), _currentConversationData);
									}
									if (!string.IsNullOrWhiteSpace(userCommand.CompletionTrigger.Trigger))
									{
										ManualTrigger?.Invoke(this, userCommand.CompletionTrigger);
									}
								}
								else
								{
									Robot.SkillLogger.Log($"Unknown command in animation script. {action?.ToUpper()}");
									return new CommandResult { Success = false };
								}
								break;
						}
					}

					//You should end up here if sending external commands only //TODO cleanup
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


		private void UserEventCallback(IUserEvent userEvent)
		{
			if (string.Compare(userEvent.EventName.Trim(), _waitingEvent.Trim(), true) == 0)
			{
				_receivedSyncEvent?.TrySetResult(true);
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

		private void SendUIEvent(string action, string data)
		{
			try
			{
				IDictionary<string, object> msgObject = new Dictionary<string, object>
				{
					{"DataType", "ui"},
					{"Action", action},
					{"Data", data}
				};

				string msg = JsonConvert.SerializeObject(msgObject);

				if (!string.IsNullOrWhiteSpace(msg))
				{
					Robot.PublishMessage(msg, null);
				}
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.LogError($"Failed to send ui event.", ex);
			}
		}

		//TODO Move audio/face/object following to separate classes
		private async void StopFollowAudio()
		{
			await Robot.StopRecordingAudioAsync();
			Robot.UnregisterEvent("SourceFocus", null);
			Robot.UnregisterEvent("SourceTrack", null);
		}

		private void SourceFocusCallback(ISourceFocusConfigMessageEvent sourceEvent)
		{
			_sourceFocusConfigMessageEvent = sourceEvent;
		}

		private void SourceTrackCallback(ISourceTrackDataMessageEvent trackEvent)
		{
			_sourceTrackDataMessageEvent = trackEvent;
		}
		
		private ISourceFocusConfigMessageEvent _sourceFocusConfigMessageEvent;
		private ISourceTrackDataMessageEvent _sourceTrackDataMessageEvent;

		private void FollowVoiceCallback(object _timerData)
		{
			if (_sourceTrackDataMessageEvent == null)
			{
				return;
			}

			int? doa = null;

			if (_followNoise)
			{
				if(_sourceTrackDataMessageEvent.DegreeOfArrivalNoise.Count() > 0)//TODO
				{
					doa = _sourceTrackDataMessageEvent.DegreeOfArrivalNoise.First();
				}
			}
			else
			{
				doa = _sourceTrackDataMessageEvent.DegreeOfArrivalSpeech;
			}
			
			//This is head based, so if head is turned, the degrees have changed as well
			if (doa == null || doa >= 358 || doa <= 2) //good enough...
			{
				return;
			}

			int? speechDegree = _sourceTrackDataMessageEvent.DegreeOfArrivalSpeech;
			if (speechDegree == null || speechDegree < 0 || speechDegree > 360)
			{
				return;
			}

			int anotherSpeechDegree = (int)speechDegree;
			
			if (anotherSpeechDegree < RobotConstants.MinimumYawDegreesInclusive)
			{
				anotherSpeechDegree = RobotConstants.MinimumYawDegreesInclusive;
			}
			if (anotherSpeechDegree >= RobotConstants.MaximumYawDegreesExclusive)
			{
				anotherSpeechDegree = RobotConstants.MaximumYawDegreesExclusive - 1;
			}

			if (anotherSpeechDegree >= 0 && anotherSpeechDegree <= 180)
			{
				Robot.MoveHead(-20, 0, anotherSpeechDegree, 80, AngularUnit.Degrees, null);
			}
			else
			{
				Robot.MoveHead(-20, 0, -anotherSpeechDegree, 80, AngularUnit.Degrees, null);
			}

			//TODO now body

		}

		private bool _followNoise = false;

		private async void FollowVoice()
		{
			_followNoise = false;
			await FollowAudio();
		}


		private async void FollowNoise()
		{
			_followNoise = true;
			await FollowAudio();
		}

		private async Task FollowAudio()
		{
			Robot.RegisterSourceFocusConfigEvent(SourceFocusCallback, 0, true, "SourceFocus", null);
			Robot.RegisterSourceTrackDataEvent(SourceTrackCallback, 250, true, "SourceTrack", null);
			await Robot.StartRecordingAudioAsync("follow-my-voice");
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


		/// <summary>
		/// Handle loco actions for this character
		/// </summary>
		public async Task HandleLocomotionAction(LocomotionAction locomotionAction)
		{
			try
			{
				CurrentLocomotionState.LocomotionAction = locomotionAction;
				CurrentLocomotionState.LocomotionStatus = LocomotionStatus.Starting;

				StartedLocomotionAction?.Invoke(this, locomotionAction);
				//based upon request, do stuff and other things

				switch (locomotionAction.Action)
				{
					case LocomotionCommand.Stop:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.Stopped;
						await Robot.StopAsync();
						LocomotionStopped?.Invoke(this, locomotionAction);
						break;
					case LocomotionCommand.Halt:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.Stopped;
						await Robot.HaltAsync(new List<MotorMask> { MotorMask.AllMotors });
						LocomotionStopped?.Invoke(this, locomotionAction);
						break;
					case LocomotionCommand.Turn:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.ScriptDriving;
						await Turn((double)locomotionAction.Degrees, (int)locomotionAction.TimeMs, locomotionAction.Reverse);
						break;
					case LocomotionCommand.TurnHeading:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.ScriptDriving;
						await TurnHeading((double)locomotionAction.Heading, (int)locomotionAction.TimeMs, locomotionAction.Reverse);
						break;
					case LocomotionCommand.Arc:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.ScriptDriving;
						await Arc((double)locomotionAction.Degrees, (double)locomotionAction.Radius, (int)locomotionAction.TimeMs, locomotionAction.Reverse);
						break;
					case LocomotionCommand.Heading:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.ScriptDriving;
						await Heading((double)locomotionAction.Heading, (double)locomotionAction.DistanceMeters, (int)locomotionAction.TimeMs, locomotionAction.Reverse);
						break;
					case LocomotionCommand.Drive:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.ScriptDriving;
						await Drive((double)locomotionAction.DistanceMeters, (int)locomotionAction.TimeMs, locomotionAction.Reverse);
						break;
				}

				CompletedLocomotionAction?.Invoke(this, locomotionAction);
			}
			catch
			{
				//TODO
				LocomotionFailed?.Invoke(this, locomotionAction);
			}
		}

		private async Task Turn(double degrees, int timeMs, bool reverse = false)
		{
			await Robot.DriveArcAsync(CurrentLocomotionState.RobotYaw + degrees, 0, timeMs, reverse);
		}

		private async Task TurnHeading(double heading, int timeMs, bool reverse = false)
		{
			await Robot.DriveArcAsync(heading, 0, timeMs, reverse);
		}

		private async Task Arc(double degrees, double radius, int timeMs, bool reverse = false)
		{
			await Robot.DriveArcAsync(CurrentLocomotionState.RobotYaw + degrees, radius, timeMs, reverse);
		}

		private async Task Heading(double heading, double distance, int timeMs, bool reverse = false)
		{
			await Robot.DriveHeadingAsync(heading, distance, timeMs, reverse);
		}

		private async Task Drive(double distance, int timeMs, bool reverse = false)
		{
			await Robot.DriveHeadingAsync(CurrentLocomotionState.RobotYaw, distance, timeMs, reverse);
		}

		private bool _isDisposed = false;

		protected void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					ClearAnimationDisplayLayers();
					_headManager.StopMovement();
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
 