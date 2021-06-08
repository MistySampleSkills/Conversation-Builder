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
using System.Text.RegularExpressions;
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
using SkillTools.AssetTools;
using MistyCharacter.SpeechIntent;

namespace MistyCharacter
{
	public abstract class BaseCharacter : IBaseCharacter
	{
		//Allow others to register for these... pass on the event data
		public event EventHandler<IFaceRecognitionEvent> FaceRecognitionEvent;
		public event EventHandler<ICapTouchEvent> CapTouchEvent;
		public event EventHandler<IBumpSensorEvent> BumperEvent;
		public event EventHandler<IBatteryChargeEvent> BatteryChargeEvent;
		public event EventHandler<IQrTagDetectionEvent> QrTagEvent;
		public event EventHandler<IArTagDetectionEvent> ArTagEvent;
		public event EventHandler<ISerialMessageEvent> SerialMessageEvent;
		public event EventHandler<IUserEvent> ExternalEvent;
		public event EventHandler<IObjectDetectionEvent> ObjectEvent;
		public event EventHandler<TriggerData> SpeechIntentEvent;		
		public event EventHandler<TriggerData> ResponseEventReceived;		
		public event EventHandler<string> StartedSpeaking;
		public event EventHandler<IAudioPlayCompleteEvent> StoppedSpeaking;
		public event EventHandler<DateTime> StartedListening;
		public event EventHandler<IVoiceRecordEvent> StoppedListening;
		public event EventHandler<bool> KeyPhraseRecognitionOn;
		public event EventHandler<IDriveEncoderEvent> DriveEncoder;
		public event EventHandler<DateTime> ConversationStarted;
		public event EventHandler<DateTime> ConversationEnded;
		public event EventHandler<DateTime> InteractionStarted;
		public event EventHandler<DateTime> InteractionEnded;
		public event EventHandler<IKeyPhraseRecognizedEvent> KeyPhraseRecognized;		
		public event EventHandler<IVoiceRecordEvent> CompletedProcessingVoice;
		public event EventHandler<IVoiceRecordEvent> StartedProcessingVoice;

		private const int MaxProcessingVoiceWaits = 30;
		private const int DelayBetweenProcessingVoiceChecksMs = 100;
		private const string MissingInlineData = "Missing";
		private int _currentProcessingVoiceWaits = 0;
		private KeyValuePair<DateTime, TriggerData> _latestTriggerMatchData = new KeyValuePair<DateTime, TriggerData>();
		private IList<string> _skillsToStop = new List<string>();
		protected IDictionary<string, object> OriginalParameters = new Dictionary<string, object>();
		protected IRobotMessenger Misty;
		protected AzureSpeechParameters AzureSpeechParameters;
		protected GoogleSpeechParameters GoogleSpeechParameters;
		protected ISDKLogger Logger;
		protected CharacterParameters CharacterParameters { get; private set; } = new CharacterParameters();
		protected ConcurrentDictionary<string, AnimationRequest> EmotionAnimations = new ConcurrentDictionary<string, AnimationRequest>();
		
		public CharacterState CharacterState { get; protected set; }  = new CharacterState();
		public CharacterState PreviousState { get; private set; } = new CharacterState();
		public CharacterState StateAtAnimationStart { get; private set; } = new CharacterState();
		public Interaction CurrentInteraction { get; private set; } = new Interaction();

		private IList<string> _listeningCallbacks = new List<string>();
		private IList<string> _audioAnimationCallbacks = new List<string>();

		private Timer _noInteractionTimer;
		private Timer _triggerActionTimeoutTimer;
		private Timer _timerTriggerTimer;
		private Timer _pollRunningSkillsTimer;
		private AssetWrapper AssetWrapper;
		private ISpeechManager SpeechManager;
		private IArmManager ArmManager;
		private IHeadManager HeadManager;
		private IAnimationManager AnimationManager;
		private ITimeManager TimeManager;
		private IEmotionManager EmotionManager;
		private ISpeechIntentManager SpeechIntentManager;
		private IList<string> LiveTriggers = new List<string>();
		private IList<string> _replacementValues = new List<string> { "face", "filter", "qrcode", "arcode", "text", "intent", "time", "robotname" };		
		private ConversationGroup _conversationGroup = new ConversationGroup();
		private IList<GenericDataStore> _genericDataStores  = new List<GenericDataStore>();
		private AnimationRequest _currentAnimation;
		private bool _processingVoice = false;
		private ManagerConfiguration _managerConfiguration;
		private object _runningSkillLock = new object();
		private IList<string> _runningSkills = new List<string>();
		private ConversationData _currentConversationData;
		private object _lockWaitingOnResponse = new object();
		private SemaphoreSlim _processingTriggersSemaphore = new SemaphoreSlim(1, 1);
		private Random _random = new Random();

		public bool WaitingForOverrideTrigger { get; private set; }
		private bool _ignoreTriggeringEvents;
		
		private object _lockListenerData = new object();

		protected ConcurrentQueue<Interaction> InteractionQueue = new ConcurrentQueue<Interaction>();
		protected ConcurrentQueue<Interaction> InteractionPriorityQueue = new ConcurrentQueue<Interaction>();

		private object _eventsClearedLock = new object();
		protected Guid UniqueAnimationId { get; private set; } = Guid.NewGuid();

		public BaseCharacter(IRobotMessenger misty, 
			CharacterParameters characterParameters,
			IDictionary<string, object> originalParameters,
			ManagerConfiguration managerConfiguration = null)
		{
			Misty = misty;
			OriginalParameters = originalParameters;
			CharacterParameters = characterParameters;
			AzureSpeechParameters = characterParameters.AzureSpeechParameters;
			GoogleSpeechParameters = characterParameters.GoogleSpeechParameters;
			_managerConfiguration = managerConfiguration;
		}

		public async Task<bool> Initialize()
		{
			try
			{
				Logger = Misty.SkillLogger;
				PopulateEmotionDefaults();

				Misty.UnregisterAllEvents(null);
                await Task.Delay(1000);
				/*Misty.UnregisterEvent("BumpSensor", null);
				Misty.UnregisterEvent("CapTouch", null);
				Misty.UnregisterEvent("Battery", null);
				Misty.UnregisterEvent("ArTag", null);
				Misty.UnregisterEvent("QrTag", null);
				Misty.UnregisterEvent("SerialMessage", null);
				Misty.UnregisterEvent("FaceRecognition", null);
				Misty.UnregisterEvent("ExternalEvent", null);*/

				AssetWrapper = new AssetWrapper(Misty);
				_ = RefreshAssetLists();

				TimeManager = _managerConfiguration?.TimeManager ?? new TimeManager(Misty, OriginalParameters, CharacterParameters);
				await TimeManager.Initialize();

				ArmManager = _managerConfiguration?.ArmManager ?? new ArmManager(Misty, OriginalParameters, CharacterParameters);
				await ArmManager.Initialize();

				HeadManager = _managerConfiguration?.HeadManager ?? new HeadManager(Misty, OriginalParameters, CharacterParameters);
				await HeadManager.Initialize();

				SpeechIntentManager = _managerConfiguration?.SpeechIntentManager ?? new SpeechIntentManager(Misty, CharacterParameters.ConversationGroup.IntentUtterances, CharacterParameters.ConversationGroup.GenericDataStores);
				
				SpeechManager = _managerConfiguration?.SpeechManager ?? new SpeechManager(Misty, OriginalParameters, CharacterParameters, SpeechIntentManager);
				await SpeechManager.Initialize();

				//TODO Pass in?
				AnimationManager = new AnimationManager(Misty, OriginalParameters, CharacterParameters);

				IgnoreEvents();

				SpeechManager.SpeechIntent += SpeechManager_SpeechIntent;
				
				LogEventDetails(Misty.RegisterBumpSensorEvent(BumpSensorCallback, 100, true, null, "BumpSensor", null));

				LogEventDetails(Misty.RegisterCapTouchEvent(CapTouchCallback, 100, true, null, "CapTouch", null));

				LogEventDetails(Misty.RegisterBatteryChargeEvent(BatteryChargeCallback, 1000 * 60, true, null, "Battery", null));

				LogEventDetails(Misty.RegisterArTagDetectionEvent(ArTagCallback, 100, true, "ArTag", null));

				LogEventDetails(Misty.RegisterQrTagDetectionEvent(QrTagCallback, 100, true, "QrTag", null));

				LogEventDetails(Misty.RegisterSerialMessageEvent(SerialMessageCallback, 0, true, "SerialMessage", null));

				LogEventDetails(Misty.RegisterFaceRecognitionEvent(FaceRecognitionCallback, 100, true, null, "FaceRecognition", null));

				LogEventDetails(Misty.RegisterUserEvent("ExternalEvent", UserEventCallback, 0, true, null));

				Misty.StartFaceRecognition(null);
				//TODO These should be configurable
				Misty.StartArTagDetector(7, 140, null); 
				Misty.StartQrTagDetector(null);

				ArmManager.RightArmActuatorEvent += ArmManager_RightArmActuatorEvent;
				ArmManager.LeftArmActuatorEvent += ArmManager_LeftArmActuatorEvent;
				HeadManager.HeadPitchActuatorEvent += HeadManager_HeadPitchActuatorEvent;
				HeadManager.HeadYawActuatorEvent += HeadManager_HeadYawActuatorEvent;
				HeadManager.HeadRollActuatorEvent += HeadManager_HeadRollActuatorEvent;
				HeadManager.ObjectEvent += ObjectCallback;

				SpeechManager.StartedSpeaking += SpeechManager_StartedSpeaking;
				SpeechManager.StoppedSpeaking += SpeechManager_StoppedSpeaking;
				SpeechManager.StartedListening += SpeechManager_StartedListening;
				SpeechManager.StoppedListening += SpeechManager_StoppedListening;
				SpeechManager.KeyPhraseRecognized += SpeechManager_KeyPhraseRecognized;

				SpeechManager.CompletedProcessingVoice += SpeechManager_CompletedProcessingVoice;
				SpeechManager.StartedProcessingVoice += SpeechManager_StartedProcessingVoice;
				SpeechManager.KeyPhraseRecognitionOn += SpeechManager_KeyPhraseRecognitionOn;

				InteractionEnded += RunNextAnimation;

				StreamAndLogInteraction($"Starting Base Character animation processing...");

				_latestTriggerMatchData = new KeyValuePair<DateTime, TriggerData>(DateTime.Now, new TriggerData("", "", Triggers.None));

				//hacky check for running skills, so there may be a 15 second delay if skill shuts down automatically to where we notice and try to restart				
				_pollRunningSkillsTimer = new Timer(UpdateRunningSkillsCallback, null, 15000, Timeout.Infinite);
				
				if (CharacterParameters.ShowListeningIndicator)
				{
					_ = Misty.SetTextDisplaySettingsAsync("Listening", new TextSettings
					{
                        /*Wrap = true,
						Visible = true,
						Weight = 25,
						Size = 25,
						HorizontalAlignment = ImageHorizontalAlignment.Center,
						VerticalAlignment = ImageVerticalAlignment.Bottom,
						Red = 240,
						Green = 240,
						Blue = 240,
						PlaceOnTop = true,
						FontFamily = "Courier New",
						Height = 50*/
                        Wrap = true,
                        Visible = true,
                        Weight = 25,
                        Size = 25,
                        HorizontalAlignment = ImageHorizontalAlignment.Center,
                        VerticalAlignment = ImageVerticalAlignment.Bottom,
                        Red = 240,
                        Green = 240,
                        Blue = 240,
                        PlaceOnTop = true,
                        FontFamily = "Courier New",
                        Height = 30
                    });
				}

				if (CharacterParameters.DisplaySpoken)
				{
					_ = Misty.SetTextDisplaySettingsAsync("SpokeText", new TextSettings
					{
						Wrap = true,
						Visible = true,
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
					_ = Misty.SetTextDisplaySettingsAsync("SpeechText", new TextSettings
					{
						Wrap = true,
						Visible = true,
						Weight = 15,
						Size = 20,
						HorizontalAlignment = ImageHorizontalAlignment.Center,
						VerticalAlignment = ImageVerticalAlignment.Bottom,
						Red = 255,
						Green = 221,
						Blue = 71,
						PlaceOnTop = true,
						FontFamily = "Courier New",
						Height = 250
					});
				}

				ManageListeningDisplay(ListeningState.Waiting);
				_ = ProcessNextAnimationRequest();
				return true;
			}
			catch
			{
				return false;
			}
		}

		private void SpeechManager_KeyPhraseRecognized(object sender, IKeyPhraseRecognizedEvent keyPhraseEvent)
		{
			if (CharacterState == null || keyPhraseEvent == null)
			{
				return;
			}

			CharacterState.KeyPhraseRecognized = (KeyPhraseRecognizedEvent)keyPhraseEvent;			
			if(!SendManagedResponseEvent(new TriggerData(keyPhraseEvent?.Confidence.ToString(), "", Triggers.KeyPhraseRecognized)))
			{
				SendManagedResponseEvent(new TriggerData(keyPhraseEvent?.Confidence.ToString(), "", Triggers.KeyPhraseRecognized), true);
			}
			KeyPhraseRecognized?.Invoke(this, keyPhraseEvent);
		}

		public async void UpdateRunningSkillsCallback(object timerData)
		{
			IList<string> newSkills = new List<string>();
			IGetRunningSkillsResponse response = await Misty.GetRunningSkillsAsync();
			if(response.Data != null && response.Data.Count() > 0)
			{
				foreach (RunningSkillDetails details in response.Data.Distinct())
				{
					newSkills.Add(details.UniqueId.ToString());
				}
			}

			lock(_runningSkillLock)
			{
				_runningSkills = newSkills;
			}

			_pollRunningSkillsTimer = new Timer(UpdateRunningSkillsCallback, null, 15000, Timeout.Infinite);			
		}

		private void SpeechManager_StartedProcessingVoice(object sender, IVoiceRecordEvent e)
		{
			StartedProcessingVoice?.Invoke(this, e);            
            _processingVoice = true;
            ManageListeningDisplay(ListeningState.ProcessingSpeech);
        }

		private void SpeechManager_CompletedProcessingVoice(object sender, IVoiceRecordEvent e)
		{
			_processingVoice = false;
			CompletedProcessingVoice?.Invoke(this, e);
            ManageListeningDisplay(ListeningState.Waiting);
        }

		public void RestartTriggerHandling()
		{
			_processingTriggersSemaphore.Wait();
            try
            {
                WaitingForOverrideTrigger = false;
                _ignoreTriggeringEvents = false;
            }
            catch (Exception ex)
            {
                Misty.SkillLogger.Log($"Failed to restart trigger handling.", ex);
            }
            finally
            {
                _processingTriggersSemaphore.Release();
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
                Misty.SkillLogger.Log($"Failed to restart current interaction.", ex);
            }
			finally
			{
				_processingTriggersSemaphore.Release();
			}
		}

		public void PauseTriggerHandling(bool ignoreTriggeringEvents = true)
		{
			_processingTriggersSemaphore.Wait();
			try
			{
				WaitingForOverrideTrigger = true;
				_ignoreTriggeringEvents = ignoreTriggeringEvents;
			}
			catch (Exception ex)
            {
                Misty.SkillLogger.Log($"Failed to pause trigger handling.", ex);
            }
			finally
			{
				_processingTriggersSemaphore.Release();
			}
		}


		public void SimulateTrigger(TriggerData triggerData, bool setAsOverrideEvent = true)
		{
			triggerData.OverrideIntent = setAsOverrideEvent;
			if(!SendManagedResponseEvent(triggerData))
			{
				SendManagedResponseEvent(triggerData, true);
			}
		}

		public void ChangeVolume(int volume)
		{
			SpeechManager.Volume = volume;
		}

		public async Task ResaveAssetFiles()
		{
			await AssetWrapper.LoadAssets(true);
		}

		public async Task RefreshAssetLists()
		{
			await AssetWrapper.RefreshAssetLists();
		}
		
		public async Task<bool> StartConversation(string conversationId = null, string interactionId = null)
		{
			Misty.StopKeyPhraseRecognition(null);
			Misty.SetFlashlight(false, null);

			if (CharacterParameters.StartVolume != null && CharacterParameters.StartVolume > 0)
			{
				Misty.SetDefaultVolume((int)CharacterParameters.StartVolume, null);
			}

			_conversationGroup = CharacterParameters.ConversationGroup;
			_genericDataStores = CharacterParameters.ConversationGroup.GenericDataStores;

			string startConversation = conversationId ?? _conversationGroup.StartupConversation;
			_currentConversationData = _conversationGroup.Conversations.FirstOrDefault(x => x.Id == startConversation);

			EmotionManager = _managerConfiguration?.EmotionManager ?? new EmotionManager(_currentConversationData.StartingEmotion);

			string startInteraction = interactionId ?? _currentConversationData.StartupInteraction;
			CurrentInteraction = _currentConversationData.Interactions.FirstOrDefault(x => x.Id == startInteraction);
			
			if (CurrentInteraction != null)
			{
				Misty.SkillLogger.Log($"STARTING CONVERSATION");
				Misty.SkillLogger.Log($"Conversation: {_currentConversationData.Name} | Interaction: {CurrentInteraction?.Name} | Going to starting interaction...");				
				if (_currentConversationData.InitiateSkillsAtConversationStart && _currentConversationData.SkillMessages != null)
				{
					foreach (SkillMessage skillMessage in _currentConversationData.SkillMessages)
					{
						if(!string.IsNullOrWhiteSpace(skillMessage.Skill) && 
							!_runningSkills.Contains(skillMessage.Skill) &&
							skillMessage.Skill != "8be20a90-1150-44ac-a756-ebe4de30689e")
						{
							_runningSkills.Add(skillMessage.Skill);
							IDictionary<string, object> payloadData = OriginalParameters;
							Misty.RunSkill(skillMessage.Skill, payloadData, null);

							StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name} | Running skill {skillMessage.Skill}.");
							await Task.Delay(3000);
						}
					}

					await Task.Delay(5000);
				}

				ConversationStarted?.Invoke(this, DateTime.Now);
				QueueInteraction(CurrentInteraction);
				return true;
			}
			return false;
		}

		public void StopConversation(string speak = null)
		{
			InteractionQueue.Clear();
			IgnoreEvents();

			Misty.Speak(string.IsNullOrWhiteSpace(speak) ? "Ending conversation." : speak, true, "InteractionTimeout", null);
			ConversationEnded?.Invoke(this, DateTime.Now);
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
							if (LiveTriggers.Contains(detail.Trigger))
							{
								return;
							}
							LiveTriggers.Add(detail.Trigger);
						}
						StreamAndLogInteraction($"Listening to event type {detail.Trigger}");
					}
				}
				catch
				{

				}
			});
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
							if (LiveTriggers.Contains(detail.Trigger))
							{
								LiveTriggers.Remove(detail.Trigger);
								StreamAndLogInteraction($"Ignoring event type {detail.Trigger}");
								return;
							}
						}
					}
				}
				catch
				{

				}
			});
		}

		private void IgnoreEvents()
		{
			StreamAndLogInteraction($"Ignoring interaction events");
			lock (_eventsClearedLock)
			{
				LiveTriggers.Clear();
			}
		}
		
		private void TryToPersonalizeDataOne(string text, out string newText, out string newImage)
		{
			newText = text;
			newImage = null;
			if (CharacterState == null)
			{
				return;
			}

			if (newText.Contains("{{") && newText.Contains("}}"))
			{
				int replacementItemCount = Regex.Matches(newText, "{{").Count;

				//Loop through all inline text groups
				for (int i = 0; i < replacementItemCount; i++)
				{

					int indexOpen = 0;
					int indexClose = 0;
					string replacementTextList = "";

					indexOpen = newText.IndexOf("{{");
					indexClose = newText.IndexOf("}}");

					if (indexClose - 2 <= indexOpen)
					{
						continue;
					}

					replacementTextList = newText.Substring(indexOpen + 2, (indexClose - 2) - indexOpen);

					if (string.IsNullOrWhiteSpace(replacementTextList))
					{
						continue;
					}
					
					IList<string> optionList = new List<string>();					
					if (!replacementTextList.Contains("||"))
					{
						optionList.Add(replacementTextList);
					}

					if (replacementTextList.Contains("||"))
					{
						string[] dataArray = replacementTextList.ToLower().Trim().Split("||");
						if (dataArray != null && dataArray.Count() > 0)
						{
							foreach (string option in dataArray)
							{
								if (!optionList.Contains(option))
								{
									optionList.Add(option);
								}
							}
						}
					}
                    
					//Loop through the options to find match
					int optionCount = 0;
					bool textChanged = false;
					foreach (string option in optionList)
					{
						if(textChanged)
						{
							break;
						}

						//Extract the replacement Name/Key pair if it exists - old format vs new format
						int nameKeyIndexOpen = option.IndexOf("[[");
						int nameKeyIndexClose = option.IndexOf("]]");

						string replacementNameKey;
						if (nameKeyIndexClose - 2 <= nameKeyIndexOpen)
						{
							replacementNameKey = option;
						}
						else
						{
							replacementNameKey = option.Substring(nameKeyIndexOpen + 2, (nameKeyIndexClose - 2) - nameKeyIndexOpen);
						}

						if (string.IsNullOrWhiteSpace(replacementNameKey))
						{
							continue;
						}
						
						optionCount++;
						//does it contain a :
						if (replacementNameKey.Contains(":"))
						{
							string[] dataArray = replacementNameKey.ToLower().Trim().Split(":");
							if (dataArray != null && dataArray.Count() == 2)
							{
								string userDataName = dataArray[0].Trim().ToLower();

								if (_replacementValues != null &&
								   _replacementValues.Count() > 0 &&
								   _replacementValues.Contains(userDataName))
								{
									//if it is a built in item in the FIRST position, it replaces the NAME with the lookup item
									//{ { face: team} }
									//looks up as { { Brad: team} }
									//where face/ Brad is the Name of the user data and team is the key

									string newData = GetBuiltInReplacement(userDataName);
									if (newData == MissingInlineData)
									{
										newData = userDataName;
									}
									
									//try looking up the user data by name now
									GenericDataStore dataStore = _genericDataStores.FirstOrDefault(x => x.Name.ToLower().Trim() == newData.ToLower().Trim());
									if (dataStore != null)
									{
										//found a match for the name, now look up the key 2nd position

										string dataKey = dataArray[1].Trim().ToLower();
										string newKey = dataKey;
										
										if (_replacementValues.Contains(dataKey))
										{
											newKey = GetBuiltInReplacement(dataKey);
										}
										if (newKey == MissingInlineData)
										{
											newKey = dataKey;
										}

										if (dataKey == "random")
										{
											//grab a random user data item from this group
											//{{Greetings:random}}
											GenericDataStore genericDataStore = _genericDataStores.FirstOrDefault(x => x.Name == dataStore.Name);
											if(genericDataStore != null)
											{
												int dataCount = genericDataStore.Data.Count();
												int randomItem = _random.Next(optionCount, dataCount);
												GenericData genericData = genericDataStore.Data.ElementAt(randomItem).Value;
												if (genericData?.Value != null)
												{
													textChanged = true;
													ProcessUserDataUpdates(genericData, out newImage);
													newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value);
												}
											}
										}
										else if (dataStore.TreatKeyAsUtterance)
										{
											GenericData genericData = SpeechIntentManager.FindUserDataFromText(dataStore.Name, newKey);
											if (genericData.Value != null)
											{
												textChanged = true;
												ProcessUserDataUpdates(genericData, out newImage);
												newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value);
											}
										}
										else
										{
											KeyValuePair<string, GenericData> genericData = dataStore.Data.FirstOrDefault(x => x.Value.Key.ToLower().Trim() == newKey.ToLower().Trim());
											if (genericData.Value != null)
											{
												textChanged = true;
												ProcessUserDataUpdates(genericData.Value, out newImage);
												newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value.Value);
											}
										}
									}
								}
								else
								{
									GenericDataStore dataStore = _genericDataStores.FirstOrDefault(x => x.Name.ToLower().Trim() == userDataName);
									if (dataStore != null)
									{
										//found a match for the name, now look up the key 2nd position
										string dataKey = dataArray[1].Trim().ToLower();
										string newKey = dataKey;
										if (_replacementValues.Contains(dataKey))
										{
											newKey = GetBuiltInReplacement(dataKey);
										}
										if (newKey == MissingInlineData)
										{
											newKey = dataKey;
										}


										if (dataStore.TreatKeyAsUtterance)
										{
											GenericData genericData = SpeechIntentManager.FindUserDataFromText(dataStore.Name, newKey);
											if (genericData.Value != null)
											{
												textChanged = true;
												ProcessUserDataUpdates(genericData, out newImage);
												newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value);
											}
										}
										else
										{
											KeyValuePair<string, GenericData> genericData = dataStore.Data.FirstOrDefault(x => x.Value.Key == newKey);
											if (genericData.Value != null)
											{
												textChanged = true;
												ProcessUserDataUpdates(genericData.Value, out newImage);
												newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value.Value);
											}
										}
									}
								}
							}
						}
						else
						{
							//no, then check for a replacement value
							if (_replacementValues != null &&
							_replacementValues.Count() > 0 &&
							_replacementValues.Contains(replacementNameKey))
							{
								string newData = GetBuiltInReplacement(replacementNameKey);
								if (newData != MissingInlineData)
								{
									textChanged = true;
									newText = newText.Replace("{{" + replacementTextList + "}}", newData);
								}
							}
							else
							{
								//replace it with this option as is
								textChanged = true;
								newText = newText.Replace("{{" + replacementTextList + "}}", replacementNameKey);
							}
						}
					}
				}
			}
		}

		private void TryToPersonalizeDataTwo(string text, out string newText, out string newImage)
		{
			newText = text;
			newImage = null;
			if (CharacterState == null)
			{
				return;
			}

			if (newText.Contains("{{") && newText.Contains("}}"))
			{
				int replacementItemCount = Regex.Matches(newText, "{{").Count;

				//Loop through all inline text groups
				for (int i = 0; i < replacementItemCount; i++)
				{

					int indexOpen = 0;
					int indexClose = 0;
					string replacementTextList = "";

					indexOpen = newText.IndexOf("{{");
					indexClose = newText.IndexOf("}}");

					if (indexClose - 2 <= indexOpen)
					{
						continue;
					}

					replacementTextList = newText.Substring(indexOpen + 2, (indexClose - 2) - indexOpen);

					if (string.IsNullOrWhiteSpace(replacementTextList))
					{
						continue;
					}

					IList<string> optionList = new List<string>();
					if (!replacementTextList.Contains("||"))
					{
						optionList.Add(replacementTextList);
					}

					if (replacementTextList.Contains("||"))
					{
						string[] dataArray = replacementTextList.ToLower().Trim().Split("||");
						if (dataArray != null && dataArray.Count() > 0)
						{
							foreach (string option in dataArray)
							{
								if (!optionList.Contains(option))
								{
									optionList.Add(option);
								}
							}
						}
					}

					//Loop through the options to find match
					int optionCount = 0;
					bool textChanged = false;
					foreach (string option in optionList)
					{
						if (textChanged)
						{
							break;
						}

						optionCount++;
						//does it contain a :
						if (option.Contains(":"))
						{
							string[] dataArray = option.ToLower().Trim().Split(":");
							if (dataArray != null && dataArray.Count() == 2)
							{
								string userDataName = dataArray[0].Trim().ToLower();

								if (_replacementValues != null &&
								   _replacementValues.Count() > 0 &&
								   _replacementValues.Contains(userDataName))
								{
									//if it is a built in item in the FIRST position, it replaces the NAME with the lookup item
									//{ { face: team} }
									//looks up as { { Brad: team} }
									//where face/ Brad is the Name of the user data and team is the key

									string newData = GetBuiltInReplacement(userDataName);
									if (newData == MissingInlineData)
									{
										newData = userDataName;
									}
									//try looking up the user data by name now

									GenericDataStore dataStore = _genericDataStores.FirstOrDefault(x => x.Name.ToLower().Trim() == newData.ToLower().Trim());
									if (dataStore != null)
									{
										//found a match for the name, now look up the key 2nd position
										//TODO CLEANUP
										string dataKey = dataArray[1].Trim().ToLower();
										string newKey = dataKey;
										if (_replacementValues.Contains(dataKey))
										{
											newKey = GetBuiltInReplacement(dataKey);
										}
										if (newKey == MissingInlineData)
										{
											newKey = dataKey;
										}

										if (dataStore.TreatKeyAsUtterance)
										{
											GenericData genericData = SpeechIntentManager.FindUserDataFromText(dataStore.Name, newKey);
											if (genericData.Value != null)
											{
												textChanged = true;
												ProcessUserDataUpdates(genericData, out newImage);
												newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value);
											}
										}
										else
										{
											KeyValuePair<string, GenericData> genericData = dataStore.Data.FirstOrDefault(x => x.Key.ToLower().Trim() == newKey.ToLower().Trim());
											if (genericData.Value != null)
											{
												textChanged = true;
												ProcessUserDataUpdates(genericData.Value, out newImage);
												newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value.Value);
											}
										}
									}
								}
								else
								{
									GenericDataStore dataStore = _genericDataStores.FirstOrDefault(x => x.Name.ToLower().Trim() == userDataName);
									if (dataStore != null)
									{
										//found a match for the name, now look up the key 2nd position
										string dataKey = dataArray[1].Trim().ToLower();
										string newKey = dataKey;
										if (_replacementValues.Contains(dataKey))
										{
											newKey = GetBuiltInReplacement(dataKey);
										}
										if (newKey == MissingInlineData)
										{
											newKey = dataKey;
										}


										if (dataStore.TreatKeyAsUtterance)
										{
											GenericData genericData = SpeechIntentManager.FindUserDataFromText(dataStore.Name, newKey);
											if (genericData.Value != null)
											{
												textChanged = true;
												ProcessUserDataUpdates(genericData, out newImage);
												newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value);
											}
										}
										else
										{
											KeyValuePair<string, GenericData> genericData = dataStore.Data.FirstOrDefault(x => x.Key == newKey);
											if (genericData.Value != null)
											{
												textChanged = true;
												ProcessUserDataUpdates(genericData.Value, out newImage);
												newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value.Value);
											}
										}
									}
								}
							}
						}
						else
						{
							//no, then check for a replacement value
							if (_replacementValues != null &&
							_replacementValues.Count() > 0 &&
							_replacementValues.Contains(option))
							{
								string newData = GetBuiltInReplacement(option);
								if (newData != MissingInlineData)
								{
									textChanged = true;
									newText = newText.Replace("{{" + replacementTextList + "}}", newData);
								}
							}
							else
							{
								//replace it with this option as is
								textChanged = true;
								newText = newText.Replace("{{" + replacementTextList + "}}", option);
							}
						}
					}
				}
			}
		}

		public void ProcessUserDataUpdates(GenericData genericData, out string newImage)
		{
			newImage = null;
			if (!string.IsNullOrWhiteSpace(genericData.ScreenText))
			{
				_ = Misty.SetTextDisplaySettingsAsync("UserDataText", new TextSettings
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

				Misty.DisplayText(genericData.ScreenText, "UserDataText", null);
			}

			if (!string.IsNullOrWhiteSpace(genericData.Image))
			{
				Misty.DisplayImage(genericData.Image, null, genericData.Image.Contains("http:") || genericData.Image.Contains("https:"), null);
				newImage = genericData.Image;
			}
		}

		private string GetBuiltInReplacement(string option)
		{
			string newData = "";
			switch (option.ToLower().Trim())
			{
				case "face":
					newData = CharacterState.LastKnownFaceSeen ??
						CharacterState.FaceRecognitionEvent?.Label ??
						StateAtAnimationStart?.FaceRecognitionEvent?.Label ??
						PreviousState?.FaceRecognitionEvent?.Label ?? MissingInlineData;
					break;
				case "qrcode":
					newData = CharacterState.QrTagEvent?.DecodedInfo ??
						StateAtAnimationStart?.QrTagEvent?.DecodedInfo ??
						PreviousState?.QrTagEvent?.DecodedInfo ?? MissingInlineData;
					break;
				case "arcode":
					newData = CharacterState.ArTagEvent?.TagId.ToString() ??
						StateAtAnimationStart?.ArTagEvent?.TagId.ToString() ??
						PreviousState?.ArTagEvent?.TagId.ToString() ?? MissingInlineData;
					break;
				case "text":
					newData = CharacterState.SpeechResponseEvent?.Text ??
						StateAtAnimationStart?.SpeechResponseEvent?.Text ??
						PreviousState?.SpeechResponseEvent?.Text ?? MissingInlineData;
					break;
				case "intent":
					newData = CharacterState.SpeechResponseEvent?.TriggerFilter ??
						StateAtAnimationStart?.SpeechResponseEvent?.TriggerFilter ??
						PreviousState?.SpeechResponseEvent?.TriggerFilter ?? MissingInlineData;
					break;
				case "robotname":
					newData = _conversationGroup.RobotName ?? "Misty";
					break;
				case "time":
					newData = TimeManager.GetTimeObject().SpokenTime ?? MissingInlineData;
					break;
			}
			return newData;

		}
		
		private void LocomotionManager_DriveEncoderEvent(object sender, IDriveEncoderEvent e)
		{
			if (CharacterState == null)
			{
				return;
			}
			CharacterState.DriveEncoder = (DriveEncoderEvent)e;
			DriveEncoder?.Invoke(this, e);
		}

		private void SpeechManager_KeyPhraseRecognitionOn(object sender, bool e)
		{
			if (CharacterState == null)
			{
				return;
			}
			CharacterState.KeyPhraseRecognitionOn = e;
			KeyPhraseRecognitionOn?.Invoke(this, e);

			if(e || (!e && !CurrentInteraction.StartListening))
			{
				//ManageListeningDisplay(e ? ListeningState.WaitingForKeyPhrase : ListeningState.Waiting);
			}
		}

		private ListeningState _listeningState = ListeningState.Waiting;
		private object _listeningLock = new object();

		private void SpeechManager_StoppedListening(object sender, IVoiceRecordEvent e)
		{
			if (CharacterState == null || e == null)
			{
				return;
			}
			CharacterState.Listening = false;
			if(!CurrentInteraction.AllowKeyPhraseRecognition)
			{
				ManageListeningDisplay(ListeningState.Waiting);
			}

			StoppedListening?.Invoke(this, e);

			_ = SpeechManager.UpdateKeyPhraseRecognition(CurrentInteraction, false);
		}

		private void SpeechManager_StartedListening(object sender, DateTime e)
		{
			if (CharacterState == null || e == null)
			{
				return;
			}
			CharacterState.Listening = true;
			ManageListeningDisplay(ListeningState.Recording);			
			StartedListening?.Invoke(this, e);
		}

		private void ManageListeningDisplay(ListeningState listeningState)
		{
			if (CharacterParameters.ShowListeningIndicator)
			{
				lock (_listeningLock)
				{
					switch (listeningState)
					{
						//TODO Allow people to choose own listening display
						//case ListeningState.Waiting:
						//	Misty.DisplayText("🛑", "Listening", null);
						//	break;
						//case ListeningState.Speaking:
						//	Misty.DisplayText("📋", "Listening", null);
						//	break;
						//case ListeningState.WaitingForKeyPhrase:
						//	Misty.DisplayText("📢", "Listening", null);
						//	break;
						case ListeningState.Recording:
                            //Misty.DisplayText("🌟", "Listening", null);
                            Misty.DisplayText("LISTENING...", "Listening", null);
                            //Misty.DisplayText("🌟", "SPEAK NOW", null);
                            _ = Misty.SetTextDisplaySettingsAsync("Listening", new TextSettings
                            {
                                Visible = true
                            });
							break;
                        case ListeningState.ProcessingSpeech:
                            Misty.DisplayText("PROCESSING SPEECH...", "Listening", null);
                           // Misty.DisplayText("🌟", "Listening", null);
                            _ = Misty.SetTextDisplaySettingsAsync("Listening", new TextSettings
                            {
                                Visible = true
                            });
                            break;
                        default:
                            _ = Misty.SetTextDisplaySettingsAsync("Listening", new TextSettings
                            {
                                Visible = false
                            });
                            break;

                    }

					_listeningState = listeningState;
				}
			}
		}

		private void SpeechManager_StoppedSpeaking(object sender, IAudioPlayCompleteEvent e)
		{
			//e could be null if exception thrown when trying to speak
			if (CharacterState == null || e == null)
			{
				return;
			}
			CharacterState.Speaking = false;
			CharacterState.Saying = "";
			CharacterState.Spoke = true;

			SendManagedResponseEvent(new TriggerData(e?.Name, "", Triggers.AudioCompleted));
			StoppedSpeaking?.Invoke(this, e);

			if(!CurrentInteraction.StartListening)
			{
				ManageListeningDisplay(ListeningState.Waiting);
			}
			_ = SpeechManager.UpdateKeyPhraseRecognition(CurrentInteraction, CurrentInteraction.StartListening);
		}

		private void SpeechManager_StartedSpeaking(object sender, string e)
		{
			if (CharacterState == null || e == null)
			{
				return;
			}
			CharacterState.Speaking = true;
			StartedSpeaking?.Invoke(this, e);
			ManageListeningDisplay(ListeningState.Speaking);

			if (!string.IsNullOrWhiteSpace(e) && CharacterParameters.DisplaySpoken)
			{
				Misty.DisplayText(e, "SpokeText", null);
			}
		}

		private void StreamAndLogInteraction(string message, Exception ex = null)
		{
			if(CharacterParameters.LogInteraction)
			{
				Misty.SkillLogger.Log(message, ex);
			}

			if (CharacterParameters.StreamInteraction)
			{
				if (ex != null)
				{
					message += $"|Exception:{ex.Message}";
				}
				Misty.PublishMessage(message, null);
			}
		}
		
		private bool SendManagedResponseEvent(TriggerData triggerData, bool conversationTriggerCheck = false)
		{
			if (triggerData.OverrideIntent ||
				LiveTriggers.Contains(triggerData.Trigger) ||
				triggerData.Trigger == Triggers.Timeout ||
				triggerData.Trigger == Triggers.Timer ||
				triggerData.Trigger == Triggers.AudioCompleted ||
				triggerData.Trigger == Triggers.KeyPhraseRecognized
			)
			{
				if (ProcessAndVerifyTrigger(triggerData, conversationTriggerCheck))
				{
					StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name} | Sent Handled Trigger | {triggerData.Trigger} - {triggerData.TriggerFilter} - {triggerData.Text}.");
					return true;
				}
			}
			return false;
		}
		
		private async Task GoToNextAnimation(IList<TriggerActionOption> possibleActions)
		{
			if (possibleActions == null || possibleActions.Count == 0)
            {
				//TODO Go to No Interaction selection?
                Misty.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Unmapped intent. Going to the start of the same conversation...");
                Interaction interactionRequest = _currentConversationData.Interactions.FirstOrDefault(x => x.Id == _currentConversationData.StartupInteraction);
                QueueInteraction(interactionRequest);
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

                //Is this a conversation departure?  TODO Clean this up, use empty guid?
                if(interaction == "* Conversation Departure Point")
                {
                    //Look up where the departure goes
                    KeyValuePair<string, ConversationMappingDetail> detail = _conversationGroup.ConversationMappings.FirstOrDefault(x => x.Value.DepartureMap.TriggerOptionId == selectedAction.Id);
                    if(detail.Value != null)
                    {
                        interaction = detail.Value.EntryMap.InteractionId;
                        conversation = detail.Value.EntryMap.ConversationId;
                    }
                }

                //if coming from an internal redirect, may not have an Id
                if (selectedAction.Id == null || (_currentConversationData.InteractionAnimations == null || !_currentConversationData.InteractionAnimations.TryGetValue(selectedAction.Id, out string overrideAnimation)))
				{
					overrideAnimation = null;
				}
				
				if (string.IsNullOrWhiteSpace(conversation) && string.IsNullOrWhiteSpace(interaction))
				{
					Misty.SkillLogger.Log($"Trigger has been activated, but the destination is unmapped, continuing to wait for mapped trigger.");
					return;
				}
				else
				{
					IgnoreEvents();
					SpeechManager.AbortListening(_currentAnimation.SpeakFileName ?? _currentAnimation.AudioFile);
					if (selectedAction.InterruptCurrentAction)
					{
						Misty.StopSpeaking(null);
						await Task.Delay(25);
						Misty.StopAudio(null);
						await Task.Delay(25);
					}
					else if (!string.IsNullOrWhiteSpace(_currentAnimation.Speak) || !string.IsNullOrWhiteSpace(_currentAnimation.AudioFile))
					{
						//So hacky, stop it
						int sanity = 0;
						while (CharacterState.Speaking && sanity < 6000)
						{
							sanity++;
							await Task.Delay(100);
						}
						if(sanity == 6000)
						{
							Misty.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Possible error managing speech, unless Misty has actually been talking in one interaction for a long time.");
						}
					}

					if ((string.IsNullOrWhiteSpace(conversation) || conversation == _currentConversationData.Id) && !string.IsNullOrWhiteSpace(interaction))
					{
						Misty.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Going to interaction {interaction} in the same conversation...");
						Interaction interactionRequest = _currentConversationData.Interactions.FirstOrDefault(x => x.Id == interaction);
						interactionRequest.Retrigger = selectedAction.Retrigger;
						if (overrideAnimation != null)
						{
							interactionRequest.Animation = overrideAnimation;
						}
						QueueInteraction(interactionRequest);
					}
					else if (!string.IsNullOrWhiteSpace(conversation) && string.IsNullOrWhiteSpace(interaction))
					{
						Misty.SkillLogger.Log($"Going to the start of conversation {conversation}...");

						_currentConversationData = _conversationGroup.Conversations.FirstOrDefault(x => x.Id == conversation);

						Interaction interactionRequest = _currentConversationData.Interactions.FirstOrDefault(x => x.Id == _currentConversationData.StartupInteraction);
						interactionRequest.Retrigger = selectedAction.Retrigger;
						if (overrideAnimation != null)
						{
							interactionRequest.Animation = overrideAnimation;
						}
						QueueInteraction(interactionRequest);
					}
					else if (!string.IsNullOrWhiteSpace(conversation) && !string.IsNullOrWhiteSpace(interaction))
					{
						Misty.SkillLogger.Log($"Going to interaction {interaction} in conversation {conversation}...");

						_currentConversationData = _conversationGroup.Conversations.FirstOrDefault(x => x.Id == conversation);

						Interaction interactionRequest = _currentConversationData.Interactions.FirstOrDefault(x => x.Id == interaction);
						interactionRequest.Retrigger = selectedAction.Retrigger;
						if (overrideAnimation != null)
						{
							interactionRequest.Animation = overrideAnimation;
						}

						QueueInteraction(interactionRequest);
					}
				}
			}
		}

		//Fixing order of checking, conversation should not be checked until ALL local are checked, needs cleanup
		private bool ProcessAndVerifyTrigger(TriggerData triggerData, bool conversationTriggerCheck)
		{
			_processingTriggersSemaphore.Wait();
			IDictionary<string, IList<TriggerActionOption>> allowedTriggers = new Dictionary<string, IList<TriggerActionOption>>();
			try
			{
				allowedTriggers = CurrentInteraction.TriggerMap;

				if (!WaitingForOverrideTrigger || triggerData.OverrideIntent)
				{
					CharacterState.LatestTriggerChecked = new KeyValuePair<DateTime, TriggerData>(DateTime.Now, triggerData);

					bool match = false;
					string triggerDetailString;
					IList<TriggerActionOption> triggerDetailMap = new List<TriggerActionOption>();
					TriggerDetail triggerDetail = null;

					if(!conversationTriggerCheck)
					{
						foreach (KeyValuePair<string, IList<TriggerActionOption>> possibleIntent in allowedTriggers)
						{
							triggerDetailString = possibleIntent.Key;
							triggerDetailMap = possibleIntent.Value;

							triggerDetail = _currentConversationData.Triggers.FirstOrDefault(x => x.Id == triggerDetailString);
							if (triggerDetail != null && (string.Compare(triggerData.Trigger?.Trim(), triggerDetail.Trigger?.Trim(), true) == 0))
							{
								//Not all intents need a matching text
								if (triggerData.Trigger == Triggers.Timeout ||
									triggerData.Trigger == Triggers.AudioCompleted ||
									triggerData.Trigger == Triggers.KeyPhraseRecognized ||
									triggerData.Trigger == Triggers.Timer)
								{
									match = true;
									break;
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
					if (match && triggerDetailMap != null && triggerDetailMap.Count() > 0)
					{
						//this is it!
						_noInteractionTimer?.Dispose();
						_triggerActionTimeoutTimer?.Dispose();
						_timerTriggerTimer?.Dispose();
						CharacterState.LatestTriggerMatched = new KeyValuePair<DateTime, TriggerData>(DateTime.Now, triggerData);
						_ = GoToNextAnimation(triggerDetailMap);
						TriggerAnimationComplete(CurrentInteraction);
						ResponseEventReceived?.Invoke(this, triggerData);
						StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name} | Valid intent {triggerData.Trigger} {triggerData.TriggerFilter}.");
						return true;
					}					
				}
			}
			catch (Exception ex)
            {
                Misty.SkillLogger.Log($"Failed to process and verify the trigger.", ex);
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
							CharacterState.KnownFaceSeen = false;
							CharacterState.UnknownFaceSeen = true;
						}
						else
						{
							CharacterState.KnownFaceSeen = true;
							CharacterState.UnknownFaceSeen = false;
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
					
					if(CurrentInteraction.AllowConversationTriggers && 
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
			catch
			{
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
					IgnoreEvent(triggerDetail, (int)Math.Abs((triggerDetail.StoppingTriggerDelay*1000)));
				}

				if (triggerDetail?.StartingTrigger != null && triggerDetail?.StartingTrigger != Triggers.None &&
					(triggerDetail.StartingTrigger == triggerData.Trigger && (string.IsNullOrWhiteSpace(triggerDetail.StartingTriggerFilter) || (string.Compare(triggerDetail.StartingTriggerFilter?.Trim(), triggerData.TriggerFilter?.Trim(), true) == 0) ||
					(triggerDetail.StartingTrigger == Triggers.AudioCompleted ||
						triggerDetail.StartingTrigger == Triggers.Timeout ||
						triggerDetail.StartingTrigger == Triggers.Timer)
				)))
				{
					ListenToEvent(triggerDetail, (int)Math.Abs((triggerDetail.StartingTriggerDelay * 1000)));
					
					//TODO
					//Test performance, may be too much here
					foreach (string skillMessageId in CurrentInteraction.SkillMessages)
					{
						SkillMessage skillMessage = _currentConversationData.SkillMessages.FirstOrDefault(x => x.Id == skillMessageId);
						if (skillMessage == null || !skillMessage.StreamTriggerCheck || CharacterState?.LatestTriggerChecked == null)
						{
							continue;
						}
						IDictionary<string, object> payloadData = new Dictionary<string, object>();
						payloadData.Add("Skill", skillMessage.Skill);
						payloadData.Add("EventName", skillMessage.EventName);
						payloadData.Add("MessageType", skillMessage.MessageType);
						payloadData.Add("LatestTriggerCheck", Newtonsoft.Json.JsonConvert.SerializeObject(CharacterState.LatestTriggerChecked));
						
						//if just started, may miss first trigger, should really start skills at start of conversation
						Misty.TriggerEvent(skillMessage.EventName, "MistyCharacter", payloadData, null, null);
					}
				}
			}
			catch (Exception ex)
			{
				Misty.SkillLogger.Log("Failed to manage trigger.", ex);
			}
		}
		
		private void SpeechManager_SpeechIntent(object sender, TriggerData speechIntent)
		{
			if (CharacterState == null)
			{
				return;
			}
			CharacterState.SpeechResponseEvent = speechIntent;

			if(CharacterParameters.HeardSpeechToScreen && !string.IsNullOrWhiteSpace(speechIntent.Text))
			{
				Misty.DisplayText(speechIntent.Text, "SpeechText", null);
			}

            //New data formats
            if (!SendManagedResponseEvent(new TriggerData(speechIntent.Text, speechIntent.TriggerFilter, Triggers.SpeechHeard)))
            {
                //old
                //Look up name by id in case this conversation uses old data
                bool triggerSuccessful = false;
                if(_conversationGroup.IntentUtterances.TryGetValue(speechIntent.TriggerFilter, out UtteranceData utteranceData))
                {
                    triggerSuccessful = SendManagedResponseEvent(new TriggerData(speechIntent.Text, utteranceData.Name, Triggers.SpeechHeard));                    
                }

                if (!triggerSuccessful)
                {
					triggerSuccessful = SendManagedResponseEvent(new TriggerData(speechIntent.Text, "", Triggers.SpeechHeard));
                }

				if(!triggerSuccessful)
				{
					if (!SendManagedResponseEvent(new TriggerData(speechIntent.Text, speechIntent.TriggerFilter, Triggers.SpeechHeard), true))
					{
						if (_conversationGroup.IntentUtterances.TryGetValue(speechIntent.TriggerFilter, out UtteranceData utteranceData2))
						{
							triggerSuccessful = SendManagedResponseEvent(new TriggerData(speechIntent.Text, utteranceData2.Name, Triggers.SpeechHeard), true);
						}

						if (!triggerSuccessful)
						{
							triggerSuccessful = SendManagedResponseEvent(new TriggerData(speechIntent.Text, "", Triggers.SpeechHeard), true);
						}
					}
				}
            }
            SpeechIntentEvent?.Invoke(this, speechIntent);
        }
		
		//More intent events registered in this class
		private void ArTagCallback(IArTagDetectionEvent arTagEvent)
		{
			if (CharacterState == null ||
				(CharacterState.ArTagEvent != null && arTagEvent.Created == CharacterState.ArTagEvent.Created))
			{
				return;
			}
			CharacterState.ArTagEvent = (ArTagDetectionEvent)arTagEvent;
			if (!SendManagedResponseEvent(new TriggerData(arTagEvent.TagId.ToString(), arTagEvent.TagId.ToString(), Triggers.ArTagSeen)))
			{
				if(!SendManagedResponseEvent(new TriggerData(arTagEvent.TagId.ToString(), "", Triggers.ArTagSeen)))
				{
					if (!SendManagedResponseEvent(new TriggerData(arTagEvent.TagId.ToString(), arTagEvent.TagId.ToString(), Triggers.ArTagSeen), true))
					{
						SendManagedResponseEvent(new TriggerData(arTagEvent.TagId.ToString(), "", Triggers.ArTagSeen), true);
					}
				}
			}
			
			ArTagEvent?.Invoke(this, arTagEvent);
		}

		private void QrTagCallback(IQrTagDetectionEvent qrTagEvent)
		{
			if (CharacterState == null ||
				(CharacterState.QrTagEvent != null && qrTagEvent.Created == CharacterState.QrTagEvent.Created))
			{
				return;
			}
			CharacterState.QrTagEvent = (QrTagDetectionEvent)qrTagEvent;
			if (!SendManagedResponseEvent(new TriggerData(qrTagEvent.DecodedInfo?.ToString(), qrTagEvent.DecodedInfo?.ToString(), Triggers.QrTagSeen)))
			{
				if(!SendManagedResponseEvent(new TriggerData(qrTagEvent.DecodedInfo.ToString(), "", Triggers.QrTagSeen)))
				{
					if (!SendManagedResponseEvent(new TriggerData(qrTagEvent.DecodedInfo?.ToString(), qrTagEvent.DecodedInfo?.ToString(), Triggers.QrTagSeen), true))
					{
						SendManagedResponseEvent(new TriggerData(qrTagEvent.DecodedInfo.ToString(), "", Triggers.QrTagSeen), true);
					}
				}
			}
			
			QrTagEvent?.Invoke(this, qrTagEvent);
		}

		private void SerialMessageCallback(ISerialMessageEvent serialMessageEvent)
		{
			if (CharacterState == null ||
				(CharacterState.SerialMessageEvent != null && serialMessageEvent.Created == CharacterState.SerialMessageEvent.Created))
			{
				return;
			}
			CharacterState.SerialMessageEvent = (SerialMessageEvent)serialMessageEvent;
			if (!SendManagedResponseEvent(new TriggerData(serialMessageEvent.Message, serialMessageEvent.Message, Triggers.SerialMessage)))
			{
				if(!SendManagedResponseEvent(new TriggerData(serialMessageEvent.Message, "", Triggers.SerialMessage)))
				{
					if (!SendManagedResponseEvent(new TriggerData(serialMessageEvent.Message, serialMessageEvent.Message, Triggers.SerialMessage), true))
					{
						SendManagedResponseEvent(new TriggerData(serialMessageEvent.Message, "", Triggers.SerialMessage), true);
					}
				}
			}

			SerialMessageEvent?.Invoke(this, serialMessageEvent);
		}

		private void FaceRecognitionCallback(IFaceRecognitionEvent faceRecognitionEvent)
		{
			string _lastKnownFace = CharacterState.LastKnownFaceSeen;
			if (CharacterState == null ||
				(CharacterState.FaceRecognitionEvent != null && faceRecognitionEvent.Created == CharacterState.FaceRecognitionEvent.Created))
			{
				return;
			}

			CharacterState.FaceRecognitionEvent = (FaceRecognitionEvent)faceRecognitionEvent;

			if(faceRecognitionEvent.Label != ConversationConstants.UnknownPersonFaceLabel)
			{
				CharacterState.LastKnownFaceSeen = faceRecognitionEvent.Label;
			}

			bool sentMsg = false;
			if (faceRecognitionEvent.Label == ConversationConstants.UnknownPersonFaceLabel)
			{
				sentMsg = SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, ConversationConstants.SeeUnknownFaceTrigger, Triggers.FaceRecognized));
			}
			else
			{
				if (!SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, faceRecognitionEvent.Label, Triggers.FaceRecognized)))
				{
					if(_lastKnownFace != CharacterState.LastKnownFaceSeen)
					{
						sentMsg = SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, ConversationConstants.SeeNewFaceTrigger, Triggers.FaceRecognized));
					}
				}

				if (!sentMsg)
				{
					sentMsg = SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, ConversationConstants.SeeKnownFaceTrigger, Triggers.FaceRecognized));
				}
			}

			if (!sentMsg)
			{
				sentMsg = SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, "", Triggers.FaceRecognized));


				//dupe code to cleanup
				if (!sentMsg)
				{
					if (faceRecognitionEvent.Label == ConversationConstants.UnknownPersonFaceLabel)
					{
						sentMsg = SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, ConversationConstants.SeeUnknownFaceTrigger, Triggers.FaceRecognized), true);
					}
					else
					{
						if (!SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, faceRecognitionEvent.Label, Triggers.FaceRecognized), true))
						{
							if (_lastKnownFace != CharacterState.LastKnownFaceSeen)
							{
								sentMsg = SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, ConversationConstants.SeeNewFaceTrigger, Triggers.FaceRecognized), true);
							}
						}

						if (!sentMsg)
						{
							sentMsg = SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, ConversationConstants.SeeKnownFaceTrigger, Triggers.FaceRecognized), true);
						}
					}

					if (!sentMsg)
					{
						sentMsg = SendManagedResponseEvent(new TriggerData(faceRecognitionEvent.Label, "", Triggers.FaceRecognized), true);
					}
				}
			}


			FaceRecognitionEvent?.Invoke(this, faceRecognitionEvent);
		}

		private void UserEventCallback(IUserEvent userEvent)
		{
			if (CharacterState == null)
			{
				return;
			}
			CharacterState.ExternalEvent = (UserEvent)userEvent;

			//Pull out the intent and text, required
			if(userEvent.TryGetPayload(out IDictionary<string, object> payload))
			{
				//Can override other triggers

				payload.TryGetValue("Trigger", out object triggerObject);
				payload.TryGetValue("TriggerFilter", out object triggerFilterObject);
				payload.TryGetValue("Text", out object textObject);
				string trigger = triggerObject == null ? Triggers.ExternalEvent : Convert.ToString(triggerObject);
				string text = textObject == null ? null : Convert.ToString(textObject);
				string triggerFilter = triggerFilterObject == null ? null : Convert.ToString(triggerFilterObject);

				if (!SendManagedResponseEvent(new TriggerData(text, triggerFilter, trigger)))
				{
					if(!SendManagedResponseEvent(new TriggerData(text, "", trigger)))
					{
						if (!SendManagedResponseEvent(new TriggerData(text, triggerFilter, trigger), true))
						{
							SendManagedResponseEvent(new TriggerData(text, "", trigger), true);
						}
					}
				}
				ExternalEvent?.Invoke(this, userEvent);
			}			
		}

		private void ObjectCallback(object sender, IObjectDetectionEvent objectDetectionEvent)
		{
			if (CharacterState == null ||
				(CharacterState.ObjectEvent != null && objectDetectionEvent.Created == CharacterState.ObjectEvent.Created))
			{
				return;
			}
			if(objectDetectionEvent.Description != "person")
			{
				CharacterState.NonPersonObjectEvent = (ObjectDetectionEvent)objectDetectionEvent;
			}
			CharacterState.ObjectEvent = (ObjectDetectionEvent)objectDetectionEvent;
			if (!SendManagedResponseEvent(new TriggerData(objectDetectionEvent.Confidence.ToString(), objectDetectionEvent.Description, Triggers.ObjectSeen)))
			{
				if(!SendManagedResponseEvent(new TriggerData(objectDetectionEvent.Confidence.ToString(), "", Triggers.ObjectSeen)))
				{
					if (!SendManagedResponseEvent(new TriggerData(objectDetectionEvent.Confidence.ToString(), objectDetectionEvent.Description, Triggers.ObjectSeen), true))
					{
						SendManagedResponseEvent(new TriggerData(objectDetectionEvent.Confidence.ToString(), "", Triggers.ObjectSeen), true);
					}
				}
			}
			
			ObjectEvent?.Invoke(this, objectDetectionEvent);
		}

		private void CapTouchCallback(ICapTouchEvent capTouchEvent)
		{
			if (CharacterState == null ||
				(CharacterState.CapTouchEvent != null && capTouchEvent.Created == CharacterState.CapTouchEvent.Created))
			{
				return;
			}

			CharacterState.CapTouchEvent = (CapTouchEvent)capTouchEvent;
            
			if (!SendManagedResponseEvent(new TriggerData(capTouchEvent.IsContacted ? "Contacted" : "Released", capTouchEvent.SensorPosition.ToString(), capTouchEvent.IsContacted ? Triggers.CapTouched : Triggers.CapReleased)))
			{
				if(!SendManagedResponseEvent(new TriggerData(capTouchEvent.IsContacted ? "Contacted" : "Released", "", capTouchEvent.IsContacted ? Triggers.CapTouched : Triggers.CapReleased)))
				{
					if (!SendManagedResponseEvent(new TriggerData(capTouchEvent.IsContacted ? "Contacted" : "Released", capTouchEvent.SensorPosition.ToString(), capTouchEvent.IsContacted ? Triggers.CapTouched : Triggers.CapReleased), true))
					{
						SendManagedResponseEvent(new TriggerData(capTouchEvent.IsContacted ? "Contacted" : "Released", "", capTouchEvent.IsContacted ? Triggers.CapTouched : Triggers.CapReleased), true);
					}
				}
			}
			CapTouchEvent?.Invoke(this, capTouchEvent);
		}
		
		private void BumpSensorCallback(IBumpSensorEvent bumpEvent)
		{
			if (CharacterState == null || 
				(CharacterState.BumpEvent != null && bumpEvent.Created == CharacterState.BumpEvent.Created))
			{
				return;
			}
			CharacterState.BumpEvent = (BumpSensorEvent)bumpEvent;
            
			if (!SendManagedResponseEvent(new TriggerData(bumpEvent.IsContacted ? "Contacted" : "Released", bumpEvent.SensorPosition.ToString(), bumpEvent.IsContacted ? Triggers.BumperPressed : Triggers.BumperReleased)))
			{
				if(!SendManagedResponseEvent(new TriggerData(bumpEvent.IsContacted ? "Contacted" : "Released", "", bumpEvent.IsContacted ? Triggers.BumperPressed : Triggers.BumperReleased)))
				{
					if (!SendManagedResponseEvent(new TriggerData(bumpEvent.IsContacted ? "Contacted" : "Released", bumpEvent.SensorPosition.ToString(), bumpEvent.IsContacted ? Triggers.BumperPressed : Triggers.BumperReleased), true))
					{
						SendManagedResponseEvent(new TriggerData(bumpEvent.IsContacted ? "Contacted" : "Released", "", bumpEvent.IsContacted ? Triggers.BumperPressed : Triggers.BumperReleased), true);						
					}
				}
			}
			BumperEvent?.Invoke(this, bumpEvent);
		}


		private void BatteryChargeCallback(IBatteryChargeEvent batteryEvent)
		{
			if (CharacterState == null)
			{
				return;
			}
			CharacterState.BatteryChargeEvent = (BatteryChargeEvent)batteryEvent;			
			BatteryChargeEvent?.Invoke(this, batteryEvent);
		}

		//Non-trigger events from other classes
		private void HeadManager_HeadRollActuatorEvent(object sender, IActuatorEvent e)
		{
			if (CharacterState == null)
			{
				return;
			}
			CharacterState.HeadRollActuatorEvent = (ActuatorEvent)e;
		}

		private void HeadManager_HeadYawActuatorEvent(object sender, IActuatorEvent e)
		{
			if (CharacterState == null)
			{
				return;
			}
			CharacterState.HeadYawActuatorEvent = (ActuatorEvent)e;
		}

		private void HeadManager_HeadPitchActuatorEvent(object sender, IActuatorEvent e)
		{
			if (CharacterState == null)
			{
				return;
			}
			CharacterState.HeadPitchActuatorEvent = (ActuatorEvent)e;
		}

		private void ArmManager_LeftArmActuatorEvent(object sender, IActuatorEvent e)
		{
			if (CharacterState == null)
			{
				return;
			}
			CharacterState.LeftArmActuatorEvent = (ActuatorEvent)e;
		}

		private void ArmManager_RightArmActuatorEvent(object sender, IActuatorEvent e)
		{
			if (CharacterState == null)
			{
				return;
			}
			CharacterState.RightArmActuatorEvent = (ActuatorEvent)e;
		}
		
		private void LogEventDetails(IEventDetails eventDetails)
		{
			Misty.SkillLogger.Log($"Registered event '{eventDetails.EventName}' at {DateTime.Now}.  Id = {eventDetails.EventId}, Type = {eventDetails.EventType}, KeepAlive = {eventDetails.KeepAlive}");
		}
		
		private void TriggerAnimationComplete(Interaction interaction)
		{
			PreviousState = new CharacterState(CharacterState);
			UniqueAnimationId = Guid.NewGuid();

			InteractionEnded?.Invoke(this, DateTime.Now);
		}


		private void RunNextAnimation(object sender, DateTime e)
		{
			try
			{
				_latestTriggerMatchData = CharacterState.LatestTriggerMatched;                
				UniqueAnimationId = Guid.NewGuid();
				
				if (Misty.SkillStatus == NativeSkillStatus.Running)
				{
					CharacterState.Spoke = false;
					CharacterState.UnknownFaceSeen = false;
					CharacterState.KnownFaceSeen = false;
					CharacterState.Listening = false;
					CharacterState.Saying = "";
					
					_ = ProcessNextAnimationRequest();
				}
				else
				{
					StopConversation("Skill shutting down, stopping conversation.");
				}
			}
			catch (Exception ex)
			{
				Misty.SkillLogger.Log("Exception running animation.", ex);
			}
		}
		
		private async Task ProcessNextAnimationRequest()
		{
			try
			{
				StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name}  | LOOKING FOR NEXT ANIMATION IN QUEUE.");
				
				Interaction interaction = null;
				bool dequeued = false;
				
				while (!dequeued || interaction == null)
				{
					dequeued = InteractionPriorityQueue.TryDequeue(out interaction);
					if (!dequeued)
					{
						dequeued = InteractionQueue.TryDequeue(out interaction);
					}
					await Task.Delay(50);
				}

				Misty.SkillLogger.Log($"STARTING NEW INTERACTION.");
				Misty.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Processing next interaction in queue.");				
				if(_skillsToStop != null && _skillsToStop.Count > 0 )
				{
					foreach (string skill in _skillsToStop)
					{
						Misty.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Stop skill request for skill {skill}.");
						await Misty.CancelRunningSkillAsync(skill);
					}
					_skillsToStop.Clear();
				}

				_ = Misty.SetTextDisplaySettingsAsync("UserDataText", new TextSettings
				{
					Deleted = true
				});

				CurrentInteraction = new Interaction(interaction);
				StateAtAnimationStart = new CharacterState(CharacterState);
				InteractionStarted?.Invoke(this, DateTime.Now);

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
				_currentAnimation = animationRequest;
				
				if (interaction == null)
				{
					Misty.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Failed processing null conversation phrase.");
					return;
				}

				if (CharacterParameters.LogLevel == SkillLogLevel.Verbose || CharacterParameters.LogLevel == SkillLogLevel.Info)
				{
					Misty.SkillLogger.Log($"Animation message '{animationRequest?.Name}' sent in to : Say '{animationRequest?.Speak ?? "nothing"}' : Play Audio '{animationRequest?.Speak ?? "none"}'.");
				}

				//Arrgh headaches
				if (!string.IsNullOrWhiteSpace(animationRequest.Speak))
				{
					animationRequest.Speak = animationRequest.Speak.Replace("’", "'").Replace("“", "\"").Replace("”", "\"");
				}
                
				if (string.IsNullOrWhiteSpace(animationRequest.SpeakFileName) && !string.IsNullOrWhiteSpace(animationRequest.Speak))
				{
					animationRequest.SpeakFileName = AssetHelper.MakeFileName(animationRequest.Speak);
				}
				
				//Restart trigger handling in case a developer stopped in template and didn't restart
				RestartTriggerHandling();

				CharacterState.AnimationEmotion = animationRequest.Emotion;
				CharacterState.CurrentMood = EmotionManager.GetNextEmotion(CharacterState.AnimationEmotion);
				StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name} | New attitude adjustment:{animationRequest.Emotion} - Current mood:{CharacterState.CurrentMood}");

				SpeechManager.SetListenTimingMs((int)(animationRequest.ListenTimeout*1000), (int)(animationRequest.SilenceTimeout * 1000));
				
				AnimationRequestProcessor(interaction);
			}
			catch (Exception ex)
			{
				Misty.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Exception processing animation request.", ex);
			}
		}


		private async void AnimationRequestProcessor(Interaction interaction)
		{
			try
			{
                if (interaction?.Animation == null)
                {
                    Misty.SkillLogger.Log($"Received null interaction or animation for interaction {interaction?.Name ?? interaction?.Id}.");
                    return;
                }

                AnimationRequest originalAnimationRequest = _currentConversationData.Animations.FirstOrDefault(x => x.Id == interaction.Animation);
                if(originalAnimationRequest == null)
                {
                    Misty.SkillLogger.Log($"Could not find animation for interaction {interaction?.Name ?? interaction?.Id}.");
                    return;
                }
				//Make copy cuz we are decorating and changing things here
				AnimationRequest animationRequest = new AnimationRequest(originalAnimationRequest);
				Interaction newInteraction = new Interaction(interaction);
				
				if (animationRequest == null || newInteraction == null)
				{
                    Misty.SkillLogger.Log($"Failed to copy data.");
                    return;
				}

				AnimationManager.StopRunningAnimationScripts();

				//should we start skill listening even if it may retrigger?
				foreach (string skillMessageId in CurrentInteraction.SkillMessages)
				{
					SkillMessage skillMessage = _currentConversationData.SkillMessages.FirstOrDefault(x => x.Id == skillMessageId);
					if (skillMessage == null)
					{
						continue;
					}

					if(skillMessage.StopIfRunning)
					{
						Misty.CancelRunningSkill(skillMessage.Skill, null);
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
					if (skillMessage.IncludeCharacterState && CharacterState != null)
					{
						payloadData.Add("CharacterState", Newtonsoft.Json.JsonConvert.SerializeObject(CharacterState));
					}
					if(skillMessage.IncludeLatestTriggerMatch && _latestTriggerMatchData.Value != null)
					{
						payloadData.Add("LatestTriggerMatch", Newtonsoft.Json.JsonConvert.SerializeObject(_latestTriggerMatchData));
					}

					//Test
					if (skillMessage.StartIfStopped)
					{
						lock (_runningSkillLock)
						{
							if (!string.IsNullOrWhiteSpace(skillMessage.Skill) &&
							!_runningSkills.Contains(skillMessage.Skill) &&
							skillMessage.Skill != "8be20a90-1150-44ac-a756-ebe4de30689e")
							{

								StreamAndLogInteraction($"Running skill {skillMessage.Skill}.");
								_ = Misty.RunSkillAsync(skillMessage.Skill, OriginalParameters);
							}
						}
					}

					StreamAndLogInteraction($"Sending event {skillMessage.EventName} to trigger handler skill {skillMessage.Skill}.");
					//if just started, may miss first trigger, should really start skills at start of conversation
					Misty.TriggerEvent(skillMessage.EventName, "MistyCharacter", payloadData, null, null);

					if (skillMessage.StopOnNextAnimation && !_skillsToStop.Contains(skillMessage.Skill))
					{
						_skillsToStop.Add(skillMessage.Skill);
					}
				}
				
				//Animation started, make sure all the immediate events are going and set listen by intent
				IDictionary<string, IList<TriggerActionOption>> allowedIntents = CurrentInteraction.TriggerMap;
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
				
				if (CurrentInteraction.AllowConversationTriggers &&
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

				//Use image default if NULL , if EMPTY Speak (just whitespace), no new image
				if (animationRequest.ImageFile == null)
				{
					animationRequest.ImageFile = defaultAnimation.ImageFile;
				}

				//Set default LED for emotion if not already set
				if (animationRequest.LEDTransitionAction == null)
				{
					animationRequest.LEDTransitionAction = defaultAnimation.LEDTransitionAction;
				}
				
				if (animationRequest.Volume != null && animationRequest.Volume > 0)
				{
					SpeechManager.Volume = (int)animationRequest.Volume;
				}
				else if (defaultAnimation.Volume != null && defaultAnimation.Volume > 0)
				{
					SpeechManager.Volume = (int)defaultAnimation.Volume;
				}


				//NEED TO REPROCESS TEXT FOR INTENT!!!

				if (interaction.Retrigger &&
					CharacterState.LatestTriggerMatched.Value != null &&
					CharacterState.LatestTriggerMatched.Value.Trigger == Triggers.SpeechHeard) //for now
				{
					await Task.Delay(100);
					//SpeechMatchData data = SpeechIntentManager.GetIntent(CharacterState.LatestTriggerMatched.Value.Text);
					//SpeechManager_SpeechIntent(this, );

					//SendManagedResponseEvent(TriggerData triggerData, bool conversationTriggerCheck)
					//if (ProcessAndVerifyTrigger(new TriggerData(CharacterState.LatestTriggerMatched.Value.Text, data.Id)))

					//need to really call each appropriate method to trigger all
					if (SendManagedResponseEvent(new TriggerData(CharacterState.LatestTriggerMatched.Value.Text, CharacterState.LatestTriggerMatched.Value.TriggerFilter), false))
					{
						StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name} | Sent Handled Retrigger | {CharacterState.LatestTriggerMatched.Value.Trigger} - {CharacterState.LatestTriggerMatched.Value.TriggerFilter} - {CharacterState.LatestTriggerMatched.Value.Text}.");
						return;
					}
					else
					{
						switch (CharacterState.LatestTriggerMatched.Value.Trigger)
						{
							//Hmmm, so if the second one is listening for unknown, this will say it can't find it

							case Triggers.SpeechHeard:
								//send in unknown
								if (SendManagedResponseEvent(new TriggerData(CharacterState.LatestTriggerMatched.Value.Text, ConversationConstants.HeardUnknownTrigger, Triggers.SpeechHeard), false))
								{
									StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name} | Sent Handled Retrigger | {CharacterState.LatestTriggerMatched.Value.Trigger} - Unknown - {CharacterState.LatestTriggerMatched.Value.Text}.");
									return;
								}
								break;
						}
					}
				}


				if (hasAudio)
				{
					lock (_lockListenerData)
					{
						string audioFile = "";
						if (!string.IsNullOrWhiteSpace(animationRequest.AudioFile))
						{
							audioFile = SpeechManager.GetLocaleName(animationRequest.AudioFile);
						}
						else
						{
							audioFile = SpeechManager.GetLocaleName(animationRequest.SpeakFileName);
						}

						_audioAnimationCallbacks.Remove(audioFile);
						_audioAnimationCallbacks.Add(audioFile);
					}
				}

				//Start speech or audio playing
				if (!string.IsNullOrWhiteSpace(animationRequest.Speak))
				{
					hasAudio = true;

					TryToPersonalizeDataOne(animationRequest.Speak, out string newText, out string newImage);
					animationRequest.Speak = newText;
					animationRequest.ImageFile = string.IsNullOrWhiteSpace(newImage) ? animationRequest.ImageFile : newImage;

					SpeechManager.Speak(animationRequest, newInteraction);
					StreamAndLogInteraction($"Animation: { animationRequest.Name} | Saying {animationRequest.Speak}.");
				}
				else if (!string.IsNullOrWhiteSpace(animationRequest.AudioFile))
				{
					hasAudio = true;
					CharacterState.Audio = animationRequest.AudioFile;
					Misty.PlayAudio(animationRequest.AudioFile, null, null);
					StreamAndLogInteraction($"Animation: { animationRequest.Name} | Playing audio {animationRequest.AudioFile}.");
				}
				
				//Display image
				if (!string.IsNullOrWhiteSpace(animationRequest.ImageFile))
				{
					CharacterState.Image = animationRequest.ImageFile;

					if (!animationRequest.ImageFile.Contains("."))
					{
						animationRequest.ImageFile = animationRequest.ImageFile + ".jpg";
					}
					StreamAndLogInteraction($"Animation: { animationRequest.Name} | Displaying image {animationRequest.ImageFile}");
					Misty.DisplayImage(animationRequest.ImageFile, null, false, null);
				}

				if (animationRequest.SetFlashlight != CharacterState.FlashLightOn)
				{
					Misty.SetFlashlight(animationRequest.SetFlashlight, null);
					CharacterState.FlashLightOn = animationRequest.SetFlashlight;
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
								Misty.TransitionLED(ledTransitionAction.Red, ledTransitionAction.Green, ledTransitionAction.Blue, ledTransitionAction.Red2, ledTransitionAction.Green2, ledTransitionAction.Blue2, ledTransition, ledTransitionAction.PatternTime * 1000, null);
							});

							CharacterState.AnimationLED = ledTransitionAction;
						}
					}
				}

				//Move arms
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

				//Move head
				if (!string.IsNullOrWhiteSpace(animationRequest.HeadLocation))
				{
					_ = Task.Run(async () =>
					{
						if (animationRequest.HeadActionDelay > 0)
						{
							await Task.Delay((int)(animationRequest.HeadActionDelay * 1000));
						}
						HeadManager.HandleHeadAction(animationRequest, _currentConversationData);
					});
				}

				if (!string.IsNullOrWhiteSpace(animationRequest.AnimationScript))
				{
					_ = Task.Run(() =>
					{
						//Run the animation script
						AnimationManager.RunAnimationScript(animationRequest.AnimationScript);
					});
				}

				//If animation is shorter than audio, there could be some oddities in conversations... should we still allow it?
				int interactionTimeoutMs = newInteraction.InteractionFailedTimeout <= 0 ? 100 : (int)(newInteraction.InteractionFailedTimeout*1000);				
				_noInteractionTimer = new Timer(CommunicationBreakdownCallback, UniqueAnimationId, interactionTimeoutMs, Timeout.Infinite);

				if (CurrentInteraction.StartListening && !hasAudio)
				{
					//Can still listen without speaking
					_ = Misty.CaptureSpeechAsync(false, true, (int)(animationRequest.ListenTimeout * 1000), (int)(animationRequest.SilenceTimeout * 1000), null);

					StartedListening?.Invoke(this, DateTime.Now);
				}
			}
			catch (Exception ex)
			{
				Misty.SkillLogger.Log($"Interaction: {CurrentInteraction?.Name} | Animation Id: { interaction.Animation} | Exception while attempting to process animation request callback.", ex);
			}
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

		private void IntentTimeoutTimerCallback(object timerData)
		{
			if(timerData != null && ((TimerTriggerData)timerData)?.AnimationId == UniqueAnimationId)
			{
				if(!SendManagedResponseEvent(new TriggerData(DateTime.Now.ToString(), "", Triggers.Timeout)))
				{
					SendManagedResponseEvent(new TriggerData(DateTime.Now.ToString(), "", Triggers.Timeout), true);
				}
			}
		}

		private void IntentTriggerTimerCallback(object timerData)
		{
			if (timerData != null && ((TimerTriggerData)timerData)?.AnimationId == UniqueAnimationId)
			{
				if(!SendManagedResponseEvent(new TriggerData(DateTime.Now.ToString(), "", Triggers.Timer)))
				{
					SendManagedResponseEvent(new TriggerData(DateTime.Now.ToString(), "", Triggers.Timer), true);
				}
			}
		}

		private async void CommunicationBreakdownCallback(object timerData)
		{
			//TODO Experiment! If in the middle of processing speech, check again in a bit
			//Do we want this?  config?
			if(!CurrentInteraction.AllowVoiceProcessingOverride && _processingVoice &&  _currentProcessingVoiceWaits < MaxProcessingVoiceWaits)
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
				if(string.IsNullOrWhiteSpace(_currentConversationData.NoTriggerInteraction))
				{
					StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name} | Interaction timeout. No intents were triggered, stopping conversation.");
					TriggerAnimationComplete(CurrentInteraction);
					StopConversation();
					Misty.Speak("Interaction timed out with unmapped conversation 'No Trigger Interaction' action.  Cancelling skill.", true, "InteractionTimeout", null);
					await Task.Delay(5000);
					Misty.SkillCompleted();
				}
				else
				{
					StreamAndLogInteraction($"Interaction: {CurrentInteraction?.Name} | Interaction timeout. No intents were triggered, going to default interaction {_currentConversationData.NoTriggerInteraction}.");
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

		/// <summary>
		/// Populate all the emotion defaults for this character
		/// Is this still what we want?
		/// </summary>
		private void PopulateEmotionDefaults()
		{
			EmotionAnimations.TryAdd(Emotions.Admiration,
				new AnimationRequest
				{
					Emotion = Emotions.Admiration,
					SpeakingStyle = "cheerful",
					AudioFile = "s_" + SystemSound.PhraseHello.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Amazement.ToString() + ".jpg"
				});
			
			EmotionAnimations.TryAdd(Emotions.Adoration,
				new AnimationRequest
				{
					Emotion = Emotions.Adoration,
					SpeakingStyle = "cheerful",
					AudioFile = "s_" + SystemSound.Acceptance.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Amazement.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.AestheticAppreciation,
				new AnimationRequest
				{
					Emotion = Emotions.AestheticAppreciation,
					AudioFile = "s_" + SystemSound.Awe2.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.ContentLeft.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Amusement,
				new AnimationRequest
				{
					Emotion = Emotions.Amusement,
					SpeakingStyle = "cheerful",
					AudioFile = "s_" + SystemSound.Joy2.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.JoyGoofy3.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Anxiety,
				new AnimationRequest
				{
					Emotion = Emotions.Anxiety,
					AudioFile = "s_" + SystemSound.Sadness.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.ApprehensionConcerned.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Awe,
				new AnimationRequest
				{
					Emotion = Emotions.Awe,
					SpeakingStyle = "cheerful",
					AudioFile = "s_" + SystemSound.Awe.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Amazement.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Awkwardness,
				new AnimationRequest
				{
					Emotion = Emotions.Awkwardness,
					AudioFile = "s_" + SystemSound.DisorientedConfused2.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Sleepy.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Boredom,
				new AnimationRequest
				{
					Emotion = Emotions.Boredom,
					AudioFile = "s_" + SystemSound.Boredom.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Sleepy3.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Calmness,
				new AnimationRequest
				{
					Emotion = Emotions.Calmness,
					ImageFile = "e_" + SystemImage.DefaultContent.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Confusion,
				new AnimationRequest
				{
					Emotion = Emotions.Confusion,
					SpeakingStyle = "empathetic",
					AudioFile = "s_" + SystemSound.DisorientedConfused2.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Disoriented.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Craving,
				new AnimationRequest
				{
					Emotion = Emotions.Craving,
					AudioFile = "s_" + SystemSound.PhraseHello.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.ContentRight.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Desire,
				new AnimationRequest
				{
					Emotion = Emotions.Desire,
					AudioFile = "s_" + SystemSound.Love.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Love.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Disgust,
				new AnimationRequest
				{
					Emotion = Emotions.Disgust,
					SpeakingStyle = "empathetic",
					AudioFile = "s_" + SystemSound.Disgust2.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Rage2.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.EmpatheticPain,
				new AnimationRequest
				{
					Emotion = Emotions.EmpatheticPain,
					SpeakingStyle = "empathetic",
					AudioFile = "s_" + SystemSound.Disapproval.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Sadness.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Entrancement,
				new AnimationRequest
				{
					Emotion = Emotions.Entrancement,
					SpeakingStyle = "cheerful",
					AudioFile = "s_" + SystemSound.PhraseHello.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.EcstacyStarryEyed.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Envy,
				new AnimationRequest
				{
					Emotion = Emotions.Envy,
					AudioFile = "s_" + SystemSound.Loathing.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Disgust.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Avoidance,
				new AnimationRequest
				{
					Emotion = Emotions.Avoidance,
					AudioFile = "",
					ImageFile = "e_" + SystemImage.ContentLeft.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Excitement,
				new AnimationRequest
				{
					Emotion = Emotions.Calmness,
					SpeakingStyle = "cheerful",
					AudioFile = "s_" + SystemSound.Joy4.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Joy2.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Fear,
				new AnimationRequest
				{
					Emotion = Emotions.Fear,
					SpeakingStyle = "empathetic",
					AudioFile = "s_" + SystemSound.Anger.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Terror.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Horror,
				new AnimationRequest
				{
					Emotion = Emotions.Horror,
					SpeakingStyle = "empathetic",
					AudioFile = "s_" + SystemSound.Anger4.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Terror2.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Interest,
				new AnimationRequest
				{
					Emotion = Emotions.Interest,
					AudioFile = "s_" + SystemSound.Acceptance.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.ContentRight.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Joy,
				new AnimationRequest
				{
					Emotion = Emotions.Joy,
					SpeakingStyle = "cheerful",
					AudioFile = "s_" + SystemSound.Joy3.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.JoyGoofy3.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Nostalgia,
				new AnimationRequest
				{
					Emotion = Emotions.Nostalgia,
					AudioFile = "s_" + SystemSound.Amazement2.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Admiration.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Romance,
				new AnimationRequest
				{
					Emotion = Emotions.Romance,
					SpeakingStyle = "cheerful",
					AudioFile = "s_" + SystemSound.Love.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Love.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Sadness,
				new AnimationRequest
				{
					Emotion = Emotions.Sadness,
					SpeakingStyle = "empathetic",
					AudioFile = "s_" + SystemSound.Grief4.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Sadness.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Satisfaction,
				new AnimationRequest
				{
					Emotion = Emotions.Satisfaction,
					SpeakingStyle = "cheerful",
					AudioFile = "s_" + SystemSound.Ecstacy2.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Admiration.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Sympathy,
				new AnimationRequest
				{
					Emotion = Emotions.Sympathy,
					SpeakingStyle = "empathetic",
					AudioFile = "s_" + SystemSound.Grief3.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.Grief.ToString() + ".jpg"
				});

			EmotionAnimations.TryAdd(Emotions.Triumph,
				new AnimationRequest
				{
					Emotion = Emotions.Triumph,
					SpeakingStyle = "cheerful",
					AudioFile = "s_" + SystemSound.PhraseEvilAhHa.ToString() + ".wav",
					ImageFile = "e_" + SystemImage.EcstacyStarryEyed.ToString() + ".jpg"
				});
		}

		private void QueueInteraction(Interaction interaction)
		{
			try
			{
				Misty.SkillLogger.Log($"QUEUEING NEXT INTERACTION : {interaction.Name}");
				InteractionQueue.Enqueue(interaction);

				//We'll wait for an intent for the next animation
				//Eventually the Timeout trigger will be sent if no other intents are handled...				
			}
			catch (Exception ex)
			{
				Misty.SkillLogger.Log($"Interaction: {interaction?.Name} | Failed attempting to handle phrase in AnimatePhrase.", ex);

				ConversationEnded?.Invoke(this, DateTime.Now);
			}
		}
		
		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					IgnoreEvents();
					_noInteractionTimer?.Dispose();
					_triggerActionTimeoutTimer?.Dispose();
					_timerTriggerTimer?.Dispose();
					_pollRunningSkillsTimer?.Dispose();
					
					SpeechManager.Dispose();
					ArmManager.Dispose();
					HeadManager.Dispose();
					TimeManager.Dispose();

					Misty.UnregisterAllEvents(null);
					Misty.Stop(null);
					Misty.Halt(new List<MotorMask> { MotorMask.LeftArm, MotorMask.RightArm }, null);
					
					Misty.StopFaceDetection(null);
					Misty.StopObjectDetector(null);
					Misty.StopFaceRecognition(null);
					Misty.StopArTagDetector(null);
					Misty.StopQrTagDetector(null);
					Misty.StopKeyPhraseRecognition(null);
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
 