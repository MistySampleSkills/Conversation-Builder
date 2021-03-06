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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conversation.Common;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Logger;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using MistyRobotics.SDK;
using MistyRobotics.Common.Data;
using Newtonsoft.Json;
using SkillTools.AssetTools;
using TimeManager;
using SpeechTools;
using CommandManager;

namespace MistyCharacter
{
	public abstract class BaseCharacter : IBaseCharacter
	{
		//TODO Dupe events in MistyState and here, cleanup and use MistyState as subscribable item
		//TODO Lots of refactoring can be done with new pipeline and other updates

		public event EventHandler<IFaceRecognitionEvent> FaceRecognitionEvent;
		public event EventHandler<ICapTouchEvent> CapTouchEvent;
		public event EventHandler<IBumpSensorEvent> BumperEvent;
		public event EventHandler<IBatteryChargeEvent> BatteryChargeEvent;
		public event EventHandler<IQrTagDetectionEvent> QrTagEvent;
		public event EventHandler<IArTagDetectionEvent> ArTagEvent;
		public event EventHandler<ITimeOfFlightEvent> TimeOfFlightEvent;
		public event EventHandler<ISerialMessageEvent> SerialMessageEvent;
		public event EventHandler<IUserEvent> ExternalEvent;
		public event EventHandler<IObjectDetectionEvent> ObjectEvent;
		public event EventHandler<IObjectDetectionEvent> PersonObjectEvent;
		public event EventHandler<IObjectDetectionEvent> NonPersonObjectEvent;
		public event EventHandler<TriggerData> SpeechIntentEvent;
		public event EventHandler<string> StartedSpeaking;
		public event EventHandler<IAudioPlayCompleteEvent> StoppedSpeaking;
		public event EventHandler<DateTime> StartedListening;
		public event EventHandler<IVoiceRecordEvent> StoppedListening;
		public event EventHandler<bool> KeyPhraseRecognitionOn;
		public event EventHandler<IKeyPhraseRecognizedEvent> KeyPhraseRecognized;
		public event EventHandler<IVoiceRecordEvent> CompletedProcessingVoice;
		public event EventHandler<IVoiceRecordEvent> StartedProcessingVoice;
		public event EventHandler<IActuatorEvent> LeftArmActuatorEvent;
		public event EventHandler<IActuatorEvent> RightArmActuatorEvent;
		public event EventHandler<IActuatorEvent> HeadPitchActuatorEvent;
		public event EventHandler<IActuatorEvent> HeadYawActuatorEvent;
		public event EventHandler<IActuatorEvent> HeadRollActuatorEvent;		
		public event EventHandler<IUserEvent> SyncEvent;
		public event EventHandler<IUserEvent> RobotCommand;
		
		public event EventHandler<TriggerData> ValidTriggerReceived;		
		public event EventHandler<DateTime> ConversationStarted;
		public event EventHandler<DateTime> ConversationEnded;
		public event EventHandler<DateTime> TriggerConversationCleanup;
		public event EventHandler<string> InteractionStarted;
		public event EventHandler<string> InteractionEnded;

		public event EventHandler<IDriveEncoderEvent> DriveEncoder;//TODO

		private const int MaxProcessingVoiceWaits = 30;
		private const int DelayBetweenProcessingVoiceChecksMs = 100;

		private const string ConversationDeparturePoint = "* Conversation Departure Point";
		private int _currentProcessingVoiceWaits = 0;
		private KeyValuePair<DateTime, TriggerData> _latestTriggerMatchData = new KeyValuePair<DateTime, TriggerData>();
		private IList<string> _skillsToStop = new List<string>();
		public IDictionary<string, object> OriginalParameters { get; set; } = new Dictionary<string, object>();
		protected IRobotMessenger Robot;
		protected IMistyState MistyState;
		protected ISDKLogger Logger;
		protected CharacterParameters CharacterParameters { get; private set; } = new CharacterParameters();
		protected ConcurrentDictionary<string, AnimationRequest> EmotionAnimations = new ConcurrentDictionary<string, AnimationRequest>();		
		public Interaction CurrentInteraction { get; private set; } = new Interaction();
		private bool _waitingOnPrespeech;
		private Timer _noInteractionTimer;
		private Timer _triggerActionTimeoutTimer;
		private Timer _timerTriggerTimer;
		private Timer _pollRunningSkillsTimer;
		private Timer _stateEventTimer;
		private AssetWrapper AssetWrapper;
		private ISpeechManager SpeechManager;
		private IArmManager ArmManager;
		private IHeadManager HeadManager;
		private ICommandManager CommandManager;
		private IAnimationManager AnimationManager;
		private ITimeManager TimeManager;
		private IEmotionManager EmotionManager;
		private ISpeechIntentManager SpeechIntentManager;
		private IList<string> LiveTriggers = new List<string>();
		private ConversationGroup _conversationGroup = new ConversationGroup();
		private IList<GenericDataStore> _genericDataStores  = new List<GenericDataStore>();
		private AnimationRequest _currentAnimation;
		private bool _processingVoice = false;
		private ManagerConfiguration _managerConfiguration;
		private object _runningSkillLock = new object();
		private IList<string> _runningSkills = new List<string>();
		private ConversationData _currentConversationData = new ConversationData();
		private object _lockWaitingOnResponse = new object();
		private SemaphoreSlim _processingTriggersSemaphore = new SemaphoreSlim(1, 1);
		private Random _random = new Random();
		private object _listeningLock = new object();
		public bool WaitingForOverrideTrigger { get; private set; }
		private bool _ignoreTriggeringEvents;
		
		//TODO Queues not really used/needed anymore with new pipeline process
		private ConcurrentQueue<Interaction> _interactionQueue = new ConcurrentQueue<Interaction>();
		private ConcurrentQueue<Interaction> _interactionPriorityQueue = new ConcurrentQueue<Interaction>();

		private object _eventsClearedLock = new object();
		public Guid UniqueAnimationId { get; private set; } = Guid.NewGuid();

		private TOFCounter _tofCountFrontRight = new TOFCounter();
		private TOFCounter _tofCountFrontLeft = new TOFCounter();
		private TOFCounter _tofCountFrontCenter = new TOFCounter();
		private TOFCounter _tofCountBackRight = new TOFCounter();
		private TOFCounter _tofCountBackLeft = new TOFCounter();
		private TOFCounter _tofCountFrontRange = new TOFCounter();
		private TOFCounter _tofCountBackRange = new TOFCounter();
		private TOFCounter _tofCountFrontEdge = new TOFCounter();
		private TOFCounter _tofCountBackEdge = new TOFCounter();

		private bool _triggerHandled = false;
		private IList<string> _allowedUtterances = new List<string>();
		private HeadLocation _currentHeadRequest = new HeadLocation(null, null, null);
		private IList<string> _allowedTriggers;
		private IList<ICommandAuthorization> _listOfAuthorizations;
		private bool _sendStateThrottle = false;
		private bool _sendInteractionThrottle = false;
		private JavaScriptRecorder _javaScriptRecorder;

		protected BaseCharacter(IRobotMessenger misty, 
			IDictionary<string, object> originalParameters,
			ManagerConfiguration managerConfiguration = null)
		{
			Robot = misty;
			OriginalParameters = originalParameters;
			_managerConfiguration = managerConfiguration;
		}

		public void ClearRegistrations()
		{
			try
			{ 
				MistyState.HeadPitchActuatorEvent -= HeadManager.HandleActuatorEvent;
				MistyState.HeadYawActuatorEvent -= HeadManager.HandleActuatorEvent;

				SpeechManager.SpeechIntent -= MistyState.HandleSpeechIntentReceived;

				SpeechManager.StartedSpeaking -= MistyState.HandleStartedSpeakingReceived;
				SpeechManager.StoppedSpeaking -= MistyState.HandleStoppedSpeakingReceived;
				SpeechManager.StartedListening -= MistyState.HandleStartedListeningReceived;
				SpeechManager.StoppedListening -= MistyState.HandleStoppedListeningReceived;
				SpeechManager.KeyPhraseRecognized -= MistyState.HandleKeyPhraseRecognizedReceived;
				SpeechManager.CompletedProcessingVoice -= MistyState.HandleCompletedProcessingVoiceReceived;
				SpeechManager.StartedProcessingVoice -= MistyState.HandleStartedProcessingVoiceReceived;

				SpeechManager.SpeechIntent -= SpeechManager_SpeechIntent;
				SpeechManager.PreSpeechCompleted -= SpeechManager_PreSpeechCompleted;
				SpeechManager.StartedSpeaking -= SpeechManager_StartedSpeaking;
				SpeechManager.StoppedSpeaking -= SpeechManager_StoppedSpeaking;
				SpeechManager.StartedListening -= SpeechManager_StartedListening;
				SpeechManager.StoppedListening -= SpeechManager_StoppedListening;
				SpeechManager.KeyPhraseRecognized -= SpeechManager_KeyPhraseRecognized;
				SpeechManager.CompletedProcessingVoice -= SpeechManager_CompletedProcessingVoice;
				SpeechManager.StartedProcessingVoice -= SpeechManager_StartedProcessingVoice;
				SpeechManager.KeyPhraseRecognitionOn -= SpeechManager_KeyPhraseRecognitionOn;

				InteractionStarted -= MistyState.HandleInteractionStarted;
				InteractionEnded -= MistyState.HandleInteractionEnded;
				InteractionEnded -= SpeechManager.HandleInteractionEnded;
				ConversationStarted -= MistyState.HandleConversationStarted;
				ConversationEnded -= MistyState.HandleConversationEnded;
				ValidTriggerReceived -= MistyState.HandleValidTriggerReceived;

				//TODO Cleanup of event vs commands since passing in anyway
				AnimationManager.SyncEvent -= AnimationManager_SyncEvent;
				AnimationManager.AddTrigger -= AddTrigger;
				AnimationManager.AddTrigger -= SpeechManager.AddValidIntent;
				AnimationManager.RemoveTrigger -= RemoveTrigger;
				AnimationManager.ManualTrigger -= ManualTrigger;
				AnimationManager.TriggerAnimation -= HandleAnimationRequest;

				MistyState.ArTagEvent -= HandleArTagEvent;
				MistyState.BumperEvent -= HandleBumperEvent;
				MistyState.CapTouchEvent -= HandleCapTouchEvent;
				MistyState.DriveEncoder -= HandleDriveEncoder;
				MistyState.FaceRecognitionEvent -= HandleFaceRecognitionEvent;
				MistyState.NonPersonObjectEvent -= HandleNonPersonObjectEvent;
				MistyState.PersonObjectEvent -= HandlePersonObjectEvent;
				MistyState.PersonObjectEvent -= HeadManager.HandleObjectDetectionEvent;
				MistyState.FaceRecognitionEvent -= HeadManager.HandleFaceRecognitionEvent;
				MistyState.HeadPitchActuatorEvent -= HandleHeadPitchEvent;
				MistyState.HeadRollActuatorEvent -= HandleHeadRollEvent;
				MistyState.HeadYawActuatorEvent -= HandleHeadYawEvent;
				MistyState.LeftArmActuatorEvent -= HandleLeftArmEvent;
				MistyState.RightArmActuatorEvent -= HandleRightArmEvent;
				MistyState.TimeOfFlightEvent -= HandleTimeOfFlightEvent;
				SyncEvent -= MistyState.HandleSyncEvent;
				RobotCommand -= MistyState.HandleRobotCommand;
				BatteryChargeEvent -= MistyState.HandleBatteryChargeEvent;
				ExternalEvent -= MistyState.HandleExternalEvent;

			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Failed to clear registrations.", ex);
			}
		}

		public async Task<bool> Initialize(CharacterParameters characterParameters, IList<ICommandAuthorization> listOfAuthorizations)
		{
			try
			{
				IgnoreEvents();

				Robot.UnregisterEvent("Battery", null);
				Robot.UnregisterEvent("ExternalEvent", null);
				Robot.UnregisterEvent("SyncEvent", null);
				Robot.UnregisterEvent("CrossRobotCommand", null);
				await Task.Delay(1500);

				CharacterParameters = characterParameters;
				Logger = Robot.SkillLogger;

				_conversationGroup = CharacterParameters.ConversationGroup;
				_genericDataStores = CharacterParameters.ConversationGroup.GenericDataStores;
				_listOfAuthorizations = listOfAuthorizations;

				AssetWrapper = new AssetWrapper(Robot);
				_ = RefreshAssetLists();

				if (CharacterParameters.CreateJavaScriptTemplate)
				{
					_javaScriptRecorder = new JavaScriptRecorder(Robot);
					string name = $"javascript_{CharacterParameters.ConversationGroup.Name}_{DateTime.Now.ToString()}";
					if (await _javaScriptRecorder.CreateDataStore(name))
					{
						Robot.SkillLogger.LogError($"Created javascript recording file with name {name}.");
					}
					else
					{
						Robot.SkillLogger.LogError($"Failed to create javascript recording file with name {name}.");
						return false;
					}
				}

				MistyState = new MistyState(Robot, OriginalParameters, CharacterParameters, _javaScriptRecorder);
				await MistyState.Initialize();

				TimeManager = _managerConfiguration?.TimeManager ?? new EnglishTimeManager(Robot, OriginalParameters, CharacterParameters);
				await TimeManager.Initialize();

				ArmManager = _managerConfiguration?.ArmManager ?? new ArmManager(Robot, OriginalParameters, CharacterParameters, _javaScriptRecorder);
				await ArmManager.Initialize();

				HeadManager = _managerConfiguration?.HeadManager ?? new HeadManager(Robot, OriginalParameters, CharacterParameters, _javaScriptRecorder);
				await HeadManager.Initialize();
				
				CommandManager = _managerConfiguration?.CommandManager ?? new DefaultCommandManager(Robot, OriginalParameters);
				await CommandManager.Initialize(CharacterParameters, MistyState, _listOfAuthorizations);
				
				SpeechIntentManager = _managerConfiguration?.SpeechIntentManager ?? new SpeechIntentManager(Robot, CharacterParameters.ConversationGroup.IntentUtterances, CharacterParameters.ConversationGroup.GenericDataStores);
				
				SpeechManager = _managerConfiguration?.SpeechManager ?? new SpeechManager(Robot, OriginalParameters, CharacterParameters, MistyState.GetCharacterState(),_genericDataStores, SpeechIntentManager, CommandManager);
				await SpeechManager.Initialize();

				Robot.GetVolume(GetVolumeCallback);
				
				AnimationManager = _managerConfiguration?.AnimationManager ?? new AnimationManager(Robot, OriginalParameters, CharacterParameters, SpeechManager, MistyState, TimeManager, HeadManager, CommandManager, _javaScriptRecorder);
				await AnimationManager.Initialize();
				
				var eventDetails = Robot.RegisterBatteryChargeEvent(BatteryChargeCallback, 1000 * 60, true, null, "Battery", null);
				eventDetails = Robot.RegisterUserEvent("ExternalEvent", ExternalEventCallback, 0, true, null);
				eventDetails = Robot.RegisterUserEvent("SyncEvent", SyncEventCallback, 0, true, null);
				eventDetails = Robot.RegisterUserEvent("CrossRobotCommand", RobotCommandCallback, 0, true, null);

				//subscribe to head events for deprecated head mgr				
				MistyState.HeadPitchActuatorEvent += HeadManager.HandleActuatorEvent;
				MistyState.HeadYawActuatorEvent += HeadManager.HandleActuatorEvent;
				
				SpeechManager.SpeechIntent += MistyState.HandleSpeechIntentReceived;
				
				SpeechManager.StartedSpeaking += MistyState.HandleStartedSpeakingReceived;
				SpeechManager.StoppedSpeaking += MistyState.HandleStoppedSpeakingReceived;
				SpeechManager.StartedListening += MistyState.HandleStartedListeningReceived;
				SpeechManager.StoppedListening += MistyState.HandleStoppedListeningReceived;
				SpeechManager.KeyPhraseRecognized += MistyState.HandleKeyPhraseRecognizedReceived;
				SpeechManager.CompletedProcessingVoice += MistyState.HandleCompletedProcessingVoiceReceived;
				SpeechManager.StartedProcessingVoice += MistyState.HandleStartedProcessingVoiceReceived;
				
				SpeechManager.SpeechIntent += SpeechManager_SpeechIntent;
				SpeechManager.PreSpeechCompleted += SpeechManager_PreSpeechCompleted;
				SpeechManager.StartedSpeaking += SpeechManager_StartedSpeaking;
				SpeechManager.StoppedSpeaking += SpeechManager_StoppedSpeaking;
				SpeechManager.StartedListening += SpeechManager_StartedListening;
				SpeechManager.StoppedListening += SpeechManager_StoppedListening;
				SpeechManager.KeyPhraseRecognized += SpeechManager_KeyPhraseRecognized;
				SpeechManager.CompletedProcessingVoice += SpeechManager_CompletedProcessingVoice;
				SpeechManager.StartedProcessingVoice += SpeechManager_StartedProcessingVoice;
				SpeechManager.KeyPhraseRecognitionOn += SpeechManager_KeyPhraseRecognitionOn;

				InteractionStarted += MistyState.HandleInteractionStarted;
				InteractionEnded += MistyState.HandleInteractionEnded;
				InteractionEnded += SpeechManager.HandleInteractionEnded;
				ConversationStarted += MistyState.HandleConversationStarted;
				ConversationEnded += MistyState.HandleConversationEnded;
				ValidTriggerReceived += MistyState.HandleValidTriggerReceived;

				//TODO Cleanup of event vs commands since passing in anyway
				AnimationManager.SyncEvent += AnimationManager_SyncEvent;
				AnimationManager.AddTrigger += AddTrigger;
				AnimationManager.AddTrigger += SpeechManager.AddValidIntent;
				AnimationManager.RemoveTrigger += RemoveTrigger;
				AnimationManager.ManualTrigger += ManualTrigger;
				AnimationManager.TriggerAnimation += HandleAnimationRequest;

				MistyState.ArTagEvent += HandleArTagEvent;
				MistyState.BumperEvent += HandleBumperEvent;
				MistyState.CapTouchEvent += HandleCapTouchEvent;
				MistyState.DriveEncoder += HandleDriveEncoder;
				MistyState.FaceRecognitionEvent += HandleFaceRecognitionEvent;
				MistyState.NonPersonObjectEvent += HandleNonPersonObjectEvent;
				MistyState.PersonObjectEvent += HandlePersonObjectEvent;
				MistyState.PersonObjectEvent += HeadManager.HandleObjectDetectionEvent;
				MistyState.FaceRecognitionEvent += HeadManager.HandleFaceRecognitionEvent;
				MistyState.HeadPitchActuatorEvent += HandleHeadPitchEvent;
				MistyState.HeadRollActuatorEvent += HandleHeadRollEvent;
				MistyState.HeadYawActuatorEvent += HandleHeadYawEvent;
				MistyState.LeftArmActuatorEvent += HandleLeftArmEvent;
				MistyState.RightArmActuatorEvent += HandleRightArmEvent;
				MistyState.TimeOfFlightEvent += HandleTimeOfFlightEvent;
				SyncEvent += MistyState.HandleSyncEvent;
				RobotCommand += MistyState.HandleRobotCommand;
				BatteryChargeEvent += MistyState.HandleBatteryChargeEvent;
				ExternalEvent += MistyState.HandleExternalEvent;
				
				StreamAndLogInteraction($"Starting Base Character animation processing...");

				_latestTriggerMatchData = new KeyValuePair<DateTime, TriggerData>(DateTime.Now, new TriggerData("", "", Triggers.None));

				//hacky check for running skills, so there may be a 15 second delay if skill shuts down automatically to where we notice and try to restart				
				_pollRunningSkillsTimer = new Timer(UpdateRunningSkillsCallback, null, 15000, Timeout.Infinite);

				//TODO make configurable
				_stateEventTimer = new Timer(SendStateEvent, null, 500, 500);
		
				_ = Robot.SetImageDisplaySettingsAsync(null, new ImageSettings
				{
					PlaceOnTop = false
				});
				
				//TODO Move to speech manager
				if (CharacterParameters.DisplaySpoken)
				{
					_ = Robot.SetTextDisplaySettingsAsync("SpokeText", new TextSettings
					{
						Wrap = true,
						Visible = false,
						Weight = CharacterParameters.LargePrint ? 20 : 15,
						Size = CharacterParameters.LargePrint ? 40 : 20,
						HorizontalAlignment = ImageHorizontalAlignment.Center,
						VerticalAlignment = ImageVerticalAlignment.Bottom,
						Red = 240,
						Green = 240,
						Blue = 240,
						PlaceOnTop = true,
						FontFamily = "Courier New",
						Height = CharacterParameters.LargePrint ? 230 : 150
					});
				}

				if (CharacterParameters.HeardSpeechToScreen)
				{
					_ = Robot.SetTextDisplaySettingsAsync("HeardSpeech", new TextSettings
					{
						Wrap = true,
						Visible = false,
						Weight = 15,
						Size = 20,
						HorizontalAlignment = ImageHorizontalAlignment.Center,
						VerticalAlignment = ImageVerticalAlignment.Top,
						Red = 255,
						Green = 221,
						Blue = 71,
						PlaceOnTop = true,
						FontFamily = "Courier New",
						Height = 60
					});
				}
				
				_ = ProcessNextAnimationRequest();
				return true;
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Failed to initialize.", ex);
				return false;
			}
		}
		
		private void GetVolumeCallback(IGetVolumeResponse volumeResponse)
		{
			SpeechManager.Volume = volumeResponse != null && volumeResponse.Status == ResponseStatus.Success ? volumeResponse.Data : 20;
		}

		public void ChangeVolume(int volume)
		{
			SpeechManager.Volume = volume;
			_ = ManageJavaScriptRecording($"misty.SetDefaultVolume({SpeechManager.Volume});");
		}

		public async Task RefreshAssetLists()
		{
			await AssetWrapper.RefreshAssetLists();
		}

		#region Cross robot and other event callbacks

		private void SyncEventCallback(IUserEvent userEvent)
		{
			AnimationManager.HandleSyncEvent(userEvent);
			SyncEvent?.Invoke(this, userEvent);
		}

		private void RobotCommandCallback(IUserEvent userEvent)
		{
			RobotCommand?.Invoke(this, userEvent);
			_ = AnimationManager.HandleExternalCommand(userEvent);
		}

		private void BatteryChargeCallback(IBatteryChargeEvent batteryEvent)
		{
			MistyState.GetCharacterState().BatteryChargeEvent = (BatteryChargeEvent)batteryEvent;
			BatteryChargeEvent?.Invoke(this, batteryEvent);
		}

		private void ExternalEventCallback(IUserEvent userEvent)
		{
			//TODO Deny cross robot communication per robot
			
			MistyState.GetCharacterState().ExternalEvent = (UserEvent)userEvent;

			HandleExternalEvent(userEvent);
		}

		#endregion Cross robot and other event callbacks


		#region Trigger Checking and management

		public void RestartTriggerHandling()
		{
			try
			{
				WaitingForOverrideTrigger = false;
				_ignoreTriggeringEvents = false;
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Failed to restart trigger handling.", ex);
			}
		}
		public void PauseTriggerHandling(bool ignoreTriggeringEvents = true)
		{
			try
			{
				WaitingForOverrideTrigger = true;
				_ignoreTriggeringEvents = ignoreTriggeringEvents;
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Failed to pause trigger handling.", ex);
			}
		}

		public async void SimulateTrigger(TriggerData triggerData, bool setAsOverrideEvent = true)
		{
			triggerData.OverrideIntent = setAsOverrideEvent;
			if (!await SendManagedResponseEvent(triggerData))
			{
				await SendManagedResponseEvent(triggerData, true);
			}
		}

		private void ListenToEvent(TriggerDetail detail, int delayMs)
		{
			_ = Task.Run(async () =>
			{
				try
				{
					if (detail.Trigger == Triggers.Timeout)
					{
						_triggerActionTimeoutTimer = new Timer(IntentTimeoutTimerCallback, new TimerTriggerData(UniqueAnimationId, detail, delayMs), delayMs, Timeout.Infinite);
					}
					else if (detail.Trigger == Triggers.Timer)
					{
						_timerTriggerTimer = new Timer(IntentTriggerTimerCallback, new TimerTriggerData(UniqueAnimationId, detail, delayMs), delayMs, Timeout.Infinite);
					}
					else
					{
						await Task.Delay(delayMs);

						lock (_eventsClearedLock)
						{
							if (LiveTriggers.Contains(detail.Trigger, StringComparer.OrdinalIgnoreCase))
							{
								return;
							}

							MistyState.RegisterEvent(detail.Trigger);
							LiveTriggers.Add(detail.Trigger.ToLower().Trim());
						}
						StreamAndLogInteraction($"Listening to event type {detail.Trigger}");
						SendInteractionUIEvent();
					}
				}
				catch (Exception ex)
				{
					Robot.SkillLogger.Log($"Failed to listen to event {detail.Trigger}.", ex);				
				}
			});
		}

		public void ManualTrigger(object sender, TriggerData trigger)
		{		
			_ = SendManagedResponseEvent(trigger);			
		}

		//Script added triggers
		public void AddTrigger(object sender, KeyValuePair<string, TriggerData> trigger)
		{
			TriggerDetail triggerDetail = new TriggerDetail(trigger.Key, trigger.Value.Trigger, trigger.Value.TriggerFilter);

			_allowedTriggers.Add(trigger.Key);

			ListenToEvent(triggerDetail, 0);
		}

		public void RemoveTrigger(object sender, string trigger)
		{
			TriggerDetail triggerDetail = new TriggerDetail(trigger, trigger);

			_allowedTriggers.Remove(trigger);

			IgnoreEvent(triggerDetail, 0);			
		}

		private void IgnoreEvent(TriggerDetail detail, int delayMs)
		{
			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(delayMs);
					if (detail.Trigger == Triggers.Timeout)
					{
						_triggerActionTimeoutTimer?.Dispose();
					}
					else if (detail.Trigger == Triggers.Timer)
					{
						_timerTriggerTimer?.Dispose();
					}
					else
					{
						lock (_eventsClearedLock)
						{
							if (LiveTriggers.Contains(detail.Trigger, StringComparer.OrdinalIgnoreCase))
							{
								LiveTriggers.Remove(detail.Trigger.ToLower().Trim());
								//MistyState.UnregisterEvent(detail.Trigger); //? TODO only if all users of the event are off, currently, once events are on, we keep them on, but just ignore them
								StreamAndLogInteraction($"Ignoring event type {detail.Trigger}");
								SendInteractionUIEvent();
								return;
							}
						}
					}
				}
				catch (Exception ex)
				{
					Robot.SkillLogger.Log($"Failed to ignore to event {detail.Trigger}.", ex);
				}
			});
		}

		private void IgnoreEvents()
		{
			lock (_eventsClearedLock)
			{
				LiveTriggers.Clear();
			}
		}
		private async Task<bool> SendManagedResponseEvent(TriggerData triggerData, bool conversationTriggerCheck = false)
		{
			if (triggerData.OverrideIntent ||
				LiveTriggers.Contains(triggerData.Trigger, StringComparer.OrdinalIgnoreCase) ||
				string.Compare(triggerData.Trigger, Triggers.Timeout, true) == 0 ||
				string.Compare(triggerData.Trigger, Triggers.Timer, true) == 0 ||
				string.Compare(triggerData.Trigger, Triggers.Manual, true) == 0 ||
				string.Compare(triggerData.Trigger, Triggers.ExternalEvent, true) == 0 ||
				string.Compare(triggerData.Trigger, Triggers.AudioCompleted, true) == 0 ||
				string.Compare(triggerData.Trigger, Triggers.KeyPhraseRecognized, true) == 0
			)
			{
				if (await ProcessAndVerifyTrigger(triggerData, conversationTriggerCheck))
				{
					StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name} | Sent Handled Trigger | {triggerData.Trigger} - {triggerData.TriggerFilter} - {triggerData.Text}.");
					return true;
				}
			}
			return false;
		}

		//Fixing order of checking, conversation should not be checked until ALL local are checked, needs cleanup
		private async Task<bool> ProcessAndVerifyTrigger(TriggerData triggerData, bool conversationTriggerCheck)
		{
			_processingTriggersSemaphore.Wait();
			IDictionary<string, IList<TriggerActionOption>> allowedTriggers = new Dictionary<string, IList<TriggerActionOption>>();
			try
			{
				if (_triggerHandled)
				{
					return false;
				}

				allowedTriggers = CurrentInteraction.TriggerMap;

				if (!WaitingForOverrideTrigger || triggerData.OverrideIntent)
				{
					MistyState.GetCharacterState().LatestTriggerChecked = new KeyValuePair<DateTime, TriggerData>(DateTime.Now, triggerData);

					bool match = false;
					string triggerDetailString;
					IList<TriggerActionOption> triggerDetailMap = new List<TriggerActionOption>();
					TriggerDetail triggerDetail = null;

					if (triggerData.Trigger == Triggers.Manual)
					{
						match = true;
						triggerDetailMap = new List<TriggerActionOption>();
						triggerDetailMap.Add(new TriggerActionOption { Id = "-1", GoToConversation = _currentConversationData.Id, GoToInteraction = triggerData.Text, InterruptCurrentAction = true, Retrigger = false, Weight = 1 });
					}
					else if (!conversationTriggerCheck)
					{
						foreach (KeyValuePair<string, IList<TriggerActionOption>> possibleIntent in allowedTriggers)
						{
							triggerDetailString = possibleIntent.Key;
							triggerDetailMap = possibleIntent.Value;

							triggerDetail = _currentConversationData.Triggers.FirstOrDefault(x => x.Id == triggerDetailString);
							if (triggerDetail == null)
							{
								//old functionality
								triggerDetail = _currentConversationData.Triggers.FirstOrDefault(x => string.Compare(x.Name, triggerDetailString, true) == 0);
							}
							if (triggerDetail != null && (string.Compare(triggerData.Trigger?.Trim(), triggerDetail.Trigger?.Trim(), true) == 0))
							{
								//Not all intents need a matching text
								if (
									string.Compare(triggerData.Trigger, Triggers.Timeout, true) == 0 ||
									string.Compare(triggerData.Trigger, Triggers.Timer, true) == 0 ||
									string.Compare(triggerData.Trigger, Triggers.Manual, true) == 0 ||
									string.Compare(triggerData.Trigger, Triggers.AudioCompleted, true) == 0 ||
									string.Compare(triggerData.Trigger, Triggers.KeyPhraseRecognized, true) == 0)
								{
									match = true;
									break;
								}
								else if (triggerData.Trigger == Triggers.TimeOfFlightRange)
								{
									try
									{
										string[] fields = triggerDetail.TriggerFilter.Split(" ");
										if (fields.Length == 5)
										{
											string sensor = fields[0].Trim().ToLower();
											string equality = fields[1].Trim();
											string valueString = fields[2].Trim();
											string timesString = fields[3].Trim();
											string durationMsString = fields[4].Trim();

											double value = Convert.ToDouble(valueString);
											int times = Convert.ToInt32(timesString);
											int durationMs = Convert.ToInt32(durationMsString);

											//FrontRange, BackRange, FrontRight, FrontCenter, FrontLeft, BackLeft, BackRight
											//SensorName Equality value duration--> FrontRange == X 5 1000

											switch (sensor)
											{
												case "frontrange":
													match = IsValidCompare(triggerData, sensor, equality, value, _tofCountFrontRange, times, durationMs);
													break;
												case "backrange":
													match = IsValidCompare(triggerData, sensor, equality, value, _tofCountBackRange, times, durationMs);
													break;
												case "frontright":
													match = IsValidCompare(triggerData, sensor, equality, value, _tofCountFrontRight, times, durationMs);
													break;
												case "frontcenter":
													match = IsValidCompare(triggerData, sensor, equality, value, _tofCountFrontCenter, times, durationMs);
													break;
												case "frontleft":
													match = IsValidCompare(triggerData, sensor, equality, value, _tofCountFrontLeft, times, durationMs);
													break;
												case "backleft":
													match = IsValidCompare(triggerData, sensor, equality, value, _tofCountBackLeft, times, durationMs);
													break;
												case "backright":
													match = IsValidCompare(triggerData, sensor, equality, value, _tofCountBackRight, times, durationMs);
													break;
											}

											if (match)
											{
												break;
											}
										}
									}
									catch
									{
										//Ignore bad parse as it is user created...
										match = false;
									}
								}
								else if (!string.IsNullOrWhiteSpace(triggerDetail.TriggerFilter))
								{
									match = string.Compare(triggerData.TriggerFilter?.Trim(), triggerDetail.TriggerFilter?.Trim(), true) == 0;
									if (match)
									{
										break;
									}
								}
								else
								{
									match = true;
									break;
								}
							}
						}
					}
					else
					{
						//TOO SOON, should do this after all local checked first
						//TODO DUPE CODE!!!

						//If no match, and allowed, check for conversation triggers to handle this
						if (!match && CurrentInteraction.AllowConversationTriggers && _currentConversationData.ConversationTriggerMap != null && _currentConversationData.ConversationTriggerMap.Count() > 0)
						{
							foreach (KeyValuePair<string, IList<TriggerActionOption>> possibleConversationTrigger in _currentConversationData.ConversationTriggerMap)
							{
								triggerDetailString = possibleConversationTrigger.Key;
								triggerDetailMap = possibleConversationTrigger.Value;

								triggerDetail = _currentConversationData.Triggers.FirstOrDefault(x => x.Id == triggerDetailString);
								if (triggerDetail != null && (string.Compare(triggerData.Trigger?.Trim(), triggerDetail.Trigger?.Trim(), true) == 0))
								{
									if (triggerData.Trigger == Triggers.Timeout ||
										triggerData.Trigger == Triggers.AudioCompleted ||
										triggerData.Trigger == Triggers.KeyPhraseRecognized ||
										triggerData.Trigger == Triggers.Timer)
									{
										match = true;
										break;
									}
									else if (triggerData.Trigger == Triggers.TimeOfFlightRange)
									{
										//check if range is below value or?? do string parse with filter...

										//Comes to comparison as...
										//TriggerFilter = position
										//Text = distance errorcode

										//FrontEdge, BackEdge, FrontRight, FrontCenter, FrontLeft, BackLeft, BackRight

										//Filter example
										//SensorName Equality value duration--> FrontRange X 0 5 1000  -- this many non status 0s
										//SensorName Equality value times durationMs --> FrontRange <= 1 5 1000 -- seen 5 times in 1000 ms
										//~100ms per tof event

										//convert and compare

										//Parse the triggerDetail

										try
										{
											string[] fields = triggerDetail.TriggerFilter.Split(" ");
											if (fields.Length == 5)
											{
												string sensor = fields[0].Trim().ToLower();
												string equality = fields[1].Trim();
												string valueString = fields[2].Trim();
												string timesString = fields[3].Trim();
												string durationMsString = fields[4].Trim();

												double value = Convert.ToDouble(valueString);
												int times = Convert.ToInt32(timesString);
												int durationMs = Convert.ToInt32(durationMsString);

												//FrontRange, BackRange, FrontRight, FrontCenter, FrontLeft, BackLeft, BackRight
												//SensorName Equality value duration--> FrontRange == X 5 1000

												switch (sensor)
												{
													case "frontrange":
														match = IsValidCompare(triggerData, sensor, equality, value, _tofCountFrontRange, times, durationMs);
														break;
													case "backrange":
														match = IsValidCompare(triggerData, sensor, equality, value, _tofCountBackRange, times, durationMs);
														break;
													case "frontright":
														match = IsValidCompare(triggerData, sensor, equality, value, _tofCountFrontRight, times, durationMs);
														break;
													case "frontcenter":
														match = IsValidCompare(triggerData, sensor, equality, value, _tofCountFrontCenter, times, durationMs);
														break;
													case "frontleft":
														match = IsValidCompare(triggerData, sensor, equality, value, _tofCountFrontLeft, times, durationMs);
														break;
													case "backleft":
														match = IsValidCompare(triggerData, sensor, equality, value, _tofCountBackLeft, times, durationMs);
														break;
													case "backright":
														match = IsValidCompare(triggerData, sensor, equality, value, _tofCountBackRight, times, durationMs);
														break;
												}

												if (match)
												{
													break;
												}
											}
										}
										catch
										{
											//Ignore bad parse as it is user created...
											match = false;
										}
									}
									else if (!string.IsNullOrWhiteSpace(triggerDetail.TriggerFilter))
									{
										match = string.Compare(triggerData.TriggerFilter?.Trim(), triggerDetail.TriggerFilter?.Trim(), true) == 0;
										if (match)
										{
											break;
										}
									}
									else
									{
										match = true;
										break;
									}
								}
							}
						}
					}


					//if a match and it is mapped to something, go there, otherwise ignore as a trigger
					if (match && ((triggerDetailMap != null && triggerDetailMap.Count() > 0) || (triggerData.Trigger == Triggers.Manual)))
					{
						//this is it!
						_triggerHandled = true;
						_noInteractionTimer?.Dispose();
						_triggerActionTimeoutTimer?.Dispose();
						_timerTriggerTimer?.Dispose();
						MistyState.GetCharacterState().LatestTriggerMatched = new KeyValuePair<DateTime, TriggerData>(DateTime.Now, triggerData);
						ValidTriggerReceived?.Invoke(this, triggerData);
						Robot.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Valid intent {triggerData.Trigger} {triggerData.TriggerFilter}.");

						if(!string.IsNullOrWhiteSpace(triggerData.OverrideInteraction))
						{
							Guid guid;
							if (!Guid.TryParse(triggerData.OverrideInteraction, out guid))
							{
								//it's the name, map it to id...								
								Interaction interaction = _currentConversationData.Interactions.FirstOrDefault(x => string.Compare(x.Name, triggerData.OverrideInteraction, true) == 0);
								guid = Guid.Parse(interaction.Id);
							}
							
							triggerDetailMap = new List<TriggerActionOption>();
							triggerDetailMap.Add(new TriggerActionOption { Id = "-1", GoToConversation = _currentConversationData.Id, GoToInteraction = guid.ToString(), InterruptCurrentAction = true, Retrigger = false, Weight = 1 });
						}

						await GoToNextAnimation(triggerDetailMap);

						TriggerAnimationComplete(CurrentInteraction);
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Failed to process and verify the trigger.", ex);
			}
			finally
			{
				_processingTriggersSemaphore.Release();
			}

			try
			{
				//Set new intent info
				switch (triggerData.Trigger)
				{
					case Triggers.FaceRecognized:
						if (string.Compare(triggerData.TriggerFilter, ConversationConstants.UnknownPersonFaceLabel, true) == 0)
						{
							MistyState.GetCharacterState().KnownFaceSeen = false;
							MistyState.GetCharacterState().UnknownFaceSeen = true;
						}
						else
						{
							MistyState.GetCharacterState().KnownFaceSeen = true;
							MistyState.GetCharacterState().UnknownFaceSeen = false;
						}
						break;
				}

				if (!_ignoreTriggeringEvents)
				{
					//You are lucky enough to trigger everything else...
					foreach (KeyValuePair<string, IList<TriggerActionOption>> possibleIntent in allowedTriggers)
					{
						//each one of the possible intents for this animation and 
						// interaction has the potential to start and stop at different times
						// so check that here and get them rolling

						TriggerDetail triggerDetail = _currentConversationData.Triggers.FirstOrDefault(x => x.Id == possibleIntent.Key);
						TriggerIntentChecking(triggerData, triggerDetail);
					}

					if (CurrentInteraction.AllowConversationTriggers &&
						_currentConversationData.ConversationTriggerMap != null &&
						_currentConversationData.ConversationTriggerMap.Any())
					{
						foreach (KeyValuePair<string, IList<TriggerActionOption>> possibleIntent in _currentConversationData.ConversationTriggerMap)
						{
							//each one of the possible intents for this animation and 
							// interaction has the potential to start and stop at different times
							// so check that here and get them rolling

							TriggerDetail triggerDetail = _currentConversationData.Triggers.FirstOrDefault(x => x.Id == possibleIntent.Key);
							TriggerIntentChecking(triggerData, triggerDetail);
						}
					}
				}

				return false;
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Failed to reset triggers after handled trigger check.", ex);
				return false;
			}
		}

		private void TriggerIntentChecking(TriggerData triggerData, TriggerDetail triggerDetail)
		{
			try
			{
				if (triggerDetail?.StoppingTrigger != null && triggerDetail?.StoppingTrigger != Triggers.None &&
				(triggerDetail.StoppingTrigger == triggerData.Trigger && (string.IsNullOrWhiteSpace(triggerDetail.StoppingTriggerFilter) || (string.Compare(triggerDetail.StoppingTriggerFilter?.Trim(), triggerData.TriggerFilter?.Trim(), true) == 0) ||
					(triggerDetail.StoppingTrigger == Triggers.AudioCompleted ||
						triggerDetail.StoppingTrigger == Triggers.Timeout ||
						triggerDetail.StoppingTrigger == Triggers.Timer)
				)))
				{
					IgnoreEvent(triggerDetail, (int)Math.Abs((triggerDetail.StoppingTriggerDelay * 1000)));
				}

				if (triggerDetail?.StartingTrigger != null && triggerDetail?.StartingTrigger != Triggers.None &&
					(triggerDetail.StartingTrigger == triggerData.Trigger && (string.IsNullOrWhiteSpace(triggerDetail.StartingTriggerFilter) || (string.Compare(triggerDetail.StartingTriggerFilter?.Trim(), triggerData.TriggerFilter?.Trim(), true) == 0) ||
					(triggerDetail.StartingTrigger == Triggers.AudioCompleted ||
						triggerDetail.StartingTrigger == Triggers.Timeout ||
						triggerDetail.StartingTrigger == Triggers.Timer)
				)))
				{
					ListenToEvent(triggerDetail, (int)Math.Abs((triggerDetail.StartingTriggerDelay * 1000)));

					//TODO Test performance, may be too much here
					foreach (string skillMessageId in CurrentInteraction.SkillMessages)
					{
						SkillMessage skillMessage = _currentConversationData.SkillMessages.FirstOrDefault(x => x.Id == skillMessageId);
						if (skillMessage == null || !skillMessage.StreamTriggerCheck || MistyState.GetCharacterState()?.LatestTriggerChecked == null)
						{
							continue;
						}
						IDictionary<string, object> payloadData = new Dictionary<string, object>();
						payloadData.Add("Skill", skillMessage.Skill);
						payloadData.Add("EventName", skillMessage.EventName);
						payloadData.Add("MessageType", skillMessage.MessageType);
						payloadData.Add("LatestTriggerCheck", Newtonsoft.Json.JsonConvert.SerializeObject(MistyState.GetCharacterState().LatestTriggerChecked));

						//if just started, may miss first trigger, should really start skills at start of conversation
						Robot.TriggerEvent(skillMessage.EventName, "MistyCharacter", payloadData, null, null);
					}
				}
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log("Failed to manage trigger.", ex);
			}
		}

		#endregion

		#region Animation Innards

		private async void AnimationManager_SyncEvent(object sender, TriggerData syncEvent)
		{
			if (!await SendManagedResponseEvent(syncEvent))
			{
				if (!await SendManagedResponseEvent(new TriggerData(syncEvent.Text, "", Triggers.SyncEvent)))
				{
					if (!await SendManagedResponseEvent(syncEvent, true))
					{
						await SendManagedResponseEvent(new TriggerData(syncEvent.Text, "", Triggers.SyncEvent), true);
					}
				}
			}
		}

		public async void UpdateRunningSkillsCallback(object timerData)
		{
			if (Robot.SkillStatus == NativeSkillStatus.Ready)
			{
				IList<string> newSkills = new List<string>();
				IGetRunningSkillsResponse response = await Robot.GetRunningSkillsAsync();
				if (response.Data != null && response.Data.Count() > 0)
				{
					foreach (RunningSkillDetails details in response.Data.Distinct())
					{
						newSkills.Add(details.UniqueId.ToString());
					}
				}

				lock (_runningSkillLock)
				{
					_runningSkills = newSkills;
				}
			}

			if(Robot.SkillStatus == NativeSkillStatus.Ready)
			{
				_pollRunningSkillsTimer = new Timer(UpdateRunningSkillsCallback, null, 15000, Timeout.Infinite);
			}
		}
		
		public void RestartCurrentInteraction(bool interruptCurrentAction = true)
		{
			_processingTriggersSemaphore.Wait();
			try
			{
				_ = GoToNextAnimation(new List<TriggerActionOption>{ new TriggerActionOption
				{
					GoToConversation = _currentConversationData.Id,
					GoToInteraction = CurrentInteraction.Id,
					InterruptCurrentAction = interruptCurrentAction,
				} });

				WaitingForOverrideTrigger = false;
				_ignoreTriggeringEvents = false;
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Failed to restart current interaction.", ex);
			}
			finally
			{
				_processingTriggersSemaphore.Release();
			}
		}

		public async Task<bool> StartConversation(string conversationId = null, string interactionId = null)
		{
			Robot.StopKeyPhraseRecognition(null);
			Robot.SetFlashlight(false, null);

			string startConversation = conversationId ?? _conversationGroup.StartupConversation;
			_currentConversationData = _conversationGroup.Conversations.FirstOrDefault(x => x.Id == startConversation);

			await ManageJavaScriptRecording($"misty.Publish(\"Starting generated conversation '{_currentConversationData.Name}'\");");
			await ManageJavaScriptRecording($"misty.MoveHeadDegrees(0,0,0, null, 2);");
			await ManageJavaScriptRecording($"misty.MoveArmsDegrees(90, 90, null, null, 2);");
			await ManageJavaScriptRecording($"misty.SetFlashlight(false);");
			await ManageJavaScriptRecording($"misty.DisplayImage(\"e_Joy.jpg\");");
			await ManageJavaScriptRecording($"misty.SetBlinkSettings(true);");
			
			if (CharacterParameters.StartVolume != null && CharacterParameters.StartVolume >= 0)
			{
				SpeechManager.Volume = (int)CharacterParameters.StartVolume;
				await ManageJavaScriptRecording($"misty.SetDefaultVolume({SpeechManager.Volume});");
			}

			await ManageJavaScriptRecording($"misty.Pause(2000);");

			EmotionManager = _managerConfiguration?.EmotionManager ?? new EmotionManager(_currentConversationData.StartingEmotion);

			string startInteraction = interactionId ?? _currentConversationData.StartupInteraction;
			CurrentInteraction = new Interaction(_currentConversationData.Interactions.FirstOrDefault(x => x.Id == startInteraction));

			if (CurrentInteraction != null)
			{
				Robot.SkillLogger.Log($"STARTING CONVERSATION");
				Robot.SkillLogger.Log($"Conversation: {_currentConversationData.Name} | Interaction: {CurrentInteraction?.Name} | Going to starting interaction...");
				if (_currentConversationData.InitiateSkillsAtConversationStart && _currentConversationData.SkillMessages != null)
				{
					foreach (SkillMessage skillMessage in _currentConversationData.SkillMessages)
					{
						if (!string.IsNullOrWhiteSpace(skillMessage.Skill) &&
							!_runningSkills.Contains(skillMessage.Skill) &&
							skillMessage.Skill != "8be20a90-1150-44ac-a756-ebe4de30689e")
						{
							_runningSkills.Add(skillMessage.Skill);
							IDictionary<string, object> payloadData = OriginalParameters;
							Robot.RunSkill(skillMessage.Skill, payloadData, null);

							StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name} | Running skill {skillMessage.Skill}.");
							await Task.Delay(1000);
						}
					}

					//Give skills proper time to start as they may need to start background tasks, only happens once at start of conversation if it uses skills
					await Task.Delay(5000);
				}

				ConversationStarted?.Invoke(this, DateTime.Now);
				QueueAndRunInteraction(CurrentInteraction);
				return true;
			}
			return false;
		}

		public async Task StopConversation(string speak = null)
		{	
			_interactionQueue.Clear();
			
			HeadManager.StopMovement();
			ArmManager.StopMovement();
			await AnimationManager.StopRunningAnimationScripts();
			IgnoreEvents();
			await Task.Delay(500);

			if (!string.IsNullOrWhiteSpace(speak))
			{
				Robot.Speak(speak, true, "InteractionTimeout", null);
				await ManageJavaScriptRecording($"misty.Speak(\"{speak}\");");
			}
			ConversationEnded?.Invoke(this, DateTime.Now);
		}

		private async Task GoToNextAnimation(IList<TriggerActionOption> possibleActions)
		{
			try
			{
				if (possibleActions == null || possibleActions.Count == 0)
				{
					//TODO Go to No Interaction selection?
					Robot.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Unmapped intent. Going to the start of the same conversation...");
					Interaction interactionRequest = _currentConversationData.Interactions.FirstOrDefault(x => x.Id == _currentConversationData.StartupInteraction);
					QueueAndRunInteraction(interactionRequest);
					return;
				}

				TriggerActionOption selectedAction = new TriggerActionOption();
				int actionCount = possibleActions.Count;
				if (actionCount == 1)
				{
					selectedAction = possibleActions.FirstOrDefault();
				}
				else
				{
					try
					{
						int counter = 0;
						IDictionary<KeyValuePair<int, int>, TriggerActionOption> weightedItems = new Dictionary<KeyValuePair<int, int>, TriggerActionOption>();
						foreach (TriggerActionOption action in possibleActions)
						{
							counter += action.Weight;
							weightedItems.TryAdd(new KeyValuePair<int, int>(counter, action.Weight), action);
						}

						int randomChoice = _random.Next(1, counter + 1);

						KeyValuePair<KeyValuePair<int, int>, TriggerActionOption> weightedDetails = weightedItems.FirstOrDefault(x => randomChoice > x.Key.Key - x.Key.Value && randomChoice <= x.Key.Key);
						if (weightedDetails.Value != null)
						{
							selectedAction = weightedDetails.Value;
						}
						else
						{
							selectedAction = possibleActions.FirstOrDefault();
						}
					}
					catch
					{
						selectedAction = possibleActions.FirstOrDefault();
					}
				}

				if (selectedAction != null)
				{
					string interaction = selectedAction.GoToInteraction;
					string conversation = selectedAction.GoToConversation;

					//TODO Clean this up, use empty guid, not string
					if (interaction == ConversationDeparturePoint)
					{
						//Look up where the departure goes
						KeyValuePair<string, ConversationMappingDetail> detail = _conversationGroup.ConversationMappings.FirstOrDefault(x => x.Value.DepartureMap.TriggerOptionId == selectedAction.Id);
						if (detail.Value != null)
						{
							interaction = detail.Value.EntryMap.InteractionId;
							conversation = detail.Value.EntryMap.ConversationId;
						}
						else
						{
							//Exit and stop app
							await StopConversation();
							TriggerConversationCleanup?.Invoke(this, DateTime.UtcNow);							
							//Robot.SkillCompleted();
						}
					}

					//if coming from an internal redirect, may not have an Id
					if (selectedAction.Id == null || (_currentConversationData.InteractionAnimations == null || !_currentConversationData.InteractionAnimations.TryGetValue(selectedAction.Id, out string overrideAnimation)))
					{
						overrideAnimation = null;
					}

					if (selectedAction.Id == null || _currentConversationData.InteractionPreSpeechAnimations == null ||
						!_currentConversationData.InteractionPreSpeechAnimations.TryGetValue(selectedAction.Id, out string preSpeechOverrideAnimation))
					{
						preSpeechOverrideAnimation = null;
					}

					if (selectedAction.Id == null || _currentConversationData.InteractionInitAnimations == null ||
						!_currentConversationData.InteractionInitAnimations.TryGetValue(selectedAction.Id, out string initOverrideAnimation))
					{
						initOverrideAnimation = null;
					}

					if (selectedAction.Id == null || _currentConversationData.InteractionListeningAnimations == null ||
						!_currentConversationData.InteractionListeningAnimations.TryGetValue(selectedAction.Id, out string listeningOverrideAnimation))
					{
						listeningOverrideAnimation = null;
					}
					
					if (string.IsNullOrWhiteSpace(conversation) && string.IsNullOrWhiteSpace(interaction))
					{
						Robot.SkillLogger.Log($"Trigger has been activated, but the destination is unmapped, continuing to wait for mapped trigger.");
						return;
					}
					else
					{
						IgnoreEvents();
						SpeechManager.AbortListening(_currentAnimation.SpeakFileName ?? _currentAnimation.AudioFile);
						if (selectedAction.InterruptCurrentAction)
						{
							//TODO Hacky fix for not interupting prespeech with interrupt flag set
							int sanity = 0;
							while (_waitingOnPrespeech && sanity < 50) //5 seconds max wait on prespeech flag
							{
								sanity++;
								await Task.Delay(100);
							}
							Robot.StopSpeaking(null);
							Robot.StopAudio(null);
							await Task.Delay(25);
						}
						else if (!string.IsNullOrWhiteSpace(_currentAnimation.Speak) || !string.IsNullOrWhiteSpace(_currentAnimation.AudioFile))
						{
							//TODO Test, this shouldn't be necessary with recent updates
							int sanity = 0;
							while (MistyState.GetCharacterState().Speaking && sanity < 6000)
							{
								sanity++;
								await Task.Delay(100);
							}
							if (sanity == 6000)
							{
								Robot.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Possible error managing speech, unless Misty has actually been speaking in one interaction for a minute.");
							}
						}

						if ((string.IsNullOrWhiteSpace(conversation) || conversation == _currentConversationData?.Id) && !string.IsNullOrWhiteSpace(interaction))
						{
							Robot.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Going to interaction {interaction} in the same conversation...");
							Interaction interactionRequest = _currentConversationData?.Interactions?.FirstOrDefault(x => x.Id == interaction);
							if (interactionRequest != null)
							{
								//TODO dupe code to cleanup
								interactionRequest.Retrigger = selectedAction.Retrigger;
								if (overrideAnimation != null)
								{
									interactionRequest.Animation = overrideAnimation;
								}
								if (preSpeechOverrideAnimation != null)
								{
									interactionRequest.PreSpeechAnimation = preSpeechOverrideAnimation;
								}
								if (initOverrideAnimation != null)
								{
									interactionRequest.InitAnimation = initOverrideAnimation;
								}
								if (listeningOverrideAnimation != null)
								{
									interactionRequest.ListeningAnimation = listeningOverrideAnimation;
								}
								QueueAndRunInteraction(interactionRequest);
							}
						}
						else if (!string.IsNullOrWhiteSpace(conversation) && string.IsNullOrWhiteSpace(interaction))
						{
							Robot.SkillLogger.Log($"Going to the start of conversation {conversation}...");

							_currentConversationData = _conversationGroup.Conversations.FirstOrDefault(x => x.Id == conversation);

							Interaction interactionRequest = _currentConversationData?.Interactions?.FirstOrDefault(x => x.Id == _currentConversationData.StartupInteraction);
							if(interactionRequest != null)
							{
								interactionRequest.Retrigger = selectedAction.Retrigger;
								if (overrideAnimation != null)
								{
									interactionRequest.Animation = overrideAnimation;
								}
								if (preSpeechOverrideAnimation != null)
								{
									interactionRequest.PreSpeechAnimation = preSpeechOverrideAnimation;
								}
								if (initOverrideAnimation != null)
								{
									interactionRequest.InitAnimation = initOverrideAnimation;
								}
								if (listeningOverrideAnimation != null)
								{
									interactionRequest.ListeningAnimation = listeningOverrideAnimation;
								}
								QueueAndRunInteraction(interactionRequest);
							}
						}
						else if (!string.IsNullOrWhiteSpace(conversation) && !string.IsNullOrWhiteSpace(interaction))
						{
							Robot.SkillLogger.Log($"Going to interaction {interaction} in conversation {conversation}...");

							_currentConversationData = _conversationGroup.Conversations.FirstOrDefault(x => x.Id == conversation);

							Interaction interactionRequest = _currentConversationData?.Interactions?.FirstOrDefault(x => x.Id == interaction);
							if (interactionRequest != null)
							{
								interactionRequest.Retrigger = selectedAction.Retrigger;
								if (overrideAnimation != null)
								{
									interactionRequest.Animation = overrideAnimation;
								}
								if (preSpeechOverrideAnimation != null)
								{
									interactionRequest.PreSpeechAnimation = preSpeechOverrideAnimation;
								}
								if (initOverrideAnimation != null)
								{
									interactionRequest.InitAnimation = initOverrideAnimation;
								}
								if (listeningOverrideAnimation != null)
								{
									interactionRequest.ListeningAnimation = listeningOverrideAnimation;
								}
								QueueAndRunInteraction(interactionRequest);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Exception thrown while going to next animation", ex);
			}
		}
		
		private void RunNextAnimation()
		{			
			try
			{
				_latestTriggerMatchData = MistyState.GetCharacterState().LatestTriggerMatched;
				UniqueAnimationId = Guid.NewGuid();

				if (Robot.SkillStatus == NativeSkillStatus.Running)
				{
					MistyState.GetCharacterState().Spoke = false;
					MistyState.GetCharacterState().UnknownFaceSeen = false;
					MistyState.GetCharacterState().KnownFaceSeen = false;
					MistyState.GetCharacterState().Listening = false;
					MistyState.GetCharacterState().Saying = "";
					_triggerHandled = false;
					_ = ProcessNextAnimationRequest();
				}
				else
				{
					_ = StopConversation();
				}
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log("Exception running animation.", ex);
			}
		}
		
		//TODO Cleanup, queues are no longer needed
		private async Task ProcessNextAnimationRequest()
		{
			try
			{
				StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name}  | LOOKING FOR NEXT ANIMATION IN QUEUE.");

				Interaction interaction = null;
				bool dequeued = false;

				while (_triggerHandled)
				{
					await Task.Delay(25);
				}

				while (!dequeued || interaction == null)
				{
					dequeued = _interactionPriorityQueue.TryDequeue(out interaction);
					if (!dequeued)
					{
						dequeued = _interactionQueue.TryDequeue(out interaction);
						if (dequeued)
						{
							break;
						}
					}
					else
					{
						break;
					}
					await Task.Delay(25);
				}

				CurrentInteraction = new Interaction(interaction);

				Robot.SkillLogger.LogVerbose($"STARTING NEW INTERACTION.");
				Robot.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Processing next interaction in queue.");
				if (_skillsToStop != null && _skillsToStop.Count > 0)
				{
					foreach (string skill in _skillsToStop)
					{
						Robot.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Stop skill request for skill {skill}.");
						await Robot.CancelRunningSkillAsync(skill);
					}
					_skillsToStop.Clear();
				}
				
				_ = Robot.SetTextDisplaySettingsAsync("HeardSpeech", new TextSettings
				{
					Visible = false
				});
				_ = Robot.SetTextDisplaySettingsAsync("SpokeText", new TextSettings
				{
					Visible = false
				});
				
				InteractionStarted?.Invoke(this, CurrentInteraction?.Name);

				string triggerActionOptionId = "";
				string currentAnimationId = "";
				if (_currentConversationData.InteractionAnimations != null && _currentConversationData.InteractionAnimations.TryGetValue(triggerActionOptionId, out string overrideAnimation))
				{
					currentAnimationId = overrideAnimation;
				}
				else
				{
					currentAnimationId = interaction.Animation;
				}

				AnimationRequest animationRequest = _currentConversationData.Animations.FirstOrDefault(x => x.Id == currentAnimationId);
				_currentAnimation = new AnimationRequest(animationRequest);

				if (interaction == null)
				{
					Robot.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Failed processing null conversation phrase.");
					return;
				}

				if (CharacterParameters.LogLevel == SkillLogLevel.Verbose || CharacterParameters.LogLevel == SkillLogLevel.Info)
				{
					Robot.SkillLogger.Log($"Animation message '{animationRequest?.Name}' sent in to : Say '{animationRequest?.Speak ?? "nothing"}' : Play Audio '{animationRequest?.Speak ?? "none"}'.");
				}
				
				//Restart trigger handling in case a developer stopped in template and didn't restart
				RestartTriggerHandling();

				MistyState.GetCharacterState().AnimationEmotion = animationRequest.Emotion;
				MistyState.GetCharacterState().CurrentMood = EmotionManager.GetNextEmotion(MistyState.GetCharacterState().AnimationEmotion);
				StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name} | New attitude adjustment:{animationRequest.Emotion} - Current mood:{MistyState.GetCharacterState().CurrentMood}");
				
				_allowedTriggers = CurrentInteraction.TriggerMap.Keys.ToList();

				if (CurrentInteraction.AllowConversationTriggers && _currentConversationData.ConversationTriggerMap != null && _currentConversationData.ConversationTriggerMap.Count > 0)
				{
					foreach (KeyValuePair<string, IList<TriggerActionOption>> actionOption in _currentConversationData.ConversationTriggerMap)
					{
						if (!_allowedTriggers.Contains(actionOption.Key))
						{
							//don't re-add trigger if in interaction
							_allowedTriggers.Add(actionOption.Key);
						}
					}
				}

				_allowedUtterances.Clear();

				foreach (string trigger in _allowedTriggers)
				{
					//get utterances
					TriggerDetail triggerDetail = _currentConversationData.Triggers.FirstOrDefault(x => string.Compare(x.Trigger, Triggers.SpeechHeard, true) == 0 && (string.Compare(x.Id, trigger, true) == 0 || string.Compare(x.Name, trigger, true) == 0));
					if (triggerDetail != null && !_allowedUtterances.Contains(triggerDetail.TriggerFilter))
					{
						_allowedUtterances.Add(triggerDetail.TriggerFilter);
					}
				}

				SpeechManager.SetMaxListen((int)(animationRequest.ListenTimeout));
				SpeechManager.SetMaxSilence((int)(animationRequest.SilenceTimeout));
				SpeechManager.SetAllowedUtterances(_allowedUtterances);

				SendInteractionUIEvent();
				
				AnimationRequestProcessor(interaction);
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Exception processing animation request.", ex);
			}
		}

		private string GetInteractionUIEvent()
		{
			try
			{ 
				IList<TriggerDetail> triggerList = new List<TriggerDetail>();
				IList<TriggerDetail> utteranceList = new List<TriggerDetail>();

				//Get trigger details from id
				foreach (string triggerString in _allowedTriggers)
				{
					TriggerDetail trigger = _currentConversationData.Triggers.FirstOrDefault(x => string.Compare(x.Id, triggerString, true) == 0);
					if (trigger != null && !triggerList.Contains(trigger))
					{
						triggerList.Add(trigger);
					}
				}

				foreach (string utteranceString in _allowedUtterances)
				{
					//get utterances
					TriggerDetail trigger = _currentConversationData.Triggers.FirstOrDefault(x => string.Compare(x.Trigger, Triggers.SpeechHeard, true) == 0 && (string.Compare(x.Id, utteranceString, true) == 0 || string.Compare(x.Name, utteranceString, true) == 0));

					if (trigger != null && !triggerList.Contains(trigger))
					{
						utteranceList.Add(trigger);
					}
				}

				//Send this? Others?				
				if (triggerList.FirstOrDefault(x => string.Compare(x.Trigger, Triggers.Timeout, true) == 0) == null)
				{
					triggerList.Add(new TriggerDetail("-1", Triggers.Timeout));
				}

				IDictionary<string, object> data = new Dictionary<string, object>
				{
					{"DataType", "interaction"},
					{"ConversationGroup", _conversationGroup?.Name},
					{"RobotName", _conversationGroup?.RobotName ?? "Misty"},
					{"Conversation", _currentConversationData?.Name },
					{"CurrentInteraction", CurrentInteraction },
					{"Utterances", utteranceList},
					{"Triggers", triggerList}
				};

				return JsonConvert.SerializeObject(data);
			}
			catch
			{
				return string.Empty;
			}
		}

		private string GetStateEvent()
		{
			try
			{
				IDictionary<string, object> data = new Dictionary<string, object>
				{
					{"DataType", "state"},
					{"State", MistyState.GetCharacterState()},
				};

				return JsonConvert.SerializeObject(data);
			}
			catch
			{
				return string.Empty;
			}
		}
		
		private void SendInteractionUIEvent()
		{
			try
			{
				if (_sendInteractionThrottle)
				{
					return;
				}
				_sendInteractionThrottle = true;
				if (!CharacterParameters.SendInteractionUIEvents)
				{
					return;
				}

				string msg = GetInteractionUIEvent();
				if (!string.IsNullOrWhiteSpace(msg))
				{
					Robot.PublishMessage(msg, null);
				}
			}
			catch { }
			finally
			{
				_sendInteractionThrottle = false;
			}
		}

		private void SendStateEvent(object timerData)
		{
			try
			{
				if (_sendStateThrottle)
				{
					return;
				}
				_sendStateThrottle = true;
				//TODO Test performance and allow config of how often		
				string msg = GetStateEvent();
				if (!string.IsNullOrWhiteSpace(msg))
				{
					Robot.PublishMessage(msg, null);
				}
			}
			catch { }
			finally
			{
				_sendStateThrottle = false;
			}			
		}

		public void HandleAnimationRequest(object sender, KeyValuePair<AnimationRequest, Interaction> action)
		{
			try
			{
				HeadManager.StopMovement();
				ArmManager.StopMovement();
				_ = AnimationManager.StopRunningAnimationScripts();//or overlap?
				_ = IntermediateAnimationRequestProcessor(action.Key, action.Value, "user-request");
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Exception processing user requested script animation.", ex);
			}
		}

		//Duped
		private async Task ManageJavaScriptRecording(string command)
		{
			if (!string.IsNullOrWhiteSpace(command) && _javaScriptRecorder != null && CharacterParameters.CreateJavaScriptTemplate)
			{
				await _javaScriptRecorder.SaveRecording(command);
			}
		}

		private async Task IntermediateAnimationRequestProcessor(AnimationRequest intermediateAnimation, Interaction interaction, string actionType)
		{
			try
			{
				//TODO- interaction and animations are kind of out of sync and overlap too much, clean up

				
				Interaction newInteraction = new Interaction(interaction);
				AnimationRequest animation = new AnimationRequest(intermediateAnimation);
				AnimationRequest interactionAnimation;
				string script = "";
				bool backgroundSpeech = false;

				switch(actionType)
				{
					case "init":
						interactionAnimation = new AnimationRequest(_currentConversationData.Animations.FirstOrDefault(x => x.Id == newInteraction.InitAnimation));
						script = newInteraction.InitScript;
						if (interactionAnimation != null)
						{
							_ = AnimationManager.RunAnimationScript(script, false, interactionAnimation, newInteraction, _currentConversationData);
						}
						_ = AnimationManager.RunAnimationScript(script, false, animation, newInteraction, _currentConversationData);
						break;
					case "prespeech":
						interactionAnimation = new AnimationRequest(_currentConversationData.Animations.FirstOrDefault(x => x.Id == newInteraction.PreSpeechAnimation));
						backgroundSpeech = true;
						newInteraction.StartListening = false;
						script = newInteraction.PreSpeechScript;

						if (!_processingVoice)
						{
							return;
						}
						if (interactionAnimation != null)
						{
							_ = AnimationManager.RunAnimationScript(script, false, interactionAnimation, newInteraction, _currentConversationData);
						}
						_ = AnimationManager.RunAnimationScript(script, false, animation, newInteraction, _currentConversationData);
						break;
					case "listening":
						interactionAnimation = new AnimationRequest(_currentConversationData.Animations.FirstOrDefault(x => x.Id == newInteraction.ListeningAnimation));
						backgroundSpeech = true;
						script = newInteraction.ListeningScript;
						newInteraction.StartListening = true;
						if (interactionAnimation != null)
						{
							_ = AnimationManager.RunAnimationScript(script, false, interactionAnimation, newInteraction, _currentConversationData);
						}
						_ = AnimationManager.RunAnimationScript(script, false, animation, newInteraction, _currentConversationData);
						break;
					default:
						break;
				}
				
				
				bool hasAudio = false;
				//Set values based upon defaults and passed in
				if (!EmotionAnimations.TryGetValue(intermediateAnimation.Emotion, out AnimationRequest defaultAnimation))
				{
					defaultAnimation = new AnimationRequest();
				}

				if (intermediateAnimation.Silence)
				{
					hasAudio = false;
					intermediateAnimation.AudioFile = null;
					intermediateAnimation.Speak = "";
				}
				else if (!string.IsNullOrWhiteSpace(intermediateAnimation.AudioFile) || !string.IsNullOrWhiteSpace(intermediateAnimation.Speak))
				{
					hasAudio = true;
				}

				await SpeechManager.UpdateKeyPhraseRecognition(newInteraction, hasAudio);

				//Use image default if NULL , if EMPTY (just whitespace), no new image
				if (intermediateAnimation.ImageFile == null)
				{
					intermediateAnimation.ImageFile = defaultAnimation.ImageFile;
				}

				//Set default LED for emotion if not already set
				if (intermediateAnimation.LEDTransitionAction == null)
				{
					intermediateAnimation.LEDTransitionAction = defaultAnimation.LEDTransitionAction;
				}

				if (intermediateAnimation.Volume != null && intermediateAnimation.Volume > 0)
				{
					SpeechManager.Volume = (int)intermediateAnimation.Volume;
					await ManageJavaScriptRecording($"misty.SetDefaultVolume({SpeechManager.Volume});");
				}
				else if (defaultAnimation.Volume != null && defaultAnimation.Volume > 0)
				{
					SpeechManager.Volume = (int)defaultAnimation.Volume;
					await ManageJavaScriptRecording($"misty.SetDefaultVolume({SpeechManager.Volume});");
				}

				if (!_processingVoice && actionType == "prespeech")
				{
					return;
				}

				//Start speech or audio playing
				if (!string.IsNullOrWhiteSpace(intermediateAnimation.Speak))
				{
					hasAudio = true;

					SpeechManager.TryToPersonalizeData(intermediateAnimation.Speak, intermediateAnimation, newInteraction, out string newText);
					intermediateAnimation.Speak = newText;
					newInteraction.StartListening = false;
					_ = SpeechManager.Speak(intermediateAnimation, newInteraction, backgroundSpeech);
					Robot.SkillLogger.Log($"Saying '{ intermediateAnimation.Speak}' for intermediate animation '{ intermediateAnimation.Name}'.");
				}
				else if (!string.IsNullOrWhiteSpace(intermediateAnimation.AudioFile))
				{
					hasAudio = true;
					MistyState.GetCharacterState().Audio = intermediateAnimation.AudioFile;
					Robot.PlayAudio(intermediateAnimation.AudioFile, null, null);
					await ManageJavaScriptRecording($"misty.PlayAudio(\"{intermediateAnimation.AudioFile}\");");
					Robot.SkillLogger.Log($"Playing audio '{ intermediateAnimation.AudioFile}' for intermediate animation '{ intermediateAnimation.Name}'.");
				}
				
				//Display image
				if (!string.IsNullOrWhiteSpace(intermediateAnimation.ImageFile))
				{
					MistyState.GetCharacterState().Image = intermediateAnimation.ImageFile;

					if (!intermediateAnimation.ImageFile.Contains("."))
					{
						intermediateAnimation.ImageFile = intermediateAnimation.ImageFile + ".jpg";
					}
					StreamAndLogInteraction($"Animation: { intermediateAnimation.Name} | Displaying image {intermediateAnimation.ImageFile}");
					Robot.DisplayImage(intermediateAnimation.ImageFile, null, false, null);
					
					await ManageJavaScriptRecording($"misty.DisplayImage(\"{intermediateAnimation.ImageFile}\");");
				}

				if (intermediateAnimation.SetFlashlight != MistyState.GetCharacterState().FlashLightOn)
				{
					Robot.SetFlashlight(intermediateAnimation.SetFlashlight, null);
					MistyState.GetCharacterState().FlashLightOn = intermediateAnimation.SetFlashlight;
				}
				

				if (!string.IsNullOrWhiteSpace(intermediateAnimation.LEDTransitionAction))
				{
					LEDTransitionAction ledTransitionAction = _currentConversationData.LEDTransitionActions.FirstOrDefault(x => x.Id == intermediateAnimation.LEDTransitionAction);
					if (ledTransitionAction != null)
					{
						if (TryGetPatternToTransition(ledTransitionAction.Pattern, out LEDTransition ledTransition) && ledTransitionAction.PatternTime > 0.1)
						{
							_ = Task.Run(async () =>
							{
								if (intermediateAnimation.LEDActionDelay > 0)
								{
									await Task.Delay((int)(intermediateAnimation.LEDActionDelay * 1000));
								}
								Robot.TransitionLED(ledTransitionAction.Red, ledTransitionAction.Green, ledTransitionAction.Blue, ledTransitionAction.Red2, ledTransitionAction.Green2, ledTransitionAction.Blue2, ledTransition, ledTransitionAction.PatternTime * 1000, null);
							});

							MistyState.GetCharacterState().AnimationLED = ledTransitionAction;
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(intermediateAnimation.ArmLocation))
				{
					_ = Task.Run(async () =>
					{
						if (intermediateAnimation.ArmActionDelay > 0)
						{
							await Task.Delay((int)(intermediateAnimation.ArmActionDelay * 1000));
						}
						ArmManager.HandleArmAction(intermediateAnimation, _currentConversationData);
					});
				}

				if (!string.IsNullOrWhiteSpace(intermediateAnimation.HeadLocation))
				{
					_ = Task.Run(async () =>
					{
						MistyState.RegisterEvent(Triggers.ObjectSeen);
						if (intermediateAnimation.HeadActionDelay > 0)
						{
							await Task.Delay((int)(intermediateAnimation.HeadActionDelay * 1000));
						}
						HeadManager.HandleHeadAction(intermediateAnimation, _currentConversationData);
					});
				}
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Animation Id: { interaction.Animation} | Exception while attempting to process animation request callback.", ex);
			}
		}

		private async void AnimationRequestProcessor(Interaction interaction)
		{
			try
			{
				if (interaction?.Animation == null)
				{
					Robot.SkillLogger.Log($"Received null interaction or animation for interaction {interaction?.Name ?? interaction?.Id}.");
					return;
				}
				
				AnimationRequest originalAnimationRequest = new AnimationRequest(_currentConversationData.Animations.FirstOrDefault(x => x.Id == interaction.Animation));
				if (originalAnimationRequest == null)
				{
					Robot.SkillLogger.Log($"Could not find animation for interaction {interaction?.Name ?? interaction?.Id}.");
					return;
				}

				AnimationRequest animationRequest = new AnimationRequest(originalAnimationRequest);
				
				//Make copy cuz we are decorating and changing things here
				Interaction newInteraction = new Interaction(interaction);

				if (animationRequest == null || newInteraction == null)
				{
					Robot.SkillLogger.Log($"Failed to copy data.");
					return;
				}

				//Await completion of final commands??
				//Add options like IM?
				HeadManager.StopMovement();
				ArmManager.StopMovement();
				await AnimationManager.StopRunningAnimationScripts();

				//should we start skill listening even if it may retrigger?
				foreach (string skillMessageId in newInteraction.SkillMessages)
				{
					SkillMessage skillMessage = _currentConversationData.SkillMessages.FirstOrDefault(x => x.Id == skillMessageId);
					if (skillMessage == null)
					{
						continue;
					}

					if (skillMessage.StopIfRunning)
					{
						Robot.CancelRunningSkill(skillMessage.Skill, null);
						lock (_runningSkillLock)
						{
							_runningSkills.Remove(skillMessage.Skill);
						}
						_skillsToStop.Remove(skillMessage.Skill);
						return;
					}

					IDictionary<string, object> payloadData = new Dictionary<string, object>();

					payloadData.Add("Skill", skillMessage.Skill);
					payloadData.Add("EventName", skillMessage.EventName);
					payloadData.Add("MessageType", skillMessage.MessageType);
					if (skillMessage.IncludeCharacterState && MistyState.GetCharacterState() != null)
					{
						payloadData.Add("CharacterState", Newtonsoft.Json.JsonConvert.SerializeObject(MistyState.GetCharacterState()));
					}
					if (skillMessage.IncludeLatestTriggerMatch && _latestTriggerMatchData.Value != null)
					{
						payloadData.Add("LatestTriggerMatch", Newtonsoft.Json.JsonConvert.SerializeObject(_latestTriggerMatchData));
					}
					if(skillMessage.IncludeInteractionInformation)
					{
						string interactionMsg = GetInteractionUIEvent();
						if (!string.IsNullOrWhiteSpace(interactionMsg))
						{
							payloadData.Add("Interaction", interactionMsg);
						}
					}
					
					if (skillMessage.StartIfStopped)
					{
						lock (_runningSkillLock)
						{
							if (!string.IsNullOrWhiteSpace(skillMessage.Skill) &&
							!_runningSkills.Contains(skillMessage.Skill) &&
							skillMessage.Skill != "8be20a90-1150-44ac-a756-ebe4de30689e")//TODO Shouldn't hardcode this
							{

								StreamAndLogInteraction($"Running skill {skillMessage.Skill}.");
								_ = Robot.RunSkillAsync(skillMessage.Skill, OriginalParameters);
							}
						}
					}

					StreamAndLogInteraction($"Sending event {skillMessage.EventName} to trigger handler skill {skillMessage.Skill}.");
					//if just started, may miss first trigger, should really start skills at start of conversation
					Robot.TriggerEvent(skillMessage.EventName, "MistyCharacter", payloadData, null, null);

					if (skillMessage.StopOnNextAnimation && !_skillsToStop.Contains(skillMessage.Skill))
					{
						_skillsToStop.Add(skillMessage.Skill);
					}
				}

				//Animation started, make sure all the immediate events are going and set listen by intent
				IDictionary<string, IList<TriggerActionOption>> allowedIntents = newInteraction.TriggerMap;
				foreach (KeyValuePair<string, IList<TriggerActionOption>> possibleIntent in allowedIntents)
				{
					//each one of the possible intents for this animation and 
					// interaction has the potential to start and stop at different times
					// so check that here and get them rolling
					TriggerDetail triggerDetail = _currentConversationData.Triggers.FirstOrDefault(x => x.Id == possibleIntent.Key);

					//Timer is the only start intent immediately trigger intent...
					if (triggerDetail.StartingTrigger == Triggers.Timer)
					{
						ListenToEvent(triggerDetail, (int)(triggerDetail.StartingTriggerDelay * 1000));
					}
				}

				if (newInteraction.AllowConversationTriggers &&
					_currentConversationData.ConversationTriggerMap != null &&
					_currentConversationData.ConversationTriggerMap.Any())
				{
					IDictionary<string, IList<TriggerActionOption>> allowedConversationIntents = _currentConversationData.ConversationTriggerMap;
					foreach (KeyValuePair<string, IList<TriggerActionOption>> possibleIntent in allowedConversationIntents)
					{
						//each one of the possible intents for this animation and 
						// interaction has the potential to start and stop at different times
						// so check that here and get them rolling
						TriggerDetail triggerDetail = _currentConversationData.Triggers.FirstOrDefault(x => x.Id == possibleIntent.Key);

						//Timer is the only start intent immediately trigger intent...
						if (triggerDetail.StartingTrigger == Triggers.Timer)
						{
							ListenToEvent(triggerDetail, (int)(triggerDetail.StartingTriggerDelay * 1000));
						}
					}
				}

				bool hasAudio = false;
				//Set values based upon defaults and passed in
				if (!EmotionAnimations.TryGetValue(animationRequest.Emotion, out AnimationRequest defaultAnimation))
				{
					defaultAnimation = new AnimationRequest();
				}

				if (animationRequest.Silence)
				{
					hasAudio = false;
					animationRequest.AudioFile = null;
					animationRequest.Speak = "";
				}
				else if (!string.IsNullOrWhiteSpace(animationRequest.AudioFile) || !string.IsNullOrWhiteSpace(animationRequest.Speak))
				{
					hasAudio = true;
				}

				await SpeechManager.UpdateKeyPhraseRecognition(newInteraction, hasAudio);

				//Use image default if NULL , if EMPTY (just whitespace), no new image
				if (animationRequest.ImageFile == null)
				{
					animationRequest.ImageFile = defaultAnimation.ImageFile;
				}

				//Set default LED for emotion if not already set
				if (animationRequest.LEDTransitionAction == null)
				{
					animationRequest.LEDTransitionAction = defaultAnimation.LEDTransitionAction;
				}

				if (animationRequest.Volume != null && animationRequest.Volume >= 0)
				{
					SpeechManager.Volume = (int)animationRequest.Volume > 100 ? 100 : (int)animationRequest.Volume;
					await ManageJavaScriptRecording($"misty.SetDefaultVolume({SpeechManager.Volume});");
				}
				else if (defaultAnimation.Volume != null && defaultAnimation.Volume >= 0)
				{
					SpeechManager.Volume = (int)defaultAnimation.Volume > 100 ? 100 : (int)defaultAnimation.Volume;
					await ManageJavaScriptRecording($"misty.SetDefaultVolume({SpeechManager.Volume});");
				}

				if (newInteraction.Retrigger &&
					MistyState.GetCharacterState().LatestTriggerMatched.Value != null &&
					MistyState.GetCharacterState().LatestTriggerMatched.Value.Trigger == Triggers.SpeechHeard) //for now
				{
					SpeechMatchData data = SpeechIntentManager.GetIntent(MistyState.GetCharacterState().LatestTriggerMatched.Value.Text, _allowedUtterances);

					//Retrigger only works with speech, also ignores conversation triggers
					//This may change
					if (await SendManagedResponseEvent(new TriggerData(MistyState.GetCharacterState().LatestTriggerMatched.Value.Text, data.Id), false))
					{
						StreamAndLogInteraction($"Interaction: {newInteraction?.Name} | Sent Handled Retrigger | {MistyState.GetCharacterState().LatestTriggerMatched.Value.Trigger} - {MistyState.GetCharacterState().LatestTriggerMatched.Value.TriggerFilter} - {MistyState.GetCharacterState().LatestTriggerMatched.Value.Text}.");
						return;
					}
					else
					{
						switch (MistyState.GetCharacterState().LatestTriggerMatched.Value.Trigger)
						{
							case Triggers.SpeechHeard:
								//send in unknown
								if (await SendManagedResponseEvent(new TriggerData(MistyState.GetCharacterState().LatestTriggerMatched.Value.Text, ConversationConstants.HeardUnknownTrigger, Triggers.SpeechHeard), false))
								{
									StreamAndLogInteraction($"Interaction: {newInteraction?.Name} | Sent Handled Retrigger | {MistyState.GetCharacterState().LatestTriggerMatched.Value.Trigger} - Unknown - {MistyState.GetCharacterState().LatestTriggerMatched.Value.Text}.");
									return;
								}
								break;
						}
					}
				}

				int interactionTimeoutMs = newInteraction.InteractionFailedTimeout <= 0 ? 100 : (int)(newInteraction.InteractionFailedTimeout * 1000);
				_noInteractionTimer = new Timer(CommunicationBreakdownCallback, UniqueAnimationId, interactionTimeoutMs, Timeout.Infinite);
				
				//bool runningInitScript = false;
				if (!string.IsNullOrWhiteSpace(newInteraction.InitScript))
				{
					await AnimationManager.RunAnimationScript(newInteraction.InitScript, false, _currentAnimation, newInteraction, _currentConversationData);					
				}
				
				AnimationRequest initAnimation;
				if(CurrentInteraction.InitAnimation != null)
				{
					if ((initAnimation = _currentConversationData.Animations.FirstOrDefault(x => x.Id == newInteraction.InitAnimation)) != null)
					{
						AnimationRequest initCopy = new AnimationRequest(initAnimation);
						await IntermediateAnimationRequestProcessor(initCopy, newInteraction, "init");						
					}
				}

				//Start speech or audio playing
				if (!string.IsNullOrWhiteSpace(animationRequest.Speak))
				{
					hasAudio = true;

					SpeechManager.TryToPersonalizeData(animationRequest.Speak, animationRequest, newInteraction, out string newText);
					animationRequest.Speak = newText;
					_ = SpeechManager.Speak(animationRequest, newInteraction, false);

					await ManageJavaScriptRecording($"misty.Speak(\"{animationRequest.Speak}\");");

					Robot.SkillLogger.Log($"Saying '{ animationRequest.Speak}' for animation '{ animationRequest.Name}'.");
				}
				else if (!string.IsNullOrWhiteSpace(animationRequest.AudioFile))
				{
					hasAudio = true;
					MistyState.GetCharacterState().Audio = animationRequest.AudioFile;
					Robot.PlayAudio(animationRequest.AudioFile, null, null);
					await ManageJavaScriptRecording($"misty.PlayAudio(\"{animationRequest.AudioFile}\");");
					Robot.SkillLogger.Log($"Playing audio '{ animationRequest.AudioFile}' for animation '{ animationRequest.Name}'.");
				}

				//Display image
				if (!string.IsNullOrWhiteSpace(animationRequest.ImageFile))
				{
					MistyState.GetCharacterState().Image = animationRequest.ImageFile;

					if (!animationRequest.ImageFile.Contains("."))
					{
						animationRequest.ImageFile = animationRequest.ImageFile + ".jpg";
					}
					StreamAndLogInteraction($"Animation: { animationRequest.Name} | Displaying image {animationRequest.ImageFile}");
					Robot.DisplayImage(animationRequest.ImageFile, null, false, null);
					await ManageJavaScriptRecording($"misty.DisplayImage(\"{animationRequest.ImageFile}\");");
				}

				if (animationRequest.SetFlashlight != MistyState.GetCharacterState().FlashLightOn)
				{
					Robot.SetFlashlight(animationRequest.SetFlashlight, null);
					MistyState.GetCharacterState().FlashLightOn = animationRequest.SetFlashlight;
				}

				if (!string.IsNullOrWhiteSpace(newInteraction.AnimationScript))
				{
					_ = AnimationManager.RunAnimationScript(newInteraction.AnimationScript, animationRequest.RepeatScript, animationRequest, newInteraction, _currentConversationData);
				}

				if (!string.IsNullOrWhiteSpace(animationRequest.AnimationScript))
				{
					_ = AnimationManager.RunAnimationScript(animationRequest.AnimationScript, animationRequest.RepeatScript, animationRequest, newInteraction, _currentConversationData);
				}

				if (!string.IsNullOrWhiteSpace(animationRequest.LEDTransitionAction))
				{
					LEDTransitionAction ledTransitionAction = _currentConversationData.LEDTransitionActions.FirstOrDefault(x => x.Id == animationRequest.LEDTransitionAction);
					if (ledTransitionAction != null)
					{
						if (TryGetPatternToTransition(ledTransitionAction.Pattern, out LEDTransition ledTransition) && ledTransitionAction.PatternTime > 0.1)
						{
							_ = Task.Run(async () =>
							{
								if (animationRequest.LEDActionDelay > 0)
								{
									await Task.Delay((int)(animationRequest.LEDActionDelay * 1000));
								}
								Robot.TransitionLED(ledTransitionAction.Red, ledTransitionAction.Green, ledTransitionAction.Blue, ledTransitionAction.Red2, ledTransitionAction.Green2, ledTransitionAction.Blue2, ledTransition, ledTransitionAction.PatternTime * 1000, null);
							});

							MistyState.GetCharacterState().AnimationLED = ledTransitionAction;
						}
					}
				}

				//Move arms on top of animation script!
				if (!string.IsNullOrWhiteSpace(animationRequest.ArmLocation))
				{
					_ = Task.Run(async () =>
					{
						if (animationRequest.ArmActionDelay > 0)
						{
							await Task.Delay((int)(animationRequest.ArmActionDelay * 1000));
						}
						ArmManager.HandleArmAction(animationRequest, _currentConversationData);
					});
				}

				//Move head on top of animation script!
				if (!string.IsNullOrWhiteSpace(animationRequest.HeadLocation))
				{
					_ = Task.Run(async () =>
					{
						MistyState.RegisterEvent(Triggers.ObjectSeen);
						if (animationRequest.HeadActionDelay > 0)
						{
							await Task.Delay((int)(animationRequest.HeadActionDelay * 1000));
						}
						HeadManager.HandleHeadAction(animationRequest, _currentConversationData);
					});
				}

				//If animation is shorter than audio, there could be some oddities in conversations... should we still allow it?
				//int interactionTimeoutMs = newInteraction.InteractionFailedTimeout <= 0 ? 100 : (int)(newInteraction.InteractionFailedTimeout * 1000);
				//_noInteractionTimer = new Timer(CommunicationBreakdownCallback, UniqueAnimationId, interactionTimeoutMs, Timeout.Infinite);
				/*
				 * TODO Cleanup to work better, but for now, with scripts and animations, this causes confusion with multi triggers
				if (interaction.StartListening && !hasAudio)
				{ 
					//Can still listen without speaking
					switch (CharacterParameters.SpeechRecognitionService.Trim().ToLower())
					{
						case "googleonboard":
							_ = Robot.CaptureSpeechGoogleAsync(false, (int)(animationRequest.ListenTimeout * 1000), (int)(animationRequest.SilenceTimeout * 1000), CharacterParameters.GoogleSpeechRecognitionParameters.SubscriptionKey, CharacterParameters.GoogleSpeechRecognitionParameters.SpokenLanguage);
							break;
						case "azureonboard":
							_ = Robot.CaptureSpeechAzureAsync(false, (int)(animationRequest.ListenTimeout * 1000), (int)(animationRequest.SilenceTimeout * 1000), CharacterParameters.AzureSpeechRecognitionParameters.SubscriptionKey, CharacterParameters.AzureSpeechRecognitionParameters.Region, CharacterParameters.AzureSpeechRecognitionParameters.SpokenLanguage);
							break;
						case "vosk":
							_ = Robot.CaptureSpeechVoskAsync(false, (int)(animationRequest.ListenTimeout * 1000), (int)(animationRequest.SilenceTimeout * 1000));
							break;
						case "deepspeech":
							_ = Robot.CaptureSpeechDeepSpeechAsync(false, (int)(animationRequest.ListenTimeout * 1000), (int)(animationRequest.SilenceTimeout * 1000));
							break;
						default:
							_ = Robot.CaptureSpeechAsync(false, true, (int)(animationRequest.ListenTimeout * 1000), (int)(animationRequest.SilenceTimeout * 1000), null);
							break;
					}
				}*/

			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Interaction: {interaction?.Name} | Animation Id: { interaction.Animation} | Exception while attempting to process animation request callback.", ex);
			}
		}

		public void TriggerAnimationComplete(Interaction interaction)
		{
			InteractionEnded?.Invoke(this, interaction?.Name);
		}

		private void QueueAndRunInteraction(Interaction interaction)
		{
			try
			{
				Robot.SkillLogger.Log($"QUEUEING NEXT INTERACTION : {interaction.Name}");
				_interactionQueue.Enqueue(interaction);

				RunNextAnimation();
				
				//Eventually the Timeout trigger will be sent if no other intents are handled...				
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log($"Interaction: {interaction?.Name} | Failed attempting to handle phrase in AnimatePhrase.", ex);

				ConversationEnded?.Invoke(this, DateTime.Now);
			}
		}

		#endregion

		#region Speech event handlers

		private void SpeechManager_PreSpeechCompleted(object sender, IAudioPlayCompleteEvent e)
		{
			_waitingOnPrespeech = false;
		}

		private async void SpeechManager_KeyPhraseRecognized(object sender, IKeyPhraseRecognizedEvent keyPhraseEvent)
		{
			if (MistyState.GetCharacterState() == null || keyPhraseEvent == null)
			{
				return;
			}

			MistyState.GetCharacterState().KeyPhraseRecognized = (KeyPhraseRecognizedEvent)keyPhraseEvent;
			if (!await SendManagedResponseEvent(new TriggerData(keyPhraseEvent?.Confidence.ToString(), "", Triggers.KeyPhraseRecognized)))
			{
				await SendManagedResponseEvent(new TriggerData(keyPhraseEvent?.Confidence.ToString(), "", Triggers.KeyPhraseRecognized), true);
			}
			KeyPhraseRecognized?.Invoke(this, keyPhraseEvent);
		}
		
		private async void SpeechManager_StartedProcessingVoice(object sender, IVoiceRecordEvent e)
		{
			_processingVoice = true;
			StartedProcessingVoice?.Invoke(this, e);

			if (CharacterParameters.UsePreSpeech && CurrentInteraction.UsePreSpeech)
			{
				string[] preSpeechOverrides = null;
				if (!string.IsNullOrWhiteSpace(CurrentInteraction.PreSpeechPhrases))
				{
					string[] preSpeechStrings = CurrentInteraction.PreSpeechPhrases.Replace(Environment.NewLine, "").Split(";");
					if (preSpeechStrings != null && preSpeechStrings.Length > 0)
					{
						preSpeechOverrides = preSpeechStrings;
					}
				}

				if ((preSpeechOverrides == null || preSpeechOverrides.Length == 0) &&
					(CharacterParameters.PreSpeechList != null && CharacterParameters.PreSpeechList.Count > 0))
				{
					preSpeechOverrides = CharacterParameters.PreSpeechList.ToArray();
				}

				if (preSpeechOverrides != null && preSpeechOverrides.Length > 0)
				{
					bool changeAnimationMovements = false;
					AnimationRequest animation = null;
					AnimationRequest preSpeechAnimation = null;
					if (!string.IsNullOrWhiteSpace(CurrentInteraction.PreSpeechAnimation) && CurrentInteraction.PreSpeechAnimation != "None")
					{
						if ((preSpeechAnimation = _currentConversationData.Animations.FirstOrDefault(x => x.Id == CurrentInteraction.PreSpeechAnimation)) != null)
						{
							changeAnimationMovements = true;
							animation = new AnimationRequest(preSpeechAnimation);
						}
					}
					
					Interaction interaction = new Interaction(CurrentInteraction);
					string selectedPhrase = preSpeechOverrides[_random.Next(0, preSpeechOverrides.Length - 1)];
					
					bool runningInitScript = false;
					
					if (_processingVoice && !string.IsNullOrWhiteSpace(CurrentInteraction.PreSpeechScript))
					{
						runningInitScript = true;
						//Stop any running scripts from previous animations
						//don't await completion of those commands?
						HeadManager.StopMovement();
						ArmManager.StopMovement();
						_ = AnimationManager.StopRunningAnimationScripts();

						if (_processingVoice)
						{
							_waitingOnPrespeech = true;

							_ = AnimationManager.RunAnimationScript(CurrentInteraction.PreSpeechScript, false, _currentAnimation, CurrentInteraction, _currentConversationData);
						}						
					}
					if (_processingVoice && changeAnimationMovements)
					{
						animation.Speak = selectedPhrase;
						if (!runningInitScript)
						{
							//Stop any running scripts from previous animations
							//don't await completion of those commands?
							HeadManager.StopMovement();
							ArmManager.StopMovement();
							_ = AnimationManager.StopRunningAnimationScripts();
						}

						if (_processingVoice)
						{
							_waitingOnPrespeech = true;
							_  = IntermediateAnimationRequestProcessor(animation, interaction, "prespeech");							
						}

					}
					else if (_processingVoice)
					{
						animation = new AnimationRequest(_currentAnimation);

						//just speak and don't move...
						SpeechManager.TryToPersonalizeData(selectedPhrase, animation, interaction, out string newText);

						animation.Speak = newText;
						interaction.StartListening = false;
						Robot.SkillLogger.LogVerbose($"Prespeech saying '{animation?.Speak ?? "nothing"}' and not changing animation.");
						if (_processingVoice)
						{
							_waitingOnPrespeech = true;
							_ = SpeechManager.Speak(animation, interaction, true);
							await ManageJavaScriptRecording($"misty.Speak(\"{animation.Speak}\");");
						}
					}
				}
			}
		}

		private async void SpeechManager_CompletedProcessingVoice(object sender, IVoiceRecordEvent e)
		{
			_processingVoice = false;
			CompletedProcessingVoice?.Invoke(this, e);
		}

		private void SpeechManager_KeyPhraseRecognitionOn(object sender, bool e)
		{
			KeyPhraseRecognitionOn?.Invoke(this, e);
		}

		private async void SpeechManager_StoppedListening(object sender, IVoiceRecordEvent e)
		{
			StoppedListening?.Invoke(this, e);
			_ = SpeechManager.UpdateKeyPhraseRecognition(CurrentInteraction, false);			
		}

		private async void SpeechManager_StartedListening(object sender, DateTime e)
		{
			StartedListening?.Invoke(this, e);
	
			if (!string.IsNullOrWhiteSpace(CurrentInteraction.ListeningScript))
			{
				_ = AnimationManager.RunAnimationScript(CurrentInteraction.ListeningScript, false, _currentAnimation, CurrentInteraction, _currentConversationData);
			}
			
			AnimationRequest listeningAnimation;
			if (CurrentInteraction.ListeningAnimation != null)
			{
				if ((listeningAnimation = _currentConversationData.Animations.FirstOrDefault(x => x.Id == CurrentInteraction.ListeningAnimation)) != null)
				{
					AnimationRequest listeningCopy = new AnimationRequest(listeningAnimation);
					_ = IntermediateAnimationRequestProcessor(listeningCopy, CurrentInteraction, "listening");
				}
			}
		}
	
		private async void SpeechManager_StoppedSpeaking(object sender, IAudioPlayCompleteEvent e)
		{
			_ = SendManagedResponseEvent(new TriggerData(e?.Name, "", Triggers.AudioCompleted));
			StoppedSpeaking?.Invoke(this, e);
			
			_ = SpeechManager.UpdateKeyPhraseRecognition(CurrentInteraction, CurrentInteraction.StartListening);
		}

		private async void SpeechManager_StartedSpeaking(object sender, string e)
		{
			if (!string.IsNullOrWhiteSpace(e) && CharacterParameters.DisplaySpoken)
			{
				Robot.DisplayText(e, "SpokeText", null);
				_ = Robot.SetTextDisplaySettingsAsync("SpokeText", new TextSettings
				{
					Visible = true
				});
			}
			StartedSpeaking?.Invoke(this, e);
		}

		private async void SpeechManager_SpeechIntent(object sender, TriggerData speechIntent)
		{
			_processingVoice = false;
			if (CharacterParameters.HeardSpeechToScreen && !string.IsNullOrWhiteSpace(speechIntent.Text))
			{
				Robot.DisplayText(speechIntent.Text, "HeardSpeech", null);
				_ = Robot.SetTextDisplaySettingsAsync("HeardSpeech", new TextSettings
				{
					Visible = true
				});
			}

			//New data formats
			if (!await SendManagedResponseEvent(new TriggerData(speechIntent.Text, speechIntent.TriggerFilter, Triggers.SpeechHeard)))
			{
				//old
				//Look up name by id in case this conversation uses old data
				bool triggerSuccessful = false;
				if (_conversationGroup.IntentUtterances.TryGetValue(speechIntent.TriggerFilter, out UtteranceData utteranceData))
				{
					triggerSuccessful = await SendManagedResponseEvent(new TriggerData(speechIntent.Text, utteranceData.Name, Triggers.SpeechHeard));

					if (!triggerSuccessful)
					{
						triggerSuccessful = await SendManagedResponseEvent(new TriggerData(speechIntent.Text, ConversationConstants.HeardUnknownTrigger, Triggers.SpeechHeard));
						if (!triggerSuccessful)
						{
							triggerSuccessful = await SendManagedResponseEvent(new TriggerData(speechIntent.Text, "", Triggers.SpeechHeard));
						}
					}
				}

				if (!triggerSuccessful)
				{
					if (!await SendManagedResponseEvent(new TriggerData(speechIntent.Text, speechIntent.TriggerFilter, Triggers.SpeechHeard), true))
					{
						if (_conversationGroup.IntentUtterances.TryGetValue(speechIntent.TriggerFilter, out UtteranceData utteranceData2))
						{
							triggerSuccessful = await SendManagedResponseEvent(new TriggerData(speechIntent.Text, utteranceData2.Name, Triggers.SpeechHeard), true);
						}

						if (!triggerSuccessful)
						{
							triggerSuccessful = await SendManagedResponseEvent(new TriggerData(speechIntent.Text, ConversationConstants.HeardUnknownTrigger, Triggers.SpeechHeard), true);
							if (!triggerSuccessful)
							{
								triggerSuccessful = await SendManagedResponseEvent(new TriggerData(speechIntent.Text, "", Triggers.SpeechHeard), true);
							}
						}
					}
				}
			}
			SpeechIntentEvent?.Invoke(this, speechIntent);
		}

		#endregion

		#region Empty and external trigger only event handlers
		
		//Non-trigger events from other classes, phase out and use MistyState
		public void HandleHeadRollEvent(object sender, IActuatorEvent e)
		{
			HeadRollActuatorEvent?.Invoke(this, e);
		}

		public void HandleHeadYawEvent(object sender, IActuatorEvent e)
		{
			HeadYawActuatorEvent?.Invoke(this, e);
		}

		public void HandleHeadPitchEvent(object sender, IActuatorEvent e)
		{
			HeadPitchActuatorEvent?.Invoke(this, e);
		}

		public void HandleLeftArmEvent(object sender, IActuatorEvent e)
		{
			LeftArmActuatorEvent?.Invoke(this, e);
		}

		public void HandleRightArmEvent(object sender, IActuatorEvent e)
		{
			RightArmActuatorEvent?.Invoke(this, e);
		}

		public void HandleDriveEncoder(object sender, IDriveEncoderEvent userEvent)
		{
			//TODO
		}

		#endregion

		#region State Event handlers

		public async void HandleTimeOfFlightEvent(object sender, ITimeOfFlightEvent timeOfFlightEvent)
		{
			await SendManagedResponseEvent(new TriggerData(timeOfFlightEvent.DistanceInMeters.ToString(), timeOfFlightEvent.SensorPosition.ToString(), Triggers.TimeOfFlightRange));
			TimeOfFlightEvent?.Invoke(this, timeOfFlightEvent);
		}

		public async void HandleArTagEvent(object sender, IArTagDetectionEvent arTagEvent)
		{
			if (MistyState.GetCharacterState()?.ArTagEvent == null)
			{
				return;
			}
			if (!await SendManagedResponseEvent(new TriggerData(arTagEvent.TagId.ToString(), arTagEvent.TagId.ToString(), Triggers.ArTagSeen)))
			{
				if (!await SendManagedResponseEvent(new TriggerData(arTagEvent.TagId.ToString(), arTagEvent.TagId.ToString(), Triggers.ArTagSeen), true))
				{
					if (!await SendManagedResponseEvent(new TriggerData(arTagEvent.TagId.ToString(), "", Triggers.ArTagSeen)))
					{					
						await SendManagedResponseEvent(new TriggerData(arTagEvent.TagId.ToString(), "", Triggers.ArTagSeen), true);
					}
				}
			}

			ArTagEvent?.Invoke(this, arTagEvent);
		}

		public async void HandleQrTagEvent(object sender, IQrTagDetectionEvent qrTagEvent)
		{
			if (MistyState.GetCharacterState()?.QrTagEvent == null)
			{
				return;
			}

			if (!await SendManagedResponseEvent(new TriggerData(qrTagEvent.DecodedInfo?.ToString(), qrTagEvent.DecodedInfo?.ToString(), Triggers.QrTagSeen)))
			{
				if (!await SendManagedResponseEvent(new TriggerData(qrTagEvent.DecodedInfo?.ToString(), qrTagEvent.DecodedInfo?.ToString(), Triggers.QrTagSeen), true))
				{
					if (!await SendManagedResponseEvent(new TriggerData(qrTagEvent.DecodedInfo.ToString(), "", Triggers.QrTagSeen)))
					{					
						await SendManagedResponseEvent(new TriggerData(qrTagEvent.DecodedInfo.ToString(), "", Triggers.QrTagSeen), true);
					}
				}
			}

			QrTagEvent?.Invoke(this, qrTagEvent);
		}

		public async void HandleSerialMessageEvent(object sender, ISerialMessageEvent serialMessageEvent)
		{
			if (MistyState.GetCharacterState()?.SerialMessageEvent == null)
			{
				return;
			}

			if (!await SendManagedResponseEvent(new TriggerData(serialMessageEvent.Message, serialMessageEvent.Message, Triggers.SerialMessage)))
			{				
				if (!await SendManagedResponseEvent(new TriggerData(serialMessageEvent.Message, serialMessageEvent.Message, Triggers.SerialMessage), true))
				{
					if (!await SendManagedResponseEvent(new TriggerData(serialMessageEvent.Message, "", Triggers.SerialMessage)))
					{
						await SendManagedResponseEvent(new TriggerData(serialMessageEvent.Message, "", Triggers.SerialMessage), true);
					}
				}
			}

			SerialMessageEvent?.Invoke(this, serialMessageEvent);
		}

		public async void HandleFaceRecognitionEvent(object sender, IFaceRecognitionEvent faceRecognitionEvent)
		{
			string _lastKnownFace = MistyState.GetCharacterState().LastKnownFaceSeen;
			if (MistyState.GetCharacterState()?.FaceRecognitionEvent == null)
			{
				return;
			}

			if (faceRecognitionEvent.Label != ConversationConstants.UnknownPersonFaceLabel)
			{
				MistyState.GetCharacterState().LastKnownFaceSeen = faceRecognitionEvent.Label;
			}

			bool sentMsg = false;
			if (faceRecognitionEvent.Label == ConversationConstants.UnknownPersonFaceLabel)
			{
				sentMsg = await SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, ConversationConstants.SeeUnknownFaceTrigger, Triggers.FaceRecognized));

				if(!sentMsg)
				{
					sentMsg = await SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, ConversationConstants.SeeUnknownFaceTrigger, Triggers.FaceRecognized), true);
				}
			}
			else
			{
				if (!await SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, faceRecognitionEvent.Label, Triggers.FaceRecognized)))
				{
					if (_lastKnownFace != MistyState.GetCharacterState().LastKnownFaceSeen)
					{
						sentMsg = await SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, ConversationConstants.SeeNewFaceTrigger, Triggers.FaceRecognized));
						if (!sentMsg)
						{
							sentMsg = await SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, ConversationConstants.SeeNewFaceTrigger, Triggers.FaceRecognized), true);
						}
					}
				}

				if (!sentMsg)
				{
					sentMsg = await SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, ConversationConstants.SeeKnownFaceTrigger, Triggers.FaceRecognized));
					if (!sentMsg)
					{
						sentMsg = await SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, ConversationConstants.SeeKnownFaceTrigger, Triggers.FaceRecognized), true);
					}
				}
			}

			if (!sentMsg)
			{
				sentMsg = await SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, "", Triggers.FaceRecognized));
				if (!sentMsg)
				{
					sentMsg = await SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, "", Triggers.FaceRecognized), true);
				}				
			}
			
			FaceRecognitionEvent?.Invoke(this, faceRecognitionEvent);
		}
		
		//Callback from external skills or scripts 
		private async void HandleExternalEvent(IUserEvent userEvent)
		{
			//TODO Deny cross robot communication per robot

			//Pull out the intent and text, required
			if (userEvent.TryGetPayload(out IDictionary<string, object> payload))
			{
				//Can override other triggers

				payload.TryGetValue("Trigger", out object triggerObject);
				payload.TryGetValue("TriggerFilter", out object triggerFilterObject);
				payload.TryGetValue("Text", out object textObject);
				string trigger = triggerObject == null ? Triggers.ExternalEvent : Convert.ToString(triggerObject);
				string text = textObject == null ? null : Convert.ToString(textObject);
				string triggerFilter = triggerFilterObject == null ? null : Convert.ToString(triggerFilterObject);

				//HACK for user data, need to know if succeeded, to send again?
				if (trigger == Triggers.SpeechHeard && triggerFilter == ConversationConstants.HeardUnknownTrigger
					&& (!string.IsNullOrWhiteSpace(text) && text.ToLower().Trim() != ConversationConstants.HeardUnknownTrigger))
				{
					if(SpeechManager.HandleExternalSpeech(text))
					{
						return;
					}
				}
				else
				{
					SpeechManager.CancelSpeechProcessing();
				}

				if (!await SendManagedResponseEvent(new TriggerData(text, triggerFilter, trigger)))
				{
					if (!await SendManagedResponseEvent(new TriggerData(text, ConversationConstants.HeardUnknownTrigger, trigger)))
					{
						if (!await SendManagedResponseEvent(new TriggerData(text, "", trigger)))
						{
							if (!await SendManagedResponseEvent(new TriggerData(text, triggerFilter, trigger), true))
							{
								if (!await SendManagedResponseEvent(new TriggerData(text, ConversationConstants.HeardUnknownTrigger, trigger), true))
								{
									await SendManagedResponseEvent(new TriggerData(text, "", trigger), true);
								}
							}
						}
					}
				}
				
				ExternalEvent?.Invoke(this, userEvent);
			}
		}

		public async void HandlePersonObjectEvent(object sender, IObjectDetectionEvent objectDetectionEvent)
		{
			if (!await SendManagedResponseEvent(new TriggerData(objectDetectionEvent.Confidence.ToString(), objectDetectionEvent.Description, Triggers.ObjectSeen)))
			{
				if (!await SendManagedResponseEvent(new TriggerData(objectDetectionEvent.Confidence.ToString(), objectDetectionEvent.Description, Triggers.ObjectSeen), true))
				{
					if (!await SendManagedResponseEvent(new TriggerData(objectDetectionEvent.Confidence.ToString(), "", Triggers.ObjectSeen)))
					{					
						await SendManagedResponseEvent(new TriggerData(objectDetectionEvent.Confidence.ToString(), "", Triggers.ObjectSeen), true);
					}
				}
			}

			PersonObjectEvent?.Invoke(this, objectDetectionEvent);
			ObjectEvent?.Invoke(this, objectDetectionEvent);
		}

		public async void HandleNonPersonObjectEvent(object sender, IObjectDetectionEvent objectDetectionEvent)
		{
			if (!await SendManagedResponseEvent(new TriggerData(objectDetectionEvent.Confidence.ToString(), objectDetectionEvent.Description, Triggers.ObjectSeen)))
			{
				if (!await SendManagedResponseEvent(new TriggerData(objectDetectionEvent.Confidence.ToString(), objectDetectionEvent.Description, Triggers.ObjectSeen), true))
				{
					if (!await SendManagedResponseEvent(new TriggerData(objectDetectionEvent.Confidence.ToString(), "", Triggers.ObjectSeen)))
					{
						await SendManagedResponseEvent(new TriggerData(objectDetectionEvent.Confidence.ToString(), "", Triggers.ObjectSeen), true);
					}
				}
			}

			NonPersonObjectEvent?.Invoke(this, objectDetectionEvent);
			ObjectEvent?.Invoke(this, objectDetectionEvent);
		}

		public async void HandleCapTouchEvent(object sender, ICapTouchEvent capTouchEvent)
		{
			if (!await SendManagedResponseEvent(new TriggerData(capTouchEvent.IsContacted ? "Contacted" : "Released", capTouchEvent.SensorPosition.ToString(), capTouchEvent.IsContacted ? Triggers.CapTouched : Triggers.CapReleased)))
			{
				if (!await SendManagedResponseEvent(new TriggerData(capTouchEvent.IsContacted ? "Contacted" : "Released", capTouchEvent.SensorPosition.ToString(), capTouchEvent.IsContacted ? Triggers.CapTouched : Triggers.CapReleased), true))
				{
					if (!await SendManagedResponseEvent(new TriggerData(capTouchEvent.IsContacted ? "Contacted" : "Released", "", capTouchEvent.IsContacted ? Triggers.CapTouched : Triggers.CapReleased)))
					{					
						await SendManagedResponseEvent(new TriggerData(capTouchEvent.IsContacted ? "Contacted" : "Released", "", capTouchEvent.IsContacted ? Triggers.CapTouched : Triggers.CapReleased), true);
					}
				}
			}
			CapTouchEvent?.Invoke(this, capTouchEvent);
		}

		public async void HandleBumperEvent(object sender, IBumpSensorEvent bumpEvent)
		{

			if (!await SendManagedResponseEvent(new TriggerData(bumpEvent.IsContacted ? "Contacted" : "Released", bumpEvent.SensorPosition.ToString(), bumpEvent.IsContacted ? Triggers.BumperPressed : Triggers.BumperReleased)))
			{
				if (!await SendManagedResponseEvent(new TriggerData(bumpEvent.IsContacted ? "Contacted" : "Released", bumpEvent.SensorPosition.ToString(), bumpEvent.IsContacted ? Triggers.BumperPressed : Triggers.BumperReleased), true))
				{
					if (!await SendManagedResponseEvent(new TriggerData(bumpEvent.IsContacted ? "Contacted" : "Released", "", bumpEvent.IsContacted ? Triggers.BumperPressed : Triggers.BumperReleased)))
					{
						await SendManagedResponseEvent(new TriggerData(bumpEvent.IsContacted ? "Contacted" : "Released", "", bumpEvent.IsContacted ? Triggers.BumperPressed : Triggers.BumperReleased), true);
					}
				}
			}
			BumperEvent?.Invoke(this, bumpEvent);
		}

		#endregion

		#region Helpers

		private void StreamAndLogInteraction(string message, Exception ex = null)
		{
			if (CharacterParameters.LogInteraction)
			{
				Robot.SkillLogger.Log(message, ex);
			}

			if (CharacterParameters.StreamInteraction)
			{
				if (ex != null)
				{
					message += $"|Exception:{ex.Message}";
				}
				Robot.PublishMessage(message, null);
			}
		}

		private bool IsTOFCounterMatch(TOFCounter tofCounter, int times, int durationMs)
		{
			tofCounter.Count++;
			if (tofCounter.Count >= times)
			{
				if (times == 1 || durationMs <= 0)
				{
					tofCounter.Count = 0;
					return true;
				}
				else if (tofCounter.Started < DateTime.Now.AddMilliseconds(durationMs))
				{
					tofCounter.Count = 0;
					return true;
				}
				tofCounter.Count = 0;
			}
			else if (tofCounter.Count == 1)
			{
				tofCounter.Started = DateTime.Now;
			}
			return false;
		}

		private bool IsValidCompare(TriggerData triggerData, string sensor, string equality, double value, TOFCounter tofCounter, int times, int durationMs)
		{
			//Comes to comparison as...
			//TriggerFilter = position
			//Text = distance errorcode
			bool validCompare = false;
			string filter = triggerData.TriggerFilter.ToLower();
			if (filter == sensor ||
				(sensor == "frontrange" && (filter == "frontright" || filter == "frontcenter" || filter == "frontleft") ||
				(sensor == "backrange" && (filter == "backright" || filter == "backleft"))))
			{
				string distanceString = "";
				string errorCode = "0";

				string[] textField = triggerData.Text.Split(" ");
				if (textField.Length == 1)
				{
					distanceString = textField[0].Trim();
				}
				else if (textField.Length == 2)
				{
					distanceString = textField[0].Trim();
					errorCode = textField[1].Trim();
				}
				else
				{
					return false;
				}

				double distance = Convert.ToDouble(distanceString);

				//TODO Deal with warning error codes that should be valid
				switch (equality)
				{
					case "<":
						validCompare = distance < value && errorCode == "0";
						break;
					case "<=":
						validCompare = distance <= value && errorCode == "0";
						break;
					case ">":
						validCompare = distance > value && errorCode == "0";
						break;
					case ">=":
						validCompare = distance >= value && errorCode == "0";
						break;
					case "==":
						validCompare = value == distance && errorCode == "0";
						break;
					case "!=":
						validCompare = value != distance && errorCode == "0";
						break;
					case "X":
						validCompare = errorCode != "0";
						break;
				}

				if (validCompare)
				{
					return IsTOFCounterMatch(tofCounter, times, durationMs);
				}
			}
			return false;
		}

		private bool TryGetPatternToTransition(string pattern, out LEDTransition ledTransition)
		{
			switch (pattern?.ToLower())
			{
				case "blink":
					ledTransition = LEDTransition.Blink;
					return true;
				case "breathe":
					ledTransition = LEDTransition.Breathe;
					return true;
				case "none":
					ledTransition = LEDTransition.None;
					return true;
				case "transitOnce":
					ledTransition = LEDTransition.TransitOnce;
					return true;
				default:
					ledTransition = LEDTransition.None;
					return false;
			}
		}
		
		private bool TryGetAdjustedDistance(ITimeOfFlightEvent tofEvent, out double distance)
		{
			distance = 0;
			// From Testing, using this pattern for return data
			//   0 = valid range data
			// 101 = sigma fail - lower confidence but most likely good
			// 104 = Out of bounds - Distance returned is greater than distance we are confident about, but most likely good - error codes can be returned in distance field at this time :(  so ignore error code range
			if (tofEvent.Status == 0 ||
				(tofEvent.Status == 101 && tofEvent.DistanceInMeters >= 1) ||
				tofEvent.Status == 104)
			{
				distance = tofEvent.DistanceInMeters;
			}
			else if (tofEvent.Status == 102)
			{
				//102 generally indicates nothing substantial is in front of the robot so the TOF is returning the floor as a close distance
				//So ignore the distance returned and just set to 2 meters
				distance = 2;
			}
			else
			{
				//TOF returning uncertain data or really low confidence in distance, ignore value 
				return false;
			}
			return true;
		}
		
		#endregion

		#region Timer and timeout callbacks

		private async void IntentTimeoutTimerCallback(object timerData)
		{
			if (timerData != null && ((TimerTriggerData)timerData)?.AnimationId == UniqueAnimationId)
			{
				if (!await SendManagedResponseEvent(new TriggerData(DateTime.Now.ToString(), "", Triggers.Timeout)))
				{
					await SendManagedResponseEvent(new TriggerData(DateTime.Now.ToString(), "", Triggers.Timeout), true);
				}
			}
		}

		private async void IntentTriggerTimerCallback(object timerData)
		{
			if (timerData != null && ((TimerTriggerData)timerData)?.AnimationId == UniqueAnimationId)
			{
				if (!await SendManagedResponseEvent(new TriggerData(DateTime.Now.ToString(), "", Triggers.Timer)))
				{
					await SendManagedResponseEvent(new TriggerData(DateTime.Now.ToString(), "", Triggers.Timer), true);
				}
			}
		}

		private async void CommunicationBreakdownCallback(object timerData)
		{
			//TODO Experiment! If in the middle of processing speech, check again in a bit
			//Do we want this?  config?
			if (!CurrentInteraction.AllowVoiceProcessingOverride && _processingVoice && _currentProcessingVoiceWaits < MaxProcessingVoiceWaits)
			{
				_currentProcessingVoiceWaits++;
				_noInteractionTimer?.Dispose();
				_noInteractionTimer = new Timer(CommunicationBreakdownCallback, UniqueAnimationId, DelayBetweenProcessingVoiceChecksMs, Timeout.Infinite);
				return;
			}

			_processingVoice = false;
			_currentProcessingVoiceWaits = 0;
			if (timerData != null && (Guid)timerData == UniqueAnimationId)
			{
				if (string.IsNullOrWhiteSpace(_currentConversationData.NoTriggerInteraction))
				{
					Robot.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Interaction timeout. No intents were triggered, stopping conversation.");
					TriggerAnimationComplete(CurrentInteraction);
					await StopConversation();
					await Task.Delay(5000);
					TriggerConversationCleanup?.Invoke(this, DateTime.UtcNow);
				}
				else
				{
					Robot.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Interaction timeout. No intents were triggered, going to default interaction {_currentConversationData.NoTriggerInteraction}.");
					_ = GoToNextAnimation(new List<TriggerActionOption>{ new TriggerActionOption
					{
						GoToConversation = _currentConversationData.Id,
						GoToInteraction = _currentConversationData.NoTriggerInteraction,
						InterruptCurrentAction = true,
					} });

					WaitingForOverrideTrigger = false;
					_ignoreTriggeringEvents = false;
				}
			}
		}

		#endregion
		
		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					Robot.UpdateHazardSettings(new HazardSettings { RevertToDefault = true }, null);
					IgnoreEvents();
					_stateEventTimer?.Dispose();
					_noInteractionTimer?.Dispose();
					_triggerActionTimeoutTimer?.Dispose();
					_timerTriggerTimer?.Dispose();
					_pollRunningSkillsTimer?.Dispose();
					ClearRegistrations();
					ArmManager.StopMovement();
					HeadManager.StopMovement();
					ArmManager.Dispose();
					HeadManager.Dispose();
					SpeechManager.Dispose();
					AnimationManager.Dispose();
					TimeManager.Dispose();
					MistyState?.Dispose();
					Robot.Stop(null);
					Robot.Halt(new List<MotorMask> { MotorMask.LeftArm, MotorMask.RightArm }, null);					
				}

				_isDisposed = true;
			}
		}

		public virtual void Dispose()
		{
			Dispose(true);
		}

		#endregion
	}
}
 
 
 
 