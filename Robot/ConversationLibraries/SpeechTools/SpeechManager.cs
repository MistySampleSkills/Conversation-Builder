﻿/**********************************************************************
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Conversation.Common;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using SkillTools.AssetTools;
using SpeechTools;
using SpeechTools.AzureCognitive;
using SpeechTools.GoogleSpeech;
using TimeManager;

namespace SpeechTools
{
	public class SpeechManager : ISpeechManager
	{

		public event EventHandler<string> StartedSpeaking;
		public event EventHandler<IAudioPlayCompleteEvent> StoppedSpeaking;
		public event EventHandler<IAudioPlayCompleteEvent> PreSpeechCompleted;
		public event EventHandler<DateTime> StartedListening;
		public event EventHandler<IVoiceRecordEvent> StoppedListening;
		public event EventHandler<TriggerData> SpeechIntent;		
		public event EventHandler<bool> KeyPhraseRecognitionOn;
		public event EventHandler<IKeyPhraseRecognizedEvent> KeyPhraseRecognized;
		public event EventHandler<IVoiceRecordEvent> CompletedProcessingVoice;
		public event EventHandler<IVoiceRecordEvent> StartedProcessingVoice;
		public event EventHandler<string> UserDataAnimationScript;

		private const string MissingInlineData = "Missing";
		private const string TTSNamePreface = "misty-en-";		
		private IList<string> _audioTags = new List<string>();
		private ISpeechIntentManager _speechIntentManager;
		private AzureSpeechService _azureCognitive;
		private GoogleSpeechService _googleService;
		private bool _recording;
		private AssetWrapper _assetWrapper;
		private AzureSpeechParameters _azureSpeechRecognitionParameters;
		private GoogleSpeechParameters _googleSpeechRecognitionParameters;
		private AzureSpeechParameters _azureTTSParameters;
		private GoogleSpeechParameters _googleTTSParameters;

		private ITimeManager _timeManager;
		private IDictionary<string, UtteranceData> _intentUtterances = new Dictionary<string, UtteranceData>();
		private IList<string> _listeningCallbacks = new List<string>();
		private object _lockListenerData = new object();
		private bool _listenAborted;
		private int _audioTrim = 0;
		private int _silenceTimeout = 6000;
		private int _listenTimeout = 6000;
		private IList<string> _allowedTriggers = new List<string>();
		private bool _keyPhraseTriggered;
		private bool _keyPhraseOn;
		private bool _processingAudioCallback;
		private SemaphoreSlim _keyPhraseOnSlim = new SemaphoreSlim(1, 1);
		private Random _random = new Random();
		private IList<string> _replacementValues = new List<string> { "face", "filter", "qrcode", "arcode", "text", "intent", "time", "robotname" };
		private IList<GenericDataStore> _genericDataStores = new List<GenericDataStore>();
		private string _robotName = "Misty";

		private CharacterState _characterState;
		private CharacterState _stateAtAnimationStart;
		private CharacterState _previousState;

		private IDictionary<string, object> _parameters { get; set; }
		private IRobotMessenger _misty { get; set; }
		private CharacterParameters _characterParameters { get; set; }

		private int _volume;
		public int Volume
		{
			get
			{
				return _volume;
			}
			set
			{
				if (_volume != value)
				{
					_volume = value;
					_misty.SetDefaultVolume(_volume, null);
				}
			}
		}

		protected void LogEventDetails(IEventDetails eventDetails)
		{
			_misty.SkillLogger.LogInfo($"Registered event '{eventDetails.EventName}' at {DateTime.Now}.  Id = {eventDetails.EventId}, Type = {eventDetails.EventType}, KeepAlive = {eventDetails.KeepAlive}");
		}

		public void AddValidIntent(object sender, KeyValuePair<string, TriggerData> triggerData)
		{
			if (triggerData.Value.Trigger.Trim().ToLower() == Triggers.SpeechHeard.ToLower())
			{
				KeyValuePair<string, UtteranceData> utteranceData = _intentUtterances.FirstOrDefault(x => x.Value.Name.Trim().ToLower() == triggerData.Value.TriggerFilter.Trim().ToLower());
				if (utteranceData.Value != null && !_allowedTriggers.Contains(utteranceData.Value.Id))
				{
					_allowedTriggers.Add(utteranceData.Value.Id);
				}
			}
		}
		
		public string GetLocaleName(string name)
		{
			if (_characterParameters.AddLocaleToAudioNames)
			{
				switch (_characterParameters.TextToSpeechService)
				{
					case "Google":
						name = _googleTTSParameters.SpokenLanguage + _googleTTSParameters.SpeakingVoice + _googleTTSParameters.SpeakingGender?[0] + name;
						break;
					case "Azure":
						name = _azureTTSParameters.TranslatedLanguage + _azureTTSParameters.SpeakingVoice + name;
						break;
					default:
						name = TTSNamePreface + name;
						break;
				}
			}
			return AssetHelper.AddMissingWavExtension(name);
		}

		public void SetInteractionDetails(int listenTimeout, int silenceTimeout, IList<string> allowedUtterances)
		{
			_listenTimeout = listenTimeout >= 1000 ? listenTimeout : 1000;
			_silenceTimeout = silenceTimeout >= 1000 ? silenceTimeout : 1000;
			_allowedTriggers = allowedUtterances;
		}

		public void SetAudioTrim(int trimMs)
		{
			_audioTrim = trimMs;
		}

		public void SetMaxSilence(int silenceTimeout)
		{
			_silenceTimeout = silenceTimeout;
		}

		public void SetMaxListen(int listenTimeout)
		{
			_listenTimeout = listenTimeout;
		}

		public async Task<bool> Initialize()
		{
			_misty.UnregisterEvent("KeyPhrase", null);
			_misty.UnregisterEvent("VoiceRecord", null);
			_misty.UnregisterEvent("CharacterAudioComplete", null);
			_misty.UnregisterEvent("CharacterTTSComplete", null);

			_azureSpeechRecognitionParameters = _characterParameters.AzureSpeechRecognitionParameters;
			_googleSpeechRecognitionParameters = _characterParameters.GoogleSpeechRecognitionParameters;
			_azureTTSParameters = _characterParameters.AzureTTSParameters;
			_googleTTSParameters = _characterParameters.GoogleTTSParameters;

			_assetWrapper = new AssetWrapper(_misty);
			await _assetWrapper.RefreshAssetLists();

			if (_azureSpeechRecognitionParameters?.SubscriptionKey != null || _azureTTSParameters?.SubscriptionKey != null)
			{
				AzureServiceAuthorization recAuth = new AzureServiceAuthorization
				{
					Region = _azureSpeechRecognitionParameters.Region ?? "",
					Endpoint = _azureSpeechRecognitionParameters.Endpoint ?? "",
					SubscriptionKey = _azureSpeechRecognitionParameters.SubscriptionKey ?? ""
				};

				AzureServiceAuthorization ttsAuth = new AzureServiceAuthorization
				{
					Region = _azureTTSParameters.Region ?? "",
					Endpoint = _azureTTSParameters.Endpoint ?? "",
					SubscriptionKey = _azureTTSParameters.SubscriptionKey ?? ""
				};

				_azureCognitive = new AzureSpeechService(ttsAuth, recAuth, _misty);
				_azureCognitive.SpeakingVoice = _azureSpeechRecognitionParameters?.SpeakingVoice;
				_azureCognitive.SpokenLanguage = _azureSpeechRecognitionParameters?.SpokenLanguage;
				_azureCognitive.TranslatedLanguage = _azureSpeechRecognitionParameters?.TranslatedLanguage;
				_azureCognitive.ProfanitySetting = _azureSpeechRecognitionParameters?.ProfanitySetting;
			}

			if (_googleSpeechRecognitionParameters?.SubscriptionKey != null || _googleTTSParameters?.SubscriptionKey != null)
			{
				GoogleServiceAuthorization speechRecAuth = new GoogleServiceAuthorization
				{
					Endpoint = _googleSpeechRecognitionParameters?.Endpoint ?? "",
					SubscriptionKey = _googleSpeechRecognitionParameters?.SubscriptionKey ?? ""
				};

				GoogleServiceAuthorization ttsAuth = new GoogleServiceAuthorization
				{
					Endpoint = _googleTTSParameters?.Endpoint ?? "",
					SubscriptionKey = _googleTTSParameters?.SubscriptionKey ?? ""
				};

				_googleService = new GoogleSpeechService(ttsAuth, speechRecAuth, _misty);
				_googleService.SpeakingVoice = _googleSpeechRecognitionParameters?.SpeakingVoice;
				_googleService.SpeakingGender = _googleSpeechRecognitionParameters?.SpeakingGender;
				_googleService.SpokenLanguage = _googleSpeechRecognitionParameters?.SpokenLanguage;
			}


			LogEventDetails(_misty.RegisterVoiceRecordEvent(VoiceRecordCallback, 100, true, "VoiceRecord", null));
			LogEventDetails(_misty.RegisterKeyPhraseRecognizedEvent(KeyPhraseCallback, 100, true, "KeyPhrase", null));
			LogEventDetails(_misty.RegisterAudioPlayCompleteEvent(AudioCallback, 100, true, "CharacterAudioComplete", null));
			LogEventDetails(_misty.RegisterTextToSpeechCompleteEvent(TTSCallback, 100, true, "CharacterTTSComplete", null));
					
			return true;
		}

		/// <summary>
		/// Called when the POTENTIAL to start key phrase rec has been triggered (basically not listening already or speaking/playing audio)
		/// </summary>
		/// <param name="currentInteraction"></param>
		/// <param name="hasAudio"></param>
		/// <returns></returns>
		public async Task<bool> UpdateKeyPhraseRecognition(Interaction currentInteraction, bool hasAudio)
		{
			await _keyPhraseOnSlim.WaitAsync();
			try
			{
				if (_keyPhraseOn && (currentInteraction == null || hasAudio || !currentInteraction.AllowKeyPhraseRecognition))
				{
					await _misty.StopKeyPhraseRecognitionAsync();
					_keyPhraseOn = false;
					KeyPhraseRecognitionOn?.Invoke(this, false);
				}
				else if (currentInteraction != null && currentInteraction.AllowKeyPhraseRecognition && !hasAudio && (!_keyPhraseOn || _keyPhraseTriggered))
				{
					await _misty.StartKeyPhraseRecognitionAsync(false, true, (int)(currentInteraction.ListenTimeout * 1000), (int)(currentInteraction.SilenceTimeout * 1000), null);
					_keyPhraseOn = true;
					_keyPhraseTriggered = false;
					KeyPhraseRecognitionOn?.Invoke(this, true);
				}
			}
			catch { }
			finally
			{
				_keyPhraseOnSlim.Release();
			}

			return _keyPhraseOn;
		}

		public SpeechManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters, CharacterState characterState, CharacterState stateAtAnimationStart, CharacterState previousState, IList<GenericDataStore> genericDataStores, ISpeechIntentManager speechIntentManager = null)			
		{
			_parameters = parameters;
			_misty = misty;
			_characterParameters = characterParameters;			
			_robotName = characterParameters.ConversationGroup.RobotName ?? "Misty";
			_genericDataStores = genericDataStores;
			_speechIntentManager = speechIntentManager;
			_characterState = characterState;
			_stateAtAnimationStart = stateAtAnimationStart;
			_previousState = previousState;
		}

		public void AbortListening(string audioName)
		{
			_listeningCallbacks.Remove(audioName);
			_listeningCallbacks.Remove(audioName + ".wav");
			_listenAborted = true;

			_misty.StopRecordingAudio(null);
		}

		private void ProcessVolumeResponse(IGetVolumeResponse volumeResponse)
		{
			if (volumeResponse?.Data != null && volumeResponse?.Data >= 0 && volumeResponse?.Data <= 100)
			{
				_volume = volumeResponse.Data;
			}
			else
			{
				_volume = 0;
			}
		}

		public async Task RefreshAssetLists()
		{
			await _assetWrapper.RefreshAssetLists();
		}
		
		public async virtual void Speak(AnimationRequest currentAnimation, Interaction currentInteraction)
		{
			try
			{
				bool textChanged = false;

				if (string.IsNullOrWhiteSpace(currentAnimation.Speak))
				{
					_misty.SkillLogger.LogWarning("No text passed in to Speak command.");
					return;
				}
				
				if (string.IsNullOrWhiteSpace(currentAnimation.SpeakFileName))
				{
					currentAnimation.SpeakFileName = AssetHelper.MakeFileName(currentAnimation.Speak);
				}

				//This will save files with language and voice as part of the name
				if (_characterParameters.AddLocaleToAudioNames)
				{
					currentAnimation.SpeakFileName = GetLocaleName(currentAnimation.SpeakFileName);
				}

				_listenAborted = false;

				if (_characterParameters.TextToSpeechService == "Misty")
				{
					if (currentInteraction.StartListening && !string.IsNullOrWhiteSpace(currentAnimation.SpeakFileName))
					{
						lock (_lockListenerData)
						{
							_listeningCallbacks.Remove(currentAnimation.SpeakFileName);
							_listeningCallbacks.Add(currentAnimation.SpeakFileName);
						}
					}

					_misty.Speak(currentAnimation.Speak, _characterParameters.UsePreSpeech ? false : true, currentAnimation.SpeakFileName, null);
					StartedSpeaking?.Invoke(this, currentAnimation.Speak);
					return;
				}

				if ((_azureCognitive != null && _azureCognitive.Authorized) || (_googleService != null && _googleService.Authorized))
				{
					string newText = currentAnimation.Speak;
					bool usingSSML = _characterParameters.TextToSpeechService == "Azure" && TryGetSSMLText(currentAnimation.Speak, out  newText, currentAnimation);
					currentAnimation.Speak = newText ?? currentAnimation.Speak;

					currentAnimation.SpeakFileName = AssetHelper.AddMissingWavExtension(currentAnimation.SpeakFileName);

					if (currentInteraction.StartListening)
					{
						lock (_lockListenerData)
						{
							_listeningCallbacks.Remove(currentAnimation.SpeakFileName);
							_listeningCallbacks.Add(currentAnimation.SpeakFileName);
						}
					}

					StartedSpeaking?.Invoke(this, currentAnimation.Speak);
					if (!textChanged)
					{
						if (_audioTags.Contains(currentAnimation.SpeakFileName) ||
							(!_characterParameters.RetranslateTTS &&
							_assetWrapper.AudioList.Where(x => AssetHelper.AreEqualAudioFilenames(x.Name, currentAnimation.SpeakFileName, _characterParameters.AddLocaleToAudioNames)).Any())
						)
						{
							_misty.SkillLogger.LogInfo($"Speaking with existing audio file {currentAnimation.SpeakFileName} at volume {Volume}.");
							_misty.SkillLogger.LogVerbose(currentAnimation.Speak);							
							_misty.PlayAudio(currentAnimation.SpeakFileName, Volume, null);
						}
						else
						{
							_misty.SkillLogger.LogInfo($"Creating new audio file {currentAnimation.SpeakFileName} at volume {Volume}.");
							_misty.SkillLogger.LogVerbose(currentAnimation.Speak);
							
							switch (_characterParameters.TextToSpeechService)
							{
								case "Google":
									await _googleService.Speak(currentAnimation.Speak, currentAnimation.SpeakFileName, Volume, usingSSML, 0);
									break;
								case "Azure":
								default:
									await _azureCognitive.Speak(currentAnimation.Speak, currentAnimation.SpeakFileName, Volume, usingSSML, (int)(currentAnimation.TrimAudioSilence*1000));
									break;
							}
							_audioTags.Add(currentAnimation.SpeakFileName);
						}
					}
					else
					{
						//Make a new file						
						switch (_characterParameters.TextToSpeechService)
						{
							case "Google":
								//TODO Make configurable
								await _googleService.Speak(currentAnimation.Speak, currentAnimation.SpeakFileName ?? "TTSAudio", Volume, usingSSML, 0);
								break;
							case "Azure":
							default:
								await _azureCognitive.Speak(currentAnimation.Speak, currentAnimation.SpeakFileName ?? "TTSAudio", Volume, usingSSML, (int)currentAnimation.TrimAudioSilence * 1000);
								break;
						}
					}
				}
			}
			catch(Exception ex)
			{
				_misty.SkillLogger.Log("Failed processing Speak action in Character.", ex);
				StoppedSpeaking?.Invoke(this, null);
			}
		}


		private bool TryGetSSMLText(string text, out string newText, AnimationRequest animationRequest)
		{
			try
			{
				if (text.Trim().ToLower().Replace(" ", "").EndsWith("</speak>"))
				{
					//Don't adjust if already ssml
					newText = text;
					return true;
				}

				bool usingSSML = false;
				string[] startText = new string[2];
				string[] endText = new string[2];

				if (animationRequest.SpeechRate != 1.0)
				{
					startText[0] = $"<prosody rate=\"{animationRequest.SpeechRate}\">";
					endText[0] = "</prosody>";
					usingSSML = true;
				}
				else if (!string.IsNullOrWhiteSpace(animationRequest.SpeakingStyle))
				{
					startText[1] = $"<mstts:express-as style=\"{animationRequest.SpeakingStyle}\">";
					endText[1] = "</mstts:express-as>";
					usingSSML = true;
				}

				if (usingSSML)
				{
					/*
					Speak = "<?xml version=\"1.0\" encoding=\"utf-8\"?> <speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">" +
					"<voice name=\"en-US-GuyNeural\"><prosody rate=\"2.0\">" +
					"California, the most populous US state and the first to implement a statewide lockdown to combat the coronavirus outbreak, is setting daily records this week for new cases as officials urge caution and dangle enforcement threats to try to curb the spikes." +
					"The virus is spreading at private gatherings in homes, and more young people are testing positive, Gov.Gavin Newsom said Wednesday.Infections at some prisons are raising concerns." +
					"</prosody></voice></speak>",*/

					newText = "<?xml version=\"1.0\" encoding=\"utf-8\"?> <speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"en-US\">";
					
					//TODO allow pass in override by animation
					newText += $"<voice name=\"{(string.IsNullOrWhiteSpace(animationRequest.OverrideVoice) ? _azureTTSParameters.SpeakingVoice : animationRequest.OverrideVoice)}\">";


					if (startText[0] != null)
					{
						newText += startText[0];
					}

					if (startText[1] != null)
					{
						newText += startText[1];
					}

					newText += text;

					if (endText[1] != null)
					{
						newText += endText[1];
					}

					if (endText[0] != null)
					{
						newText += endText[0];
					}

					newText += "</voice></speak>";

					return true;
				}

				newText = text;
				return false;
			}
			catch
			{
				newText = text;
				return false;
			}
		}

		protected void TTSCallback(ITextToSpeechCompleteEvent ttsComplete)
		{
			_misty.SkillLogger.LogVerbose($"TTS Callback: UtteranceId: {ttsComplete.UttteranceId}");

			AudioCallback(new AudioPlayCompleteEvent(ttsComplete.UttteranceId, -1));
		}
		
		protected void AudioCallback(IAudioPlayCompleteEvent audioComplete)
		{
			try
			{
				_recording = false;
				_misty.SkillLogger.LogVerbose($"Audio Callback. Name: {audioComplete.Name}");
				if(_processingAudioCallback)
				{
					_misty.SkillLogger.LogWarning($"Audio Callback ignored as system is still processing previous callback. Name: {audioComplete.Name}");
					return;
				}
				_processingAudioCallback = true;
				
				if (audioComplete.Name.Contains(ConversationConstants.IgnoreCallback))
				{
					PreSpeechCompleted?.Invoke(this, audioComplete);
					_misty.SkillLogger.LogVerbose($"Prespeech complete. Name: {audioComplete.Name}");
					return;
				}
				else
				{
					StoppedSpeaking?.Invoke(this, audioComplete);
				}
				lock (_lockListenerData)
				{
					if (!_recording && !_listenAborted && (_listeningCallbacks.Remove(audioComplete.Name) || _listeningCallbacks.Remove(audioComplete.Name+".wav")))
					{
						_recording = true;

						_misty.SkillLogger.LogVerbose("Capture Speech called.");
						switch (_characterParameters.SpeechRecognitionService.Trim().ToLower())
						{
							case "googleonboard":
								_ = _misty.CaptureSpeechGoogleAsync(false, _listenTimeout, _silenceTimeout, _characterParameters.GoogleSpeechRecognitionParameters.SubscriptionKey, _characterParameters.GoogleSpeechRecognitionParameters.SpokenLanguage);
								return;
							case "azureonboard":
								_ = _misty.CaptureSpeechAzureAsync(false, _listenTimeout, _silenceTimeout, _characterParameters.AzureSpeechRecognitionParameters.SubscriptionKey, _characterParameters.AzureSpeechRecognitionParameters.Region, _characterParameters.AzureSpeechRecognitionParameters.SpokenLanguage);
								return;
							case "vosk":
								_ = _misty.CaptureSpeechVoskAsync(false, _listenTimeout, _silenceTimeout);
								return;
							case "deepspeech":
								_ = _misty.CaptureSpeechDeepSpeechAsync(false, _listenTimeout, _silenceTimeout);
								return;
							default:
								_ = _misty.CaptureSpeechAsync(false, true, _listenTimeout, _silenceTimeout, null);
								return;
						}
					}
				}
			}
			catch(Exception ex)
			{
				_misty.SkillLogger.Log("Failed processing audio callback in Character.", ex);
			}
			finally
			{
				_processingAudioCallback = false;
				if(_recording)
				{
					StartedListening?.Invoke(this, DateTime.Now);
				}
			}
		}

		private void KeyPhraseCallback(IKeyPhraseRecognizedEvent keyPhraseEvent)
		{ 
			try
			{
				_misty.SkillLogger.LogVerbose("Key Phrase Callback called, calling capture speech.");
				if (_recording)
				{
					return;
				}

				KeyPhraseRecognized?.Invoke(this, keyPhraseEvent);
				_keyPhraseTriggered = true;
				_recording = true;
				_ = _misty.CaptureSpeechAsync(false, true, _listenTimeout, _silenceTimeout, null);

				StartedListening?.Invoke(this, DateTime.Now);
				return;
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Failed to process Voice Command event.", ex);
				SpeechIntent?.Invoke(this, new TriggerData("", ConversationConstants.HeardUnknownTrigger, Triggers.SpeechHeard));
			}
		}
		
		private async void VoiceRecordCallback(IVoiceRecordEvent voiceRecordEvent)
		{
			try
			{
				_recording = false;
				StoppedListening?.Invoke(this, voiceRecordEvent);
				if (_listenAborted)
				{
					_misty.SkillLogger.LogInfo("Voice Record Callback called while processing, ignoring.");
					return;
				}

				_misty.SkillLogger.LogVerbose("Voice Record Callback - processing");
				StartedProcessingVoice?.Invoke(this, voiceRecordEvent);

				string service = _characterParameters.SpeechRecognitionService.Trim().ToLower();
				if (service == "googleonboard" || service == "azureonboard" || service == "deepspeech" || service == "vosk")
				{
					HandleSpeechResponse(voiceRecordEvent?.SpeechRecognitionResult);
					return;
				}

				if (voiceRecordEvent.ErrorCode == 3)
				{
					_misty.SkillLogger.Log("Didn't hear anything with microphone.");
					SpeechIntent?.Invoke(this, new TriggerData("", ConversationConstants.HeardNothingTrigger, Triggers.SpeechHeard));					
					return;
				}

				IGetAudioResponse audioResponse;
				audioResponse = await _misty.GetAudioAsync("capture_Dialogue.wav", false);

				if(audioResponse.Status != ResponseStatus.Success)
				{
					_misty.SkillLogger.Log($"Failed to retrieve file 'capture_Dialogue.wav', received {audioResponse.Status}.  Ignoring speech intent.");
					SpeechIntent?.Invoke(this, new TriggerData("", ConversationConstants.HeardNothingTrigger, Triggers.SpeechHeard));
					return;
				}
				else if (audioResponse?.Data?.Audio == null)
				{
					_misty.SkillLogger.Log("Couldn't find the audio file 'capture_Dialogue.wav'.");
					SpeechIntent?.Invoke(this, new TriggerData("", ConversationConstants.HeardNothingTrigger, Triggers.SpeechHeard));
					return;
				}
				else if ( audioResponse?.Data?.Audio.Count() == 0)
				{			
					_misty.SkillLogger.Log("Found empty audio file 'capture_Dialogue.wav'.");
					SpeechIntent?.Invoke(this, new TriggerData("", ConversationConstants.HeardNothingTrigger, Triggers.SpeechHeard));
					return;
				}

				SpeechToTextData description = new SpeechToTextData();
				switch (_characterParameters.SpeechRecognitionService)
				{
					case "Google":
						description = await _googleService.TranslateAudioStream((byte[])audioResponse.Data.Audio);
						break;
					case "Azure":
					default:
						description = await _azureCognitive.TranslateAudioStream((byte[])audioResponse.Data.Audio);
						break;
				}

				HandleSpeechResponse(description.Text);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Failed to process Voice Command event.", ex);
				SpeechIntent?.Invoke(this, new TriggerData("", ConversationConstants.HeardUnknownTrigger, Triggers.SpeechHeard));
			}
			finally
			{
				CompletedProcessingVoice?.Invoke(this, voiceRecordEvent);
			}
		}
		
		private void HandleSpeechResponse(string text)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
                SpeechMatchData intent = _speechIntentManager.GetIntent(text, _allowedTriggers);

                //Old conversations trigger on name, new ones on id
				SpeechIntent?.Invoke(this, new TriggerData(text, intent.Id, Triggers.SpeechHeard));
				_misty.SkillLogger.LogInfo($"VoiceRecordCallback - Heard: '{text}' - Intent: {intent.Name}");
			}
			else
			{
				_misty.SkillLogger.LogInfo("Didn't hear anything or can no longer translate.");
				SpeechIntent?.Invoke(this, new TriggerData("", ConversationConstants.HeardNothingTrigger, Triggers.SpeechHeard));
			}
		}


		public bool TryToPersonalizeData(string text, AnimationRequest animationRequest, Interaction interaction, out string newText)
		{
			newText = text;
			if (_characterState == null)
			{
				return false;
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
											if (genericDataStore != null)
											{
												int dataCount = genericDataStore.Data.Count();
												int randomItem = _random.Next(optionCount, dataCount);
												GenericData genericData = genericDataStore.Data.ElementAt(randomItem).Value;
												if (genericData?.Value != null)
												{
													textChanged = true;
													ProcessUserDataUpdates(genericData);
													newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value);
												}
											}
										}
										else if (dataStore.TreatKeyAsUtterance)
										{
											GenericData genericData = _speechIntentManager.FindUserDataFromText(dataStore.Name, newKey);
											if (genericData.Value != null)
											{
												textChanged = true;
												ProcessUserDataUpdates(genericData);
												newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value);
											}
										}
										else
										{
											KeyValuePair<string, GenericData> genericData = dataStore.Data.FirstOrDefault(x => x.Value.Key.ToLower().Trim() == newKey.ToLower().Trim());
											if (genericData.Value != null)
											{
												textChanged = true;
												ProcessUserDataUpdates(genericData.Value);
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

										if (dataKey == "random")
										{
											//grab a random user data item from this group
											//{{Greetings:random}}
											GenericDataStore genericDataStore = _genericDataStores.FirstOrDefault(x => x.Name == dataStore.Name);
											if (genericDataStore != null)
											{
												int dataCount = genericDataStore.Data.Count();
												int randomItem = _random.Next(optionCount, dataCount);
												GenericData genericData = genericDataStore.Data.ElementAt(randomItem).Value;
												if (genericData?.Value != null)
												{
													textChanged = true;
													ProcessUserDataUpdates(genericData);
													newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value);
												}
											}
										}
										else if (dataStore.TreatKeyAsUtterance)
										{
											GenericData genericData = _speechIntentManager.FindUserDataFromText(dataStore.Name, newKey);
											if (genericData.Value != null)
											{
												textChanged = true;
												ProcessUserDataUpdates(genericData);
												newText = newText.Replace("{{" + replacementTextList + "}}", genericData.Value);
											}
										}
										else
										{
											KeyValuePair<string, GenericData> genericData = dataStore.Data.FirstOrDefault(x => x.Value.Key == newKey);
											if (genericData.Value != null)
											{
												textChanged = true;
												ProcessUserDataUpdates(genericData.Value);
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
				return true;
			}
			return false;
		}


		public void ProcessUserDataUpdates(GenericData genericData)
		{
			//get rid of this now that there is an animations script
			if (!string.IsNullOrWhiteSpace(genericData.ScreenText))
			{
				_ = _misty.SetTextDisplaySettingsAsync("UserDataText", new TextSettings
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

				_misty.DisplayText(genericData.ScreenText, "UserDataText", null);
			}

			if(!string.IsNullOrWhiteSpace(genericData.DataAnimationScript))
			{
				UserDataAnimationScript?.Invoke(this, genericData.DataAnimationScript);
			}
		}

		private string GetBuiltInReplacement(string option)
		{
			string newData = "";
			switch (option.ToLower().Trim())
			{
				case "face":
					newData = _characterState.LastKnownFaceSeen ??
						_characterState.FaceRecognitionEvent?.Label ??
						_stateAtAnimationStart?.FaceRecognitionEvent?.Label ??
						_previousState?.FaceRecognitionEvent?.Label ?? MissingInlineData;
					break;
				case "qrcode":
					newData = _characterState.QrTagEvent?.DecodedInfo ??
						_stateAtAnimationStart?.QrTagEvent?.DecodedInfo ??
						_previousState?.QrTagEvent?.DecodedInfo ?? MissingInlineData;
					break;
				case "arcode":
					newData = _characterState.ArTagEvent?.TagId.ToString() ??
						_stateAtAnimationStart?.ArTagEvent?.TagId.ToString() ??
						_previousState?.ArTagEvent?.TagId.ToString() ?? MissingInlineData;
					break;
				case "text":
					newData = _characterState.SpeechResponseEvent?.Text ??
						_stateAtAnimationStart?.SpeechResponseEvent?.Text ??
						_previousState?.SpeechResponseEvent?.Text ?? MissingInlineData;
					break;
				case "intent":
					newData = _characterState.SpeechResponseEvent?.TriggerFilter ??
						_stateAtAnimationStart?.SpeechResponseEvent?.TriggerFilter ??
						_previousState?.SpeechResponseEvent?.TriggerFilter ?? MissingInlineData;
					break;
				case "robotname":
					newData = string.IsNullOrWhiteSpace(_robotName) ? "Misty" : _robotName;
					break;
				case "time":
					newData = _timeManager.GetTimeObject().SpokenTime ?? MissingInlineData;
					break;
			}
			return newData;

		}


		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_misty.UnregisterAllEvents(null);
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
 