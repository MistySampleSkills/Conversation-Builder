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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ConversationBuilder.Data.Cosmos;
using ConversationBuilder.DataModels;
using ConversationBuilder.ViewModels;
using ConversationBuilder.Extensions;

namespace ConversationBuilder.Controllers
{
	[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
	public class InteractionsController : AdminToolController
	{

		public InteractionsController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
			: base(cosmosDbService, userManager) { }

		public async Task<ActionResult> ManageTriggers(string interactionId, string message, int startItem = 1, int totalItems = 1000)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(interactionId);
				if(interaction == null)
				{
					return RedirectToAction("Error", "Home", new { message = "Failed to manage interaction." });
				}
				
				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(interaction.ConversationId);
			
				if (conversation == null)
				{
					return RedirectToAction("Error", "Home", new { message = "Failed to find interaction conversation" });
				}
				else
				{
					IList<string> conversationInteractions = conversation.Interactions;

					IList<TriggerDetail> triggers = await _cosmosDbService.ContainerManager.TriggerDetailData.GetListAsync(startItem, totalItems);//TODO				
					IList<TriggerDetailViewModel> filteredTriggers = new List<TriggerDetailViewModel>();

					foreach(var trigger in triggers)
					{
						if(!interaction.TriggerMap.ContainsKey(trigger.Id))
						{
							continue;
						}

						TriggerDetailViewModel triggerDetailViewModel = new TriggerDetailViewModel {};
						triggerDetailViewModel.Id = trigger.Id;
						triggerDetailViewModel.Name = trigger.Name;
						triggerDetailViewModel.Trigger = trigger.Trigger;
						triggerDetailViewModel.ItemType = trigger.ItemType;

						/*IDictionary<string, string> filterList = new TriggerFilters().AllItems;

						if(filterList.Any(x => x.Value ==trigger.TriggerFilter))
						{
							triggerDetailViewModel.TriggerFilter = trigger.TriggerFilter;
							triggerDetailViewModel.UserDefinedTriggerFilter = "";
						}
						else
						{
							triggerDetailViewModel.TriggerFilter = "";
							triggerDetailViewModel.UserDefinedTriggerFilter = trigger.TriggerFilter;
						}*/
						IDictionary<string, string> filterList = (new TriggerFilters()).AllItems.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
						
						//Deal with legacy mappings of speech handlers
						IList<SpeechHandler> speechHandlers = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetListAsync(1, 1000);//TODO
						foreach(var speechHandler in speechHandlers)
						{
							filterList.Add(new KeyValuePair<string, string>(speechHandler.Id, "SpeechHeard: " + speechHandler.Name));
						}

						string displayName = await GetTriggerFilterDisplayName(trigger.Trigger, trigger.TriggerFilter, filterList);
						if(filterList.Any(x => x.Value == displayName))
						{
							triggerDetailViewModel.TriggerFilter = trigger.TriggerFilter;
							triggerDetailViewModel.UserDefinedTriggerFilter = "";
						}
						else
						{
							triggerDetailViewModel.UserDefinedTriggerFilter = trigger.TriggerFilter;
						}

						string startingDisplayName = await GetTriggerFilterDisplayName(trigger.StartingTrigger, trigger.StartingTriggerFilter, filterList);
						if(filterList.Any(x => x.Value == startingDisplayName))
						{
							triggerDetailViewModel.StartingTriggerFilter = trigger.StartingTriggerFilter;
							triggerDetailViewModel.UserDefinedStartingTriggerFilter = "";
						}
						else
						{
							triggerDetailViewModel.StartingTriggerFilter = "";
							triggerDetailViewModel.UserDefinedStartingTriggerFilter = trigger.StartingTriggerFilter;
						}

						string stoppingDisplayName = await GetTriggerFilterDisplayName(trigger.StoppingTrigger, trigger.StoppingTriggerFilter, filterList);
						if(filterList.Any(x => x.Value == stoppingDisplayName))
						{
							triggerDetailViewModel.StoppingTriggerFilter = trigger.StoppingTriggerFilter;
							triggerDetailViewModel.UserDefinedStoppingTriggerFilter = "";
						}
						else
						{
							triggerDetailViewModel.StoppingTriggerFilter = "";
							triggerDetailViewModel.UserDefinedStoppingTriggerFilter = trigger.StoppingTriggerFilter;
						}

						triggerDetailViewModel.StartingTriggerDelay = trigger.StartingTriggerDelay;
						triggerDetailViewModel.StartingTrigger = trigger.StartingTrigger;
						triggerDetailViewModel.StoppingTriggerDelay = trigger.StoppingTriggerDelay;
						triggerDetailViewModel.StoppingTrigger = trigger.StoppingTrigger;
						
						triggerDetailViewModel.Created = trigger.Created;
						filteredTriggers.Add(triggerDetailViewModel);
						
					}
					await SetViewBagData();				
					SetFilterAndPagingViewData(1, null, triggers.Count(), totalItems);  //TODO
					
					ViewBag.Animations = await AnimationList();
					ViewBag.Triggers = new Triggers().AllItems;
					ViewBag.TriggerFilters = new TriggerFilters().AllItems; 
					ViewBag.Conversations = await ConversationList();
					ViewBag.LinkedConversations = await LinkedConversationList(interaction.ConversationId);
					ViewBag.SpeechHandlers = await SpeechHandlers();
					ViewBag.SkillMessages = await SkillMessages();
					ViewBag.TriggerDetails = await TriggerDetailList();
					ViewBag.Emotions =  new DefaultEmotions().AllItems;
					ViewBag.Interactions = await InteractionList();
					ViewBag.InteractionAndOptionList = await FullInteractionAndOptionList(interaction.ConversationId);
					ViewBag.InteractionAnimationList = await InteractionAnimationList(interaction.ConversationId);
					ViewBag.InteractionPreSpeechAnimationList = await InteractionPreSpeechAnimationList(interaction.ConversationId);

					InteractionViewModel interactionViewModel = new InteractionViewModel();
					interactionViewModel.ConversationId = interaction.ConversationId;
					interactionViewModel.Id = interaction.Id;
					interactionViewModel.Created = interaction.Created;
					interactionViewModel.InteractionFailedTimeout = interaction.InteractionFailedTimeout;
					interactionViewModel.ItemType = interaction.ItemType;
					interactionViewModel.Name = interaction.Name;
					interactionViewModel.Animation = interaction.Animation;
					interactionViewModel.PreSpeechAnimation = interaction.PreSpeechAnimation;

					interactionViewModel.StartListening = interaction.StartListening;
					
					interactionViewModel.UsePreSpeech = interaction.UsePreSpeech;					
					interactionViewModel.PreSpeechPhrases = interaction.PreSpeechPhrases;		
					interactionViewModel.AllowConversationTriggers = interaction.AllowConversationTriggers;
					interactionViewModel.AllowKeyPhraseRecognition = interaction.AllowKeyPhraseRecognition;
					interactionViewModel.ConversationEntryPoint = interaction.ConversationEntryPoint;					
					interactionViewModel.AllowVoiceProcessingOverride = interaction.AllowVoiceProcessingOverride;
					interactionViewModel.ListenTimeout = interaction.ListenTimeout;
					interactionViewModel.SilenceTimeout = interaction.SilenceTimeout;
					
					Animation animation = await _cosmosDbService.ContainerManager.AnimationData.GetAsync(interaction.Animation);
					interactionViewModel.AnimationData = animation;
					Animation preSpeechAnimation = await _cosmosDbService.ContainerManager.AnimationData.GetAsync(interaction.PreSpeechAnimation);
					interactionViewModel.PreSpeechAnimationData = preSpeechAnimation;

					interactionViewModel.TriggerDetails = filteredTriggers.OrderBy(x => x.Name).ToList();

					foreach(string skillMessageId in interaction.SkillMessages)
					{
						SkillMessage skillMessage = await _cosmosDbService.ContainerManager.SkillMessageData.GetAsync(skillMessageId);
						if(skillMessage != null)
						{
							interactionViewModel.SkillMessages.Add(skillMessage);
						}
					}
					
					IList<TriggerActionOption> intentActions = new List<TriggerActionOption>();

					IDictionary<string, IList<TriggerActionOption>> currentTriggerMap = interaction.TriggerMap;
					IDictionary<TriggerDetail, IList<TriggerActionOption>> triggerMap = new Dictionary<TriggerDetail, IList<TriggerActionOption>>();
					foreach(KeyValuePair<string, IList<TriggerActionOption>> kvp in currentTriggerMap)
					{
						TriggerDetail intentDetail = triggers.FirstOrDefault(x => x.Id == kvp.Key);

						if(intentDetail != null)
						{
							triggerMap.Add(intentDetail, kvp.Value);		
						}
					}
					interactionViewModel.TriggerMap = triggerMap;

					//Lotso slow code in here...
					ViewBag.AllInteractions = await AllInteractionList(conversation.Id);
					ViewBag.Message = message;
					return View(interactionViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing interaction list.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> AddTrigger(InteractionViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.ConversationId);				
				Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(model.Id);				
				
				if(interaction != null && conversation != null)
				{
					if(!interaction.TriggerMap.ContainsKey(model.Handler))
					{
						interaction.TriggerMap.Add(model.Handler, new List<TriggerActionOption>());
						if(!conversation.Triggers.Contains(model.Handler))
						{
							conversation.Triggers.Add(model.Handler);							
						}
					}
					await _cosmosDbService.ContainerManager.InteractionData.UpdateAsync(interaction);
					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);

					return RedirectToAction("ManageTriggers", new {interactionId = model.Id});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the interaction trigger list."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the interaction trigger list.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> RemoveTrigger(InteractionViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.ConversationId);				
				Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(model.Id);				
				
				if(interaction != null && conversation != null)
				{
					interaction.TriggerMap.Remove(model.Handler);
					await _cosmosDbService.ContainerManager.InteractionData.UpdateAsync(interaction);
					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);

					return RedirectToAction("ManageTriggers", new {interactionId = model.Id});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the interaction trigger list."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the interaction trigger list.", exception = ex.Message });
			}
		}

		
		public async Task<ActionResult> AddSkillMessage(InteractionViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.ConversationId);				
				Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(model.Id);				
				
				if(interaction != null && conversation != null)
				{
					if(!interaction.SkillMessages.Contains(model.Handler))
					{
						interaction.SkillMessages.Add(model.Handler);
					}
					await _cosmosDbService.ContainerManager.InteractionData.UpdateAsync(interaction);
					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);

					return RedirectToAction("ManageTriggers", new {interactionId = model.Id});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the skill message list."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the skill message list.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> RemoveSkillMessage(InteractionViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.ConversationId);				
				Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(model.Id);				
				
				if(interaction != null && conversation != null)
				{
					interaction.SkillMessages.Remove(model.Handler);
					await _cosmosDbService.ContainerManager.InteractionData.UpdateAsync(interaction);
					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);

					return RedirectToAction("ManageTriggers", new {interactionId = model.Id});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the skill message list."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the skill message list.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> AddResponseHandler(InteractionViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.ConversationId);				
				Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(model.Id);				
				Animation animation = await _cosmosDbService.ContainerManager.AnimationData.GetAsync(model.Animation);				
				Animation preSpeechAnimation = null;

				string prespeechAnimationId = "";
				if(!string.IsNullOrWhiteSpace(model.PreSpeechAnimation))
				{
					if(model.PreSpeechAnimation == "PreSpeech Default")
					{
						prespeechAnimationId = "PreSpeech Default";
					}
					else if(model.PreSpeechAnimation == "None")
					{
						prespeechAnimationId = "None";
					}
					else
					{
						preSpeechAnimation = await _cosmosDbService.ContainerManager.AnimationData.GetAsync(model.PreSpeechAnimation);	
						prespeechAnimationId = preSpeechAnimation.Id;
					}
				}
				
				if(interaction != null && conversation != null)
				{
					IList<TriggerActionOption> triggerActions;
					if(!interaction.TriggerMap.Remove(model.SelectedTrigger, out triggerActions))
					{
						triggerActions = new List<TriggerActionOption>();
					}
					
					TriggerActionOption triggerActionOption =  new TriggerActionOption();
					triggerActionOption.Id = Guid.NewGuid().ToString();

					Interaction goToInteraction = new Interaction();
					if(model.GoToInteraction == ConversationDeparturePoint)
					{
						//map triggerAction id to conversation depature points
						DepartureMap departureMap = new DepartureMap();
						departureMap.AnimationId = animation?.Id ?? "Default Animation";
						if(prespeechAnimationId != "PreSpeech Default")
						{
							departureMap.PreSpeechAnimationId = prespeechAnimationId;
						}

						departureMap.ConversationId = conversation.Id;
						departureMap.TriggerId = model.SelectedTrigger;
						departureMap.InteractionId = model.Id;
						departureMap.TriggerOptionId = triggerActionOption.Id;

						conversation.ConversationDeparturePoints.Add(triggerActionOption.Id, departureMap);
						triggerActionOption.GoToConversation = ConversationDeparturePoint; //??
						triggerActionOption.GoToInteraction = ConversationDeparturePoint;//??
					}
					else
					{
						triggerActionOption.GoToInteraction = model.GoToInteraction;
						goToInteraction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(model.GoToInteraction);									
						triggerActionOption.GoToConversation = goToInteraction.ConversationId;
					}

					triggerActionOption.InterruptCurrentAction = model.InterruptCurrentAction;
					triggerActionOption.Weight = model.Weight;
					triggerActionOption.Retrigger = model.Retrigger;
					
					if(!string.IsNullOrWhiteSpace(model.Animation) && model.Animation != "Default Animation")
					{
						conversation.InteractionAnimations.Remove(triggerActionOption.Id);
						conversation.InteractionAnimations.Add(triggerActionOption.Id, model.Animation);

						if(!conversation.Animations.Contains(model.Animation))
						{
							conversation.Animations.Add(model.Animation);
						}
					}
					else
					{
						if(!conversation.Animations.Contains(goToInteraction.Animation))
						{
							conversation.Animations.Add(goToInteraction.Animation);
						}
					}

					if(!string.IsNullOrWhiteSpace(prespeechAnimationId))
					{
						if(model.PreSpeechAnimation != "PreSpeech Default")
						{
							conversation.InteractionPreSpeechAnimations.Remove(triggerActionOption.Id);
							conversation.InteractionPreSpeechAnimations.Add(triggerActionOption.Id, prespeechAnimationId);


							if(prespeechAnimationId != "None" && !conversation.Animations.Contains(prespeechAnimationId))
							{
								conversation.Animations.Add(prespeechAnimationId);
							}
						}
					}
					else
					{
						if(!conversation.Animations.Contains(goToInteraction.PreSpeechAnimation))
						{
							conversation.Animations.Add(goToInteraction.PreSpeechAnimation);
						}
					}

					TriggerDetail triggerDetail = await _cosmosDbService.ContainerManager.TriggerDetailData.GetAsync(model.SelectedTrigger);	
					triggerActions.Add(triggerActionOption);
					interaction.TriggerMap.Add(model.SelectedTrigger, triggerActions);

					await _cosmosDbService.ContainerManager.InteractionData.UpdateAsync(interaction);
					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);

					return RedirectToAction("ManageTriggers", new {interactionId = model.Id});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the interaction response handler list."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the interaction response handler list.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> RemoveResponseHandler(string interactionId, string conversationId, string selectedTriggerId, string removedTriggerAction)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(conversationId);				
				Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(interactionId);				
				
				if(interaction != null && conversation != null)
				{
					IList<ConversationGroup> conversationGroups = await _cosmosDbService.ContainerManager.ConversationGroupData.GetListAsync();
					foreach(ConversationGroup conversationGroup in conversationGroups)
					{
						if(conversationGroup.ConversationMappings.Values.Any(x => x.DepartureMap.TriggerOptionId == removedTriggerAction))
						{
							//used in a mapping, say it cannot be used at this time as other conversations depend on it
							return RedirectToAction("ManageTriggers", new {interactionId = interactionId, message = "This option is a mapped departure point in conversation groups and cannot be removed at this time."});
						}
					}

					IList<TriggerActionOption> triggerActionOptions;
					if(interaction.TriggerMap.Remove(selectedTriggerId, out triggerActionOptions))
					{
						TriggerActionOption triggerActionOption = triggerActionOptions.FirstOrDefault(x => x.Id == removedTriggerAction);
						if(triggerActionOption != null)
						{
							triggerActionOptions.Remove(triggerActionOption);							
							conversation.ConversationDeparturePoints.Remove(removedTriggerAction);
						}
						interaction.TriggerMap.Add(selectedTriggerId, triggerActionOptions);
					}
					
					string animationId;
					if(conversation.InteractionAnimations.TryGetValue(removedTriggerAction, out animationId))
					{
						conversation.InteractionAnimations.Remove(removedTriggerAction);
					}

					//Check if we need to remove this from the conversation mapping
					//Get all the animations from the interactions
					IList<string> conversationAnimations = new List<string>();
					foreach(string interationId in conversation.Interactions)
					{
						//get the interaction default animation and add it
						Interaction interation = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(interationId);
						if(interation != null)
						{
							if(!conversationAnimations.Contains(interation.Animation))
							{
								conversationAnimations.Add(interation.Animation);
							}
							
							//then go through interaction animation mappings
							foreach(KeyValuePair<string, IList<TriggerActionOption>> handler in interation.TriggerMap)
							{
								if(handler.Value != null && handler.Value.Count() > 0)
								{
									foreach(TriggerActionOption triggerActionOption in handler.Value)
									{
										if(conversation.InteractionAnimations.ContainsKey(triggerActionOption.Id) && !conversationAnimations.Contains(conversation.InteractionAnimations[triggerActionOption.Id]))
										{
											conversationAnimations.Add(conversation.InteractionAnimations[triggerActionOption.Id]);
										}
									}
								}
							}
						}
					}

					if(!conversationAnimations.Contains(animationId))
					{
						conversation.Animations.Remove(animationId);
					}



					string preSpeechAnimationId;
					if(conversation.InteractionPreSpeechAnimations.TryGetValue(removedTriggerAction, out preSpeechAnimationId))
					{
						conversation.InteractionPreSpeechAnimations.Remove(removedTriggerAction);
					}

					//Check if we need to remove this from the conversation mapping
					//Get all the animations from the interactions
					IList<string> prespeechConversationAnimations = new List<string>();
					foreach(string interationId in conversation.Interactions)
					{
						//get the interaction default animation and add it
						Interaction interation = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(interationId);
						if(interation != null)
						{
							if(!prespeechConversationAnimations.Contains(interation.PreSpeechAnimation))
							{
								prespeechConversationAnimations.Add(interation.PreSpeechAnimation);
							}
							
							//then go through interaction animation mappings
							foreach(KeyValuePair<string, IList<TriggerActionOption>> handler in interation.TriggerMap)
							{
								if(handler.Value != null && handler.Value.Count() > 0)
								{
									foreach(TriggerActionOption triggerActionOption in handler.Value)
									{
										if(conversation.InteractionPreSpeechAnimations.ContainsKey(triggerActionOption.Id) && !prespeechConversationAnimations.Contains(conversation.InteractionPreSpeechAnimations[triggerActionOption.Id]))
										{
											prespeechConversationAnimations.Add(conversation.InteractionPreSpeechAnimations[triggerActionOption.Id]);
										}
									}
								}
							}
						}
					}

					if(!prespeechConversationAnimations.Contains(preSpeechAnimationId))
					{
						conversation.Animations.Remove(preSpeechAnimationId);
					}
					
					conversation.ConversationDeparturePoints.Remove(removedTriggerAction);
					await _cosmosDbService.ContainerManager.InteractionData.UpdateAsync(interaction);
					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);

					return RedirectToAction("ManageTriggers", new {interactionId = interactionId});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the interaction response handler list."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the interaction response handler list.", exception = ex.Message });
			}
		}


		public async Task<ActionResult> Index(string conversationId, int startItem = 1, int totalItems = 100)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(conversationId);
				if (conversation == null)
				{
					return View();
				}
				else
				{	
					int totalCount = await _cosmosDbService.ContainerManager.InteractionData.GetCountAsync(conversationId);
					IList<Interaction> interactions = await _cosmosDbService.ContainerManager.InteractionData.GetListAsync(startItem, totalItems, conversationId);
					
					//Clean up all the bags
					ViewBag.Animations = await AnimationList();
					await SetViewBagData();
					ViewBag.ConversationId = conversationId;
					ViewBag.ConversationName = conversation.Name;
					SetFilterAndPagingViewData(1, null, interactions.Count, totalItems);
					return View(interactions.OrderBy(x => x.Name));
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing interaction list.", exception = ex.Message });
			}
		}


		public async Task<ActionResult> Clone(string id)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Interaction interaction =  await _cosmosDbService.ContainerManager.InteractionData.GetAsync(id);
				if (interaction != null)
				{
					Interaction newInteraction = new Interaction();
					newInteraction.Id = Guid.NewGuid().ToString();
					newInteraction.Name = $"CLONE: {interaction.Name} [{newInteraction.Id}]" ;
					newInteraction.Animation = interaction.Animation;
					newInteraction.InteractionFailedTimeout = interaction.InteractionFailedTimeout;
					newInteraction.ConversationId = interaction.ConversationId;					
					newInteraction.StartListening = interaction.StartListening;						
					newInteraction.UsePreSpeech = interaction.UsePreSpeech;					
					newInteraction.PreSpeechPhrases = interaction.PreSpeechPhrases;	
					newInteraction.AllowConversationTriggers = interaction.AllowConversationTriggers;
					newInteraction.AllowKeyPhraseRecognition = interaction.AllowKeyPhraseRecognition;
					newInteraction.ConversationEntryPoint = interaction.ConversationEntryPoint;					
					newInteraction.AllowVoiceProcessingOverride = interaction.AllowVoiceProcessingOverride;
					newInteraction.ListenTimeout = interaction.ListenTimeout;
					newInteraction.SilenceTimeout = interaction.SilenceTimeout;

					
					DateTimeOffset now = DateTimeOffset.UtcNow;
					newInteraction.Updated = now;
					newInteraction.Created = now;

					Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(interaction.ConversationId);
					
					foreach(KeyValuePair<string, IList<TriggerActionOption>> triggerMapData in interaction.TriggerMap)
					{
						if(triggerMapData.Value != null && triggerMapData.Value.Count() > 0)
						{
							newInteraction.TriggerMap.TryAdd(triggerMapData.Key, triggerMapData.Value);

							IList<TriggerActionOption> optionList = new List<TriggerActionOption>();
							foreach(TriggerActionOption option in triggerMapData.Value)
							{
								//Make a copy and add it
								TriggerActionOption triggerActionOption = new TriggerActionOption();
								triggerActionOption.GoToConversation = option.GoToConversation;
								triggerActionOption.GoToInteraction = option.GoToInteraction;
								triggerActionOption.Weight = option.Weight;
								triggerActionOption.Retrigger = option.Retrigger;
								triggerActionOption.InterruptCurrentAction = option.InterruptCurrentAction;

								optionList.Add(triggerActionOption);						
							}

							newInteraction.TriggerMap.TryAdd(triggerMapData.Key, optionList);
						}
					}

					foreach(string skillMessageData in interaction.SkillMessages)
					{
						if(!string.IsNullOrWhiteSpace(skillMessageData))
						{
							newInteraction.SkillMessages.Add(skillMessageData);
						}
					}

					await _cosmosDbService.ContainerManager.InteractionData.AddAsync(newInteraction);
					
					conversation.Interactions.Add(newInteraction.Id);
					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);
					return RedirectToAction(nameof(Index), new {conversationId = interaction.ConversationId});
				
				}
				return RedirectToAction("Error", "Home", new { message = "Exception accessing interaction details." });
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing interaction details.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> Details(string id)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				
				Interaction interaction =  await _cosmosDbService.ContainerManager.InteractionData.GetAsync(id);
				if (interaction == null)
				{
					return RedirectToAction(nameof(Index), new {conversationId = interaction.ConversationId});
				}
				else
				{				
					ViewBag.Animations = await AnimationList();
					await SetViewBagData();
					return View(interaction);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing interaction details.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> Create(string conversationId)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Interaction interaction = new Interaction {};
				interaction.ConversationId = conversationId;
				ViewBag.Animations = await AnimationList();
				await SetViewBagData();
				return View(interaction);				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating interaction.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(Interaction model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
				
				if (ModelState.IsValid)
				{
					model.Id = Guid.NewGuid().ToString();
					DateTimeOffset dt = DateTimeOffset.UtcNow;
					model.CreatedBy = userInfo.AccessId;
					model.Created = dt;
					model.Updated = dt;
					Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.ConversationId);					
					if(model.ConversationEntryPoint)
					{
						EntryMap entryMap = new EntryMap();
						entryMap.ConversationId = model.ConversationId;
						entryMap.InteractionId = model.Id;
						conversation.ConversationEntryPoints.Add(model.Id, entryMap);
					}
					
					if(!conversation.Interactions.Contains(model.Id))
					{
						conversation.Interactions.Add(model.Id);
					}
					ViewBag.Animations = await AnimationList();
					if(!conversation.Animations.Contains(model.Animation))
					{
						conversation.Animations.Add(model.Animation);
					}
					if(!conversation.Animations.Contains(model.PreSpeechAnimation))
					{
						conversation.Animations.Add(model.PreSpeechAnimation);
					}

					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);
					await _cosmosDbService.ContainerManager.InteractionData.AddAsync(model);
					return RedirectToAction(nameof(Index), new {conversationId = model.ConversationId});
				}
				else
				{
					return RedirectToAction(nameof(Create), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating interaction.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> Edit(string id)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
				
				Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(id);
				if (interaction == null)
				{
					return RedirectToAction(nameof(Index), new {conversationId = interaction.ConversationId});
				}
				else
				{

					ViewBag.ReadOnlyEntryPoint = false;
					IList<ConversationGroup> conversationGroups = await _cosmosDbService.ContainerManager.ConversationGroupData.GetListAsync(1, 1000);//TODO	
					foreach(ConversationGroup conversationGroup in conversationGroups)
					{
						if(conversationGroup.ConversationMappings.Any(x => x.Value.EntryMap.InteractionId == id))
						{
							ViewBag.ReadOnlyEntryPoint = true;
						}
					}

					ViewBag.Animations = await AnimationList();
					await SetViewBagData();
					return View(interaction);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing interaction.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(Interaction interaction)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
			
				if (ModelState.IsValid)
				{
					Interaction loadedInteraction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(interaction.Id);
					Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(interaction.ConversationId);

					loadedInteraction.Name = interaction.Name;
					loadedInteraction.Animation = interaction.Animation;
					loadedInteraction.PreSpeechAnimation = interaction.PreSpeechAnimation;
					loadedInteraction.InteractionFailedTimeout = interaction.InteractionFailedTimeout;
					loadedInteraction.StartListening = interaction.StartListening;			
					loadedInteraction.UsePreSpeech = interaction.UsePreSpeech;					
					loadedInteraction.PreSpeechPhrases = interaction.PreSpeechPhrases;	
					loadedInteraction.AllowKeyPhraseRecognition = interaction.AllowKeyPhraseRecognition;
					loadedInteraction.AllowConversationTriggers = interaction.AllowConversationTriggers;
					loadedInteraction.ConversationEntryPoint = interaction.ConversationEntryPoint;
				
					if (interaction.ConversationEntryPoint && !conversation.ConversationEntryPoints.ContainsKey(interaction.Id))
					{
						EntryMap entryMap = new EntryMap();
						entryMap.ConversationId = interaction.ConversationId;
						entryMap.InteractionId = interaction.Id;
						conversation.ConversationEntryPoints.Add(interaction.Id, entryMap);
					}
					else if (conversation.ConversationEntryPoints.ContainsKey(interaction.Id))
					{
						conversation.ConversationEntryPoints.Remove(interaction.Id);
					}
					
					loadedInteraction.AllowVoiceProcessingOverride = interaction.AllowVoiceProcessingOverride;
					loadedInteraction.ListenTimeout = interaction.ListenTimeout;
					loadedInteraction.SilenceTimeout = interaction.SilenceTimeout;					
					loadedInteraction.Updated = DateTimeOffset.UtcNow;
					await _cosmosDbService.ContainerManager.InteractionData.UpdateAsync(loadedInteraction);
					
					if(!conversation.Animations.Contains(interaction.Animation))
					{
						conversation.Animations.Add(interaction.Animation);
					}

					if(!conversation.Animations.Contains(interaction.PreSpeechAnimation))
					{
						conversation.Animations.Add(interaction.PreSpeechAnimation);
					}


					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);
					return RedirectToAction(nameof(Index), new {conversationId = interaction.ConversationId});
				}
				else
				{
					return RedirectToAction(nameof(Edit), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing interaction.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> Delete(string id)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
			
				Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(id);
				if (interaction == null)
				{
					return RedirectToAction(nameof(Index), new {conversationId = interaction.ConversationId});
				}
				else
				{
					
					ViewBag.CanBeDeleted = true;
					IList<ConversationGroup> conversationGroups = await _cosmosDbService.ContainerManager.ConversationGroupData.GetListAsync();
					foreach(ConversationGroup conversationGroup in conversationGroups)
					{
						if(conversationGroup.ConversationMappings.Values.Any(x => x.EntryMap.InteractionId == id))
						{
							//used in a mapping, say it cannot be deleted at this time as other conversations depend on it
							ViewBag.CanBeDeleted = false;
						}
					}
					
					ViewBag.Conversations = await ConversationList();
					ViewBag.Animations = await AnimationList();
					await SetViewBagData();
					return View(interaction);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting interaction.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Delete(string id, IFormCollection collection)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(id);
				 await _cosmosDbService.ContainerManager.InteractionData.DeleteAsync(id);
				return RedirectToAction(nameof(Index), new {conversationId = interaction.ConversationId});				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting interaction.", exception = ex.Message });
			}
		}
	}
}