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
	public class SkillMessagesController : AdminToolController
	{
		public SkillMessagesController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
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
				int totalCount = await _cosmosDbService.ContainerManager.SkillMessageData.GetCountAsync(_userConfiguration.ShowAllConversations ? "" : userInfo.AccessId);
				IList<SkillMessage> skillMessages = await _cosmosDbService.ContainerManager.SkillMessageData.GetListAsync(startItem, totalItems, _userConfiguration.ShowAllConversations ? "" : userInfo.AccessId);
				SetFilterAndPagingViewData(1, null, totalCount, totalItems);
				if (skillMessages == null)
				{
					return View();
				}
				else
				{
					ViewBag.Conversations = await ConversationList();
					return View(skillMessages.OrderBy(x => x.Name));
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
				
				SkillMessage skillMessage =  await _cosmosDbService.ContainerManager.SkillMessageData.GetAsync(id);
				if (skillMessage == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{				
					ViewBag.Conversations = await ConversationList();
					ViewBag.CharacterConfigurations = await CharacterConfigurationsList();
					await SetViewBagData();
					return View(skillMessage);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing conversation group details.", exception = ex.Message });
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

				SkillMessage skillMessage = new SkillMessage {};
				await SetViewBagData();
				ViewBag.Conversations = await ConversationList();
				ViewBag.CharacterConfigurations = await CharacterConfigurationsList();
				return View(skillMessage);				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating conversation group.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(SkillMessage model)
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
					await _cosmosDbService.ContainerManager.SkillMessageData.AddAsync(model);
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
				
				SkillMessage skillMessage = await _cosmosDbService.ContainerManager.SkillMessageData.GetAsync(id);
				if (skillMessage == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.Conversations = await ConversationList();					
					ViewBag.CharacterConfigurations = await CharacterConfigurationsList();
					await SetViewBagData();
					return View(skillMessage);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing conversation group.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(SkillMessage skillMessage)
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
					SkillMessage loadedSkillMessage = await _cosmosDbService.ContainerManager.SkillMessageData.GetAsync(skillMessage.Id);

					loadedSkillMessage.Name = skillMessage.Name;			
					loadedSkillMessage.EventName = skillMessage.EventName;			
					loadedSkillMessage.IncludeCharacterState = skillMessage.IncludeCharacterState;
					loadedSkillMessage.StreamTriggerCheck = skillMessage.StreamTriggerCheck;
					loadedSkillMessage.IncludeLatestTriggerMatch = skillMessage.IncludeLatestTriggerMatch;
					loadedSkillMessage.Payload = skillMessage.Payload;	
					loadedSkillMessage.Skill = skillMessage.Skill;	
					loadedSkillMessage.StartIfStopped = skillMessage.StartIfStopped;	
					loadedSkillMessage.StopIfRunning = skillMessage.StopIfRunning;	
					loadedSkillMessage.StopOnNextAnimation = skillMessage.StopOnNextAnimation;						
					loadedSkillMessage.Description = skillMessage.Description;		
					loadedSkillMessage.ManagementAccess = skillMessage.ManagementAccess;
					loadedSkillMessage.RequestAccess = skillMessage.RequestAccess;					
					loadedSkillMessage.Updated = DateTimeOffset.UtcNow;
					
					await _cosmosDbService.ContainerManager.SkillMessageData.UpdateAsync(loadedSkillMessage);

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
			
				SkillMessage skillMessage = await _cosmosDbService.ContainerManager.SkillMessageData.GetAsync(id);
				if (skillMessage == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					
					ViewBag.CanBeDeleted = await CanBeDeleted(id, DeleteItem.SkillMessage);
					ViewBag.Conversations = await ConversationList();
					ViewBag.CharacterConfigurations = await CharacterConfigurationsList();
					await SetViewBagData();
					return View(skillMessage);
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

				 await _cosmosDbService.ContainerManager.SkillMessageData.DeleteAsync(id);
				return RedirectToAction(nameof(Index));				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting conversation group.", exception = ex.Message });
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

				SkillMessage skillMessage = await _cosmosDbService.ContainerManager.SkillMessageData.GetAsync(id);				
				IList<SkillAuthorization> skillAuthorizations = skillMessage.SkillAuthorizations;
				int totalCount =  skillAuthorizations.Count();

				SetFilterAndPagingViewData(1, null, totalCount, totalItems);

				if (skillAuthorizations == null)
				{
					return View();
				}
				else
				{
					AuthViewModel authViewModel = new AuthViewModel();
					authViewModel.ParentId = id;
					authViewModel.ParentName = skillMessage.Name;
					foreach(SkillAuthorization skillAuthorization in skillAuthorizations)
					{
						authViewModel.Data.TryAdd(skillAuthorization.Id, new SkillAuthorizationViewModel(skillAuthorization));
					}

					return View(authViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing the skill messages auth list.", exception = ex.Message });
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

				SkillMessage skillMessage = await _cosmosDbService.ContainerManager.SkillMessageData.GetAsync(model.ParentId);					
			
				if(!skillMessage.SkillAuthorizations.Any(x => x.AccountId == model.AccountId))
				{	
					SkillAuthorization newSkillAuthorization = new SkillAuthorization();
					newSkillAuthorization.Name = model.Name;
					newSkillAuthorization.Id = Guid.NewGuid().ToString();
					newSkillAuthorization.Description = model.Description;
					newSkillAuthorization.AccountId = model.AccountId;
					newSkillAuthorization.Key = model.Key;

					skillMessage.SkillAuthorizations.Add(newSkillAuthorization);

					await _cosmosDbService.ContainerManager.SkillMessageData.UpdateAsync(skillMessage);
					
					return RedirectToAction("ManageAccess", new {id = model.ParentId});
				}
			
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the skill messages auth access, does this account already exist in the list?"});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the skill messages auth access.", exception = ex.Message });
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

				SkillMessage skillMessage = await _cosmosDbService.ContainerManager.SkillMessageData.GetAsync(model.ParentId);					
				
				if(skillMessage.SkillAuthorizations.Any(x => x.Id == model.Key))
				{	
					skillMessage.SkillAuthorizations = skillMessage.SkillAuthorizations.Where(x => x.Id != model.Key).ToList();					
					await _cosmosDbService.ContainerManager.SkillMessageData.UpdateAsync(skillMessage);
					
					return RedirectToAction("ManageAccess", new {id = model.ParentId});
				}
			
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the skill messages auth, does this account already exist in the list?"});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the skill messages auth.", exception = ex.Message });
			}
		}
	}
}