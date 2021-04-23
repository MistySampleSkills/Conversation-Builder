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
	public class ConversationGroupsController : AdminToolController
	{
		public ConversationGroupsController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
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
				int totalCount = await _cosmosDbService.ContainerManager.ConversationGroupData.GetCountAsync();
				IList<ConversationGroup> conversationGroups = await _cosmosDbService.ContainerManager.ConversationGroupData.GetListAsync(startItem, totalItems);
				SetFilterAndPagingViewData(1, null, totalCount, totalItems);
				if (conversationGroups == null)
				{
					return View();
				}
				else
				{
					ViewBag.Conversations = await ConversationList();
					return View(conversationGroups.OrderBy(x => x.Name));
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing conversation group list.", exception = ex.Message });
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
				
				ConversationGroup conversationGroup =  await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(id);
				if (conversationGroup == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{				
					ViewBag.Conversations = await ConversationList();
					ViewBag.CharacterConfigurations = await CharacterConfigurationsList();

					await SetViewBagData();
					return View(conversationGroup);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing conversation group details.", exception = ex.Message });
			}
		}
		
		public async Task<ActionResult> StopSkill(ConversationGroup model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
				else
				{				
					StopSkillParameters skillParameters = new StopSkillParameters();										
					CharacterConfiguration characterConfiguration = await _cosmosDbService.ContainerManager.CharacterConfigurationData.GetAsync(model.CharacterConfiguration);					

					//The default conversation skill is 8be20a90-1150-44ac-a756-ebe4de30689e, but it is possible to override it in case someone makes a different one that uses the same inputs
					skillParameters.Skill = characterConfiguration?.Skill ?? "8be20a90-1150-44ac-a756-ebe4de30689e";			
					string stopSkillConfiguration = Newtonsoft.Json.JsonConvert.SerializeObject(skillParameters);	
					WebMessenger webMessenger = new WebMessenger();
					WebMessengerData response = await webMessenger.PostRequest($"http://{model.RobotIp}/api/skills/cancel", stopSkillConfiguration, "application/json");

					//back to details
					return RedirectToAction("Details", new {id = model.Id});
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing conversation group.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> StartSkill(ConversationGroup model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
				else
				{				
					SkillParameters skillParameters = await GenerateSkillConfiguration(model.Id);					
					string skillConfiguration = Newtonsoft.Json.JsonConvert.SerializeObject(skillParameters);
				
					//Call and start the skill with the generated config
					WebMessenger webMessenger = new WebMessenger();
					WebMessengerData response = await webMessenger.PostRequest($"http://{model.RobotIp}/api/skills/start", skillConfiguration, "application/json");

					//back to details
					return RedirectToAction("Details", new {id = model.Id});
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing conversation group.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> Generate(string id)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
				else
				{				
					SkillParameters skillParameters = await GenerateSkillConfiguration(id);
					
					if(skillParameters == null)
					{
						return RedirectToAction("Error", "Home", new { message = "Could not find conversation group." });
					}

					string conversationGroupConfig = Newtonsoft.Json.JsonConvert.SerializeObject(skillParameters);
					if(string.IsNullOrWhiteSpace(conversationGroupConfig))
					{
						return RedirectToAction("Error", "Home", new { message = "Failed to generate configuration." });
					}

					//Download the config
					MemoryStream stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(conversationGroupConfig));
					stream.Position = 0;
					return File(stream, "application/octet-stream", $"{skillParameters.ConversationGroup.Name}_Configuration.json");									
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing conversation group.", exception = ex.Message });
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

				ConversationGroup conversationGroup = new ConversationGroup {};
				await SetViewBagData();
				ViewBag.Conversations = await ConversationList();
				ViewBag.CharacterConfigurations = await CharacterConfigurationsList();
				return View(conversationGroup);				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating conversation group.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(ConversationGroup model)
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

					model.ValidConfiguration = true;
					if(!string.IsNullOrWhiteSpace(model.StartupConversation))
					{
						Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.StartupConversation);
						if(conversation == null || string.IsNullOrWhiteSpace(conversation.StartupInteraction))
						{
							model.ValidConfiguration = false;
						}
					}
					else
					{
						model.ValidConfiguration = false;
					}
					
					model.Conversations.Remove(model.StartupConversation);
					model.Conversations.Add(model.StartupConversation);		

					await _cosmosDbService.ContainerManager.ConversationGroupData.AddAsync(model);
					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Create), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating conversation group.", exception = ex.Message });
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
				
				ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(id);
				if (conversationGroup == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.Conversations = await ConversationList();					
					ViewBag.CharacterConfigurations = await CharacterConfigurationsList();
					await SetViewBagData();
					return View(conversationGroup);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing conversation group.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(ConversationGroup conversationGroup)
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
					ConversationGroup loadedConversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(conversationGroup.Id);

					loadedConversationGroup.ValidConfiguration = true;
					
					if(!string.IsNullOrWhiteSpace(conversationGroup.CharacterConfiguration))
					{
						CharacterConfiguration characterConfiguration =  await _cosmosDbService.ContainerManager.CharacterConfigurationData.GetAsync(conversationGroup.CharacterConfiguration);
						if(characterConfiguration == null)
						{
							loadedConversationGroup.ValidConfiguration = false;
						}
					}
					else
					{
						loadedConversationGroup.ValidConfiguration = false;
					}


					if(!string.IsNullOrWhiteSpace(conversationGroup.StartupConversation))
					{
						Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(conversationGroup.StartupConversation);
						if(conversation == null || string.IsNullOrWhiteSpace(conversation.StartupInteraction))
						{
							loadedConversationGroup.ValidConfiguration = false;
						}
					}
					else
					{
						loadedConversationGroup.ValidConfiguration = false;
					}

					loadedConversationGroup.Name = conversationGroup.Name;
					loadedConversationGroup.RobotName = conversationGroup.RobotName;
					loadedConversationGroup.Description = conversationGroup.Description;
					loadedConversationGroup.StartupConversation = conversationGroup.StartupConversation;
					loadedConversationGroup.CharacterConfiguration = conversationGroup.CharacterConfiguration;
					loadedConversationGroup.Conversations.Remove(conversationGroup.StartupConversation);
					loadedConversationGroup.ManagementAccess = conversationGroup.ManagementAccess;
					loadedConversationGroup.RequestAccess = conversationGroup.RequestAccess;
					loadedConversationGroup.Conversations.Add(conversationGroup.StartupConversation);	
					loadedConversationGroup.KeyPhraseRecognizedAudio = conversationGroup.KeyPhraseRecognizedAudio;
					loadedConversationGroup.Updated = DateTimeOffset.UtcNow;
					
					await _cosmosDbService.ContainerManager.ConversationGroupData.UpdateAsync(loadedConversationGroup);

					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Edit), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing conversation group.", exception = ex.Message });
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
			
				ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(id);
				if (conversationGroup == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.Conversations = await ConversationList();
					ViewBag.CharacterConfigurations = await CharacterConfigurationsList();
					await SetViewBagData();
					return View(conversationGroup);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting conversation group.", exception = ex.Message });
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

				 await _cosmosDbService.ContainerManager.ConversationGroupData.DeleteAsync(id);
				return RedirectToAction(nameof(Index));				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting conversation group.", exception = ex.Message });
			}
		}

		
		public async Task<ActionResult> Manage(string id, int startItem = 1, int totalItems = 1000)
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
				IList<Conversation> conversations = await _cosmosDbService.ContainerManager.ConversationData.GetListAsync(startItem, 1000);//TODO
				IList<Conversation> filteredConversations = new List<Conversation>();
				ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(id);				

				foreach(var conversation in conversationGroup.Conversations)
				{
					Conversation selectedConversation  = conversations.FirstOrDefault(x => x.Id == conversation);
					if(selectedConversation != null && !filteredConversations.Contains(selectedConversation))
					{
						filteredConversations.Add(selectedConversation);
					}
				}

				SetFilterAndPagingViewData(1, null, totalCount, totalItems);
				if (conversations == null)
				{
					return View();
				}
				else
				{
					ViewBag.Conversations = await ConversationList();
					ViewBag.CharacterConfigurations = await CharacterConfigurationsList();
					ViewBag.EntryPoints = await ConversationGroupEntries(id);

					var conversationMappings = conversationGroup.ConversationMappings;
					ViewBag.ConversationMappings = conversationMappings;
					
					var allDeparturePoints = await ConversationGroupDepartures(id);
					var availableDeparturePoints = new Dictionary<string, TriggerActionOption>();
					foreach(TriggerActionOption triggerActionOption in allDeparturePoints.Values)
					{
						if(!conversationMappings.ContainsKey(triggerActionOption.Id))
						{
							availableDeparturePoints.Add(triggerActionOption.Id, triggerActionOption);
						}
					}

					 ViewBag.DeparturePoints = availableDeparturePoints;

					ConversationGroupConversationViewModel conversationGroupConversationViewModel = new ConversationGroupConversationViewModel();
					conversationGroupConversationViewModel.ConversationGroupId = id;
					conversationGroupConversationViewModel.ConversationGroupName = conversationGroup.Name;
					conversationGroupConversationViewModel.Conversations = filteredConversations.OrderBy(x => x.Name).ToList();

					return View(conversationGroupConversationViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing the conversation group list.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> AddConversation(ConversationGroupConversationViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.Handler);				
				ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(model.ConversationGroupId);				
				
				if(conversationGroup != null && conversation != null)
				{
					conversationGroup.Conversations.Remove(model.Handler);
					conversationGroup.Conversations.Add(model.Handler);
					await _cosmosDbService.ContainerManager.ConversationGroupData.UpdateAsync(conversationGroup);

					return RedirectToAction("Manage", new {id = conversationGroup.Id});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the conversation group list."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the conversation group list.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> RemoveConversation(ConversationGroupConversationViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.Handler);				
				ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(model.ConversationGroupId);				
				
				if(conversationGroup != null && conversation != null)
				{
					conversationGroup.Conversations.Remove(model.Handler);
					if(conversationGroup.StartupConversation == model.Handler)
					{
						conversationGroup.StartupConversation = null;
					}

					await _cosmosDbService.ContainerManager.ConversationGroupData.UpdateAsync(conversationGroup);

					return RedirectToAction("Manage", new {id = conversationGroup.Id});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the conversation group list."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the conversation group list.", exception = ex.Message });
			}
		}
		
		public async Task<ActionResult> ManageAccess(string id, int startItem = 1, int totalItems = 1000)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				await SetViewBagData();

				ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(id);				
				IList<SkillAuthorization> skillAuthorizations = conversationGroup.SkillAuthorizations;
				int totalCount =  skillAuthorizations.Count();

				SetFilterAndPagingViewData(1, null, totalCount, totalItems);

				if (skillAuthorizations == null)
				{
					return View();
				}
				else
				{
					AuthViewModel conversationGroupConversationViewModel = new AuthViewModel();
					conversationGroupConversationViewModel.ParentId = id;
					conversationGroupConversationViewModel.ParentName = conversationGroup.Name;
					foreach(SkillAuthorization skillAuthorization in skillAuthorizations)
					{
						conversationGroupConversationViewModel.Data.TryAdd(skillAuthorization.Id, new SkillAuthorizationViewModel(skillAuthorization));
					}

					//conversationGroupConversationViewModel.SkillAuthorizations

					return View(conversationGroupConversationViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing the conversation group list.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> AddData(AuthViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				await SetViewBagData();

				ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(model.ParentId);				
			
				if(!conversationGroup.SkillAuthorizations.Any(x => x.AccountId == model.AccountId))
				{	
					SkillAuthorization newSkillAuthorization = new SkillAuthorization();
					newSkillAuthorization.Name = model.Name;
					newSkillAuthorization.Id = Guid.NewGuid().ToString();
					newSkillAuthorization.Description = model.Description;
					newSkillAuthorization.AccountId = model.AccountId;
					newSkillAuthorization.Key = model.Key;

					conversationGroup.SkillAuthorizations.Add(newSkillAuthorization);

					await _cosmosDbService.ContainerManager.ConversationGroupData.UpdateAsync(conversationGroup);
					
					return RedirectToAction("ManageAccess", new {id = model.ParentId});
				}
			
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the conversation group access, does this account already exist in the list?"});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the conversation group access.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> RemoveData(AuthViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				await SetViewBagData();

				ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(model.ParentId);				
				
				if(conversationGroup.SkillAuthorizations.Any(x => x.Id == model.Key))
				{	
					conversationGroup.SkillAuthorizations = conversationGroup.SkillAuthorizations.Where(x => x.Id != model.Key).ToList();					
					await _cosmosDbService.ContainerManager.ConversationGroupData.UpdateAsync(conversationGroup);
					
					return RedirectToAction("ManageAccess", new {id = model.ParentId});
				}
			
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the conversation group access, does this account already exist in the list?"});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the conversation group access.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> MapConversationInteractions(ConversationGroupConversationViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
			
				ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(model.ConversationGroupId);								
				if(conversationGroup != null)  
				{
					//Map the exit to the entry
					//TODO Clean this up with pre-saved mapping... this is all discombobulated...
					ConversationMappingDetail conversationMappingDetail = new ConversationMappingDetail();
					conversationMappingDetail.DeparturePoint = model.DeparturePoint;
					conversationMappingDetail.EntryPoint = model.EntryPoint;
					bool entryFound = false;
					bool exitFound = false;
					foreach(var conversationId in conversationGroup.Conversations)
					{
						Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(conversationId);	
						
						if((conversation?.ConversationDeparturePoints != null && conversation.ConversationDeparturePoints.Count > 0) ||
						(conversation?.ConversationEntryPoints != null && conversation.ConversationEntryPoints.Count > 0))
						{
							if(!entryFound && conversation.Interactions.Contains(model.EntryPoint))
							{
								
								Interaction interaction = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(model.EntryPoint);	
								entryFound = true;
								conversationMappingDetail.EntryPointName = $"{conversation.Name}:{interaction.Name}";
							}

							if(!exitFound)
							{
								if(conversation.ConversationDeparturePoints.Contains(model.DeparturePoint))
								{
									foreach(string interactionId in conversation.Interactions)
									{
										Interaction interaction2 = await _cosmosDbService.ContainerManager.InteractionData.GetAsync(interactionId);	
										foreach(KeyValuePair<string, IList<TriggerActionOption>> map in interaction2.TriggerMap)
										{
											TriggerActionOption triggerActionOption = map.Value.FirstOrDefault(x => x.Id == model.DeparturePoint);
											if(triggerActionOption != null)
											{	
												exitFound = true;
												conversationMappingDetail.DeparturePointName = triggerActionOption.DisplayName;
											}
										}
										
									}
								
								}
							}
						}

						if(entryFound && exitFound)
						{
							break;
						}
					}
					conversationGroup.ConversationMappings.Add(model.DeparturePoint, conversationMappingDetail);
					await _cosmosDbService.ContainerManager.ConversationGroupData.UpdateAsync(conversationGroup);

					return RedirectToAction("Manage", new {id = conversationGroup.Id});
				}

				return RedirectToAction("Error", "Home", new { message = "Exception adding a conversation mapping."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception adding a conversation mapping.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> DeleteConversationInteractionMap(string id, string conversationGroupId)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
			
				ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(conversationGroupId);
				if(conversationGroup != null)
				{
					//Map the exit to the entry
					conversationGroup.ConversationMappings.Remove(id);
					await _cosmosDbService.ContainerManager.ConversationGroupData.UpdateAsync(conversationGroup);

					return RedirectToAction("Manage", new {id = conversationGroup.Id});
				}

				return RedirectToAction("Error", "Home", new { message = "Exception removing a conversation mapping."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing a conversation mapping.", exception = ex.Message });
			}
		}
	}
}