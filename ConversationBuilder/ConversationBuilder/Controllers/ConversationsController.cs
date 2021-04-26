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
using ConversationBuilder.Extensions;
using ConversationBuilder.ViewModels;

namespace ConversationBuilder.Controllers
{
	[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
	public class ConversationsController : AdminToolController
	{
		public ConversationsController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
			: base(cosmosDbService, userManager) { }

		public async Task<ActionResult> Index(int startItem = 1, int totalItems = 100)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				await SetViewBagData();
				int totalCount = await _cosmosDbService.ContainerManager.ConversationData.GetCountAsync();
				IList<Conversation> conversations = await _cosmosDbService.ContainerManager.ConversationData.GetListAsync(startItem, totalItems);
				SetFilterAndPagingViewData(1, null, totalCount, totalItems);
				if (conversations == null)
				{
					return View();
				}
				else
				{
					return View(conversations.OrderBy(x => x.Name));
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing conversation list.", exception = ex.Message });
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

				
				Conversation conversation =  await _cosmosDbService.ContainerManager.ConversationData.GetAsync(id);
				Interaction startupInteraction = null;
				Interaction noTriggerInteraction = null;
				if(!string.IsNullOrWhiteSpace(conversation.StartupInteraction))
				{
					startupInteraction =  await _cosmosDbService.ContainerManager.InteractionData.GetAsync(conversation.StartupInteraction);
				}

				if(!string.IsNullOrWhiteSpace(conversation.NoTriggerInteraction))
				{
					noTriggerInteraction =  await _cosmosDbService.ContainerManager.InteractionData.GetAsync(conversation.NoTriggerInteraction);
				}
				
				if (conversation == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{				
					if(!string.IsNullOrWhiteSpace(startupInteraction?.Name))
					{
						ViewBag.StartupInteractionName = startupInteraction?.Name;
					}
					else
					{
						ViewBag.StartupInteractionName = "None selected";
					}

					if(!string.IsNullOrWhiteSpace(noTriggerInteraction?.Name))
					{
						ViewBag.NoTriggerInteractionName = noTriggerInteraction?.Name;
					}
					else
					{
						ViewBag.NoTriggerInteractionName = "None selected";
					}

					await SetViewBagData();
					return View(conversation);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing conversation details.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> Create()
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = new Conversation();
				
				ViewBag.Emotions = (new DefaultEmotions()).AllItems;					
				ViewBag.Interactions = new Dictionary<string, Dictionary<string, string>>();
				await SetViewBagData();
				return View(conversation);				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating conversation.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(Conversation model)
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

					await _cosmosDbService.ContainerManager.ConversationData.AddAsync(model);
					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Create), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating conversation.", exception = ex.Message });
			}
		}

		/*public async Task<ActionResult> Clone(string id)
		{
			
		}*/

		public async Task<ActionResult> Edit(string id)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
				
				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(id);
				if (conversation == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.Emotions = (new DefaultEmotions()).AllItems;
					ViewBag.Interactions = await InteractionList(id);
					await SetViewBagData();
					return View(conversation);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing conversation.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(Conversation conversation)
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
					Conversation loadedConversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(conversation.Id);
					loadedConversation.Name = conversation.Name;
					loadedConversation.Description = conversation.Description;
					loadedConversation.StartingEmotion = conversation.StartingEmotion;
					loadedConversation.StartupInteraction = conversation.StartupInteraction;
					loadedConversation.NoTriggerInteraction = conversation.NoTriggerInteraction;
					loadedConversation.ManagementAccess = conversation.ManagementAccess;
					loadedConversation.InitiateSkillsAtConversationStart = conversation.InitiateSkillsAtConversationStart;
					loadedConversation.Updated = DateTimeOffset.UtcNow;
					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(loadedConversation);

					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Edit), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing conversation.", exception = ex.Message });
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
			
				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(id);
				if (conversation == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.CanBeDeleted = await CanBeDeleted(id, DeleteItem.Conversation);
					await SetViewBagData();
					return View(conversation);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting conversation.", exception = ex.Message });
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

				IList<Interaction> interactions = await _cosmosDbService.ContainerManager.InteractionData.GetListAsync(1, 1000, id);
				foreach(Interaction interaction in interactions)
				{
					if(interaction?.Id != null)
					{
						await _cosmosDbService.ContainerManager.InteractionData.DeleteAsync(interaction.Id);
					}
				}

				await _cosmosDbService.ContainerManager.ConversationData.DeleteAsync(id);
				return RedirectToAction(nameof(Index));				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting conversation.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> Manage(string conversationId, int startItem = 1, int totalItems = 1000)
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
					return RedirectToAction("Error", "Home", new { message = "Failed to manage conversation" });
				}
				else
				{
					IList<string> conversationInteractions = conversation.Interactions;
					IList<TriggerDetail> triggers = await _cosmosDbService.ContainerManager.TriggerDetailData.GetListAsync(startItem, totalItems);//TODO				
					IList<TriggerDetailViewModel> filteredTriggers = new List<TriggerDetailViewModel>();

					foreach(var trigger in triggers)
					{
						if(!conversation.ConversationTriggerMap.ContainsKey(trigger.Id))
						{
							continue;
						}

						TriggerDetailViewModel triggerDetailViewModel = new TriggerDetailViewModel {};
						triggerDetailViewModel.Id = trigger.Id;
						triggerDetailViewModel.Name = trigger.Name;
						triggerDetailViewModel.Trigger = trigger.Trigger;
						triggerDetailViewModel.ItemType = trigger.ItemType;

						IDictionary<string, string> filterList = new TriggerFilters().AllItems;

						if(filterList.Any(x => x.Value ==trigger.TriggerFilter))
						{
							triggerDetailViewModel.TriggerFilter = trigger.TriggerFilter;
							triggerDetailViewModel.UserDefinedTriggerFilter = "";
						}
						else
						{
							triggerDetailViewModel.TriggerFilter = "";
							triggerDetailViewModel.UserDefinedTriggerFilter = trigger.TriggerFilter;
						}

						if(filterList.Any(x => x.Value ==trigger.StartingTriggerFilter))
						{
							triggerDetailViewModel.StartingTriggerFilter = trigger.StartingTriggerFilter;
							triggerDetailViewModel.UserDefinedStartingTriggerFilter = "";
						}
						else
						{
							triggerDetailViewModel.StartingTriggerFilter = "";
							triggerDetailViewModel.UserDefinedStartingTriggerFilter = trigger.StartingTriggerFilter;
						}

						if(filterList.Any(x => x.Value ==trigger.StoppingTriggerFilter))
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
					ViewBag.LinkedConversations = await LinkedConversationList(conversationId);
					ViewBag.SpeechHandlers = await SpeechHandlers();
					ViewBag.SkillMessages = await SkillMessages();
					ViewBag.TriggerDetails = (await TriggerDetailList()).ToList();
					ViewBag.Emotions =  new DefaultEmotions().AllItems;
					ViewBag.Interactions = await InteractionList();
					ViewBag.InteractionAndOptionList = await FullInteractionAndOptionList(conversationId);
					ViewBag.InteractionAnimationList = await InteractionAnimationList(conversationId);

					ConversationViewModel conversationViewModel = new ConversationViewModel();
					conversationViewModel.Description = conversation.Description;
					conversationViewModel.Id = conversation.Id;
					conversationViewModel.Created = conversation.Created;
					conversationViewModel.Updated = conversation.Updated;
					conversationViewModel.Name = conversation.Name;
					conversationViewModel.NoTriggerInteraction = conversation.NoTriggerInteraction;
					conversationViewModel.StartingEmotion = conversation.StartingEmotion;
					conversationViewModel.StartupInteraction = conversation.StartupInteraction;
					conversationViewModel.InitiateSkillsAtConversationStart = conversation.InitiateSkillsAtConversationStart;
				
					conversationViewModel.TriggerDetails = filteredTriggers.OrderBy(x => x.Name).ToList();

					foreach(string skillMessageId in conversation.SkillMessages)
					{
						SkillMessage skillMessage = await _cosmosDbService.ContainerManager.SkillMessageData.GetAsync(skillMessageId);
						if(skillMessage != null)
						{
							conversationViewModel.SkillMessages.Add(skillMessage);
						}
					}
					
					IList<TriggerActionOption> intentActions = new List<TriggerActionOption>();

					IDictionary<string, IList<TriggerActionOption>> currentTriggerMap = conversation.ConversationTriggerMap;
					IDictionary<TriggerDetail, IList<TriggerActionOption>> triggerMap = new Dictionary<TriggerDetail, IList<TriggerActionOption>>();
					foreach(KeyValuePair<string, IList<TriggerActionOption>> kvp in currentTriggerMap)
					{
						TriggerDetail intentDetail = triggers.FirstOrDefault(x => x.Id == kvp.Key);

						if(intentDetail != null)
						{
							triggerMap.Add(intentDetail, kvp.Value);		
						}
					}
					conversationViewModel.ConversationTriggerMap = triggerMap;

					//Lotso slow code in here...
					ViewBag.AllInteractions = await AllInteractionList();
					return View(conversationViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing interaction list.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> AddTrigger(ConversationViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.Id);		

				if(conversation != null)
				{
					if(!conversation.ConversationTriggerMap.ContainsKey(model.Handler))
					{
						conversation.ConversationTriggerMap.Add(model.Handler, new List<TriggerActionOption>());
						if(!conversation.Triggers.Contains(model.Handler))
						{
							conversation.Triggers.Add(model.Handler);							
						}
					}
					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);

					return RedirectToAction("Manage", new {conversationId = model.Id});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the conversation trigger list."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the interaction trigger list.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> RemoveTrigger(ConversationViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.Id);
				
				if(conversation != null)
				{
					conversation.ConversationTriggerMap.Remove(model.Handler);
					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);

					return RedirectToAction("Manage", new {conversationId = model.Id});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the conversation trigger list."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the conversation trigger list.", exception = ex.Message });
			}
		}

		

		public async Task<ActionResult> AddResponseHandler(ConversationViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.Id);	
				Animation animation = await _cosmosDbService.ContainerManager.AnimationData.GetAsync(model.Animation);			
				
				if(conversation != null)
				{
					IList<TriggerActionOption> triggerActions;
					if(!conversation.ConversationTriggerMap.Remove(model.SelectedTrigger, out triggerActions))
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
						departureMap.TriggerId = model.SelectedTrigger;
						departureMap.ConversationId = conversation.Id;
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

					TriggerDetail triggerDetail = await _cosmosDbService.ContainerManager.TriggerDetailData.GetAsync(model.SelectedTrigger);	
					
					triggerActions.Add(triggerActionOption);
				
					conversation.ConversationTriggerMap.Add(model.SelectedTrigger, triggerActions);

					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);

					return RedirectToAction("Manage", new {conversationId = model.Id});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the conversation response handler list."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the conversation response handler list.", exception = ex.Message });
			}
		}


		//TODO This is hacky
		public async Task<ActionResult> RemoveResponseHandler(string conversationId, string selectedTriggerId, string removedTriggerAction)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(conversationId);			
				
				if(conversation != null)
				{
					IList<TriggerActionOption> triggerActions;
					if(conversation.ConversationTriggerMap.Remove(selectedTriggerId, out triggerActions))
					{
						TriggerActionOption intentAction = triggerActions.FirstOrDefault(x => x.Id == removedTriggerAction);
						if(intentAction != null)
						{
							triggerActions.Remove(intentAction);
						}
						conversation.ConversationTriggerMap.Add(selectedTriggerId, triggerActions);
					}
					
					string animationId;
					if(conversation.InteractionAnimations.TryGetValue(removedTriggerAction, out animationId))
					{
						conversation.InteractionAnimations.Remove(removedTriggerAction);
					}

					//Check if we need to remove this from the conversation mapping
					//Get all the animations from the interactions
					IList<string> conversationAnimations = new List<string>();
					foreach(string interactionId in conversation.Interactions)
					{
						//get the interaction default animation and add it
						Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(interactionId);
						if(interaction != null)
						{
							if(!conversationAnimations.Contains(interaction.Animation))
							{
								conversationAnimations.Add(interaction.Animation);
							}
							
							//then go through interaction animation mappings
							foreach(KeyValuePair<string, IList<TriggerActionOption>> handler in interaction.TriggerMap)
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

					conversation.ConversationDeparturePoints.Remove(removedTriggerAction);
					if(!conversationAnimations.Contains(animationId))
					{
						conversation.Animations.Remove(animationId);
					}

					await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);

					return RedirectToAction("Manage", new {conversationId = conversationId});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the conversation response handler list."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the conversation response handler list.", exception = ex.Message });
			}
		}

		
		public async Task<ActionResult> Import()
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				ImportViewModel viewModel = new ImportViewModel {};
				await SetViewBagData();
				return View(viewModel);			
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception importing conversations.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Import(ImportViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				if(!await ImportConversations(model.Config))
				{
					return RedirectToAction("Error", "Home", new { message = "Failed to successfully process configuration." });
				}

				return RedirectToAction(nameof(Index));			
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception importing conversations.", exception = ex.Message });
			}
		}

	}
}