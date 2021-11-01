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
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using Newtonsoft.Json;
using SkillTools.DataStorage;
using SkillTools.Web;

namespace MistyInteraction
{
    /// <summary>
    /// Translates parameters for conversation skill and trigger skills
    /// TODO Cleanup and better handling of issues
    /// </summary>
	public class ParameterManager
	{
		private IRobotMessenger _misty;
		
		private IDictionary<string, object> _storedData = new Dictionary<string, object>();
		private ISkillStorage _skillStorage;
		private IDictionary<string, object> _parameters = new Dictionary<string, object>();		
	
		private IDictionary<string, IDictionary<string, object>> _skillParameters = new Dictionary<string, IDictionary<string, object>>();
		private IDictionary<string, object> _userDefinedParameters = new Dictionary<string, object>();

		private static bool _overwriteLocalConfig;
        
		public CharacterParameters CharacterParameters { get; private set; } = new CharacterParameters();

		public ParameterManager(IRobotMessenger misty, IDictionary<string, object> parameters, bool overwriteLocalConfig = true)
		{
			_misty = misty;
			_parameters = parameters;
			_overwriteLocalConfig = overwriteLocalConfig;
		}

		public IDictionary<string, object> GetSkillPayload(string skillId)
		{
			return _skillParameters.FirstOrDefault(x => x.Key == skillId).Value ?? new Dictionary<string, object>();
		}

		public IDictionary<string, object> GetUserDefinedPayload()
		{
			return _userDefinedParameters;
		}
		
		public string GetUserDefinedStringData(string fieldName)
		{
			if(_userDefinedParameters.TryGetValue(fieldName.ToLower(), out object value))
			{
				return Convert.ToString(value);
			}
			return null;
		}

		public double? GetUserDefinedDoubleData(string fieldName)
		{
			if (_userDefinedParameters.TryGetValue(fieldName.ToLower(), out object value))
			{
				return Convert.ToDouble(value);
			}
			return null;
		}

		public float? GetUserDefinedFloatData(string fieldName)
		{
			if (_userDefinedParameters.TryGetValue(fieldName.ToLower(), out object value))
			{
				return Convert.ToSingle(value);
			}
			return null;
		}

		public int? GetUserDefinedIntData(string fieldName)
		{
			if (_userDefinedParameters.TryGetValue(fieldName.ToLower(), out object value))
			{
				return Convert.ToInt32(value);
			}
			return null;
		}

		public bool? GetUserDefinedBoolData(string fieldName)
		{
			if (_userDefinedParameters.TryGetValue(fieldName.ToLower(), out object value))
			{
				return Convert.ToBoolean(value);
			}
			return null;
		}

		public DateTime? GetUserDefinedDateTimeData(string fieldName)
		{
			if (_userDefinedParameters.TryGetValue(fieldName.ToLower(), out object value))
			{
				return Convert.ToDateTime(value);
			}
			return null;
		}
		
		public async Task<CharacterParameters> Initialize()
		{

			try
			{
				if (!_parameters.ContainsKey("ConversationGroup"))
				{
					string robotIp = GetStringField(_parameters, "RobotIp");
					if (string.IsNullOrWhiteSpace(robotIp))
					{
						_misty.SkillLogger.Log("No robot ip provided, any cross-robot communication will also send to self.");
					}
					else
					{
						robotIp = robotIp.Trim();
					}
					CharacterParameters.RobotIp = robotIp;

					string endpoint = GetStringField(_parameters, "Endpoint");
					if (string.IsNullOrWhiteSpace(endpoint))
                    {
                        _misty.SkillLogger.Log("No endpoint provided, using last saved conversation.");
						CharacterParameters.InitializationStatusMessage = "No endpoint provided. Running saved conversation.";
					}
					else
					{
						endpoint = endpoint.Trim();
						endpoint += "/api/conversations/group";
					}

					string conversationGroupId = GetStringField(_parameters, "ConversationGroupId") ?? null;
				
					if (!string.IsNullOrWhiteSpace(conversationGroupId))
					{
						//TODO This will prolly not be enough for different auth areas...

						string accountId = GetStringField(_parameters, "AccountId") ?? null;
						string key = GetStringField(_parameters, "Key") ?? null;
						string accessId = GetStringField(_parameters, "AccessId") ?? null;

						endpoint += $"?id={conversationGroupId.Trim()}&accountid={accountId}&key={key}&accessId={accessId}";

						IDictionary<string, object> requestedParameters = new Dictionary<string, object>();
						try
						{
							WebMessenger request = new WebMessenger();
							WebMessengerData data = await request.GetRequestAsync(endpoint);

							requestedParameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(data.Response);
						}
						catch
						{
							_misty.SkillLogger.Log("Could not locate conversation group data, using last saved conversation.");
							CharacterParameters.InitializationStatusMessage = "Failed endpoint contact. Running saved conversation.";
							CharacterParameters.InitializationErrorStatus = "Warning";
						}
					
						//copy those we didn't pass in
						foreach(KeyValuePair<string, object> param in requestedParameters)
						{
							if(!_parameters.ContainsKey(param.Key))
							{
								_parameters.Add(param.Key, param.Value);
							}
						}
					}
					else
					{
						_misty.SkillLogger.Log("Could not locate conversation group data, using last saved conversation.");
						CharacterParameters.InitializationStatusMessage = "Failed endpoint contact. Running saved conversation.";
						CharacterParameters.InitializationErrorStatus = "Warning";
					}
				}

				//Load the data from the data store only 1 time per start or else will overwrite in memory _storedData
				_skillStorage = SkillStorage.GetDatabase("conversation-skill");
				_storedData = await _skillStorage.LoadDataAsync() ?? new Dictionary<string, object>();
				
				string logLevelString = GetStringField(_parameters, ConversationConstants.LogLevel) ?? null;
				if (string.IsNullOrWhiteSpace(logLevelString))
				{
					CharacterParameters.LogLevel = _misty.SkillLogger.LogLevel;
				}
				else
				{
					SkillLogLevel logResult = _misty.SkillLogger.LogLevel;
					if (!string.IsNullOrWhiteSpace(logLevelString) && Enum.TryParse(logLevelString, true, out logResult))
					{
						CharacterParameters.LogLevel = logResult;
					}
					else
					{
						CharacterParameters.LogLevel = _misty.SkillLogger.LogLevel;
					}
				}

				RobotLogLevel robotLogLevel = RobotLogLevel.Info;

				switch (CharacterParameters.LogLevel)
				{
					case SkillLogLevel.Verbose:
						robotLogLevel = RobotLogLevel.Debug;
						break;
					case SkillLogLevel.Warning:
						robotLogLevel = RobotLogLevel.Warn;
						break;
					case SkillLogLevel.Error:
						robotLogLevel = RobotLogLevel.Error;
						break;
				}

				_misty.SetLogLevel(robotLogLevel, robotLogLevel, null);

				ConversationGroup conversationGroup = null;

				string conversationGroupJson = GetStringField(_parameters, ConversationConstants.ConversationGroup) ?? null;
				if (!string.IsNullOrWhiteSpace(conversationGroupJson))
				{
					try
					{
						conversationGroup = JsonConvert.DeserializeObject<ConversationGroup>(conversationGroupJson);
					}
					catch (Exception ex)
					{
						CharacterParameters.InitializationStatusMessage = "Couldn't parse conversation. Cannot continue.";
						CharacterParameters.InitializationErrorStatus = "Error";
						_misty.SkillLogger.Log("Failed parsing the conversation group data, cannot continue.", ex);
						return CharacterParameters;
					}
				}
				else
				{
					CharacterParameters.InitializationStatusMessage = "No conversation data. Cannot continue.";
					CharacterParameters.InitializationErrorStatus = "Error";
					_misty.SkillLogger.Log("Failed parsing conversation, no data was pass in for the conversation group.");
				}

				if (conversationGroup == null)
				{
					CharacterParameters.InitializationErrorStatus = "Error";
					CharacterParameters.InitializationStatusMessage = "No conversation data. Cannot continue.";
					return CharacterParameters;
				}


				string extraPayload = GetStringField(_parameters, ConversationConstants.Payload) ?? null;
				if(!string.IsNullOrWhiteSpace(extraPayload))
				{
					try
					{
						IDictionary<string, object> extraPayloadList = JsonConvert.DeserializeObject<Dictionary<string, object>>(extraPayload);
						foreach (KeyValuePair<string, object> newParamData in extraPayloadList.Where(x => x.Value != null))
						{
							if (!_userDefinedParameters.ContainsKey(newParamData.Key))
							{
								_userDefinedParameters.Add(newParamData.Key.ToLower(), newParamData.Value);
							}
						}
					}
					catch
					{
						//user has invalid payload
						_misty.SkillLogger.Log($"Failed parsing user defined payload data, skill may not work as expected.");
						CharacterParameters.InitializationStatusMessage = "Warning. Could not parse user defined payload.";
						CharacterParameters.InitializationErrorStatus = "Warning";
					}
				}

				foreach (ConversationData conversation in conversationGroup.Conversations)
				{
					foreach (SkillMessage skillMessage in conversation.SkillMessages.Where(x => !string.IsNullOrWhiteSpace(x.Payload)))
					{
						try
						{
							IDictionary<string, object> currentSkillData = new Dictionary<string, object>();
							//it's possible there are multiple messages with overlapping params, at least don't fail on them
							if (_skillParameters.TryGetValue(skillMessage.Skill, out currentSkillData))
							{
								IDictionary<string, object> newSkillDataList = JsonConvert.DeserializeObject<Dictionary<string, object>>(skillMessage.Payload);
								foreach (KeyValuePair<string, object> newSkillData in newSkillDataList.Where(x => x.Value != null))
								{
									if (!currentSkillData.ContainsKey(newSkillData.Key))
									{
										currentSkillData.Add(newSkillData.Key, newSkillData.Value);
									}
								}

								_skillParameters.Remove(skillMessage.Skill);
								_skillParameters.Add(skillMessage.Skill, currentSkillData);
							}
							else
							{
								_skillParameters.Add(skillMessage.Skill, JsonConvert.DeserializeObject<Dictionary<string, object>>(skillMessage.Payload));
							}
						}
						catch
						{
							//user has invalid payload
							_misty.SkillLogger.Log($"Failed parsing user defined skill {skillMessage.Skill} payload, skill may not work as expected.");
							CharacterParameters.InitializationStatusMessage = "Warning. Could not parse user defined payload.";
							CharacterParameters.InitializationErrorStatus = "Warning";
						}
					}
				}
				
				CharacterParameters.DisplaySpoken = GetBoolField(_parameters, ConversationConstants.DisplaySpoken) ?? false;
				CharacterParameters.LargePrint = GetBoolField(_parameters, ConversationConstants.LargePrint) ?? false;
				CharacterParameters.ShowListeningIndicator = GetBoolField(_parameters, ConversationConstants.ShowListeningIndicator) ?? false;
				CharacterParameters.HeardSpeechToScreen = GetBoolField(_parameters, ConversationConstants.HeardSpeechToScreen) ?? false;
				CharacterParameters.StartVolume = GetIntField(_parameters, ConversationConstants.StartVolume) ?? null;

				try
				{
					CharacterParameters.Robots = JsonConvert.DeserializeObject<IList<Robot>>(GetStringField(_parameters, "Robots")) ?? new List<Robot>();
				}
				catch
				{
					_misty.SkillLogger.Log($"Failed parsing robot information, skill may not work as expected.");
					CharacterParameters.InitializationStatusMessage = "Warning. Could not parse robot information.";
					CharacterParameters.InitializationErrorStatus = "Warning";
				}
				try
				{
					CharacterParameters.Recipes = JsonConvert.DeserializeObject<IList<Recipe>>(GetStringField(_parameters, "Recipes")) ?? new List<Recipe>();
				}
				catch
				{
					_misty.SkillLogger.Log($"Failed parsing recipe information, skill may not work as expected.");
					CharacterParameters.InitializationStatusMessage = "Warning. Could not parse recipe information.";
					CharacterParameters.InitializationErrorStatus = "Warning";
				}
				CharacterParameters.ConversationGroup = conversationGroup;

				CharacterParameters.Character = GetStringField(_parameters, ConversationConstants.Character) ?? "basic";
				CharacterParameters.RequestedCharacter = GetStringField(_parameters, ConversationConstants.Character) ?? "";

				CharacterParameters.UsePreSpeech = GetBoolField(_parameters, ConversationConstants.UsePreSpeech) ?? false;

				//Parse string into prespeech by semicolon
				try
				{
					string preSpeechString = GetStringField(_parameters, ConversationConstants.PreSpeechPhrases) ?? "";
					if (!string.IsNullOrWhiteSpace(preSpeechString))
					{
						string[] preSpeechStrings = preSpeechString.Replace(Environment.NewLine, "").Split(";");
						if (preSpeechStrings != null && preSpeechStrings.Length > 0)
						{
							CharacterParameters.PreSpeechPhrases = preSpeechStrings.ToList();
						}
					}
				}
				catch
				{
					_misty.SkillLogger.Log($"Failed parsing pre-speech phrases, using defaults.");
					CharacterParameters.InitializationStatusMessage = "Failed parsing pre-speech, using defaults.";
					CharacterParameters.InitializationErrorStatus = "Warning";
				}
				finally
				{
					if(CharacterParameters.PreSpeechPhrases == null || CharacterParameters.PreSpeechPhrases.Count == 0)
					{
						CharacterParameters.PreSpeechPhrases = new List<string>
						{
							"One second please.",
							"Hold on one moment.",
							"I think I can help with that.",
							"Let me see.",
							"Let me find that.",
						};
					}
				}

				if (CharacterParameters.Character != CharacterParameters.RequestedCharacter)
				{
					string msg = $"Requested character '{CharacterParameters.RequestedCharacter}' could not be found, using {CharacterParameters.Character}.";
					CharacterParameters.InitializationStatusMessage = $"Requested character not found, using {CharacterParameters.Character}.";
					CharacterParameters.InitializationErrorStatus = "Warning";
					_misty.SkillLogger.Log(msg);
					_misty.PublishMessage(msg, null);
				}

				//Get speech configuration from string
				SpeechConfiguration speechConfiguration = null;
				string speechConfigurationJson = GetStringField(_parameters, ConversationConstants.SpeechConfiguration) ?? null;
				if (!string.IsNullOrWhiteSpace(speechConfigurationJson))
				{
					try
					{
						speechConfiguration = JsonConvert.DeserializeObject<SpeechConfiguration>(speechConfigurationJson);
					}
					catch (Exception ex)
					{
						_misty.SkillLogger.Log("Failed parsing the speech configuration data, speech intent will not work.", ex);
						CharacterParameters.InitializationErrorStatus = "Warning";
						CharacterParameters.InitializationStatusMessage = $"Speech configuration failed, speech intent will not work.";
					}
				}
				else
				{
					_misty.SkillLogger.Log("Failed parsing conversation, no data was pass in for the speech configuration.");
					CharacterParameters.InitializationErrorStatus = "Warning";
					CharacterParameters.InitializationStatusMessage = $"Speech configuration not set, speech intent will not work.";
				}

				/*

				CharacterParameters.AzureSpeechParameters = new AzureSpeechParameters();
				CharacterParameters.GoogleSpeechParameters = new GoogleSpeechParameters();

				//TODO Cleanup,. this doesn't allow multiple sub keys per account yet
				if (!string.IsNullOrWhiteSpace(speechConfiguration?.SpeechRecognitionSubscriptionKey) || 
					!string.IsNullOrWhiteSpace(speechConfiguration?.TextToSpeechSubscriptionKey))
				{

					string speechRecService = speechConfiguration.SpeechRecognitionService.Trim().ToLower();
					string ttsService = speechConfiguration.TextToSpeechService.Trim().ToLower();

					if (speechConfiguration.SpeechRecognitionService == "Azure" ||
						speechConfiguration.SpeechRecognitionService == "AzureOnboard" ||
						speechConfiguration.TextToSpeechService == "Azure" ||
						speechConfiguration.TextToSpeechService == "AzureOnboard")
					{
						CharacterParameters.AzureSpeechParameters.SubscriptionKey = speechConfiguration.SpeechRecognitionSubscriptionKey ?? speechConfiguration?.TextToSpeechSubscriptionKey;
						CharacterParameters.AzureSpeechParameters.Region = speechConfiguration.SpeechRecognitionRegion ?? "";
						CharacterParameters.AzureSpeechParameters.Endpoint = speechConfiguration.SpeechRecognitionEndpoint ?? "";
						CharacterParameters.AzureSpeechParameters.SpeakingVoice = speechConfiguration.SpeakingVoice ?? "en-US-AriaNeural";
						CharacterParameters.AzureSpeechParameters.TranslatedLanguage = speechConfiguration.TranslatedLanguage ?? "en";
						CharacterParameters.AzureSpeechParameters.SpokenLanguage = speechConfiguration.SpokenLanguage ?? "en-US";
						CharacterParameters.AzureSpeechParameters.ProfanitySetting = speechConfiguration.ProfanitySetting ?? "Raw";
					}
					
					if (speechConfiguration.SpeechRecognitionService == "Google" ||
						speechConfiguration.SpeechRecognitionService == "GoogleOnboard" ||
						speechConfiguration.TextToSpeechService == "Google" ||
						speechConfiguration.TextToSpeechService == "GoogleOnboard")
					{
						CharacterParameters.GoogleSpeechParameters.SubscriptionKey = speechConfiguration.SpeechRecognitionSubscriptionKey ?? speechConfiguration?.TextToSpeechSubscriptionKey;
						CharacterParameters.GoogleSpeechParameters.STTEndpoint = speechConfiguration.SpeechRecognitionEndpoint ?? "https://speech.googleapis.com/v1p1beta1/speech:recognize?key=";
						CharacterParameters.GoogleSpeechParameters.TTSEndpoint = speechConfiguration.TextToSpeechEndpoint ?? "https://texttospeech.googleapis.com/v1/text:synthesize?key=";
						CharacterParameters.GoogleSpeechParameters.SpeakingVoice = speechConfiguration.SpeakingVoice ?? "en-US-Standard-C";
						CharacterParameters.GoogleSpeechParameters.SpeakingGender = speechConfiguration.SpeakingGender ?? "FEMALE";
						CharacterParameters.GoogleSpeechParameters.SpokenLanguage = speechConfiguration.SpokenLanguage ?? "en-US";					
					}
				}
				
				CharacterParameters.SpeechRecognitionService = speechConfiguration.SpeechRecognitionService ?? "Azure";
				CharacterParameters.TextToSpeechService = speechConfiguration.TextToSpeechService ?? "Misty";




	*/

				SetSpeechParameters(speechConfiguration);

				CharacterParameters.TrackHistory = GetIntField(_parameters, ConversationConstants.TrackHistory) ?? 3;
				CharacterParameters.PersonConfidence = GetDoubleField(_parameters, ConversationConstants.PersonConfidence) ?? 0.6;
				CharacterParameters.LogInteraction = GetBoolField(_parameters, ConversationConstants.LogInteraction) ?? true;
				CharacterParameters.StreamInteraction = GetBoolField(_parameters, ConversationConstants.StreamInteraction) ?? false;
				CharacterParameters.FacePitchOffset = GetIntField(_parameters, ConversationConstants.FacePitchOffset) ?? 0;
				CharacterParameters.ObjectDetectionDebounce = GetIntField(_parameters, ConversationConstants.FollowFaceDebounce) ?? 0.333;
				
				try
				{
					bool audioEnabled = false;
					IRobotCommandResponse audioResults = null;
					audioEnabled = (await _misty.AudioServiceEnabledAsync()).Data;
					if (!audioEnabled)
					{
						audioResults = await _misty.EnableAudioServiceAsync();
						audioEnabled = audioResults.Status == ResponseStatus.Success;
					}
					
					if (!audioEnabled)
					{
						_misty.SkillLogger.Log($"Failed to get enabled response from audio system.");
						CharacterParameters.InitializationErrorStatus = "Warning";
						CharacterParameters.InitializationStatusMessage = $"Warning. Unknown status of audio system.";
						await Task.Delay(2000);
					}
				}
				catch (Exception ex)
				{
					_misty.SkillLogger.Log("Enable services threw an exception. 820 services not loaded properly.", ex);
					CharacterParameters.InitializationErrorStatus = "Warning";
					CharacterParameters.InitializationStatusMessage = $"Warning. Unknown status of audio and detection systems.";
					await Task.Delay(2000);
					return CharacterParameters;
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Exception processing parameters.", ex);
				CharacterParameters.InitializationStatusMessage = "Exception processing parameters. Cannot continue.";
				CharacterParameters.InitializationErrorStatus = "Error";
				return CharacterParameters;
			}

			if (_overwriteLocalConfig)
			{
				await _skillStorage.SaveDataAsync(_storedData);
			}

			//Return what we have...
			return CharacterParameters;
		}

	
		private ConversationGroup GetDefaultSurvey()
		{
			try
			{
				//TODO
				return new ConversationGroup();
			}
			catch
			{
				return new ConversationGroup();
			}
		}

		private void SetSpeechParameters(SpeechConfiguration speechConfiguration)
		{
			//Speech Rec Parameters
			if (!string.IsNullOrWhiteSpace(speechConfiguration?.SpeechRecognitionSubscriptionKey))
			{
				string recService = speechConfiguration.SpeechRecognitionService.ToLower().Trim();
				if (recService == "azure" || recService == "azureonboard")
				{
					CharacterParameters.AzureSpeechRecognitionParameters.SubscriptionKey = speechConfiguration.SpeechRecognitionSubscriptionKey;
					CharacterParameters.AzureSpeechRecognitionParameters.Region = speechConfiguration.SpeechRecognitionRegion ?? "";
					CharacterParameters.AzureSpeechRecognitionParameters.Endpoint = speechConfiguration.SpeechRecognitionEndpoint ?? "";
					CharacterParameters.AzureSpeechRecognitionParameters.TranslatedLanguage = speechConfiguration.TranslatedLanguage ?? "en";
					CharacterParameters.AzureSpeechRecognitionParameters.SpokenLanguage = speechConfiguration.SpokenLanguage ?? "en-US";
					CharacterParameters.AzureSpeechRecognitionParameters.ProfanitySetting = speechConfiguration.ProfanitySetting ?? "Raw";
				}
				else if (recService == "google" || recService == "googleonboard")
				{
					CharacterParameters.GoogleSpeechRecognitionParameters.SubscriptionKey = speechConfiguration.SpeechRecognitionSubscriptionKey ?? "";
					CharacterParameters.GoogleSpeechRecognitionParameters.Endpoint = speechConfiguration.SpeechRecognitionEndpoint ?? "https://speech.googleapis.com/v1p1beta1/speech:recognize?key=";
					CharacterParameters.GoogleSpeechRecognitionParameters.SpeakingVoice = speechConfiguration.SpeakingVoice ?? "en-US-Standard-C";
					CharacterParameters.GoogleSpeechRecognitionParameters.SpokenLanguage = speechConfiguration.SpokenLanguage ?? "en-US";
				}
			}

			//TTS Parameters
			if (!string.IsNullOrWhiteSpace(speechConfiguration?.TextToSpeechSubscriptionKey))
			{
				string ttsService = speechConfiguration.TextToSpeechService.ToLower().Trim();
				if (ttsService == "azure" || ttsService == "azureonboard")
				{
					CharacterParameters.AzureTTSParameters.SubscriptionKey = speechConfiguration.TextToSpeechSubscriptionKey;
					CharacterParameters.AzureTTSParameters.Region = speechConfiguration.SpeechRecognitionRegion ?? "";
					CharacterParameters.AzureTTSParameters.Endpoint = speechConfiguration.SpeechRecognitionEndpoint ?? "";
					CharacterParameters.AzureTTSParameters.SpeakingVoice = speechConfiguration.SpeakingVoice ?? "en-US-AriaNeural";
					CharacterParameters.AzureTTSParameters.TranslatedLanguage = speechConfiguration.TranslatedLanguage ?? "en";
					CharacterParameters.AzureTTSParameters.SpokenLanguage = speechConfiguration.SpokenLanguage ?? "en-US";
					CharacterParameters.AzureTTSParameters.ProfanitySetting = speechConfiguration.ProfanitySetting ?? "Raw";
				}
				else if (ttsService == "google" || ttsService == "googleonboard")
				{
					CharacterParameters.GoogleTTSParameters.SubscriptionKey = speechConfiguration.TextToSpeechSubscriptionKey;
					CharacterParameters.GoogleTTSParameters.Endpoint = speechConfiguration.TextToSpeechEndpoint ?? "https://texttospeech.googleapis.com/v1/text:synthesize?key=";
					CharacterParameters.GoogleTTSParameters.SpeakingVoice = speechConfiguration.SpeakingVoice ?? "en-US-Standard-C";
					CharacterParameters.GoogleTTSParameters.SpeakingGender = speechConfiguration.SpeakingGender ?? "FEMALE";
					CharacterParameters.GoogleTTSParameters.SpokenLanguage = speechConfiguration.SpokenLanguage ?? "en-US";
				}
			}
		}

		private string GetStringField(IDictionary<string, object> parameters, string dataKey)
		{
			try
			{
				string newValue = null;
				KeyValuePair<string, object> dataKVP = parameters.FirstOrDefault(x => x.Key.ToLower().Trim() == dataKey.ToLower());
				if (dataKVP.Value != null)
				{
					//getting passed in param and saving to local db
					newValue = Convert.ToString(dataKVP.Value);
					_storedData.Remove(dataKey);
					_storedData.Add(dataKey, newValue ?? "");
				}
				else if (_storedData.ContainsKey(dataKey))
				{
					//getting from local storage
					newValue = Convert.ToString(_storedData[dataKey]);
				}
				return newValue;
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Exception in GetStringField", ex);
				return null;
			}
		}

		private bool? GetBoolField(IDictionary<string, object> parameters, string dataKey)
		{
			try
			{
				bool? newValue = null;
				KeyValuePair<string, object> dataKVP = parameters.FirstOrDefault(x => x.Key.ToLower().Trim() == dataKey.ToLower());
				if (dataKVP.Value != null)
				{
					newValue = Convert.ToBoolean(dataKVP.Value);
					_storedData.Remove(dataKey);
					_storedData.Add(dataKey, newValue ?? false);
				}
				else if (_storedData.ContainsKey(dataKey))
				{
					newValue = Convert.ToBoolean(_storedData[dataKey]);
				}
				return newValue;
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Exception in GetBoolField", ex);
				return null;
			}
		}

		private DateTime? GetDateTimeField(IDictionary<string, object> parameters, string dataKey)
		{
			try
			{
				DateTime? newValue = null;
				KeyValuePair<string, object> dataKVP = parameters.FirstOrDefault(x => x.Key.ToLower().Trim() == dataKey.ToLower());
				if (dataKVP.Value != null)
				{
					newValue = Convert.ToDateTime(dataKVP.Value);
					if(newValue != null)
					{
						_storedData.Remove(dataKey);
						_storedData.Add(dataKey, newValue);
					}
				}
				else if (_storedData.ContainsKey(dataKey))
				{
					newValue = Convert.ToDateTime(_storedData[dataKey]);
				}
				return newValue;
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Exception in GetDateTimeField", ex);
				return null;
			}
		}

		private int? GetIntField(IDictionary<string, object> parameters, string dataKey)
		{
			try
			{
				int? newValue = null;
				KeyValuePair<string, object> dataKVP = parameters.FirstOrDefault(x => x.Key.ToLower().Trim() == dataKey.ToLower());
				if (dataKVP.Value != null)
				{
					newValue = Convert.ToInt32(dataKVP.Value);
					_storedData.Remove(dataKey);
					_storedData.Add(dataKey, newValue ?? null);
				}
				else if (_storedData.ContainsKey(dataKey))
				{
					newValue = Convert.ToInt32(_storedData[dataKey]);
				}
				return newValue;
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Exception in GetIntField", ex);
				return null;
			}
		}

		private double? GetDoubleField(IDictionary<string, object> parameters, string dataKey)
		{
			try
			{
				double? newValue = null;
				KeyValuePair<string, object> dataKVP = parameters.FirstOrDefault(x => x.Key.ToLower().Trim() == dataKey.ToLower());
				if (dataKVP.Value != null)
				{
					newValue = Convert.ToDouble(dataKVP.Value);
					_storedData.Remove(dataKey);
					_storedData.Add(dataKey, newValue ?? null);
				}
				else if (_storedData.ContainsKey(dataKey))
				{
					newValue = Convert.ToDouble(_storedData[dataKey]);
				}
				return newValue;
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Exception in GetDoubleField", ex);
				return null;
			}
		}

		private float? GetFloatField(IDictionary<string, object> parameters, string dataKey)
		{
			try
			{
				float? newValue = null;
				KeyValuePair<string, object> dataKVP = parameters.FirstOrDefault(x => x.Key.ToLower().Trim() == dataKey.ToLower());
				if (dataKVP.Value != null)
				{
					newValue = Convert.ToSingle(dataKVP.Value);
					_storedData.Remove(dataKey);
					_storedData.Add(dataKey, newValue ?? null);
				}
				else if (_storedData.ContainsKey(dataKey))
				{
					newValue = Convert.ToSingle(_storedData[dataKey]);
				}
				return newValue;
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Exception in GetFloatField", ex);
				return null;
			}
		}

	}
}
 