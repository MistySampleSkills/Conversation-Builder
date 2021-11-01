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
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using Newtonsoft.Json;
using SkillTools.Web;
using MistyInteraction.Common;

namespace MistyInteraction.StateSystem
{
	public class MistyStateSystem : IDisposable
	{
		private IRobotMessenger _misty;
		private StartupParameters _startupParameters { get; set; }
		private WebMessenger _webMessenger;
		private MistyState _robotState;
		private Timer _robotStateTimer;
		private Timer _timeoutTimer;
		private ConcurrentDictionary<string, TriggerData> _triggers = new ConcurrentDictionary<string, TriggerData>();
		private object _triggerLock = new object();
		private string _triggerRule = "clear";

		public MistyStateSystem(IRobotMessenger misty, StartupParameters startupParameters)
		{
			_misty = misty;
			_startupParameters = startupParameters;
			_webMessenger = new WebMessenger();
			_robotState = new MistyState();
			_robotState.Volume = startupParameters.Robot.StartVolume;
			_robotState.Status = "Starting";
			if (_robotStateTimer != null)
			{
				_robotStateTimer?.Dispose();
			}

			if (_startupParameters.Robot.EventSendDebounce >= 1)
			{
				int debounce = _startupParameters.Robot.EventSendDebounce < 1000 ? 1000 : _startupParameters.Robot.EventSendDebounce;
				_robotStateTimer = new Timer(SendStateData, null, debounce, debounce);
			}

			_misty.RegisterBatteryChargeEvent(BatteryCallback, 1000, true, null, "BatteryEvent", null);

			_robotState.RobotName = _startupParameters.Robot.Name;
			_robotState.DemoName = _startupParameters.Demo.Name;
			_robotState.DemoStarted = DateTime.Now; //use local robot time, should we?
			_robotState.RoleName = _startupParameters.Role.Name;
			_robotState.GroupName = _startupParameters.DemoGroup.Name;
		}

		private void BatteryCallback(IBatteryChargeEvent battery)
		{
			_robotState.BatteryChargeEvent = (BatteryChargeEvent)battery;
			_robotState.ChargePercent = battery.ChargePercent;
			_robotState.IsCharging = battery.IsCharging;
		}

		public void SetCurrentAnimation(string interaction, string animation)
		{
			_robotState.CurrentAnimation = animation;
			_robotState.CurrentInteraction = interaction;
		}

		public void ClearTriggers(bool includeKeepAlive = false)
		{
			lock (_triggerLock)
			{
				if (includeKeepAlive)
				{
					_triggers.Clear();
				}
				else
				{
					IList<KeyValuePair<string, TriggerData>> toRemove = _triggers.Where(x => !x.Value.KeepAlive).ToList();
					foreach (KeyValuePair<string, TriggerData> item in toRemove)
					{
						_triggers.TryRemove(item.Key.ToLower().Trim(), out TriggerData oldData);
					}
				}
			}
		}

		private void SendStateData(object timerData)
		{
			try
			{
				if (_webMessenger == null)
				{
					return;
				}

				RobotStateEvent robotStateEvent = GetRobotStateEvent();
				_ = _misty.PublishMessageAsync(JsonConvert.SerializeObject(robotStateEvent));
			}
			catch (Exception ex)
			{
				_robotState.Status = "Error";
				_misty.SkillLogger.LogError($"Send state data threw an exception: {ex.Message}", ex);
			}
		}

		public void AddTriggerHandler(object sender, KeyValuePair<string, TriggerData> triggerData)
		{
			lock (_triggerLock)
			{
				_triggers.TryRemove(triggerData.Key.ToLower().Trim(), out TriggerData oldValue);
				_triggers.TryAdd(triggerData.Key.ToLower().Trim(), triggerData.Value);
			}

			if(triggerData.Key.ToLower().Trim() == Triggers.Timeout.ToLower() && triggerData.Value.TriggerFilter.ToLower().Trim() != "manual")
			{
				//start a timer callback
				_timeoutTimer?.Dispose();
				_timeoutTimer = new Timer(TimeoutCallback, triggerData.Value.TriggerFilter, Convert.ToInt32(triggerData.Value.TriggerFilter), Timeout.Infinite);
			}
		}

		private async void TimeoutCallback(object timerData)
		{
			string time = Convert.ToString(timerData);
			lock (_triggerLock)
			{
				if (!_triggers.ContainsKey(Triggers.Timeout.ToLower().Trim()))
				{
					return;
				}
			}
			_ = await ProcessEvent(Triggers.Timeout, time, "");

		}

		public void RemoveTriggerHandler(object sender, string name)
		{
			lock (_triggerLock)
			{
				_triggers.TryRemove(name.ToLower().Trim(), out TriggerData triggerData);

			}
		}

		public void SetTriggerRule(string rule)
		{
			_triggerRule = rule;
		}

		private async Task<bool> ProcessEvent(string trigger, string triggerFilter, string text)
		{
			try
			{
				//Check against valid triggers
				//TODO Check against current state too?
				KeyValuePair<string, TriggerData> data = new KeyValuePair<string, TriggerData>();

				if(trigger.ToLower().Trim() == "timeout")
				{
					data = _triggers.FirstOrDefault(x => x.Value.Trigger.Trim().ToLower() == trigger.Trim().ToLower() && Convert.ToInt32(x.Value.TriggerFilter) <= Convert.ToInt32(triggerFilter.Trim()));
				}
				if (data.Value == null)
				{
					data = _triggers.FirstOrDefault(x => x.Value.Trigger.Trim().ToLower() == trigger.Trim().ToLower() && x.Value.TriggerFilter.Trim().ToLower() == triggerFilter.Trim().ToLower());
				}

				if (data.Value == null)
				{
					return false;
				}

				lock (_triggerLock)
				{
					switch (_triggerRule)
					{

						case "clear-all":
							_triggers.Clear();
							break;
						case "clear-trigger":
							_triggers.TryRemove(data.Key.ToLower().Trim(), out TriggerData matchedTrigger);
							break;
						case "clear":
						default:
							Dictionary<string, TriggerData> newTriggers = _triggers.Where(x => !x.Value.KeepAlive).ToDictionary(x => x.Key, y => y.Value);
							foreach (KeyValuePair<string, TriggerData> triggerX in newTriggers)
							{
								_triggers.Remove(triggerX.Key.ToLower().Trim(), out TriggerData value);
							}
							break;
					}
					//A match, so remove it so it doesn't keep triggering
				}

				_robotState.Status = $"Valid Trigger: {trigger} - {triggerFilter} - {text}";

				//send to IMS if a valid trigger
				RobotTriggerEvent robotTriggerEvent = new RobotTriggerEvent
				{
					DemoId = _startupParameters.Demo.Id,
					RobotId = _startupParameters.Robot.Id,
					RoleId = _startupParameters.Role.Id,
					DemoGroupId = _startupParameters.DemoGroup.Id,
					AccessId = _startupParameters.AccessId,
					Trigger = trigger,
					TriggerFilter = triggerFilter,
					//this may not be mapped if using chatbot
					NextInteraction = data.Value.OverrideInteraction,
					Text = text,
					RobotStateEvent = GetRobotStateEvent()
				};

				//Send message to IMS with trigger information
				_ = await _webMessenger.PostRequest(_startupParameters.Endpoint + "api/interactions/trigger", JsonConvert.SerializeObject(robotTriggerEvent), "application/json");
				_misty.SkillLogger.LogInfo($"INTERACTION MGR API - {trigger} sent");

				//send out the updated state data
				SendStateData(null);

				return true;
			}
			catch (Exception ex)
			{
				_robotState.Status = "Error";
				_misty.SkillLogger.LogError($"Process event threw an exception: {ex.Message}", ex);
				return false;
			}
		}

		//State Speech Event Handlers
		public async void SpeechResponseHandler(object sender, TriggerData data)
		{
			_robotState.LastHeard = data.Text;

			if (_startupParameters.Robot.DisplayHeardSpeechMode.ToLower() == "on" && !string.IsNullOrWhiteSpace(data.Text)) //enums
			{
				_ = _misty.SetTextDisplaySettingsAsync("AudioStateText", new TextSettings
				{
					Wrap = true,
					Visible = true,
					Weight = 25,
					Size = 30,
					HorizontalAlignment = ImageHorizontalAlignment.Center,
					VerticalAlignment = ImageVerticalAlignment.Top,
					Red = 255,
					Green = 255,
					Blue = 255,
					PlaceOnTop = true,
					FontFamily = "Courier New",
					Height = 100
				});

				_misty.DisplayText("Heard:" + data.Text, "AudioStateText", null);
			}

			//TODO process in service option?
			_robotState.SpeechResponseEvent = data;
			_robotState.Status = $"Processing: {data.Trigger} - {data.TriggerFilter} - {data.Text}";

			if (!await ProcessEvent(data.Trigger, data.TriggerFilter, data.Text))
			{
				if (!await ProcessEvent(data.Trigger, AnimationConstants.HeardUnknownTrigger, data.Text))
				{
					await ProcessEvent(data.Trigger, AnimationConstants.HeardAnythingTrigger, data.Text);
				}
			}
		}

		public void StartedSpeakingHandler(object sender, string data)
		{
			if (_startupParameters.Robot.DisplaySpokenMode.ToLower() == "on")
			{
				_ = _misty.SetTextDisplaySettingsAsync("AudioStateText", new TextSettings
				{
					Wrap = true,
					Visible = true,
					Weight = 25,
					Size = 30,
					HorizontalAlignment = ImageHorizontalAlignment.Center,
					VerticalAlignment = ImageVerticalAlignment.Top,
					Red = 255,
					Green = 255,
					Blue = 255,
					PlaceOnTop = true,
					FontFamily = "Courier New",
					Height = 100
				});

				_misty.DisplayText("Saying:" + data, "AudioStateText", null);
			}

			_robotState.LastSaid = data;
			_robotState.Speaking = true;
		}

		public void StoppedSpeakingHandler(object sender, IAudioPlayCompleteEvent data)
		{
			_ = _misty.SetTextDisplaySettingsAsync("AudioStateText", new TextSettings
			{
				Deleted = true
			});

			_robotState.Speaking = false;
			_robotState.AudioPlayCompleteEvent = (AudioPlayCompleteEvent)data;
		}

		public void PreSpeechCompletedHandler(object sender, IAudioPlayCompleteEvent data)
		{
			_ = _misty.SetTextDisplaySettingsAsync("AudioStateText", new TextSettings
			{
				Deleted = true
			});

			_robotState.Speaking = false;
			_robotState.AudioPlayCompleteEvent = (AudioPlayCompleteEvent)data;
		}

		public void StartedListeningHandler(object sender, DateTime data)
		{
			if (_startupParameters.Robot.DisplayListeningMode.ToLower() == "on")
			{
				_ = _misty.SetTextDisplaySettingsAsync("AudioStateText", new TextSettings
				{
					Wrap = true,
					Visible = true,
					Weight = 25,
					Size = 30,
					HorizontalAlignment = ImageHorizontalAlignment.Center,
					VerticalAlignment = ImageVerticalAlignment.Top,
					Red = 255,
					Green = 255,
					Blue = 255,
					PlaceOnTop = true,
					FontFamily = "Courier New",
					Height = 100
				});

				_misty.DisplayText("LISTENING...", "AudioStateText", null);
			}

			_robotState.Listening = true;
		}

		public void StoppedListeningHandler(object sender, IVoiceRecordEvent data)
		{
			if (_startupParameters.Robot.DisplayListeningMode.ToLower() == "on")
			{
				_ = _misty.SetTextDisplaySettingsAsync("AudioStateText", new TextSettings
				{
					Deleted = true
				});
			}

			_robotState.VoiceRecordEvent = (VoiceRecordEvent)data;
			_robotState.Listening = false;
		}
		public void KeyPhraseRecognitionOnHandler(object sender, bool data)
		{
			_robotState.KeyPhraseRecognitionOn = data;
		}

		public void KeyPhraseRecognizedHandler(object sender, IKeyPhraseRecognizedEvent data)
		{
			_robotState.KeyPhraseRecognized = (KeyPhraseRecognizedEvent)data;
			_robotState.KeyPhraseRecognizedEvent = (KeyPhraseRecognizedEvent)data;
		}

		public void StartedProcessingVoiceHandler(object sender, IVoiceRecordEvent data)
		{
			_robotState.ProcessingSpeech = true;
			_robotState.VoiceRecordEvent = (VoiceRecordEvent)data;
		}

		public void CompletedProcessingVoiceHandler(object sender, IVoiceRecordEvent data)
		{
			_robotState.ProcessingSpeech = false;
			_robotState.VoiceRecordEvent = (VoiceRecordEvent)data;
		}

		public void VolumeChangedHandler(object sender, int data)
		{
			_robotState.Volume = data;
		}

		//State Locomotion Event Handlers
		public void StartedLocomotionActionHandler(object sender, LocomotionAction locomotionAction)
		{
			_robotState.RunningLocomotionAction = locomotionAction;
		}

		public void CompletedLocomotionActionHandler(object sender, LocomotionAction locomotionAction)
		{
			_robotState.LastLocomotionAction = locomotionAction;
		}

		public void LocomotionFailedHandler(object sender, LocomotionAction locomotionAction)
		{
			_robotState.LastLocomotionAction = locomotionAction;
		}

		public void LocomotionStoppedHandler(object sender, LocomotionAction locomotionAction)
		{
			_robotState.LastLocomotionAction = locomotionAction;
		}

		public void IMUEventHandler(object sender, IIMUEvent data)
		{
			_robotState.RobotPitch = data.Pitch;
			_robotState.RobotRoll = data.Roll;
			_robotState.RobotYaw = data.Yaw;

			_robotState.RobotPitchVelocity = data.PitchVelocity;
			_robotState.RobotRollVelocity = data.RollVelocity;
			_robotState.RobotYawVelocity = data.YawVelocity;

			_robotState.RobotXAcceleration = data.XAcceleration;
			_robotState.RobotYAcceleration = data.YAcceleration;
			_robotState.RobotZAcceleration = data.ZAcceleration;

			_robotState.IMUEvent = (IMUEvent)data;
		}

		public void LocomotionStateHandler(object sender, LocomotionState data)
		{
			_robotState.LocomotionState = data;
		}

		//State Animation Event Handlers		
		public void CompletedAnimationScriptHandler(object sender, string data)
		{
			_ = _misty.SetTextDisplaySettingsAsync("AudioStateText", new TextSettings
			{
				Deleted = true
			});

			_robotState.CompletedAnimationScript = data;
		}

		public void StartedAnimationScriptHandler(object sender, string data)
		{
			_robotState.Status = $"Starting {data}";
			_robotState.StartedAnimationScript = data;
		}

		public void AnimationScriptActionsCompleteHandler(object sender, string data)
		{
			_robotState.AnimationScriptActionsComplete = data;
		}

		public void RepeatingAnimationScriptHandler(object sender, bool data)
		{
			_robotState.Status = $"Repeating {data}";
			_robotState.RepeatingAnimationScript = data;
		}

		public void LeftArmHandler(object sender, IActuatorEvent data)
		{
			_robotState.LeftArm = data.ActuatorValue;
			_robotState.LeftArmActuatorEvent = (ActuatorEvent)data;
		}

		public void RightArmHandler(object sender, IActuatorEvent data)
		{
			_robotState.RightArm = data.ActuatorValue;
			_robotState.RightArmActuatorEvent = (ActuatorEvent)data;
		}

		public void HeadPitchHandler(object sender, IActuatorEvent data)
		{
			_robotState.HeadPitch = data.ActuatorValue;
			_robotState.HeadPitchActuatorEvent = (ActuatorEvent)data;
		}

		public void HeadRollHandler(object sender, IActuatorEvent data)
		{
			_robotState.HeadRoll = data.ActuatorValue;
			_robotState.HeadRollActuatorEvent = (ActuatorEvent)data;
		}

		public void HeadYawHandler(object sender, IActuatorEvent data)
		{
			_robotState.HeadYaw = data.ActuatorValue;
			_robotState.HeadYawActuatorEvent = (ActuatorEvent)data;
		}

		public async void FaceRecognitionEventHandler(object sender, IFaceRecognitionEvent data)
		{
			_robotState.LastFaceSeen = data.Label;
			_robotState.FaceRecognitionEvent = (FaceRecognitionEvent)data;

			_robotState.Status = $"Processing: {Triggers.FaceRecognized} - {data.Label}";

			if (!await ProcessEvent(Triggers.FaceRecognized, data.Label, null))
			{
				await ProcessEvent(Triggers.FaceRecognized, "", null);
			}
		}

		public void FlashlightOnHandler(object sender, bool data)
		{
			_robotState.FlashLightOn = data;
		}

		public void ImageUpdatedHandler(object sender, string data)
		{
			_robotState.Image = data;
		}

		public void AudioPlayedHandler(object sender, string data)
		{
			_robotState.LastAudioPlayed = data;
		}

		public async void ManualTriggerHandler(object sender, TriggerData triggerData)
		{
			_robotState.Status = $"Processing Manual: {triggerData.Trigger} - {triggerData.TriggerFilter} - {triggerData.Text}";
			_ = await ProcessEvent(triggerData.Trigger, triggerData.TriggerFilter, triggerData.Text);
		}

		public async void CapTouchHandler(object sender, ICapTouchEvent data)
		{
			_robotState.CapTouchEvent = (CapTouchEvent)data;

			switch (data.SensorPosition)
			{
				case CapTouchPosition.Chin:
					_robotState.Chin = data.IsContacted;
					break;
				case CapTouchPosition.Scruff:
					_robotState.Scruff = data.IsContacted;
					break;
				case CapTouchPosition.Front:
					_robotState.FrontCap = data.IsContacted;
					break;
				case CapTouchPosition.Back:
					_robotState.BackCap = data.IsContacted;
					break;
				case CapTouchPosition.Right:
					_robotState.RightCap = data.IsContacted;
					break;
				case CapTouchPosition.Left:
					_robotState.LeftCap = data.IsContacted;
					break;
			}

			_robotState.Status = $"Processing: {(data.IsContacted ? Triggers.CapTouched : Triggers.CapReleased)} - {data.SensorPosition}";
			if (!await ProcessEvent(data.IsContacted ? Triggers.CapTouched : Triggers.CapReleased, data.SensorPosition.ToString(), null))
			{
				await ProcessEvent(data.IsContacted ? Triggers.CapTouched : Triggers.CapReleased, "", null);
			}
		}

		public async void BumperHandler(object sender, IBumpSensorEvent data)
		{
			_robotState.BumperEvent = (BumpSensorEvent)data;

			switch (data.SensorPosition)
			{
				case BumpSensorPosition.FrontLeft:
					_robotState.FrontLeftBumper = data.IsContacted;
					break;
				case BumpSensorPosition.FrontRight:
					_robotState.FrontRightBumper = data.IsContacted;
					break;
				case BumpSensorPosition.BackLeft:
					_robotState.BackLeftBumper = data.IsContacted;
					break;
				case BumpSensorPosition.BackRight:
					_robotState.BackRightBumper = data.IsContacted;
					break;
			}

			_robotState.Status = $"Processing: {(data.IsContacted ? Triggers.BumperPressed : Triggers.BumperReleased)} - {data.SensorPosition}";
			if (!await ProcessEvent(data.IsContacted ? Triggers.BumperPressed : Triggers.BumperReleased, data.SensorPosition.ToString(), null))
			{
				await ProcessEvent(data.IsContacted ? Triggers.BumperPressed : Triggers.BumperReleased, "", null);
			}
		}

		public void LEDActionHandler(object sender, LEDTransitionAction data)
		{
			_robotState.LED = data;
		}

		public async void ArTagHandler(object sender, IArTagDetectionEvent data)
		{
			_robotState.LastArTagSeen = data.TagId;
			_robotState.ArTagEvent = (ArTagDetectionEvent)data;

			_robotState.Status = $"Processing: {Triggers.ArTagSeen} - {data.TagId}";
			if (!await ProcessEvent(Triggers.ArTagSeen, data.TagId.ToString(), null))
			{
				await ProcessEvent(Triggers.ArTagSeen, "", null);
			}
		}

		public async void QrTagHandler(object sender, IQrTagDetectionEvent data)
		{
			_robotState.LastQrTagSeen = data.DecodedInfo;
			_robotState.QrTagEvent = (QrTagDetectionEvent)data;

			_robotState.Status = $"Processing: {Triggers.QrTagSeen} - {data.DecodedInfo}";
			if (!await ProcessEvent(Triggers.QrTagSeen, data.DecodedInfo, null))
			{
				await ProcessEvent(Triggers.QrTagSeen, "", null);
			}
		}

		public async void SerialMessageHandler(object sender, ISerialMessageEvent data)
		{
			_robotState.LastSerialMessageReceived = data.Message;
			_robotState.SerialMessageEvent = (SerialMessageEvent)data;

			_robotState.Status = $"Processing: {Triggers.SerialMessage} - {data.Message}";
			if (!await ProcessEvent(Triggers.SerialMessage, data.Message, null))
			{
				await ProcessEvent(Triggers.SerialMessage, "", null);
			}
		}

		public async void ObjectHandler(object sender, IObjectDetectionEvent data)
		{
			_robotState.LastObjectSeen = data.Description;
			_robotState.ObjectEvent = (ObjectDetectionEvent)data;

			_robotState.Status = $"Processing: {Triggers.ObjectSeen} - {data.Description}";
			if (!await ProcessEvent(Triggers.ObjectSeen, data.Description, null))
			{
				await ProcessEvent(Triggers.ObjectSeen, "", null);
			}
		}


		public RobotStateEvent GetRobotStateEvent()
		{
			return new RobotStateEvent
			{
				RobotName = _robotState.RobotName,
				DemoName = _robotState.DemoName,
				DemoStarted = _robotState.DemoStarted,
				RoleName = _robotState.RoleName,
				GroupName = _robotState.GroupName,

				ChargePercent = _robotState.ChargePercent,
				IsCharging = _robotState.IsCharging,

				CurrentInteraction = _robotState.CurrentInteraction,
				CurrentAnimation = _robotState.CurrentAnimation,

				DemoId = _startupParameters.Demo?.Id,
				DemoGroupId = _startupParameters?.DemoGroup.Id,
				RoleId = _startupParameters.Role?.Id,
				RobotId = _startupParameters.Robot?.Id,

				Demo = _startupParameters.Demo,
				DemoGroup = _startupParameters.DemoGroup,
				Role = _startupParameters.Role,
				Robot = _startupParameters.Robot,
				AccessId = _startupParameters.AccessId,

				FlashLightOn = _robotState.FlashLightOn,
				AnimationLED = _robotState.LED,
				Image = _robotState.Image,
				Speaking = _robotState.Speaking,
				LastSaid = _robotState.LastSaid,
				Listening = _robotState.Listening,
				KeyPhraseRecognitionOn = _robotState.KeyPhraseRecognitionOn,
				ProcessingSpeech = _robotState.ProcessingSpeech,
				Volume = _robotState.Volume,

				AnimationScriptActionsComplete = _robotState.AnimationScriptActionsComplete,
				StartedAnimationScript = _robotState.StartedAnimationScript,
				CompletedAnimationScript = _robotState.CompletedAnimationScript,

				LastUserEventName = _robotState.LastUserEventName,
				LastFaceSeen = _robotState.LastFaceSeen,
				LastAudioPlayed = _robotState.LastAudioPlayed,

				LastHeard = _robotState.LastHeard,

				HeadPitch = _robotState.HeadPitch,
				HeadRoll = _robotState.HeadRoll,
				HeadYaw = _robotState.HeadYaw,

				RightArm = _robotState.RightArm,
				LeftArm = _robotState.LeftArm,

				RobotPitch = _robotState.RobotPitch,
				RobotRoll = _robotState.RobotRoll,
				RobotYaw = _robotState.RobotYaw,
				RobotPitchVelocity = _robotState.RobotPitchVelocity,
				RobotRollVelocity = _robotState.RobotRollVelocity,
				RobotYawVelocity = _robotState.RobotYawVelocity,
				RobotXAcceleration = _robotState.RobotXAcceleration,
				RobotYAcceleration = _robotState.RobotYAcceleration,
				RobotZAcceleration = _robotState.RobotZAcceleration,

				Chin = _robotState.Chin,
				Scruff = _robotState.Scruff,
				FrontCap = _robotState.FrontCap,
				BackCap = _robotState.BackCap,
				RightCap = _robotState.RightCap,
				LeftCap = _robotState.LeftCap,

				FrontRightBumper = _robotState.FrontRightBumper,
				FrontLeftBumper = _robotState.FrontLeftBumper,
				BackRightBumper = _robotState.BackRightBumper,
				BackLeftBumper = _robotState.BackLeftBumper,

				Status = _robotState.Status,
				LastObjectSeen = _robotState.LastObjectSeen,
				LastArTagSeen = _robotState.LastArTagSeen,
				LastQrTagSeen = _robotState.LastQrTagSeen,
				LastSerialMessageReceived = _robotState.LastSerialMessageReceived,
			};
		}

		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_triggers.Clear();
					_robotStateTimer?.Dispose();
					_timeoutTimer?.Dispose();
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
