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

	public class EventPayload
	{
		public string Trigger { get; set; }
		public string Filter { get; set; }
		public string Text { get; set; }
	}

	public class AnimationManager : BaseManager, IAnimationManager
	{
		public event EventHandler<DateTime> CompletedAnimationScript;
		public event EventHandler<DateTime> StartedAnimationScript;
		public event EventHandler<DateTime> AnimationScriptActionsComplete;
		public event EventHandler<DateTime> RepeatingAnimationScript;

		public event EventHandler<DateTime> UserScriptEvent;

		private bool _userTextLayerVisible;
		private bool _webLayerVisible;
		private bool _videoLayerVisible;
		private bool _userImageLayerVisible;

		private bool _animationsCanceled;
		private bool _runningAnimation;
		private object _animationsCanceledLock = new object();
		private IList<string> _completionCommands = new List<string>();
		private ISpeechManager _speechManager;
		private ILocomotionManager _locomotionManager;
		private AnimationRequest _currentAnimation;
		private Interaction _currentInteraction;
		private bool _repeatScript;

		public AnimationManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters, ISpeechManager speechManager, ILocomotionManager locomotionManager)
		: base(misty, parameters, characterParameters)
		{
			_speechManager = speechManager;
			_locomotionManager = locomotionManager;
		}

		public override Task<bool> Initialize()
		{
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

		public async void SendCrossRobotEvent(string robotIpString, string trigger, string triggerFilter, string text, bool includeSelf=true)
		{
			string[] ips = robotIpString.Replace(Environment.NewLine, "").Split(";");
			foreach (string ip in ips)
			{
				WebMessenger webMessenger = new WebMessenger();
				
				IDictionary<string, object> payload = new Dictionary<string, object>();
				payload.Add("Trigger", trigger);
				payload.Add("Filter", triggerFilter);
				payload.Add("Text", text);

				UserEvent userEvent = new UserEvent("ExternalEvent", "ConversationBuilder", EventOriginator.Skill, payload, -1);
				string data = Newtonsoft.Json.JsonConvert.SerializeObject(userEvent);
				WebMessengerData response = await webMessenger.PostRequest(ip, data, "application/json");
			}
		}

		public void StopRunningAnimationScripts()
		{
			lock(_animationsCanceledLock)
			{
				_animationsCanceled = true;
				foreach(string command in _completionCommands)
				{
					_ = ProcessCommand(command, true);
				}
				_completionCommands.Clear();
				_runningAnimation = false;
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
						if (command.Contains("#"))
						{
							//Contains 'finally'/cleanup cmds
							_completionCommands.Add(command.Replace("#", "").Trim());
						}
						//deal with repeat command!
					}

					bool firstLoop = true;
					do
					{
						if ((!firstLoop && !_repeatScript) || _animationsCanceled)
						{
							return false;
						}
						else if (!firstLoop && _repeatScript)
						{
							RepeatingAnimationScript?.Invoke(this, DateTime.Now);
						}

						foreach (string runCommand in commands)
						{
							if (_animationsCanceled)
							{
								lock (_animationsCanceledLock)
								{
									_animationsCanceled = false;
								}
								return false;
							}
							else
							{
								CommandResult commandResult = await ProcessCommand(runCommand, false);
								if (!commandResult.Success && stopOnFailedCommand)
								{
									_runningAnimation = false;
									return false;
								}

							}
						}
						AnimationScriptActionsComplete?.Invoke(this, DateTime.Now);
						firstLoop = false;
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
		

		private async Task<CommandResult> ProcessCommand(string command, bool cleanupCommand)
		{
			// ; delimted :0)
			try
			{
				string[] commandData = command.Split(":", 2); //find just the first one!
				string action = "EMPTY";

				if (commandData.Length > 0)
				{
					if(commandData[0].Contains("#") && !cleanupCommand)
					{
						//a completion command to always run when animation ends, ignore for now
						return new CommandResult { Success = true };
					}

					action = commandData[0];
					action = action.Replace("#", "").Trim();

					//split the rest based on action, hacky wacky for now
					//deal with inconsistencies for arms and head, so all scripting is ms
					switch (action.ToUpper())
					{
						case "ARMS":
							//ARMS:leftDegrees,rightDegrees,timeMs;
							string[] data = commandData[1].Split(",");
							_ =  Robot.MoveArmsAsync(Convert.ToDouble(data[0]), Convert.ToDouble(data[1]), null, null, Convert.ToDouble(data[2]) / 1000, AngularUnit.Degrees);
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
						case "HEAD": 
							//HEAD:pitch,roll,yaw,timeMs;
							string[] headData = commandData[1].Split(",");
							_ = Robot.MoveHeadAsync(Convert.ToDouble(headData[0]), Convert.ToDouble(headData[1]), Convert.ToDouble(headData[2]), null, Convert.ToDouble(headData[3]) / 1000, AngularUnit.Degrees);
							break;
						case "HEAD-V":
							//HEAD:pitch,roll,yaw,velocity;
							string[] headVData = commandData[1].Split(",");
							_ = Robot.MoveHeadAsync(Convert.ToDouble(headVData[0]), Convert.ToDouble(headVData[1]), Convert.ToDouble(headVData[2]), Convert.ToDouble(headVData[3]), null, AngularUnit.Degrees);
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
							break;
						case "RESET-EYES":
							//RESET-EYES;
							_ = Robot.SetBlinkSettingsAsync(true, null, null, null, null, null);
							break;
						case "IMAGE":
							//IMAGE:imageName;
							if (!_userImageLayerVisible)
							{
								await Robot.SetImageDisplaySettingsAsync("UserImageLayer", new ImageSettings
								{
									Visible = true,
									PlaceOnTop = true
								});
								_userImageLayerVisible = true;
							}
							_ = Robot.DisplayImageAsync(Convert.ToString(commandData[1]), "UserImageLayer", false);
							break;
						case "CLEAR-IMAGE":							
							//CLEAR-IMAGE;
							_userImageLayerVisible = false;
							_ = Robot.SetImageDisplaySettingsAsync("UserImageLayer", new ImageSettings
							{
								Deleted = true
							});
							break;
						case "IMAGE-URL":
							//IMAGE-URL:URL;
							if (!_userImageLayerVisible)
							{
								await Robot.SetImageDisplaySettingsAsync("UserImageLayer", new ImageSettings
								{
									Visible = true,
									PlaceOnTop = true
								});
								_userImageLayerVisible = true;
							}
							_ = Robot.DisplayImageAsync(commandData[1], "UserImageLayer", true);
							break;
						case "TEXT":
							//TEXT:text to display;
							if (!_userTextLayerVisible)
							{
								await Robot.SetTextDisplaySettingsAsync("AnimationText", new TextSettings
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

							if(transType != LEDTransition.None)
							{
								_ = Robot.TransitionLEDAsync(Convert.ToUInt32(transitLedData[0]), Convert.ToUInt32(transitLedData[1]), Convert.ToUInt32(transitLedData[2]),
									Convert.ToUInt32(transitLedData[3]), Convert.ToUInt32(transitLedData[4]), Convert.ToUInt32(transitLedData[5]),
									transType, Convert.ToUInt32(transitLedData[6]));
							}
							break;

						case "SPEAK":
							//SPEAK:What to say;
							string toTalkyTalk = Convert.ToString(commandData[1]);

							if (_speechManager.TryToPersonalizeData(toTalkyTalk, _currentAnimation, _currentInteraction, out string newText, out _))
							{
								_currentAnimation.Speak = newText;
							}
							else
							{
								_currentAnimation.Speak = toTalkyTalk;
							}

							_currentInteraction.StartListening = false;
							_speechManager.Speak(_currentAnimation, _currentInteraction);
							break;

						case "SPEAK-AND-LISTEN":
							//SPEAK-AND-LISTEN:What to say;
							string toTalkyTalk2 = Convert.ToString(commandData[1]);

							if (_speechManager.TryToPersonalizeData(toTalkyTalk2, _currentAnimation, _currentInteraction, out string newText2, out _))
							{
								_currentAnimation.Speak = newText2;
							}
							else
							{
								_currentAnimation.Speak = toTalkyTalk2;
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
								Reverse = Convert.ToBoolean(driveData[2])
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
								Reverse = Convert.ToBoolean(headingData[3])
							});
							break;

						case "TURN":
							//TURN:degrees,timeMs,left/right;
							string[] turnData = commandData[1].Split(",");
							string direction = Convert.ToString(turnData[2]);
							direction = direction.ToLower();
							_ = _locomotionManager.HandleLocomotionAction(new LocomotionAction
							{
								Action = LocomotionCommand.Turn,
								Degrees = direction.StartsWith("r") ? -Math.Abs(Convert.ToDouble(turnData[0])) : Math.Abs(Convert.ToDouble(turnData[0])),
								TimeMs = Convert.ToInt32(turnData[1]),
								Reverse = false
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
								Reverse = Convert.ToBoolean(arcData[3])
							});
							break;

						//TODO
						case "RETURN":
							//RETURN; //return to last known waypoint
						case "WANDER":
							//WANDER:leftAreaMeters,upAreaMeters,rightAreaMeters,downAreaMeters,velocity;
						case "WAYPOINT":
							//WAYPOINT:waypoint-name,velocity?;
						default:
							Robot.SkillLogger.Log($"Unknown command in animation script. {action?.ToUpper()}");
							return new CommandResult { Success = false };
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
 