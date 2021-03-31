
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ConversationBuilder.Data.Cosmos;
using ConversationBuilder.DataModels;
using ConversationBuilder.Extensions;

namespace ConversationBuilder.Controllers
{
	public class CommonViewData
	{
		public CommonViewData(UserInformation userInformation, string message = null)
		{
			IsSiteAdmin = userInformation.IsSiteAdmin;
			Message = message;
		}

		public bool IsSiteAdmin { get; set; }
		public string Message { get; set; } = "";
	}

	public class UserInformation
	{
		public bool IsSiteAdmin { get; set; }
		public string AccessId { get; set; }
	}

	public class AdminToolController : Controller
	{
		protected const string UserNotFoundMessage = "Sorry, I failed to find your user information.";
		protected const string NoOrgAccessMessage = "Sorry, you can't access the requested page.";
		protected const string FailedCreatingMessage = "Sorry, we encountered an error while creating.";
		protected const string FailedUpdatingMessage = "Sorry, we encountered an error while updating.";

		protected readonly ICosmosDbService _cosmosDbService;
		protected readonly UserManager<ApplicationUser> _userManager;

		private UserInformation _userInformation = null;

		public AdminToolController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
		{
			_cosmosDbService = cosmosDbService;
			_userManager = userManager;
		}

		public async Task<SkillParameters> GenerateSkillConfiguration(string id)
		{
			SkillParameters skillParameters = new SkillParameters();
			SkillConversationGroup skillConversationGroup = new SkillConversationGroup();
			ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(id);
			
			if (conversationGroup == null)
			{
				return null;
			}
			else
			{
				skillConversationGroup.Id = conversationGroup.Id;
				skillConversationGroup.Name = conversationGroup.Name;
				skillConversationGroup.Description = conversationGroup.Description;
				skillConversationGroup.RobotName = conversationGroup.RobotName;
				skillConversationGroup.KeyPhraseRecognizedAudio = conversationGroup.KeyPhraseRecognizedAudio;
				skillConversationGroup.CharacterConfiguration = conversationGroup.CharacterConfiguration;
				skillConversationGroup.StartupConversation = conversationGroup.StartupConversation;

				foreach(string conversationId in conversationGroup.Conversations)
				{
					Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(conversationId);
					if(conversation == null)
					{
						continue;
					}

					//Add all the speech handlers from the conversations
					foreach(string speechHandlerId in conversation.SpeechHandlers)
					{
						SpeechHandler speechHandler = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetAsync(speechHandlerId);
						skillConversationGroup.IntentUtterances.TryAdd(speechHandler.Name, new KeyValuePair<bool, IList<string>>(speechHandler.ExactMatchesOnly, speechHandler.Utterances));
					}

					foreach(string genericDataStoreId in conversation.GenericDataStores)
					{
						if(genericDataStoreId != null && !skillConversationGroup.GenericDataStores.Any(x => x.Id == genericDataStoreId))
						{
							GenericDataStore genericDataStore = await _cosmosDbService.ContainerManager.GenericDataStoreData.GetAsync(genericDataStoreId);							
							if(genericDataStore != null)
							{
								skillConversationGroup.GenericDataStores.Add(genericDataStore);
							}
							
						}
					}

					SkillConversation skillConversation = new SkillConversation();

					foreach(string animationId in conversation.Animations)
					{
						if(animationId != null && !skillConversation.Animations.Any(x => x.Id == animationId))
						{
							Animation animation = await _cosmosDbService.ContainerManager.AnimationData.GetAsync(animationId);
							if(animation != null)
							{
								if(!string.IsNullOrWhiteSpace(animation.HeadLocation))
								{
									HeadLocation headLocation = await _cosmosDbService.ContainerManager.HeadLocationData.GetAsync(animation.HeadLocation);
									if(headLocation != null && !skillConversation.HeadLocations.Any(x => x.Id == animation.HeadLocation))
									{
										skillConversation.HeadLocations.Add(headLocation);
									}
								}

								if(!string.IsNullOrWhiteSpace(animation.ArmLocation))
								{
									ArmLocation armLocation = await _cosmosDbService.ContainerManager.ArmLocationData.GetAsync(animation.ArmLocation);
									if(armLocation != null && !skillConversation.ArmLocations.Any(x => x.Id == animation.ArmLocation))
									{
										skillConversation.ArmLocations.Add(armLocation);
									}
								}

								if(!string.IsNullOrWhiteSpace(animation.LEDTransitionAction))
								{
									LEDTransitionAction ledTransitionAction = await _cosmosDbService.ContainerManager.LEDTransitionActionData.GetAsync(animation.LEDTransitionAction);
									if(ledTransitionAction != null && !skillConversation.LEDTransitionActions.Any(x => x.Id == animation.LEDTransitionAction))
									{
										skillConversation.LEDTransitionActions.Add(ledTransitionAction);
									}
								}

								skillConversation.Animations.Add(animation);
							}
						}
					}

					foreach(string triggerId in conversation.Triggers)
					{
						if(!skillConversation.Triggers.Any(x => x.Id == triggerId))
						{
							TriggerDetail triggerDetail = await _cosmosDbService.ContainerManager.TriggerDetailData.GetAsync(triggerId);
							if(triggerDetail!= null && !skillConversation.Triggers.Any(x => x.Id == triggerId))
							{
								skillConversation.Triggers.Add(triggerDetail);
							}
						}
					}

					foreach(string interactionId in conversation.Interactions)
					{
						if(!skillConversation.Interactions.Any(x => x.Id == interactionId))
						{
							Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(interactionId);
							if(interaction != null)
							{
								SkillInteraction skillInteraction = new SkillInteraction();
								skillInteraction.Id = interaction.Id;
								skillInteraction.Name = interaction.Name;
								skillInteraction.InteractionFailedTimeout = interaction.InteractionFailedTimeout;
								skillInteraction.ConversationId = interaction.ConversationId;
								skillInteraction.TriggerMap = interaction.TriggerMap;
								skillInteraction.AllowVoiceProcessingOverride = interaction.AllowVoiceProcessingOverride;
								skillInteraction.StartListening = interaction.StartListening;
								skillInteraction.AllowConversationTriggers = interaction.AllowConversationTriggers;
								skillInteraction.AllowKeyPhraseRecognition = interaction.AllowKeyPhraseRecognition;
								skillInteraction.SilenceTimeout = interaction.SilenceTimeout;
								skillInteraction.ListenTimeout = interaction.ListenTimeout;
								skillInteraction.Animation = interaction.Animation;
								skillConversation.Interactions.Remove(skillInteraction);
								skillConversation.Interactions.Add(skillInteraction);

								if(interaction.SkillMessages != null)
								{
									//Move this to group for json?
									foreach(string skillMessageId in interaction.SkillMessages)
									{					
										skillInteraction.SkillMessages.Add(skillMessageId);
										if(!skillConversation.SkillMessages.Any(x => x.Id == skillMessageId))
										{
											SkillMessage skillMessage = await _cosmosDbService.ContainerManager.SkillMessageData.GetAsync(skillMessageId);
											if(skillMessage!= null)
											{
												skillConversation.SkillMessages.Add(skillMessage);													
											}
										}
									}
								}
							}
						}
					}
					
					skillConversation.InteractionAnimations = conversation.InteractionAnimations;
					skillConversation.Description = conversation.Description;
					skillConversation.InitiateSkillsAtConversationStart = conversation.InitiateSkillsAtConversationStart;
					skillConversation.Name = conversation.Name;
					skillConversation.Id = conversation.Id;
					skillConversation.StartingEmotion = conversation.StartingEmotion;
					skillConversation.StartupInteraction = conversation.StartupInteraction;
					skillConversation.NoTriggerInteraction = conversation.NoTriggerInteraction;
					skillConversation.ConversationTriggerMap = conversation.ConversationTriggerMap;
					skillConversationGroup.Conversations.Add(skillConversation);	
				}

				skillParameters.ConversationGroup = skillConversationGroup;
				
				if(!string.IsNullOrWhiteSpace(conversationGroup.CharacterConfiguration))
				{
					CharacterConfiguration characterConfiguration = await _cosmosDbService.ContainerManager.CharacterConfigurationData.GetAsync(conversationGroup.CharacterConfiguration);
					if(characterConfiguration != null)
					{
						skillParameters.FacePitchOffset = characterConfiguration.FacePitchOffset;
						skillParameters.LogInteraction = characterConfiguration.LogInteraction;
						skillParameters.HeardSpeechToScreen = characterConfiguration.HeardSpeechToScreen;
						skillParameters.LargePrint = characterConfiguration.LargePrint;
						skillParameters.ShowListeningIndicator = characterConfiguration.ShowListeningIndicator;
						skillParameters.DisplaySpoken = characterConfiguration.DisplaySpoken;						
						skillParameters.StartVolume = characterConfiguration.StartVolume;
						skillParameters.Payload = characterConfiguration.Payload;
						skillParameters.LogLevel = characterConfiguration.LogLevel;
						skillParameters.ObjectDetectionDebounce = characterConfiguration.ObjectDetectionDebounce;
						skillParameters.PersonConfidence = characterConfiguration.PersonConfidence;
						skillParameters.StreamInteraction = characterConfiguration.StreamInteraction;
						skillParameters.Skill = characterConfiguration.Skill ?? "8be20a90-1150-44ac-a756-ebe4de30689e";
						skillParameters.Character = characterConfiguration.Character ?? "basic";
					
						if(!string.IsNullOrEmpty(characterConfiguration.SpeechConfiguration))
						{
							SpeechConfiguration speechConfiguration = await _cosmosDbService.ContainerManager.SpeechConfigurationData.GetAsync(characterConfiguration.SpeechConfiguration);
							if(speechConfiguration != null)
							{
								skillParameters.SpeechConfiguration = speechConfiguration;
							}
						}
					}
					else
					{
						skillParameters.Skill = "8be20a90-1150-44ac-a756-ebe4de30689e";
						skillParameters.Character = "basic";
					}
				}

				return skillParameters;
			}
		}

		public async Task<bool> ImportConversations(string conversationGroupJson)
		{

			try
			{
				SkillParameters skillParameters = new SkillParameters();

				if (!string.IsNullOrWhiteSpace(conversationGroupJson))
				{
					try
					{
						skillParameters = Newtonsoft.Json.JsonConvert.DeserializeObject<SkillParameters>(conversationGroupJson);
				
					}
					catch (Exception)
					{
						return false;
					}
				}
				
				SkillConversationGroup skillConversationGroup = skillParameters.ConversationGroup;

				DateTimeOffset now = DateTime.Now;
				foreach(KeyValuePair<string, KeyValuePair<bool, IList<string>>> utterance in skillConversationGroup.IntentUtterances) 
				{
					//TODO Update data for specific requests
					//Only add speech handler if NAME (which is also trigger filter) is not in use for now.
					if((await _cosmosDbService.ContainerManager.SpeechHandlerData.GetListAsync(1000))?.FirstOrDefault(x => x.Name == utterance.Key) == null)
					{
						SpeechHandler speechHandler = new SpeechHandler();
						speechHandler.Id = Guid.NewGuid().ToString();
						speechHandler.Name = utterance.Key;
						speechHandler.Updated = now;
						speechHandler.Created = now;
						speechHandler.ManagementAccess = "Public";
						speechHandler.ExactMatchesOnly = utterance.Value.Key;
						speechHandler.Utterances = utterance.Value.Value;
						await _cosmosDbService.ContainerManager.SpeechHandlerData.AddAsync(speechHandler);
					}
				}
				
				IDictionary<string, string> conversationGuidMap = new Dictionary<string, string>();
				IDictionary<string, string> interactionGuidMap = new Dictionary<string, string>();

				foreach(SkillConversation conversation in skillConversationGroup.Conversations)
				{
					conversationGuidMap.Add(conversation.Id, Guid.NewGuid().ToString());

					foreach(SkillInteraction interaction in conversation.Interactions)
					{
						interactionGuidMap.Add(interaction.Id, Guid.NewGuid().ToString());
					}
				}
				
				foreach(SkillConversation conversation in skillConversationGroup.Conversations)
				{
					//Make a new conversation with the same data
					Conversation newConversation = new Conversation();
					newConversation.Id = conversationGuidMap[conversation.Id];					
					newConversation.Name = conversation.Name + $" [Imported on {now}]";
					newConversation.Description = conversation.Description;
					newConversation.Updated = now;
					newConversation.Created = now;
					newConversation.ManagementAccess = "Public";
					newConversation.StartingEmotion = conversation.StartingEmotion;
					newConversation.InitiateSkillsAtConversationStart = conversation.InitiateSkillsAtConversationStart;

					//go through top level items and add them if needed (id doesn't exist)
					
					//arm movement
					foreach(ArmLocation newData in conversation.ArmLocations)
					{
						if(await _cosmosDbService.ContainerManager.ArmLocationData.GetAsync(newData.Id) == null)
						{
							newData.Name = newData.Name + " [Imported]";
							newData.ManagementAccess = "Public";
							newData.Updated = now;
							newData.Created = now;
							await _cosmosDbService.ContainerManager.ArmLocationData.AddAsync(newData);
						}
					}
					//head movement
					foreach(HeadLocation newData in conversation.HeadLocations)
					{
						if(await _cosmosDbService.ContainerManager.HeadLocationData.GetAsync(newData.Id) == null)
						{
							newData.Name = newData.Name + " [Imported]";
							newData.ManagementAccess = "Public";
							newData.Updated = now;
							newData.Created = now;
							await _cosmosDbService.ContainerManager.HeadLocationData.AddAsync(newData);
						}
					}

					//led
					foreach(LEDTransitionAction newData in conversation.LEDTransitionActions)
					{
						if(await _cosmosDbService.ContainerManager.LEDTransitionActionData.GetAsync(newData.Id) == null)
						{
							newData.Name = newData.Name + " [Imported]";
							newData.ManagementAccess = "Public";
							newData.Updated = now;
							newData.Created = now;
							await _cosmosDbService.ContainerManager.LEDTransitionActionData.AddAsync(newData);
						}
					}
					
					//animations
					foreach(Animation newData in conversation.Animations)
					{
						if(await _cosmosDbService.ContainerManager.AnimationData.GetAsync(newData.Id) == null)
						{
							newData.Name = newData.Name + " [Imported]";
							newData.ManagementAccess = "Public";
							newData.Updated = now;
							newData.Created = now;
							await _cosmosDbService.ContainerManager.AnimationData.AddAsync(newData);
						}
						if(!newConversation.Animations.Contains(newData.Id))
						{
							newConversation.Animations.Add(newData.Id);
						}
					}

					//triggers
					foreach(TriggerDetail newData in conversation.Triggers)
					{
						if(await _cosmosDbService.ContainerManager.TriggerDetailData.GetAsync(newData.Id) == null)
						{
							newData.Name = newData.Name + " [Imported]";
							newData.ManagementAccess = "Public";
							newData.Updated = now;
							newData.Created = now;
							await _cosmosDbService.ContainerManager.TriggerDetailData.AddAsync(newData);
						}
						
						if(!newConversation.Triggers.Contains(newData.Id))
						{
							newConversation.Triggers.Add(newData.Id);
						}
					}

					//skill messages
					foreach(SkillMessage newData in conversation.SkillMessages)
					{
						if(await _cosmosDbService.ContainerManager.SkillMessageData.GetAsync(newData.Id) == null)
						{
							newData.Name = newData.Name + " [Imported]";
							newData.ManagementAccess = "Public";
							newData.Updated = now;
							newData.Created = now;
							await _cosmosDbService.ContainerManager.SkillMessageData.AddAsync(newData);
						}
						if(!newConversation.SkillMessages.Contains(newData.Id))
						{
							newConversation.SkillMessages.Add(newData.Id);
						}
					}
					
					//NOT user data, character configs speech configs
					//users need to add those to conversations and groups

					//loop through skill interactions and create NEW matching interactions 
					foreach(SkillInteraction interaction in conversation.Interactions)
					{
						Interaction newInteraction = new Interaction();
						newInteraction.Id = interactionGuidMap[interaction.Id];						
						newInteraction.ListenTimeout = interaction.ListenTimeout;
						newInteraction.SilenceTimeout = interaction.SilenceTimeout;
						newInteraction.StartListening = interaction.StartListening;
						newInteraction.InteractionFailedTimeout = interaction.InteractionFailedTimeout;
						newInteraction.Name = interaction.Name;
						newInteraction.Animation = interaction.Animation;
						newInteraction.ConversationId = newConversation.Id;
						newInteraction.SkillMessages = interaction.SkillMessages;
						newInteraction.Updated = now;
						newInteraction.Created = now;


						//loop though action options and add and point to NEW interaction id
						foreach(KeyValuePair<string, IList<TriggerActionOption>> triggerOption in interaction.TriggerMap)
						{
							IList<TriggerActionOption> newTriggerOptions = new List<TriggerActionOption>();
							foreach(TriggerActionOption triggerActionOption in triggerOption.Value)
							{
								TriggerActionOption newTriggerActionOption = new TriggerActionOption();
								newTriggerActionOption.InterruptCurrentAction = triggerActionOption.InterruptCurrentAction;
								newTriggerActionOption.GoToConversation = conversationGuidMap[triggerActionOption.GoToConversation];
								newTriggerActionOption.GoToInteraction = interactionGuidMap[triggerActionOption.GoToInteraction];
								newTriggerActionOption.Weight = triggerActionOption.Weight;
								newTriggerActionOption.Id = Guid.NewGuid().ToString();
								newTriggerOptions.Add(newTriggerActionOption);

								if(conversation.InteractionAnimations.ContainsKey(triggerActionOption.Id))
								{
									newConversation.InteractionAnimations.Add(newTriggerActionOption.Id, conversation.InteractionAnimations[triggerActionOption.Id]);
								}
							}
							newInteraction.TriggerMap.Add(triggerOption.Key, newTriggerOptions);
						}
						
						await _cosmosDbService.ContainerManager.InteractionData.AddAsync(newInteraction);

						newConversation.StartupInteraction = interactionGuidMap[conversation.StartupInteraction];
						newConversation.NoTriggerInteraction = interactionGuidMap[conversation.NoTriggerInteraction];
						newConversation.Interactions.Add(newInteraction.Id);
					}

					foreach(KeyValuePair<string, IList<TriggerActionOption>> triggerOption in conversation.ConversationTriggerMap)
					{
						IList<TriggerActionOption> newTriggerOptions = new List<TriggerActionOption>();
						foreach(TriggerActionOption triggerActionOption in triggerOption.Value)
						{
							TriggerActionOption newTriggerActionOption = new TriggerActionOption();
							newTriggerActionOption.InterruptCurrentAction = triggerActionOption.InterruptCurrentAction;
							newTriggerActionOption.GoToConversation = conversationGuidMap[triggerActionOption.GoToConversation];
							newTriggerActionOption.GoToInteraction = interactionGuidMap[triggerActionOption.GoToInteraction];
							newTriggerActionOption.Weight = triggerActionOption.Weight;
							newTriggerActionOption.Id = Guid.NewGuid().ToString();
							newTriggerOptions.Add(newTriggerActionOption);
						}

						newConversation.ConversationTriggerMap.Add(triggerOption.Key, newTriggerOptions);
					}

					await _cosmosDbService.ContainerManager.ConversationData.AddAsync(newConversation);
				}
				return true;
			}
			catch
			{
				return false;
			}
		}


		protected async Task SetViewBagData(string message = null)
		{
			if (_userInformation == null)
			{
				await GetUserInformation();
			}

			UserConfiguration userConfiguration = await _cosmosDbService.ContainerManager.UserConfigurationData.GetAsync(_userInformation.AccessId);
			if(!string.IsNullOrWhiteSpace(userConfiguration?.OverrideCssFile))
			{
				ViewBag.CssFile = userConfiguration.OverrideCssFile + (userConfiguration.OverrideCssFile.EndsWith(".css") ? "" : ".css");
			}
			else
			{
				ViewBag.CssFile = "lite-theme.css";
			}
			
			ViewBag.Message = message ?? "";
			ViewBag.Data = new CommonViewData(_userInformation);
		}

		protected void SetFilterAndPagingViewData(int startItem, string filterName, int total, int count = 50)
		{
			//TODO Cleanup
			ViewBag.PagingSize = count;
			if (string.IsNullOrWhiteSpace(filterName))
			{
				ViewBag.FilterInfo = "";
				ViewBag.FIlterData = "";
			}
			else
			{
				ViewBag.FilterInfo = $"Filtered by {filterName}.";
				ViewBag.FIlterData = filterName;
			}

			ViewBag.ForwardStartItem = startItem + count > total ? -1 : startItem + count;
			if (startItem < 1 || startItem > total+1)
			{
				startItem = 1;
			}

			if (startItem == 1)
			{
				ViewBag.BackwardStartItem = -1;
			}
			else if (startItem - count >= 1)
			{
				ViewBag.BackwardStartItem = startItem - count;
			}
			else
			{
				ViewBag.BackwardStartItem = 1;
			}

			if (total > count)
			{
				int endItem = startItem + count > total ? total : startItem + (count-1);
				ViewBag.PagingInfo = $"Viewing {startItem} to {endItem} of {total} items.";
			}
			else
			{
				ViewBag.PagingInfo = $"Viewing {total} item{(total == 1 ? "" : "s")}.";
			}
		}

		protected async Task<UserInformation> GetUserInformation()
		{
			try
			{
				System.Security.Claims.ClaimsPrincipal currentUser = User;
				ApplicationUser applicationUser = await _userManager.GetUserAsync(User);
				_userInformation = new UserInformation();

				_userInformation.AccessId = applicationUser.Id;
				if (User.IsInRole(Roles.SiteAdministrator))
				{
					_userInformation.IsSiteAdmin = true;
				}

				return _userInformation;
			}
			catch
			{
				//TODO Log
				return null;
			}
		}

		public async Task<string> GetConversationName(string id)
		{
			Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(id);
			if(conversation != null)
			{
				return conversation.Name;
			}
			return "";
		}

		public async Task<string> GetAnimationName(string id)
		{
			Animation animation = await _cosmosDbService.ContainerManager.AnimationData.GetAsync(id);
			if(animation != null)
			{
				return animation.Name;
			}
			return "";
		}

		public async Task<string> GetTriggerDetailName(string id)
		{
			TriggerDetail intentDetail = await _cosmosDbService.ContainerManager.TriggerDetailData.GetAsync(id);
			if(intentDetail != null)
			{
				return intentDetail.Name;
			}
			return "";
		}

		public async Task<string> GetConversationGroupName(string id)
		{
			ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(id);
			if(conversationGroup != null)
			{
				return conversationGroup.Name;
			}
			return "";
		}

		public async Task<string> GetSpeechHandlerName(string id)
		{
			SpeechHandler speechHandler = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetAsync(id);
			if(speechHandler != null)
			{
				return speechHandler.Name;
			}
			return "";
		}

		public async Task<string> GetInteractionName(string id)
		{
			Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(id);
			if(interaction != null)
			{
				return interaction.Name;
			}
			return "";
		}
		
		protected async Task<IDictionary<string, string>> GenericDataStores()
		{
			//TODO Deal with performance reloading and paging
			IDictionary<string, string> list = new Dictionary<string, string>();
			IList<GenericDataStore> genericDataStores = await _cosmosDbService.ContainerManager.GenericDataStoreData.GetListAsync();
			foreach(GenericDataStore genericDataStore in genericDataStores)
			{
				list.Add(genericDataStore.Id, genericDataStore.Name);
			}
			return list ?? new Dictionary<string, string>();
		}

		protected async Task<IDictionary<string, string>> SpeechHandlers()
		{
			//TODO Deal with performance reloading and paging
			IDictionary<string, string> list = new Dictionary<string, string>();
			IList<SpeechHandler> speechHandlers = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetListAsync();
			foreach(SpeechHandler speechHandler in speechHandlers)
			{
				list.Add(speechHandler.Id, speechHandler.Name);
			}
			return list ?? new Dictionary<string, string>();
		}

		protected async Task<IDictionary<string, string>> SkillMessages()
		{
			//TODO Deal with performance reloading and paging
			IDictionary<string, string> list = new Dictionary<string, string>();
			IList<SkillMessage> speechHandlers = await _cosmosDbService.ContainerManager.SkillMessageData.GetListAsync();
			foreach(SkillMessage speechHandler in speechHandlers)
			{
				list.Add(speechHandler.Id, speechHandler.Name);
			}
			return list ?? new Dictionary<string, string>();
		}

		protected async Task<IDictionary<string, string>> AnimationList()
		{
			//TODO Deal with performance reloading and paging
			IDictionary<string, string> list = new Dictionary<string, string>();
			IList<Animation> animations = await _cosmosDbService.ContainerManager.AnimationData.GetListAsync();
			foreach(Animation animation in animations)
			{
				list.Add(animation.Id, animation.Name);
			}
			return list ?? new Dictionary<string, string>();
		}

		protected async Task<IDictionary<string, string>> ArmLocationList()
		{
			//TODO Deal with performance reloading and paging
			IDictionary<string, string> list = new Dictionary<string, string>();
			IList<ArmLocation> armLocations = await _cosmosDbService.ContainerManager.ArmLocationData.GetListAsync();
			foreach(ArmLocation armLocation in armLocations)
			{
				list.Add(armLocation.Id, armLocation.Name);
			}
			return list ?? new Dictionary<string, string>();
		}

		protected async Task<IDictionary<string, string>> SpeechConfigurationList()
		{
			//TODO Deal with performance reloading and paging
			IDictionary<string, string> list = new Dictionary<string, string>();
			IList<SpeechConfiguration> speechConfigurations = await _cosmosDbService.ContainerManager.SpeechConfigurationData.GetListAsync();
			foreach(SpeechConfiguration speechConfiguration in speechConfigurations)
			{
				list.Add(speechConfiguration.Id, speechConfiguration.Name);
			}
			return list ?? new Dictionary<string, string>();
		}

		protected async Task<IDictionary<string, string>> LEDTransitionActionList()
		{
			//TODO Deal with performance reloading and paging
			IDictionary<string, string> list = new Dictionary<string, string>();
			IList<LEDTransitionAction> ledTransitionActions = await _cosmosDbService.ContainerManager.LEDTransitionActionData.GetListAsync();
			foreach(LEDTransitionAction ledTransitionAction in ledTransitionActions)
			{
				list.Add(ledTransitionAction.Id, ledTransitionAction.Name);
			}
			return list ?? new Dictionary<string, string>();
		}

		protected async Task<IDictionary<string, string>> HeadLocationList()
		{
			//TODO Deal with performance reloading and paging
			IDictionary<string, string> list = new Dictionary<string, string>();
			IList<HeadLocation> headLocations = await _cosmosDbService.ContainerManager.HeadLocationData.GetListAsync();
			foreach(HeadLocation headLocation in headLocations)
			{
				list.Add(headLocation.Id, headLocation.Name);
			}
			return list ?? new Dictionary<string, string>();
		}

		protected async Task<IDictionary<string, string>> TriggerDetailList()
		{
			//TODO Deal with performance reloading and paging
			IDictionary<string, string> list = new Dictionary<string, string>();
			IList<TriggerDetail> intentDetails = await _cosmosDbService.ContainerManager.TriggerDetailData.GetListAsync();
			foreach(TriggerDetail intentDetail in intentDetails)
			{
				list.Add(intentDetail.Id, $"{intentDetail.Name}");
			}

			return list ?? new Dictionary<string, string>();
		}

		protected async Task<Dictionary<string, string>> InteractionList(string conversationId = null)
		{
			//TODO Deal with performance reloading and paging
			Dictionary<string, string> conversationInteractions = new Dictionary<string, string>();
			IList<Interaction> interactions = await _cosmosDbService.ContainerManager.InteractionData.GetListAsync(1, 10000, conversationId);
			
			foreach(Interaction interaction in interactions)
			{
				if(interaction.ConversationId == null) continue;  //temp hack for old data
				conversationInteractions.TryAdd(interaction.Id, interaction.Name);
			}
			return conversationInteractions ?? new Dictionary<string, string>();
		}

		protected async Task<Dictionary<string, string>> InteractionAnimationList(string conversationId)
		{
			//TODO Deal with performance reloading and paging
			Dictionary<string, string> interactionAnimations = new Dictionary<string, string>();
			Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(conversationId);
			
			foreach(KeyValuePair<string, string> interactionAnimation in conversation.InteractionAnimations)
			{
				if(interactionAnimation.Value == null) continue;

				interactionAnimations.TryAdd(interactionAnimation.Key, interactionAnimation.Value);
			}
			return interactionAnimations ?? new Dictionary<string, string>();
		}

		protected async Task<IDictionary<string, Dictionary<string, string>>> FullInteractionAndOptionList(string conversationId)
		{
			//Get conversations from this group
			IDictionary<string, Dictionary<string, string>> conversationInteractions = new Dictionary<string, Dictionary<string, string>>();
			IList<ConversationGroup> conversationGroups = await _cosmosDbService.ContainerManager.ConversationGroupData.GetListAsync(1, 10000);
			IList<ConversationGroup> filteredConversationGroups = conversationGroups.Where(x => x.Conversations.Contains(conversationId)).ToList();
			if(filteredConversationGroups == null || !filteredConversationGroups.Any())
			{
				Dictionary<string, string> interactionList = new Dictionary<string, string>();
				IList<Interaction> interactions = await _cosmosDbService.ContainerManager.InteractionData.GetListAsync(1, 10000, conversationId);
				foreach(Interaction interaction in interactions)
				{
					if(interaction?.Id != null && !interactionList.ContainsKey(interaction.Id))
					{
						interactionList.Add(interaction.Id, interaction.Name);				
					}							
				}
				conversationInteractions.Add(conversationId, interactionList);
			}
			else
			{
				foreach(ConversationGroup conversationGroup in filteredConversationGroups)
				{
					foreach(string otherConversationId in conversationGroup.Conversations)
					{
						if(!string.IsNullOrWhiteSpace(otherConversationId) && !conversationInteractions.ContainsKey(otherConversationId))
						{
							Dictionary<string, string> interactionList = new Dictionary<string, string>();
							IList<Interaction> interactions = await _cosmosDbService.ContainerManager.InteractionData.GetListAsync(1, 10000, otherConversationId);
							foreach(Interaction interaction in interactions)
							{
								if(interaction?.Id != null && !interactionList.ContainsKey(interaction.Id))
								{
									interactionList.Add(interaction.Id, interaction.Name);				
								}							
							}
							
							if(interactionList != null && !conversationInteractions.ContainsKey(otherConversationId))
							{
								conversationInteractions.Add(otherConversationId, interactionList);
							}
						}
						
					}
				}
			}
		
			conversationInteractions.OrderByDescending(x => x.Key);
			return conversationInteractions ?? new Dictionary<string, Dictionary<string, string>>();
		}

		protected async Task<IDictionary<string, string>> AllInteractionList()
		{
			
			//TODO Deal with performance reloading and paging
			IDictionary<string, string> list = new Dictionary<string, string>();
			IList<Interaction> interactions = await _cosmosDbService.ContainerManager.InteractionData.GetListAsync();
			foreach(Interaction interaction in interactions)
			{
				list.Add(interaction.Id, interaction.Name);
			}
			return list ?? new Dictionary<string, string>();
		}

		protected async Task<IDictionary<string, string>> LinkedConversationList(string conversationId)
		{
			//Get conversations from this group
			IList<ConversationGroup> conversationGroups = await _cosmosDbService.ContainerManager.ConversationGroupData.GetListAsync(1, 10000);
			IEnumerable<ConversationGroup> filteredConversationGroups = conversationGroups.Where(x => x.Conversations.Contains(conversationId));	
			IDictionary<string, string> list = new Dictionary<string, string>();

			if(filteredConversationGroups == null || !filteredConversationGroups.Any())
			{
				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(conversationId);
				list.Add(conversation.Id, conversation.Name);											
			}
			else
			{
				foreach(ConversationGroup conversationGroup in filteredConversationGroups)
				{
					foreach(string otherConversationId in conversationGroup.Conversations)
					{
						if(!string.IsNullOrWhiteSpace(otherConversationId) && !list.ContainsKey(otherConversationId))
						{
							Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(otherConversationId);
							list.Add(conversation.Id, conversation.Name);						
						}
						
					}
				}
			}			

			list.OrderByDescending(x => x.Key);
			return list ?? new Dictionary<string, string>();
		}

		protected async Task<IDictionary<string, string>> ConversationList()
		{
			//TODO Deal with performance reloading and paging
			IDictionary<string, string> list = new Dictionary<string, string>();
			IList<Conversation> conversations = await _cosmosDbService.ContainerManager.ConversationData.GetListAsync();
			foreach(Conversation conversation in conversations)
			{
				list.Add(conversation.Id, conversation.Name);
			}
			return list ?? new Dictionary<string, string>();
		}

		protected async Task<IDictionary<string, string>> CharacterConfigurationsList()
		{
			//TODO Deal with performance reloading and paging
			IDictionary<string, string> list = new Dictionary<string, string>();
			IList<CharacterConfiguration> conversations = await _cosmosDbService.ContainerManager.CharacterConfigurationData.GetListAsync();
			foreach(CharacterConfiguration characterConfiguration in conversations)
			{
				list.Add(characterConfiguration.Id, characterConfiguration.Name);
			}
			return list ?? new Dictionary<string, string>();
		}

		protected async Task<bool> CanBeDeleted(string itemId, DeleteItem itemType)
		{
			switch(itemType)
			{
				case DeleteItem.GenericDataStore:
					IList<Conversation> conversations0 = await _cosmosDbService.ContainerManager.ConversationData.GetListAsync();
					return !conversations0.Any(x => x.GenericDataStores.Contains(itemId));
				case DeleteItem.Animation:
					IList<Conversation> conversations1 = await _cosmosDbService.ContainerManager.ConversationData.GetListAsync();
					return !conversations1.Any(x => x.Animations.Contains(itemId));
				case DeleteItem.ArmLocation:
					IList<Animation> animations1 = await _cosmosDbService.ContainerManager.AnimationData.GetListAsync();
					return !animations1.Any(x => x.ArmLocation == itemId);
				case DeleteItem.HeadLocation:
					IList<Animation> animations2 = await _cosmosDbService.ContainerManager.AnimationData.GetListAsync();
					return !animations2.Any(x => x.HeadLocation == itemId);
				case DeleteItem.LedTransitionAction:
					IList<Animation> animations3 = await _cosmosDbService.ContainerManager.AnimationData.GetListAsync();
					return !animations3.Any(x => x.LEDTransitionAction == itemId);
				case DeleteItem.SpeechConfiguration:
					IList<CharacterConfiguration> characterData = await _cosmosDbService.ContainerManager.CharacterConfigurationData.GetListAsync();
					return !characterData.Any(x => x.SpeechConfiguration == itemId);
				case DeleteItem.CharacterConfiguration:
					IList<ConversationGroup> conversationsGroups1 = await _cosmosDbService.ContainerManager.ConversationGroupData.GetListAsync();
					return !conversationsGroups1.Any(x => x.CharacterConfiguration == itemId);
				case DeleteItem.Conversation:
					IList<ConversationGroup> conversationsGroups2 = await _cosmosDbService.ContainerManager.ConversationGroupData.GetListAsync();
					return !conversationsGroups2.Any(x => x.Conversations.Contains(itemId));
				case DeleteItem.SkillMessage:
					IList<ConversationGroup> conversationsGroups3 = await _cosmosDbService.ContainerManager.ConversationGroupData.GetListAsync();
					return !conversationsGroups3.Any(x => x.SkillMessages.Contains(itemId));
				case DeleteItem.Trigger:
					IList<Conversation> conversations5 = await _cosmosDbService.ContainerManager.ConversationData.GetListAsync();
					return !conversations5.Any(x => x.Triggers.Contains(itemId));
				case DeleteItem.SpeechHandler:
					IList<Conversation> conversations6 = await _cosmosDbService.ContainerManager.ConversationData.GetListAsync();
					return !conversations6.Any(x => x.SpeechHandlers.Contains(itemId));				
			}

			return true;
		}
	}

	public enum DeleteItem
	{
		Animation,
		CharacterConfiguration,
		Conversation,
		SkillMessage,
		Trigger,		
		SpeechHandler,
		ArmLocation,
		HeadLocation,
		LedTransitionAction,
		SpeechConfiguration,
		GenericDataStore
	}
}