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
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using Newtonsoft.Json;
using SkillTools.DataStorage;
using SkillTools.Web;

namespace MistyCharacter
{
	public class LoadedConversation
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public bool Running { get; set; }
	}

    /// <summary>
    /// Translates parameters for conversation skill and trigger skills
    /// TODO Cleanup and better handling of issues
    /// </summary>
	public class ParameterManager
	{
		private IRobotMessenger _misty;
		
		//private IDictionary<string, object> _parameters = new Dictionary<string, object>();
		private ISkillStorage _database;
		private IDictionary<string, object> _conversationGroupList = new Dictionary<string, object>();
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
		
		private static string _runningConversationGroup;
		private SemaphoreSlim _dbSlim = new SemaphoreSlim(1, 1);

		public void PublishConversationList()
		{
			try
			{
				IList<LoadedConversation> list = new List<LoadedConversation>();
				foreach (var item in _conversationGroupList)
				{
					ConversationGroup conversationGroup = JsonConvert.DeserializeObject<CharacterParameters>(Convert.ToString(item.Value)).ConversationGroup;
					list.Add(new LoadedConversation
					{
						Id = conversationGroup.Id,
						Name = conversationGroup.Name,
						Description = conversationGroup.Description,
						Running = conversationGroup.Id == _runningConversationGroup ? true : false
					});
				}

				IDictionary<string, object> data = new Dictionary<string, object>
				{
					{"DataType", "conversations"},
					{"Conversations", list},
				};

				_misty.PublishMessage(JsonConvert.SerializeObject(data), null);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogError("Failed to send conversation list.", ex);
			}
			
		}

		private async Task<bool> LoadConversationGroup()
		{
			await _dbSlim.WaitAsync();
			try
			{
				if (_parameters.ContainsKey("ConversationGroup"))
				{
					//run as is
					ConversationGroup conversationGroup = JsonConvert.DeserializeObject<ConversationGroup>(Convert.ToString(_parameters["ConversationGroup"]));
					
					_runningConversationGroup = conversationGroup.Id;
					return true;
				}

				//If no conversation group config passed in, find the right conversation
				string conversationGroupId = GetStringField(_parameters, "ConversationGroupId", false) ?? null;
				_runningConversationGroup = conversationGroupId;

				string endpoint = GetStringField(_parameters, "Endpoint", false);

				//TODO
				string robotIp = GetStringField(_parameters, "RobotIp", false);
				if (string.IsNullOrWhiteSpace(robotIp))
				{
					_misty.SkillLogger.Log("No robot ip provided, any cross-robot communication will also send to self.");
				}
				else
				{
					robotIp = robotIp.Trim();
				}
				CharacterParameters.RobotIp = robotIp;

				if (string.IsNullOrWhiteSpace(conversationGroupId))
				{
					//If started without any conversation id						
					if(string.IsNullOrWhiteSpace(_runningConversationGroup))
					{
						CharacterParameters.InitializationErrorStatus = InitializationStatus.Waiting;
					}
				}
				else if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(conversationGroupId))
				{
					//if started with id and endpoint, look it up

					endpoint = endpoint.Trim();
					endpoint += "/api/conversations/group";

					//TODO This will prolly not be enough for different auth areas...
					string accountId = GetStringField(_parameters, "AccountId", false) ?? null;
					string key = GetStringField(_parameters, "Key", false) ?? null;
					string accessId = GetStringField(_parameters, "AccessId", false) ?? null;

					endpoint += $"?id={conversationGroupId.Trim()}&accountid={accountId}&key={key}&accessId={accessId}";

					IDictionary<string, object> requestedParameters = new Dictionary<string, object>();
					try
					{
						WebMessenger request = new WebMessenger();
						WebMessengerData data = await request.GetRequestAsync(endpoint);

						requestedParameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(data.Response);
						_runningConversationGroup = conversationGroupId;
					}
					catch
					{
						if (await LoadOnboardConversation(null))
						{
							_misty.SkillLogger.Log("Could not locate conversation group data, using saved conversation.");
							CharacterParameters.InitializationStatusMessage = "Endpoint failure. Running saved conversation.";
							CharacterParameters.InitializationErrorStatus = InitializationStatus.Warning;
						}
						else
						{
							_misty.SkillLogger.Log("Could not locate conversation group data, and no saved conversations. Waiting for conversation data.");
							CharacterParameters.InitializationStatusMessage = "Endpoint failure. Waiting for conversation.";
							CharacterParameters.InitializationErrorStatus = InitializationStatus.Waiting;
						}
					}

					//copy those we didn't pass in
					foreach (KeyValuePair<string, object> param in requestedParameters)
					{
						if (!_parameters.ContainsKey(param.Key))
						{
							_parameters.Add(param.Key, param.Value);
						}
					}

				}
				else if (!string.IsNullOrWhiteSpace(conversationGroupId))
				{
					//Try to load from onboard collection
					if (!await LoadOnboardConversation(conversationGroupId))
					{
						_misty.SkillLogger.Log("Could not locate conversation group data, and no saved conversations. Waiting for new conversation data.");
						CharacterParameters.InitializationStatusMessage = "Invalid onboard id. Waiting for conversation.";
						CharacterParameters.InitializationErrorStatus = InitializationStatus.Waiting;
					}

				}
				else
				{
					if (!await LoadOnboardConversation(null))
					{
						_misty.SkillLogger.Log("Invalid startup parameters and no saved conversations. Waiting for new conversation data.");
						CharacterParameters.InitializationStatusMessage = "Invalid onboard id. Waiting for conversation.";
						CharacterParameters.InitializationErrorStatus = InitializationStatus.Waiting;
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogError("Failed processing conversation request.", ex);
				return false;
			}
			finally
			{
				_dbSlim.Release();
			}
		}

		private async Task<bool> LoadOnboardConversation(string conversationGroupId = null, IDictionary<string, object> updateParameters = null)
		{
			try
			{
				_conversationGroupList = await _database.LoadDataAsync() ?? new Dictionary<string, object>();
				if (_conversationGroupList.Count() == 0)
				{
					return false;
				}

				KeyValuePair<string, object> onBoardConversationGroup;
				if (conversationGroupId == null)
				{
					onBoardConversationGroup = _conversationGroupList.First();
				}
				else
				{
					onBoardConversationGroup = _conversationGroupList.FirstOrDefault(x => x.Key == conversationGroupId);
				}

				_overwriteLocalConfig = false;
				if (onBoardConversationGroup.Value != null)
				{
					await ParseCharacterParameters(conversationGroupId, (IDictionary<string, object>)onBoardConversationGroup.Value);
				}
				else
				{
					await ParseCharacterParameters(conversationGroupId, (IDictionary<string, object>)_conversationGroupList.First().Value);
				}
				
				return true;
			}
			catch
			{
				return false;
			}
		}

		
		public async Task<bool> StartConversation(string conversationGroupId, IDictionary<string, object> parameters)
		{
			try
			{
				//CharacterParameters = await Initialize();
				//if(CharacterParameters.InitializationErrorStatus != InitializationStatus.Error)
				//{

				//}
				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				PublishConversationList();
			}
		}

		public async Task<bool> LoadConversation(CharacterParameters cp)
		{
			await _dbSlim.WaitAsync();
			try
			{
				if(cp != null)
				{
					_conversationGroupList = await _database.LoadDataAsync() ?? new Dictionary<string, object>();
					
					KeyValuePair<string, object> onBoardConversationGroup = _conversationGroupList.FirstOrDefault(x => x.Key == cp.ConversationGroup.Id);
					if (onBoardConversationGroup.Value != null)
					{
						_conversationGroupList.Remove(cp.ConversationGroup.Id);
					}
					_conversationGroupList.Add(cp.ConversationGroup.Id, JsonConvert.SerializeObject(cp));
					await _database.SaveDataAsync(_conversationGroupList);
					return true;
				}
				return false;

			}
			catch
			{
				return false;
			}
			finally
			{
				_dbSlim.Release();
				PublishConversationList();
			}
		}

		public async Task<bool> RemoveConversation(string conversationGroupId)
		{
			await _dbSlim.WaitAsync();
			try
			{
				_conversationGroupList = await _database.LoadDataAsync() ?? new Dictionary<string, object>();
				KeyValuePair<string, object> onBoardConversationGroup = _conversationGroupList.FirstOrDefault(x => x.Key == conversationGroupId);
				if (onBoardConversationGroup.Value != null)
				{
					_conversationGroupList.Remove(conversationGroupId);
					await _database.SaveDataAsync(_conversationGroupList);
				}
				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				_dbSlim.Release();
				PublishConversationList();
			}
		}

		public async Task<CharacterParameters> ParseCharacterParameters(string conversationGroupId, IDictionary<string, object> data)
		{
			CharacterParameters characterParameters = new CharacterParameters();
			try
			{
				_conversationGroupList = await _database.LoadDataAsync() ?? new Dictionary<string, object>();
				
				CharacterParameters storedParameters = new CharacterParameters();
				
				//TODO Refactor for new startup procedure!!
				if (string.IsNullOrWhiteSpace(_runningConversationGroup) && string.IsNullOrWhiteSpace(conversationGroupId))
				{
					characterParameters.InitializationStatusMessage = "Waiting for conversation.";
					CharacterParameters.InitializationErrorStatus = InitializationStatus.Waiting;
					_misty.SkillLogger.Log("Waiting for conversation.");
					return characterParameters;
				}
				else if (!string.IsNullOrWhiteSpace(_runningConversationGroup))
				{
					conversationGroupId = _runningConversationGroup;
				}
				else if (!string.IsNullOrWhiteSpace(conversationGroupId))
				{
					_runningConversationGroup = conversationGroupId;
				}

				if (_conversationGroupList != null)
				{
					var onBoardConversationGroup = _conversationGroupList.FirstOrDefault(x => x.Key == conversationGroupId);
					if (onBoardConversationGroup.Value != null)
					{
						characterParameters = JsonConvert.DeserializeObject<CharacterParameters>(Convert.ToString(onBoardConversationGroup.Value));
					}
				}
				
				if (CharacterParameters.InitializationErrorStatus == InitializationStatus.Error)
				{
					return characterParameters;
				}
				
				string logLevelString = GetStringField(_parameters, ConversationConstants.LogLevel) ?? CharacterParameters?.LogLevel.ToString() ?? null;
				if (string.IsNullOrWhiteSpace(logLevelString))
				{
					characterParameters.LogLevel = _misty.SkillLogger.LogLevel;
				}
				else
				{
					SkillLogLevel logResult = _misty.SkillLogger.LogLevel;
					if (!string.IsNullOrWhiteSpace(logLevelString) && Enum.TryParse(logLevelString, true, out logResult))
					{
						characterParameters.LogLevel = logResult;
					}
					else
					{
						characterParameters.LogLevel = _misty.SkillLogger.LogLevel;
					}
				}

				RobotLogLevel robotLogLevel = RobotLogLevel.Info;

				switch (characterParameters.LogLevel)
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
				if (conversationGroupJson != null)
				{
					try
					{
						conversationGroup = JsonConvert.DeserializeObject<ConversationGroup>(conversationGroupJson);
					}
					catch (Exception ex)
					{						
						if (characterParameters.ConversationGroup != null)
						{
							_misty.SkillLogger.Log("Using stored conversation.");
							conversationGroup = characterParameters.ConversationGroup;
						}
						else
						{
							characterParameters.InitializationStatusMessage = "Couldn't parse conversation. Waiting for conversation.";
							CharacterParameters.InitializationErrorStatus = InitializationStatus.Waiting;
							_misty.SkillLogger.Log("Couldn't parse conversation. Waiting for conversation.", ex);

						}
					}
				}
				else if (characterParameters.ConversationGroup != null)
				{
					_misty.SkillLogger.Log("Using stored conversation.");
					conversationGroup = characterParameters.ConversationGroup;
				}
				else
				{
					characterParameters.InitializationStatusMessage = "No conversation data. Waiting for conversation.";
					CharacterParameters.InitializationErrorStatus = InitializationStatus.Waiting;
					_misty.SkillLogger.Log("No conversation data. Waiting for conversation.");
				}

				if (conversationGroup == null)
				{
					CharacterParameters.InitializationErrorStatus = InitializationStatus.Waiting;
					characterParameters.InitializationStatusMessage = "No conversation data. Waiting for conversation";
					_misty.SkillLogger.Log("No conversation data. Waiting for conversation.");
					return characterParameters;
				}


				characterParameters.AnimationCreationMode = conversationGroup.AnimationCreationMode;
				characterParameters.AnimationCreationDebounceSeconds = conversationGroup.AnimationCreationDebounceSeconds;
				characterParameters.IgnoreArmCommands = conversationGroup.IgnoreArmCommands;
				characterParameters.IgnoreHeadCommands = conversationGroup.IgnoreHeadCommands;
				characterParameters.RetranslateTTS = conversationGroup.RetranslateTTS;
				characterParameters.SmoothRecording = conversationGroup.SmoothRecording;

				string extraPayload = GetStringField(_parameters, ConversationConstants.Payload) ?? null;
				if (!string.IsNullOrWhiteSpace(extraPayload))
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
						characterParameters.InitializationStatusMessage = "Warning. Could not parse user defined payload.";
						CharacterParameters.InitializationErrorStatus = InitializationStatus.Warning;
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
							characterParameters.InitializationStatusMessage = "Warning. Could not parse user defined payload.";
							CharacterParameters.InitializationErrorStatus = InitializationStatus.Warning;
						}
					}
				}

				characterParameters.DisplaySpoken = GetBoolField(_parameters, ConversationConstants.DisplaySpoken) ?? CharacterParameters?.DisplaySpoken ?? false;
				characterParameters.LargePrint = GetBoolField(_parameters, ConversationConstants.LargePrint) ?? CharacterParameters?.LargePrint ?? false;
				characterParameters.ShowListeningIndicator = GetBoolField(_parameters, ConversationConstants.ShowListeningIndicator) ?? CharacterParameters?.ShowListeningIndicator ?? false;
				characterParameters.ShowSpeakingIndicator = GetBoolField(_parameters, ConversationConstants.ShowSpeakingIndicator) ?? CharacterParameters?.ShowSpeakingIndicator ?? false;
				characterParameters.SendInteractionUIEvents = GetBoolField(_parameters, ConversationConstants.SendInteractionUIEvents) ?? CharacterParameters?.SendInteractionUIEvents ?? true;
				characterParameters.HeardSpeechToScreen = GetBoolField(_parameters, ConversationConstants.HeardSpeechToScreen) ?? CharacterParameters?.HeardSpeechToScreen ?? false;
				characterParameters.StartVolume = GetIntField(_parameters, ConversationConstants.StartVolume) ?? CharacterParameters?.StartVolume ?? null;

				try
				{
					characterParameters.Robots = JsonConvert.DeserializeObject<IList<Robot>>(GetStringField(_parameters, "Robots")) ?? CharacterParameters?.Robots ?? new List<Robot>();
				}
				catch
				{
					_misty.SkillLogger.Log($"Failed parsing robot information, skill may not work as expected.");
					characterParameters.InitializationStatusMessage = "Warning. Could not parse robot information.";
					CharacterParameters.InitializationErrorStatus = InitializationStatus.Warning;
				}
				try
				{
					characterParameters.Recipes = JsonConvert.DeserializeObject<IList<Recipe>>(GetStringField(_parameters, "Recipes")) ?? CharacterParameters?.Recipes ?? new List<Recipe>();
				}
				catch
				{
					_misty.SkillLogger.Log($"Failed parsing recipe information, skill may not work as expected.");
					characterParameters.InitializationStatusMessage = "Warning. Could not parse recipe information.";
					CharacterParameters.InitializationErrorStatus = InitializationStatus.Warning;
				}
				characterParameters.ConversationGroup = conversationGroup;

				characterParameters.SpeakingImage = GetStringField(_parameters, ConversationConstants.SpeakingImage) ?? CharacterParameters?.SpeakingImage ?? "";
				characterParameters.ListeningImage = GetStringField(_parameters, ConversationConstants.ListeningImage) ?? CharacterParameters?.ListeningImage ?? "";
				characterParameters.ProcessingImage = GetStringField(_parameters, ConversationConstants.ProcessingImage) ?? CharacterParameters?.ProcessingImage ?? "";
				characterParameters.UsePreSpeech = GetBoolField(_parameters, ConversationConstants.UsePreSpeech) ?? CharacterParameters?.UsePreSpeech ?? false;

				//Parse string into prespeech by semicolon
				try
				{
					//TODO 

					string preSpeechString = GetStringField(_parameters, ConversationConstants.PreSpeechPhrases) ?? "";
					if(string.IsNullOrWhiteSpace(preSpeechString) && CharacterParameters.PreSpeechPhrases != null & CharacterParameters.PreSpeechPhrases.Count() > 0)
					{
						characterParameters.PreSpeechPhrases = CharacterParameters.PreSpeechPhrases;
					}
					else if (!string.IsNullOrWhiteSpace(preSpeechString))
					{
						characterParameters.PreSpeechPhrases = preSpeechString;					
					}
					else if (characterParameters.UsePreSpeech)
					{
						characterParameters.PreSpeechList = new List<string>
						{
							"One second please.",
							"Hold on one moment.",
							"I think I can help with that.",
							"Let me see.",
							"Let me find that.",
						};
					}
				}
				catch
				{
					_misty.SkillLogger.Log($"Failed parsing pre-speech phrases, using defaults.");
					characterParameters.InitializationStatusMessage = "Failed parsing pre-speech, using defaults.";
					characterParameters.InitializationErrorStatus = InitializationStatus.Warning;
				}
				finally
				{
					if (CharacterParameters.UsePreSpeech && (CharacterParameters.PreSpeechPhrases == null))
					{
						characterParameters.PreSpeechList = new List<string>
						{
							"One second please.",
							"Hold on one moment.",
							"I think I can help with that.",
							"Let me see.",
							"Let me find that.",
						};
					}
				}


				//Get speech configuration from string
				try
				{
					characterParameters.SpeechConfiguration = JsonConvert.DeserializeObject<SpeechConfiguration>(GetStringField(_parameters, ConversationConstants.SpeechConfiguration)) ?? CharacterParameters?.SpeechConfiguration ?? new SpeechConfiguration();
					SetSpeechParameters(characterParameters);
				}
				catch (Exception ex)
				{
					_misty.SkillLogger.Log("Failed parsing the speech configuration data, speech intent may not work.", ex);
					CharacterParameters.InitializationErrorStatus = InitializationStatus.Warning;
					characterParameters.InitializationStatusMessage = $"Speech configuration failed, speech intent may not work.";
				}
			
				characterParameters.AzureTTSParameters = new AzureSpeechParameters();
				characterParameters.GoogleTTSParameters = new GoogleSpeechParameters();
				characterParameters.AzureSpeechRecognitionParameters = new AzureSpeechParameters();
				characterParameters.GoogleSpeechRecognitionParameters = new GoogleSpeechParameters();

				characterParameters.TrackHistory = GetIntField(_parameters, ConversationConstants.TrackHistory) ?? CharacterParameters?.TrackHistory ?? 3;
				characterParameters.PersonConfidence = GetDoubleField(_parameters, ConversationConstants.PersonConfidence) ?? CharacterParameters?.PersonConfidence ?? 0.6;
				characterParameters.LogInteraction = GetBoolField(_parameters, ConversationConstants.LogInteraction) ?? CharacterParameters?.LogInteraction ?? true;
				characterParameters.StreamInteraction = GetBoolField(_parameters, ConversationConstants.StreamInteraction) ?? CharacterParameters?.StreamInteraction ?? false;
				characterParameters.FacePitchOffset = GetIntField(_parameters, ConversationConstants.FacePitchOffset) ?? CharacterParameters?.FacePitchOffset ?? 0;
				characterParameters.ObjectDetectionDebounce = GetIntField(_parameters, ConversationConstants.ObjectDetectionDebounce) ?? CharacterParameters?.ObjectDetectionDebounce ?? 0.333; //FollowFaceDebounce?
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Exception processing parameters.", ex);
				characterParameters.InitializationStatusMessage = "Exception processing parameters. Cannot continue.";
				CharacterParameters.InitializationErrorStatus = InitializationStatus.Error;
				return characterParameters;
			}

			try
			{
				if (!string.IsNullOrWhiteSpace(characterParameters.ConversationGroup.Id) && _overwriteLocalConfig && 
					characterParameters.InitializationErrorStatus != InitializationStatus.Warning && 
					characterParameters.InitializationErrorStatus != InitializationStatus.Error)
				{
					_conversationGroupList.Remove(characterParameters.ConversationGroup.Id);					
					_conversationGroupList.Add(characterParameters.ConversationGroup.Id, JsonConvert.SerializeObject(characterParameters));					
					await _database.SaveDataAsync(_conversationGroupList);
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Exception saving parameters.", ex);
				characterParameters.InitializationStatusMessage = "Exception saving parameters. Attempting to continue.";
				characterParameters.InitializationErrorStatus = InitializationStatus.Warning;
			}

			//Return what we have...
			characterParameters.InitializationErrorStatus = InitializationStatus.Success;
			return characterParameters;
		}

		public async Task<CharacterParameters> Initialize()
		{
			try
			{
				_database = SkillStorage.GetDatabase("conversation-skill");

				if (!await LoadConversationGroup())
				{
					CharacterParameters.InitializationErrorStatus = InitializationStatus.Error;
					CharacterParameters.InitializationStatusMessage = "Failed to set parameters. Cannot continue.";
					return CharacterParameters;
				}

				if (CharacterParameters.InitializationErrorStatus == InitializationStatus.Error)
				{
					return CharacterParameters;
				}

				CharacterParameters = await ParseCharacterParameters(_runningConversationGroup, _parameters);
				
				return CharacterParameters;
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Exception initializing parameters.", ex);
				CharacterParameters.InitializationStatusMessage = "Exception initializing parameters. Cannot continue.";
				CharacterParameters.InitializationErrorStatus = InitializationStatus.Error;
				return CharacterParameters;
			}
		}


		private void SetSpeechParameters(CharacterParameters characterParameters)
		{
			//Speech Rec Parameters
			if (!string.IsNullOrWhiteSpace(characterParameters.SpeechConfiguration?.SpeechRecognitionSubscriptionKey))
			{
				string recService = characterParameters.SpeechConfiguration.SpeechRecognitionService.ToLower().Trim();

				if (recService == "azure" || recService == "azureonboard")
				{
					characterParameters.AzureSpeechRecognitionParameters.SubscriptionKey = characterParameters.SpeechConfiguration.SpeechRecognitionSubscriptionKey;
					characterParameters.AzureSpeechRecognitionParameters.Region = characterParameters.SpeechConfiguration.SpeechRecognitionRegion ?? "";
					characterParameters.AzureSpeechRecognitionParameters.Endpoint = characterParameters.SpeechConfiguration.SpeechRecognitionEndpoint ?? "";
					characterParameters.AzureSpeechRecognitionParameters.TranslatedLanguage = characterParameters.SpeechConfiguration.TranslatedLanguage ?? "en";
					characterParameters.AzureSpeechRecognitionParameters.SpokenLanguage = characterParameters.SpeechConfiguration.SpokenLanguage ?? "en-US";
					characterParameters.AzureSpeechRecognitionParameters.ProfanitySetting = characterParameters.SpeechConfiguration.ProfanitySetting ?? "Raw";
				}
				else if (recService == "google" || recService == "googleonboard")
				{
					characterParameters.GoogleSpeechRecognitionParameters.SubscriptionKey = characterParameters.SpeechConfiguration.SpeechRecognitionSubscriptionKey ?? "";
					characterParameters.GoogleSpeechRecognitionParameters.Endpoint = characterParameters.SpeechConfiguration.SpeechRecognitionEndpoint ?? "https://speech.googleapis.com/v1p1beta1/speech:recognize?key=";
					characterParameters.GoogleSpeechRecognitionParameters.SpeakingVoice = characterParameters.SpeechConfiguration.SpeakingVoice ?? "en-US-Standard-C";
					characterParameters.GoogleSpeechRecognitionParameters.SpokenLanguage = characterParameters.SpeechConfiguration.SpokenLanguage ?? "en-US";
				}


				characterParameters.SpeechRecognitionService = recService?.ToLower().Trim() ?? "vosk";
			}
			
			//TTS Parameters
			if (!string.IsNullOrWhiteSpace(characterParameters.SpeechConfiguration?.TextToSpeechSubscriptionKey))
			{
				string ttsService = characterParameters.SpeechConfiguration.TextToSpeechService.ToLower().Trim();

				if (ttsService == "azure" || ttsService == "azureonboard")
				{
					characterParameters.AzureTTSParameters.SubscriptionKey = characterParameters.SpeechConfiguration.TextToSpeechSubscriptionKey;
					characterParameters.AzureTTSParameters.Region = characterParameters.SpeechConfiguration.SpeechRecognitionRegion ?? "";
					characterParameters.AzureTTSParameters.Endpoint = characterParameters.SpeechConfiguration.SpeechRecognitionEndpoint ?? "";
					characterParameters.AzureTTSParameters.SpeakingVoice = characterParameters.SpeechConfiguration.SpeakingVoice ?? "en-US-AriaNeural";
					characterParameters.AzureTTSParameters.TranslatedLanguage = characterParameters.SpeechConfiguration.TranslatedLanguage ?? "en";
					characterParameters.AzureTTSParameters.SpokenLanguage = characterParameters.SpeechConfiguration.SpokenLanguage ?? "en-US";
					characterParameters.AzureTTSParameters.ProfanitySetting = characterParameters.SpeechConfiguration.ProfanitySetting ?? "Raw";
				}
				else if (ttsService == "google" || ttsService == "googleonboard")
				{
					characterParameters.GoogleTTSParameters.SubscriptionKey = characterParameters.SpeechConfiguration.TextToSpeechSubscriptionKey;
					characterParameters.GoogleTTSParameters.Endpoint = characterParameters.SpeechConfiguration.TextToSpeechEndpoint ?? "https://texttospeech.googleapis.com/v1/text:synthesize?key=";
					characterParameters.GoogleTTSParameters.SpeakingVoice = characterParameters.SpeechConfiguration.SpeakingVoice ?? "en-US-Standard-C";
					characterParameters.GoogleTTSParameters.SpeakingGender = characterParameters.SpeechConfiguration.SpeakingGender ?? "FEMALE";
					characterParameters.GoogleTTSParameters.SpokenLanguage = characterParameters.SpeechConfiguration.SpokenLanguage ?? "en-US";
				}

				characterParameters.TextToSpeechService = ttsService ?? "misty";
			}
			else
			{
				characterParameters.TextToSpeechService = characterParameters.SpeechConfiguration.TextToSpeechService.ToLower().Trim();
			}
			
		}

		#region Available public methods to get character payload data
		
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
			if (_userDefinedParameters.TryGetValue(fieldName.ToLower(), out object value))
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

		#endregion

		#region Get Data Helper methods

		private string GetStringField(IDictionary<string, object> parameters, string dataKey, bool useDB = true)
		{
			try
			{
				string newValue = null;
				KeyValuePair<string, object> dataKVP = parameters.FirstOrDefault(x => x.Key.ToLower().Trim() == dataKey.ToLower());
				if (dataKVP.Value != null)
				{
					//getting passed in param and saving to local db
					newValue = Convert.ToString(dataKVP.Value);
					if(useDB)
					{
						_parameters.Remove(dataKey);
						_parameters.Add(dataKey, newValue ?? "");
					}
				}
				else if (useDB && _parameters.ContainsKey(dataKey))
				{
					//getting from local storage
					newValue = Convert.ToString(_parameters[dataKey]);
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
					_parameters.Remove(dataKey);
					_parameters.Add(dataKey, newValue ?? false);
				}
				else if (_parameters.ContainsKey(dataKey))
				{
					newValue = Convert.ToBoolean(_parameters[dataKey]);
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
						_parameters.Remove(dataKey);
						_parameters.Add(dataKey, newValue);
					}
				}
				else if (_parameters.ContainsKey(dataKey))
				{
					newValue = Convert.ToDateTime(_parameters[dataKey]);
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
					_parameters.Remove(dataKey);
					_parameters.Add(dataKey, newValue ?? null);
				}
				else if (_parameters.ContainsKey(dataKey))
				{
					newValue = Convert.ToInt32(_parameters[dataKey]);
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
					_parameters.Remove(dataKey);
					_parameters.Add(dataKey, newValue ?? null);
				}
				else if (_parameters.ContainsKey(dataKey))
				{
					newValue = Convert.ToDouble(_parameters[dataKey]);
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
					_parameters.Remove(dataKey);
					_parameters.Add(dataKey, newValue ?? null);
				}
				else if (_parameters.ContainsKey(dataKey))
				{
					newValue = Convert.ToSingle(_parameters[dataKey]);
				}
				return newValue;
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Exception in GetFloatField", ex);
				return null;
			}
		}

		#endregion

	}
}
 