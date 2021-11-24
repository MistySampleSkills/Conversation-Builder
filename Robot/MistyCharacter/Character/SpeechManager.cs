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
using System.IO;
using System.Linq;
using System.Text;
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

namespace MistyCharacter
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

		private const string MissingInlineData = "unknown";
		private const string TTSNamePreface = "misty-en-";
		private const string SkillNamePreface = "skill-";

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
		private int _silenceTimeout = 10000;
		private int _listenTimeout = 10000;
		private IList<string> _allowedTriggers = new List<string>();
		private bool _keyPhraseTriggered;
		private bool _keyPhraseOn;
		private Random _random = new Random();
		private IList<string> _replacementValues = new List<string> { "face", "filter", "qrcode", "arcode", "text", "day", "partofday", "intent", "time", "robotname", "emotion", "audio", "charge", "image", "serial", "object" };

		private IList<GenericDataStore> _genericDataStores = new List<GenericDataStore>();
		private string _robotName = "Misty";
		private CharacterState _characterState;
		private string _speakingStyle = "";
		private double _speechRate = 1.0;
		private string _language = "en-US";
		private string _emphasis = "none";
		private string _sayAs = "";
		private string _voice = "";
		private string _pitch = "medium";
		private SkillSpeech _skillSpeech;

		private ListeningState _listeningState = ListeningState.Waiting;

		private SemaphoreSlim _displaySlim = new SemaphoreSlim(1, 1);
		private SemaphoreSlim _speakingSlim = new SemaphoreSlim(1, 1);
		private SemaphoreSlim _keyPhraseOnSlim = new SemaphoreSlim(1, 1);
		private IDictionary<string, object> _parameters { get; set; }
		private IRobotMessenger _misty { get; set; }
		private CharacterParameters _characterParameters { get; set; }
		private bool _speechOverridden = false;
		private bool _externalOverridden = false;
		private ICommandManager _commandManager;

		private int _volume = 20;
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

		public void HandleInteractionEnded(object sender, string interaction)
		{
			_ = _misty.SetTextDisplaySettingsAsync("UserDataText", new TextSettings
			{
				 Visible = false
			});

			_misty.DisplayText("", "UserDataText", null);
		}

		public string MakeTextBasedFileName(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}

			StringBuilder sb = new StringBuilder();
			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
			{
				byte[] inputBytes = Encoding.ASCII.GetBytes(text);
				byte[] hashBytes = md5.ComputeHash(inputBytes);

				for (int i = 0; i < hashBytes.Length; i++)
				{
					sb.Append(hashBytes[i].ToString("X2"));
				}
			}

			return AssetHelper.AddMissingWavExtension(_characterParameters.AddLocaleToAudioNames ? GetLocaleName(sb.ToString()): sb.ToString());
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

		public bool CancelSpeechProcessing()
		{
			if (_externalOverridden)
			{
				_externalOverridden = false;
				return false;
			}
			_speechOverridden = true;
			return true;
		}

		public bool HandleExternalSpeech(string text = null)
		{
			if (CancelSpeechProcessing() && !string.IsNullOrWhiteSpace(text))
			{
				return HandleSpeechResponse(text);
			}
			return false;
		}
		
		private string GetLocaleName(string name)
		{
			if (_characterParameters.AddLocaleToAudioNames)
			{
				string speakingVoice = "";

				switch (_characterParameters.TextToSpeechService)
				{
					case "google":
					case "googleonboard":
						speakingVoice = (string.IsNullOrWhiteSpace(_googleTTSParameters.SpeakingVoice) ? "def" : _googleTTSParameters.SpeakingVoice);
						string spokenLanguage = (string.IsNullOrWhiteSpace(_googleTTSParameters.SpokenLanguage) ? "en-US" : _googleTTSParameters.SpokenLanguage);
						string speakingGender = (string.IsNullOrWhiteSpace(_googleTTSParameters.SpeakingGender) ? "Female" : _googleTTSParameters.SpeakingGender);

						if (!name.Contains(speakingVoice))
						{
							name += speakingVoice;
						}
						if (!name.Contains(spokenLanguage))
						{
							name += spokenLanguage;
						}
						if (!name.Contains(speakingGender))
						{
							name += speakingGender;
						}
						break;
					case "azure":
					case "azureonboard":
						speakingVoice = (string.IsNullOrWhiteSpace(_azureTTSParameters.SpeakingVoice) ? "def" : _azureTTSParameters.SpeakingVoice);
						string translatedLanguage = string.IsNullOrWhiteSpace(_azureTTSParameters.TranslatedLanguage) ? "en-US" : _azureTTSParameters.TranslatedLanguage;
						if (!name.Contains(speakingVoice))
						{
							name += speakingVoice;
						}
						if (!name.Contains(translatedLanguage))
						{
							name += translatedLanguage;
						}
						break;
					case "skill":
						if (!name.Contains(SkillNamePreface))
						{
							name = SkillNamePreface + name;
						}
						if (!name.Contains("_v_"))
						{
							name += $"_v_{_voice.Replace(".", "p").Replace(",", "c")}";
						}
						break;
					default:
						if (!name.Contains(TTSNamePreface))
						{
							name = TTSNamePreface + name;
						}
						if (!name.Contains("_v_"))
						{
							name += $"_v_{_voice.Replace(".", "p").Replace(",", "c")}";
						}
						break;
				}

				if (!name.Contains("_p"))
				{
					name += $"_p{_pitch.Replace(".", "p").Replace(",", "c")}";
				}
				if (!name.Contains("_r"))
				{
					name += $"_r{_speechRate.ToString().Replace(".", "p").Replace(",", "c")}";
				}
				if (!name.Contains("_st"))
				{
					name += $"_st{_speakingStyle.Replace(".", "p").Replace(",", "c")}";
				}
				if (!name.Contains("_e"))
				{
					name += $"_e{_emphasis.Replace(".", "p").Replace(",", "c")}";
				}
				if (!name.Contains("_sa"))
				{
					name += $"_sa{_sayAs.Replace(".", "p").Replace(",", "c")}";
				}
			}
			return name;
		}

		public void SetAllowedUtterances(IList<string> allowedUtterances)
		{
			_allowedTriggers = allowedUtterances;
		}

		public void SetAudioTrim(int trimMs)
		{
			_audioTrim = trimMs;
		}

		public void SetMaxSilence(int silenceTimeout)
		{
			_silenceTimeout = silenceTimeout >= 1000 ? silenceTimeout : 1000;
		}

		public void SetMaxListen(int listenTimeout)
		{
			_listenTimeout = listenTimeout >= 1000 ? listenTimeout : 1000;
		}

		public async Task<bool> Initialize()
		{
			if(_commandManager != null && _commandManager.TryGetReplacements(out IList<string> userReplacements))
			{
				foreach(string replaceString in userReplacements)
				{
					if(!_replacementValues.Contains(replaceString.Trim().ToLower()))
					{
						_replacementValues.Add(replaceString.Trim().ToLower());
					}
				}
			}

			//Passed in speech parameters
			_azureSpeechRecognitionParameters = _characterParameters.AzureSpeechRecognitionParameters;
			_googleSpeechRecognitionParameters = _characterParameters.GoogleSpeechRecognitionParameters;
			_azureTTSParameters = _characterParameters.AzureTTSParameters;
			_googleTTSParameters = _characterParameters.GoogleTTSParameters;

			_timeManager = new EnglishTimeManager(_misty, _parameters, _characterParameters);
			_assetWrapper = new AssetWrapper(_misty);
			await _assetWrapper.RefreshAssetLists();

			AzureServiceAuthorization azureRecAuth = null;
			AzureServiceAuthorization azureTtsAuth = null;
			GoogleServiceAuthorization googleRecAuth = null;
			GoogleServiceAuthorization googleTtsAuth = null;

			//File based auth parameters
			bool userAsrAuth =_commandManager.TryGetAuth("speech-recognition", out ICommandAuthorization robotSpeechRecognitionAuth) && robotSpeechRecognitionAuth?.AuthFields != null && robotSpeechRecognitionAuth.AuthFields.Count() > 1;
			bool userTtsAuth = _commandManager.TryGetAuth("text-to-speech", out ICommandAuthorization robotTTSAuth) && robotTTSAuth?.AuthFields != null && robotTTSAuth.AuthFields.Count() > 1;
			
			//speech rec auth check
			try
			{
				if (userAsrAuth &&
				robotSpeechRecognitionAuth.AuthFields.TryGetValue("Service", out string service))
				{
					if (service.Trim().ToLower() == "azure")
					{

						bool servAcct = false;
						if(robotSpeechRecognitionAuth.AuthFields.TryGetValue("IsServiceAccount", out string isServiceAccount))
						{
							servAcct = Convert.ToBoolean(isServiceAccount);
						}
						
						if(servAcct)
						{
							_characterParameters.SpeechRecognitionService = "azureonboard";
						}
						else
						{
							_characterParameters.SpeechRecognitionService = "azure";
						}

						if (robotSpeechRecognitionAuth.AuthFields.TryGetValue("Region", out string region) &&
							   robotSpeechRecognitionAuth.AuthFields.TryGetValue("Endpoint", out string endpoint) &&
							   robotSpeechRecognitionAuth.AuthFields.TryGetValue("SubscriptionKey", out string key))
						{
							azureRecAuth = new AzureServiceAuthorization
							{
								Region = region ?? "",
								Endpoint = endpoint ?? "",
								SubscriptionKey = key ?? ""
							};
						}
					}
					else if (service.Trim().ToLower() == "google")
					{
						bool servAcct = false;
						if (robotSpeechRecognitionAuth.AuthFields.TryGetValue("IsServiceAccount", out string isServiceAccount))
						{
							servAcct = Convert.ToBoolean(isServiceAccount);
						}

						if (servAcct)
						{
							_characterParameters.SpeechRecognitionService = "googleonboard";
						}
						else
						{
							_characterParameters.SpeechRecognitionService = "google";
						}
						if (robotSpeechRecognitionAuth.AuthFields.TryGetValue("Endpoint", out string endpoint) &&
								robotSpeechRecognitionAuth.AuthFields.TryGetValue("SubscriptionKey", out string key))
						{
							googleRecAuth = new GoogleServiceAuthorization
							{
								Endpoint = endpoint ?? "",
								SubscriptionKey = key ?? ""
							};
						}
					}
					else
					{
						_characterParameters.SpeechRecognitionService = service.Trim().ToLower();
					}
				}
				else if (!string.IsNullOrWhiteSpace(_characterParameters?.SpeechRecognitionService))
				{
					if (_characterParameters?.SpeechRecognitionService.Trim().ToLower() == "azure")
					{
						azureRecAuth = new AzureServiceAuthorization
						{
							Region = _azureSpeechRecognitionParameters.Region ?? "",
							Endpoint = _azureSpeechRecognitionParameters.Endpoint ?? "",
							SubscriptionKey = _azureSpeechRecognitionParameters.SubscriptionKey ?? ""
						};
					}
					else if (_characterParameters?.SpeechRecognitionService.Trim().ToLower() == "google")
					{
						googleRecAuth = new GoogleServiceAuthorization
						{
							Endpoint = _googleSpeechRecognitionParameters.Endpoint ?? "",
							SubscriptionKey = _googleSpeechRecognitionParameters.SubscriptionKey ?? ""
						};
					}
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogError("Failed processing speech recognition parameters.", ex);
			}

			//TTS check
			try
			{
				if (userTtsAuth &&
				robotTTSAuth.AuthFields.TryGetValue("Service", out string service))
				{
					if (service.Trim().ToLower() == "azure")
					{
						bool servAcct = false;
						if (robotTTSAuth.AuthFields.TryGetValue("IsServiceAccount", out string isServiceAccount))
						{
							servAcct = Convert.ToBoolean(isServiceAccount);
						}

						if (servAcct)
						{
							_characterParameters.TextToSpeechService = "azureonboard";
						}
						else
						{
							_characterParameters.TextToSpeechService = "azure";
						}

						if (robotTTSAuth.AuthFields.TryGetValue("Region", out string region) &&
							   robotTTSAuth.AuthFields.TryGetValue("Endpoint", out string endpoint) &&
							   robotTTSAuth.AuthFields.TryGetValue("SubscriptionKey", out string key))
						{
							azureTtsAuth = new AzureServiceAuthorization
							{
								Region = region ?? "",
								Endpoint = endpoint ?? "",
								SubscriptionKey = key ?? ""
							};
						}
					}
					else if (service.Trim().ToLower() == "google")
					{
						bool servAcct = false;
						if (robotTTSAuth.AuthFields.TryGetValue("IsServiceAccount", out string isServiceAccount))
						{
							servAcct = Convert.ToBoolean(isServiceAccount);
						}

						if (servAcct)
						{
							_characterParameters.TextToSpeechService = "googleonboard";
						}
						else
						{
							_characterParameters.TextToSpeechService = "google";
						}

						if (robotTTSAuth.AuthFields.TryGetValue("Endpoint", out string endpoint) &&
								robotTTSAuth.AuthFields.TryGetValue("SubscriptionKey", out string key))
						{
							googleTtsAuth = new GoogleServiceAuthorization
							{
								Endpoint = endpoint ?? "",
								SubscriptionKey = key ?? ""
							};
						}
					}
					else
					{
						_characterParameters.TextToSpeechService = service.Trim().ToLower();
					}
				}
				else if (!string.IsNullOrWhiteSpace(_characterParameters?.TextToSpeechService))
				{
					if (_characterParameters?.TextToSpeechService.Trim().ToLower() == "azure")
					{
						azureRecAuth = new AzureServiceAuthorization
						{
							Region = _azureTTSParameters.Region ?? "",
							Endpoint = _azureTTSParameters.Endpoint ?? "",
							SubscriptionKey = _azureTTSParameters.SubscriptionKey ?? ""
						};
					}
					else if (_characterParameters?.TextToSpeechService.Trim().ToLower() == "google")
					{
						googleRecAuth = new GoogleServiceAuthorization
						{
							Endpoint = _googleTTSParameters.Endpoint ?? "",
							SubscriptionKey = _googleTTSParameters.SubscriptionKey ?? ""
						};
					}
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogError("Failed processing text to speech parameters.", ex);
			}
			

			if(azureTtsAuth != null || azureRecAuth != null)
			{
				azureTtsAuth = azureTtsAuth ?? azureRecAuth;
				azureRecAuth = azureRecAuth ?? azureTtsAuth;

				_azureCognitive = new AzureSpeechService(azureTtsAuth, azureRecAuth, _misty);
				if (_azureCognitive != null && _azureCognitive.Initialize())
				{
					if (userTtsAuth)
					{
						if(robotTTSAuth.AuthFields.TryGetValue("SpeakingVoice", out string speakingVoice))
						{
							_azureCognitive.SpeakingVoice = speakingVoice ?? "en-US-AriaNeural";
						}

						if (robotTTSAuth.AuthFields.TryGetValue("SpokenLanguage", out string spokenLanguage))
						{
							_azureCognitive.SpokenLanguage = spokenLanguage ?? "en-US";
						}

						if (robotTTSAuth.AuthFields.TryGetValue("TranslatedLanguage", out string translatedLanguage))
						{
							_azureCognitive.TranslatedLanguage = translatedLanguage ?? "en-US";
						}

						if (robotTTSAuth.AuthFields.TryGetValue("ProfanitySetting", out string profanitySetting))
						{
							_azureCognitive.ProfanitySetting = profanitySetting ?? "raw";
						}
					}
					else
					{
						_azureCognitive.SpeakingVoice = _azureTTSParameters?.SpeakingVoice ?? "en-US-AriaNeural";
						_azureCognitive.SpokenLanguage = _azureTTSParameters?.SpokenLanguage ?? "en-US";
						_azureCognitive.TranslatedLanguage = _azureSpeechRecognitionParameters?.TranslatedLanguage ?? "en-US";
						_azureCognitive.ProfanitySetting = _azureSpeechRecognitionParameters?.ProfanitySetting ?? "raw";
					}
				}
				
			}

			if (googleTtsAuth != null || googleRecAuth != null)
			{
				googleTtsAuth = googleTtsAuth ?? googleRecAuth;
				googleRecAuth = googleRecAuth ?? googleTtsAuth;

				_googleService = new GoogleSpeechService(googleTtsAuth, googleRecAuth, _misty);
				if (_googleService != null && _googleService.Initialize())
				{
					if (userTtsAuth)
					{
						if (robotTTSAuth.AuthFields.TryGetValue("SpeakingVoice", out string speakingVoice))
						{
							_googleService.SpeakingVoice = speakingVoice ?? "en-US-Standard-C";
						}

						if (robotTTSAuth.AuthFields.TryGetValue("SpokenLanguage", out string spokenLanguage))
						{
							_googleService.SpokenLanguage = spokenLanguage ?? "en-US";
						}

						if (robotTTSAuth.AuthFields.TryGetValue("SpeakingGender", out string speakingGender))
						{
							_googleService.SpeakingGender = speakingGender ?? "Female";
						}
					}
					else
					{
						_googleService.SpeakingVoice = _googleSpeechRecognitionParameters?.SpeakingVoice ?? "en-US-Standard-C";
						_googleService.SpeakingGender = _googleSpeechRecognitionParameters?.SpeakingGender ?? "Female";
						_googleService.SpokenLanguage = _googleSpeechRecognitionParameters?.SpokenLanguage ?? "en-US";
					}
				}

			}



			//else if (_azureSpeechRecognitionParameters?.SubscriptionKey != null || 
			//	_azureTTSParameters?.SubscriptionKey != null)
			//{
			//	AzureServiceAuthorization recAuth = new AzureServiceAuthorization
			//	{
			//		Region = _azureSpeechRecognitionParameters.Region ?? "",
			//		Endpoint = _azureSpeechRecognitionParameters.Endpoint ?? "",
			//		SubscriptionKey = _azureSpeechRecognitionParameters.SubscriptionKey ?? ""
			//	};

			//	AzureServiceAuthorization ttsAuth = new AzureServiceAuthorization
			//	{
			//		Region = _azureTTSParameters.Region ?? "",
			//		Endpoint = _azureTTSParameters.Endpoint ?? "",
			//		SubscriptionKey = _azureTTSParameters.SubscriptionKey ?? ""
			//	};

			//	_azureCognitive = new AzureSpeechService(ttsAuth, recAuth, _misty);
			//	if (_azureCognitive != null && _azureCognitive.Initialize())
			//	{
			//		_azureCognitive.SpeakingVoice = _azureSpeechRecognitionParameters?.SpeakingVoice ?? "en-US-AriaNeural";
			//		_azureCognitive.SpokenLanguage = _azureSpeechRecognitionParameters?.SpokenLanguage ?? "en-US";
			//		_azureCognitive.TranslatedLanguage = _azureSpeechRecognitionParameters?.TranslatedLanguage ?? "en-US";
			//		_azureCognitive.ProfanitySetting = _azureSpeechRecognitionParameters?.ProfanitySetting ?? "raw";
			//	}			
			//}

			//if (_googleSpeechRecognitionParameters?.SubscriptionKey != null || _googleTTSParameters?.SubscriptionKey != null)
			//{
			//	GoogleServiceAuthorization speechRecAuth = new GoogleServiceAuthorization
			//	{
			//		Endpoint = _googleSpeechRecognitionParameters?.Endpoint ?? "",
			//		SubscriptionKey = _googleSpeechRecognitionParameters?.SubscriptionKey ?? ""
			//	};

			//	GoogleServiceAuthorization ttsAuth = new GoogleServiceAuthorization
			//	{
			//		Endpoint = _googleTTSParameters?.Endpoint ?? "",
			//		SubscriptionKey = _googleTTSParameters?.SubscriptionKey ?? ""
			//	};

			//	_googleService = new GoogleSpeechService(ttsAuth, speechRecAuth, _misty);
			//	if (_googleService != null && _googleService.Initialize())
			//	{
			//		_googleService.SpeakingVoice = _googleSpeechRecognitionParameters?.SpeakingVoice ?? "en-US-Standard-C";
			//		_googleService.SpeakingGender = _googleSpeechRecognitionParameters?.SpeakingGender ?? "Female";
			//		_googleService.SpokenLanguage = _googleSpeechRecognitionParameters?.SpokenLanguage ?? "en-US";
			//	}
			//}

			

			_skillSpeech = new SkillSpeech(_azureSpeechRecognitionParameters?.SpeakingVoice ?? _googleSpeechRecognitionParameters?.SpeakingVoice ?? "Zira");

			LogEventDetails(_misty.RegisterVoiceRecordEvent(VoiceRecordCallback, 0, true, "VoiceRecord", null));
			LogEventDetails(_misty.RegisterKeyPhraseRecognizedEvent(KeyPhraseCallback, 0, true, "KeyPhrase", null));
			LogEventDetails(_misty.RegisterAudioPlayCompleteEvent(AudioCallback, 0, true, "CharacterAudioComplete", null));
			LogEventDetails(_misty.RegisterTextToSpeechCompleteEvent(TTSCallback, 0, true, "CharacterTTSComplete", null));

			if (_characterParameters.ShowSpeakingIndicator)
			{
				if(!string.IsNullOrWhiteSpace(_characterParameters.SpeakingImage))
				{
					await _misty.SetImageDisplaySettingsAsync("Speaking", new ImageSettings
					{
						VerticalAlignment = ImageVerticalAlignment.Bottom,
						HorizontalAlignment = ImageHorizontalAlignment.Center,
						PlaceOnTop = true,
						Stretch = ImageStretch.None,
						Visible = false,
						Height = 50
					});
				}
				else
				{
					await _misty.SetTextDisplaySettingsAsync("ListeningText", new TextSettings
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
						Height = 40
					});
				}
				
			}

			if (_characterParameters.ShowListeningIndicator)
			{
				if (!string.IsNullOrWhiteSpace(_characterParameters.ListeningImage) || !string.IsNullOrWhiteSpace(_characterParameters.ProcessingImage))
				{
					await _misty.SetImageDisplaySettingsAsync("Listening", new ImageSettings
					{
						VerticalAlignment = ImageVerticalAlignment.Bottom,
						HorizontalAlignment = ImageHorizontalAlignment.Right,
						PlaceOnTop = true,
						Stretch = ImageStretch.None,
						Visible = false,
						Height = 50
					});
				}
				else
				{
					await _misty.SetTextDisplaySettingsAsync("ListeningText", new TextSettings
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
						Height = 40
					});
				}
			}			

			_ = ManageListeningDisplay(ListeningState.Waiting);
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

		public SpeechManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters, CharacterState characterState, IList<GenericDataStore> genericDataStores, ISpeechIntentManager speechIntentManager = null, ICommandManager commandManager = null)
		{
			_parameters = parameters;
			_misty = misty;
			_characterParameters = characterParameters;			
			_robotName = characterParameters.ConversationGroup.RobotName ?? "Misty";
			_genericDataStores = genericDataStores;
			_speechIntentManager = speechIntentManager;
			_characterState = characterState;
			_commandManager = commandManager;
		}

		public void AbortListening(string audioName)
		{
			_listeningCallbacks.Remove(audioName);
			_listeningCallbacks.Remove(audioName + ".wav");
			_listenAborted = true;

			_misty.StopRecordingAudio(null);
		}

		private bool _speakingIndictorShowing = false;
		private bool _listeningIndictorShowing = false;
		private bool _textIndictorShowing = false;

		private async Task ManageListeningDisplay(ListeningState listeningState)
		{
			try
			{
				//TODO Cleanup
				await _displaySlim.WaitAsync();

				if (_listeningState == listeningState)
				{
					return;
				}

				switch (listeningState)
				{
					case ListeningState.Speaking:
						if(_characterParameters.ShowSpeakingIndicator)
						{
							if (!string.IsNullOrWhiteSpace(_characterParameters.SpeakingImage))
							{
								if(!_speakingIndictorShowing)
								{
									_listeningIndictorShowing = false;
									_speakingIndictorShowing = true;
									_textIndictorShowing = false;

									await _misty.SetImageDisplaySettingsAsync("Listening", new ImageSettings
									{
										Visible = false
									});

									await _misty.SetImageDisplaySettingsAsync("Speaking", new ImageSettings
									{
										PlaceOnTop = true,
										Visible = true,
									});

									await _misty.SetTextDisplaySettingsAsync("ListeningText", new TextSettings
									{
										Visible = false
									});
									
									_misty.DisplayImage(_characterParameters.SpeakingImage, "Speaking", false, null);
								}
								
							}
							else
							{
								if (!_textIndictorShowing)
								{
									_listeningIndictorShowing = false;
									_speakingIndictorShowing = false;
									_textIndictorShowing = true;

									await _misty.SetImageDisplaySettingsAsync("Listening", new ImageSettings
									{
										Visible = false
									});

									await _misty.SetImageDisplaySettingsAsync("Speaking", new ImageSettings
									{
										Visible = false
									});

									await _misty.SetTextDisplaySettingsAsync("ListeningText", new TextSettings
									{
										Visible = true,
										PlaceOnTop = true
									});
								}

								_misty.DisplayText("Speaking...", "ListeningText", null);
							}
						}
						break;
					case ListeningState.ProcessingSpeech:
						if (_characterParameters.ShowListeningIndicator && !_characterParameters.UsePreSpeech)
						{
							if (!string.IsNullOrWhiteSpace(_characterParameters.ProcessingImage))
							{
								if (!_listeningIndictorShowing)
								{
									_speakingIndictorShowing = false;
									_listeningIndictorShowing = true;
									_textIndictorShowing = false;

									await _misty.SetImageDisplaySettingsAsync("Speaking", new ImageSettings
									{
										Visible = false
									});

									await _misty.SetImageDisplaySettingsAsync("Listening", new ImageSettings
									{
										PlaceOnTop = true,
										Visible = true,
									});

									await _misty.SetTextDisplaySettingsAsync("ListeningText", new TextSettings
									{
										Visible = false
									});
								}

								_misty.DisplayImage(_characterParameters.ProcessingImage, "Listening", false, null);
							}
							else
							{
								if (!_textIndictorShowing)
								{
									_listeningIndictorShowing = false;
									_speakingIndictorShowing = false;
									_textIndictorShowing = true;

									await _misty.SetImageDisplaySettingsAsync("Listening", new ImageSettings
									{
										Visible = false
									});

									await _misty.SetImageDisplaySettingsAsync("Speaking", new ImageSettings
									{
										Visible = false
									});

									await _misty.SetTextDisplaySettingsAsync("ListeningText", new TextSettings
									{
										Visible = true,
										PlaceOnTop = true
									});
								}

								_misty.DisplayText("Processing...", "ListeningText", null);
							}
						}
						
						break;
					case ListeningState.Recording:
						if (_characterParameters.ShowListeningIndicator && !_characterParameters.UsePreSpeech)
						{
							if (!string.IsNullOrWhiteSpace(_characterParameters.ListeningImage))
							{
								if (!_listeningIndictorShowing)
								{
									_speakingIndictorShowing = false;
									_listeningIndictorShowing = true;
									_textIndictorShowing = false;

									await _misty.SetImageDisplaySettingsAsync("Speaking", new ImageSettings
									{
										Visible = false
									});

									await _misty.SetImageDisplaySettingsAsync("Listening", new ImageSettings
									{
										PlaceOnTop = true,
										Visible = true,
									});


									await _misty.SetTextDisplaySettingsAsync("ListeningText", new TextSettings
									{
										Visible = false
									});
								}
								_misty.DisplayImage(_characterParameters.ListeningImage, "Listening", false, null);
							}
							else
							{
								if (!_textIndictorShowing)
								{
									_listeningIndictorShowing = false;
									_speakingIndictorShowing = false;
									_textIndictorShowing = true;

									await _misty.SetImageDisplaySettingsAsync("Listening", new ImageSettings
									{
										Visible = false
									});

									await _misty.SetImageDisplaySettingsAsync("Speaking", new ImageSettings
									{
										Visible = false
									});

									await _misty.SetTextDisplaySettingsAsync("ListeningText", new TextSettings
									{
										Visible = true,
										PlaceOnTop = true
									});
								}

								_misty.DisplayText("Listening...", "ListeningText", null);
							}
						}
						break;
					case ListeningState.Waiting:
					default:
						if (_characterParameters.ShowSpeakingIndicator || _characterParameters.ShowListeningIndicator)
						{
							_speakingIndictorShowing = false;
							_listeningIndictorShowing = false;
							_textIndictorShowing = false;

							await _misty.SetImageDisplaySettingsAsync("Speaking", new ImageSettings
							{
								Visible = false
							});

							await _misty.SetImageDisplaySettingsAsync("Listening", new ImageSettings
							{
								Visible = false
							});

							await _misty.SetTextDisplaySettingsAsync("ListeningText", new TextSettings
							{
								Visible = false
							});
						}
						break;
				}
				_listeningState = listeningState;
			}
			catch
			{ }
			finally
			{
				_displaySlim.Release();
			}

		}

		public async virtual Task Speak(AnimationRequest currentAnimation, Interaction currentInteraction, bool backgroundSpeech)
		{
			try
			{
				await _speakingSlim.WaitAsync();
				if (string.IsNullOrWhiteSpace(currentAnimation.Speak))
				{
					_misty.SkillLogger.LogWarning("No text passed in to Speak command.");
					return;
				}

				currentAnimation.Speak = currentAnimation.Speak.Replace("’", "'").Replace("“", "\"").Replace("”", "\"");
				
				if (string.IsNullOrWhiteSpace(currentAnimation.SpeakFileName) && !string.IsNullOrWhiteSpace(currentAnimation.Speak))
				{
					currentAnimation.SpeakFileName = MakeTextBasedFileName(currentAnimation.Speak);
					if (backgroundSpeech)
					{
						currentAnimation.SpeakFileName = ConversationConstants.IgnoreCallback + currentAnimation.SpeakFileName;
					}
				}

				if(!string.IsNullOrWhiteSpace(currentAnimation.OverrideVoice))
				{
					SetVoice(currentAnimation.OverrideVoice);
				}

				_listenAborted = false;
				_ = ManageListeningDisplay(ListeningState.Speaking);

				if (_characterParameters.TextToSpeechService == "misty")
				{
					if (currentInteraction.StartListening && !string.IsNullOrWhiteSpace(currentAnimation.SpeakFileName))
					{
						lock (_lockListenerData)
						{
							_listeningCallbacks.Remove(currentAnimation.SpeakFileName);
							_listeningCallbacks.Add(currentAnimation.SpeakFileName);
						}
					}
					
					string newText = currentAnimation.Speak;
					bool usingSSML = TryGetSSMLText(currentAnimation.Speak, out newText, currentAnimation);
					StartedSpeaking?.Invoke(this, currentAnimation.Speak);
					//_misty.Speak(currentAnimation.Speak, _characterParameters.UsePreSpeech ? false : true, currentAnimation.SpeakFileName, null);
					await _misty.SpeakAsync(currentAnimation.Speak, true, currentAnimation.SpeakFileName);
					return;
				}
				else if (_characterParameters.TextToSpeechService == "skill")
				{
					if (currentInteraction.StartListening && !string.IsNullOrWhiteSpace(currentAnimation.SpeakFileName))
					{
						lock (_lockListenerData)
						{
							_listeningCallbacks.Remove(currentAnimation.SpeakFileName);
							_listeningCallbacks.Add(currentAnimation.SpeakFileName);
						}
					}
					
					if (_audioTags.Contains(currentAnimation.SpeakFileName) ||
						(!_characterParameters.RetranslateTTS &&
						_assetWrapper.AudioList.Where(x => AssetHelper.AreEqualAudioFilenames(x.Name, currentAnimation.SpeakFileName, _characterParameters.AddLocaleToAudioNames)).Any())
					)
					{
						_misty.SkillLogger.LogInfo($"Speaking with existing audio file {currentAnimation.SpeakFileName} at volume {Volume}.");
						_misty.SkillLogger.LogVerbose(currentAnimation.Speak);
						StartedSpeaking?.Invoke(this, currentAnimation.Speak);
						await _misty.PlayAudioAsync(currentAnimation.SpeakFileName, Volume);
					}
					else
					{
						_misty.SkillLogger.LogInfo($"Creating new audio file {currentAnimation.SpeakFileName} at volume {Volume}.");
						_misty.SkillLogger.LogVerbose(currentAnimation.Speak);

						string newText = currentAnimation.Speak;

						StartedSpeaking?.Invoke(this, currentAnimation.Speak);
						Stream audio;
						if (newText.Trim().ToLower().Replace(" ", "").EndsWith("</speak>"))
						{
							audio = await _skillSpeech.SsmlToStream(newText);
						}
						else
						{
							audio = await _skillSpeech.TextToStream(newText);
						}

						MemoryStream ms = new MemoryStream();
						audio.CopyTo(ms);
						_audioTags.Add(currentAnimation.SpeakFileName);
						await _misty.SaveAudioAsync(currentAnimation.SpeakFileName, ms.ToArray(), true, true);
					}					
					return;
				}
				else if ((_azureCognitive != null && _azureCognitive.Authorized) || (_googleService != null && _googleService.Authorized))
				{
					string newText = currentAnimation.Speak;
					bool usingSSML = _characterParameters.TextToSpeechService == "azure" && TryGetSSMLText(currentAnimation.Speak, out  newText, currentAnimation);
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
							case "google":
								await _googleService.Speak(currentAnimation.Speak, currentAnimation.SpeakFileName, Volume, usingSSML, 0);
								break;
							case "azure":
							default:
								await _azureCognitive.Speak(currentAnimation.Speak, currentAnimation.SpeakFileName, Volume, usingSSML, (int)(currentAnimation.TrimAudioSilence*1000));
								break;
						}
						_audioTags.Add(currentAnimation.SpeakFileName);
					}
				
				}
			}
			catch(Exception ex)
			{
				_misty.SkillLogger.Log("Failed processing Speak action in Character.", ex);
				StoppedSpeaking?.Invoke(this, null);
			}
			finally
			{
				//Don't hate me
				//There is a possibility, even with my skill locks, that 2 audio files can be sent so close together that they process out of order, even if sent in order
				//assuming any speech is at least 250
				await Task.Delay(250);
				_speakingSlim.Release();
			}
		}

		public void SetSpeechRate(double rate)
		{
			if (_characterParameters.TextToSpeechService == "skill")
			{
				_skillSpeech.SetRate(rate);
			}

			_speechRate = rate;
		}
		public void SetSpeakingStyle(string speakingStyle)
		{
			if(!string.IsNullOrWhiteSpace(speakingStyle))
			{
				_speakingStyle = speakingStyle;
			}
		}
		
		public void SetLanguage(string language)
		{
			if (!string.IsNullOrWhiteSpace(language))
			{
				_language = language;
			}
		}
		
		public void SetVoice(string voice)
		{
			if (!string.IsNullOrWhiteSpace(voice))
			{
				if(_characterParameters.TextToSpeechService == "skill")
				{
					_skillSpeech.SetVoice(voice);
				}
				_voice = voice;
			}
		}

		public void SetPitch(string pitch)
		{
			try
			{
				//overloaded poorly
				if (!string.IsNullOrWhiteSpace(pitch))
				{
					if (_characterParameters.TextToSpeechService == "skill")
					{
						_skillSpeech.SetPitch(Convert.ToDouble(pitch));
					}
					_pitch = pitch;
				}
			}
			catch
			{

			}
			
		}

		//TODO this isn't really being used right in conversation...
		private bool TryGetSSMLText(string text, out string newText, AnimationRequest animationRequest)
		{
			try
			{
				SetSpeakingStyle(animationRequest.SpeakingStyle);
				SetSpeechRate(animationRequest.SpeechRate);

				if (text.Trim().ToLower().Replace(" ", "").EndsWith("</speak>"))
				{
					//Don't adjust if already ssml
					newText = text;
					return true;
				}
				
				string[] startText = new string[3];
				string[] endText = new string[3];

				if (_characterParameters.TextToSpeechService == "misty")
				{
					newText = text;
					
					string prosody = $"<prosody rate=\"{_speechRate}\"";
					if(!string.IsNullOrWhiteSpace(_pitch))
					{
						prosody += $" pitch=\"{_pitch}\">";
					}
					else
					{
						prosody += ">";
					}
					startText[0] = prosody;
					endText[0] = "</prosody>";

					if (!string.IsNullOrWhiteSpace(_sayAs))
					{
						startText[1] = $"<say-as interpret-as=\"{_sayAs}\">";
						endText[1] = "</say-as>";
					}
					
					if (!string.IsNullOrWhiteSpace(_emphasis))
					{
						startText[2] = $"<emphasis level=\"{_emphasis}\">";
						endText[2] = "</emphasis>";
					}

					newText = $"?xml version=\"1.0\" encoding=\"utf-8\"?> <speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"{_language}\">";

					if(!string.IsNullOrWhiteSpace(_voice))
					{
						newText += $"<voice name=\"{_voice}\">";
					}

					if (startText[0] != null)
					{
						newText += startText[0];
					}

					if (startText[1] != null)
					{
						newText += startText[1];
					}

					if (startText[2] != null)
					{
						newText += startText[2];
					}


					newText += text;

					if (endText[2] != null)
					{
						newText += endText[2];
					}

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

				if (animationRequest.SpeechRate != 1.0)
				{
					startText[0] = $"<prosody rate=\"{_speechRate}\">";
					endText[0] = "</prosody>";
				}
				else if (!string.IsNullOrWhiteSpace(_speakingStyle))
				{
					startText[1] = $"<mstts:express-as style=\"{_speakingStyle}\">";
					endText[1] = "</mstts:express-as>";
				}
			
				_azureTTSParameters.SpeakingVoice = _voice;

				/*
				Speak = "<?xml version=\"1.0\" encoding=\"utf-8\"?> <speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">" +
				"<voice name=\"en-US-GuyNeural\"><prosody rate=\"2.0\">" +
				"California, the most populous US state and the first to implement a statewide lockdown to combat the coronavirus outbreak, is setting daily records this week for new cases as officials urge caution and dangle enforcement threats to try to curb the spikes." +
				"The virus is spreading at private gatherings in homes, and more young people are testing positive, Gov.Gavin Newsom said Wednesday.Infections at some prisons are raising concerns." +
				"</prosody></voice></speak>",*/

				newText = $"<?xml version=\"1.0\" encoding=\"utf-8\"?> <speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"{_language}\">";
					
				//TODO allow pass in override by animation
				newText += $"<voice name=\"{(string.IsNullOrWhiteSpace(_voice) ? _azureTTSParameters.SpeakingVoice : _voice)}\">";

					
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
		
		protected async void AudioCallback(IAudioPlayCompleteEvent audioComplete)
		{
			try
			{
				_recording = false;
				_misty.SkillLogger.LogVerbose($"Audio Callback. Name: {audioComplete.Name}");
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
						if (_speechOverridden)
						{
							_speechOverridden = false;
							return;
						}

						_recording = true;
						switch (_characterParameters.SpeechRecognitionService.Trim().ToLower())
						{
							case "googleonboard":
								_ = _misty.CaptureSpeechGoogleAsync(false, _listenTimeout, _silenceTimeout, _characterParameters.GoogleSpeechRecognitionParameters.SubscriptionKey, _characterParameters.GoogleSpeechRecognitionParameters.SpokenLanguage);
								break;
							case "azureonboard":
								_ = _misty.CaptureSpeechAzureAsync(false, _listenTimeout, _silenceTimeout, _characterParameters.AzureSpeechRecognitionParameters.SubscriptionKey, _characterParameters.AzureSpeechRecognitionParameters.Region, _characterParameters.AzureSpeechRecognitionParameters.SpokenLanguage);
								break;
							case "vosk":
								_ = _misty.CaptureSpeechVoskAsync(false, _listenTimeout, _silenceTimeout);
								break;
							case "deepspeech":
								_ = _misty.CaptureSpeechDeepSpeechAsync(false, _listenTimeout, _silenceTimeout);
								break;
							default:
								_ = _misty.CaptureSpeechAsync(false, true, _listenTimeout, _silenceTimeout, null);
								break;
						}

						_misty.SkillLogger.LogInfo("Capture Speech called.");
					}
				}
			}
			catch(Exception ex)
			{
				_misty.SkillLogger.Log("Failed processing audio callback in Character.", ex);
			}
			finally
			{
				if(_recording)
				{
					StartedListening?.Invoke(this, DateTime.Now);
					_ = ManageListeningDisplay(ListeningState.Recording);
				}
				else
				{
					_ = ManageListeningDisplay(ListeningState.Waiting);
				}
			}
		}

		private async void KeyPhraseCallback(IKeyPhraseRecognizedEvent keyPhraseEvent)
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

				_ = ManageListeningDisplay(ListeningState.Recording);
				return;
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Failed to process Key Phrase Callback.", ex);
				SpeechIntent?.Invoke(this, new TriggerData("", ConversationConstants.HeardUnknownTrigger, Triggers.SpeechHeard));
			}
		}


		private async void VoiceRecordCallback(IVoiceRecordEvent voiceRecordEvent)
		{
			bool succesfulRetrieval = false;
			try
			{
				if (_speechOverridden)
				{
					_speechOverridden = false;
					return;
				}

				_recording = false;
				StoppedListening?.Invoke(this, voiceRecordEvent);
				_externalOverridden = true;
				if (_listenAborted)
				{
					_misty.SkillLogger.LogInfo("Listening stopped early.");
					return;
				}
				StartedProcessingVoice?.Invoke(this, voiceRecordEvent);
				_ = ManageListeningDisplay(ListeningState.ProcessingSpeech);
				_misty.SkillLogger.LogVerbose("Voice Record Callback - processing");
				
				if (voiceRecordEvent.ErrorCode == 3)
				{
					_misty.SkillLogger.Log($"Didn't hear anything with microphone. {voiceRecordEvent.ErrorMessage}");
					SpeechIntent?.Invoke(this, new TriggerData("", ConversationConstants.HeardNothingTrigger, Triggers.SpeechHeard));
					return;
				}
				
				string service = _characterParameters.SpeechRecognitionService.Trim().ToLower();
				if (service == "googleonboard" || service == "azureonboard" || service == "deepspeech" || service == "vosk")
				{
					succesfulRetrieval = true;
					CompletedProcessingVoice?.Invoke(this, voiceRecordEvent);
					_ = ManageListeningDisplay(ListeningState.Waiting);
					HandleSpeechResponse(voiceRecordEvent?.SpeechRecognitionResult);
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
					case "google":
						description = await _googleService.TranslateAudioStream((byte[])audioResponse.Data.Audio);
						break;
					case "azure":
					default:
						description = await _azureCognitive.TranslateAudioStream((byte[])audioResponse.Data.Audio);
						break;
				}

				succesfulRetrieval = true;
				CompletedProcessingVoice?.Invoke(this, voiceRecordEvent);
				_ = ManageListeningDisplay(ListeningState.Waiting);
				HandleSpeechResponse(description.Text);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Failed to process Voice Command event.", ex);
				SpeechIntent?.Invoke(this, new TriggerData("", ConversationConstants.HeardUnknownTrigger, Triggers.SpeechHeard));
			}
			finally
			{
				if(!succesfulRetrieval)
				{
					CompletedProcessingVoice?.Invoke(this, voiceRecordEvent);
					_ = ManageListeningDisplay(ListeningState.Waiting);
				}
			}
		}

		private bool HandleSpeechResponse(string text)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(text))
				{
					SpeechMatchData intent = _speechIntentManager.GetIntent(text, _allowedTriggers);


					//Old conversations trigger on name, new ones on id
					SpeechIntent?.Invoke(this, new TriggerData(text, intent.Id, Triggers.SpeechHeard));
					
					_misty.SkillLogger.LogInfo($"VoiceRecordCallback - Heard: '{text}' - Intent: {intent.Name}");
					return true;
				}
				else
				{
					_misty.SkillLogger.LogInfo("Didn't hear anything or can no longer translate.");
					SpeechIntent?.Invoke(this, new TriggerData("", ConversationConstants.HeardNothingTrigger, Triggers.SpeechHeard));
					return false;
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Failed processing speech response.", ex);
				SpeechIntent?.Invoke(this, new TriggerData("", ConversationConstants.HeardNothingTrigger, Triggers.SpeechHeard));
				return false;
			}
		}

		//TODO Simplify and cleanup
		public bool TryToPersonalizeData(string text, AnimationRequest animationRequest, Interaction interaction, out string newText)
		{
			newText = text;
			try
			{
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
			catch (Exception ex)
			{
				_misty.SkillLogger.LogInfo("Failed personalizing data.", ex);
				return false;
			}
		}
		
		public void ProcessUserDataUpdates(GenericData genericData)
		{
			//get rid of this now that there is an animations script?
			if (!string.IsNullOrWhiteSpace(genericData.ScreenText))
			{
				_ = _misty.SetTextDisplaySettingsAsync("UserDataText", new TextSettings
				{
					Wrap = true,
					Visible = true,
					Weight = 40,
					Size = 25,
					HorizontalAlignment = ImageHorizontalAlignment.Center,
					VerticalAlignment = ImageVerticalAlignment.Bottom,
					Red = 255,
					Green = 255,
					Blue = 255,
					PlaceOnTop = true,
					FontFamily = "Courier New",
					Height = 50
				});

				_characterState.DisplayedScreenText = genericData.ScreenText;
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
						_characterState.FaceRecognitionEvent?.Label ?? MissingInlineData;
					break;
				case "qrcode":
					newData = _characterState.QrTagEvent?.DecodedInfo ?? MissingInlineData;
					break;
				case "arcode":
					newData = _characterState.ArTagEvent?.TagId.ToString() ?? MissingInlineData;
					break;
				case "text":
					newData = _characterState.SpeechResponseEvent?.Text ?? MissingInlineData;
					break;
				case "intent":
					newData = _characterState.SpeechResponseEvent?.TriggerFilter ?? MissingInlineData;
					break;
				case "robotname":
					newData = string.IsNullOrWhiteSpace(_robotName) ? "Misty" : _robotName;
					break;
				case "time":
					newData = _timeManager.GetTimeObject().SpokenTime ?? MissingInlineData;
					break;
				case "partofday":
					newData = _timeManager.GetTimeObject().Description.ToString() ?? MissingInlineData;
					break;
				case "day":
					newData = _timeManager.GetTimeObject().SpokenDay.ToString() ?? MissingInlineData;
					break;
				case "emotion":
					newData = _characterState.CurrentMood ?? MissingInlineData;
					break;
				case "audio":
					newData = _characterState.Audio ?? MissingInlineData;
					break;
				case "charge":
					newData = (Convert.ToInt32(_characterState.BatteryChargeEvent.ChargePercent*100)).ToString() ?? MissingInlineData;
					break;
				case "image":
					newData = _characterState.Image ?? MissingInlineData;
					break;
				case "serial":
					newData = _characterState.SerialMessageEvent?.Message ?? MissingInlineData;
					break;
				case "object":
					newData = _characterState.ObjectEvent?.Description ?? MissingInlineData;
					break;
				default:
					//look for user replacement
					if(_commandManager != null && _commandManager.TryGetLastResponse(option, out string lastResponse))
					{
						newData = lastResponse ?? MissingInlineData;
					}
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
			//		_misty.UnregisterAllEvents(null);
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
 