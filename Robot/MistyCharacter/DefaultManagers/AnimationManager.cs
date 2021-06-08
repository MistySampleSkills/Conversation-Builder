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
using Conversation.Common;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Messengers;
using System.Threading.Tasks;
using MistyRobotics.SDK;

namespace MistyCharacter
{
	public class CommandResult
	{
		public bool Success { get; set; }
	}

	public class AnimationManager : BaseManager, IAnimationManager
	{
		public AnimationManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters)
		: base(misty, parameters, characterParameters) {}

		private bool _userTextLayerVisible;
		private bool _webLayerVisible;
		private bool _videoLayerVisible;
		private bool _userImageLayerVisible;

		private bool _animationsCanceled;
		private bool _runningAnimation;
		private object _animationsCanceledLock = new object();
		private IList<string> _completionCommands = new List<string>();

		public override Task<bool> Initialize()
		{
			//Reusing this layer for these, should we?
			_ = Robot.SetTextDisplaySettingsAsync("UserDataText", new TextSettings
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

		public async Task<bool> RunAnimationScript(string animationScript, bool stopOnFailedCommand = false)
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
							if(!commandResult.Success && stopOnFailedCommand)
							{
								_runningAnimation = false;
								return false;
							}

						}
					}
					
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
					switch (action.ToUpper())
					{
						case "ARMS":
							//ARMS:leftDegrees,rightDegrees,timeMs;
							string[] data = commandData[1].Split(",");
							_ =  Robot.MoveArmsAsync(Convert.ToInt32(data[0]), Convert.ToInt32(data[1]), null, null, Convert.ToInt32(data[2]), AngularUnit.Degrees);
							break;
						case "ARM-V":
							//ARMS-V:leftDegrees,rightDegrees,velocity;
							string[] armVData = commandData[1].Split(",");
							RobotArm selectedVArm = armVData[0].ToLower().StartsWith("r") ? RobotArm.Right : RobotArm.Left;
							_ = Robot.MoveArmAsync(Convert.ToInt32(armVData[1]), selectedVArm, Convert.ToInt32(armVData[2]), null, AngularUnit.Degrees);
							break;
						case "ARM":
							//ARM:left/right,degrees,timeMs;
							string[] armData = commandData[1].Split(",");
							RobotArm selectedArm = armData[0].ToLower().StartsWith("r") ? RobotArm.Right : RobotArm.Left;
							_ = Robot.MoveArmAsync(Convert.ToInt32(armData[1]), selectedArm, null, Convert.ToInt32(armData[2]), AngularUnit.Degrees);
							break;
						case "HEAD": 
							//TODO THIS IS NOT WORKING, VEEEEERRRRY SLOW!
							//HEAD:pitch,roll,yaw,timeMs;
							string[] headData = commandData[1].Split(",");
							_ = Robot.MoveHeadAsync(Convert.ToInt32(headData[0]), Convert.ToInt32(headData[1]), Convert.ToInt32(headData[2]), null, Convert.ToInt32(headData[3]), AngularUnit.Degrees);
							break;
						case "HEAD-V":
							//HEAD:pitch,roll,yaw,velocity;
							string[] headVData = commandData[1].Split(",");
							_ = Robot.MoveHeadAsync(Convert.ToInt32(headVData[0]), Convert.ToInt32(headVData[1]), Convert.ToInt32(headVData[2]), Convert.ToInt32(headVData[3]), null, AngularUnit.Degrees);
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
							//PICTURE:image-name;

							//TODO Fit on screen!!
							_ = await Robot.TakePictureAsync(Convert.ToString(commandData[1]), false, true, true, null, null);
							break;
						case "SERIAL":
							//SERIAL:write to the serial stream;
							_ = await Robot.WriteToSerialStreamAsync(Convert.ToString(commandData[1]));
							break;							
						case "STOP":
							//STOP;
							await Robot.StopAsync();
							break;
						case "HALT":
							//HALT;
							await Robot.HaltAsync(new List<MotorMask> { MotorMask.AllMotors });
							break;
						case "RESET-LAYERS":
							//RESET-LAYERS;
							_ = Robot.SetDisplaySettingsAsync(true);							
							break;
						case "RESET-EYES":
							//HALT;
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
								await Robot.SetTextDisplaySettingsAsync("UserDataText", new TextSettings
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
							_ = Robot.DisplayTextAsync(Convert.ToString(commandData[1]), "UserDataText");
							break;
						case "CLEAR-TEXT":
							//CLEAR-TEXT;
							_userTextLayerVisible = false;
							_ = Robot.SetTextDisplaySettingsAsync("UserDataText", new TextSettings
							{
								Deleted = true
							});
							break;
						case "SPEAK":
							//SPEAK:What to say;

							//TODO Make this use the speech system....

							_ = Robot.SpeakAsync(Convert.ToString(commandData[1]), false, "ignore");
							break;
						case "AUDIO":
							//AUDIO:audio-file-name.wav;
							_ = Robot.PlayAudioAsync(Convert.ToString(commandData[1]), null);
							break;
						case "VIDEO":
							//VIDEO:videoName;
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
							//VIDEO-URL:videoName;

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
							//WEB:http://site-name;
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

						//TODO
						case "REPEAT":
							//REPEAT;
						case "DRIVE":
							//DRIVE:distanceMeters,timeMs,reverse;
						case "HEADING":
							//HEADING:heading,distanceMeters,timeMs,reverse;
						case "TURN":
							//TURN:+/-degrees,timeMs;
						case "ARC":
							//ARC:heading,radius,timeMs,reverse;
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

		private bool _isDisposed = false;

		protected void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					Robot.SetVideoDisplaySettings("VideoLayer", new VideoSettings
					{
						Deleted = true
					}, null);
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
 