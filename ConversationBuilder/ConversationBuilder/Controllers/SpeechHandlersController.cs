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
	public class SpeechHandlersController : AdminToolController
	{
		public SpeechHandlersController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
			: base(cosmosDbService, userManager) { }

		public async Task<ActionResult> Index(int startItem = 1, int totalItems = 1000)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				await SetViewBagData();
				int totalCount = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetCountAsync(_userConfiguration.ShowAllConversations ? "" : userInfo.AccessId);
				IList<SpeechHandler> speechHandlers = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetListAsync(startItem, totalItems, _userConfiguration.ShowAllConversations ? "" : userInfo.AccessId);
				SetFilterAndPagingViewData(1, null, totalCount, totalItems);
				if (speechHandlers == null)
				{
					return View();
				}
				else
				{
					return View(speechHandlers.OrderBy(x => x.Name));
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing speech handler list.", exception = ex.Message });
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

				
				SpeechHandler speechHandler =  await _cosmosDbService.ContainerManager.SpeechHandlerData.GetAsync(id);
				SpeechHandlerViewModel speechHandlerViewModel = new SpeechHandlerViewModel();
				speechHandlerViewModel.Created = speechHandler.Created;
				speechHandlerViewModel.Description = speechHandler.Description;
				speechHandlerViewModel.Id = speechHandler.Id;
				speechHandlerViewModel.Name = speechHandler.Name;
				speechHandlerViewModel.ExactMatchesOnly = speechHandler.ExactMatchesOnly;
				speechHandlerViewModel.WordMatchRule = speechHandler.WordMatchRule;
				speechHandlerViewModel.ItemType = speechHandler.ItemType;
				speechHandlerViewModel.Updated = speechHandler.Updated;
				speechHandlerViewModel.Utterances = speechHandler.Utterances;
				//TODO More cleanup and better UI
				speechHandlerViewModel.UtteranceString = String.Join(",", speechHandler.Utterances);
				speechHandlerViewModel.Created = speechHandler.Created;
				if (speechHandler == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{				
					await SetViewBagData();
					return View(speechHandlerViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing speech handler details.", exception = ex.Message });
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

				SpeechHandlerViewModel speechHandlerViewModel = new SpeechHandlerViewModel {};						
				ViewBag.WordMatchRules = new WordMatchRules().AllItems.OrderBy(x => x.Value);	
				await SetViewBagData();
				return View(speechHandlerViewModel);				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating speech handler.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(SpeechHandlerViewModel model)
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
					SpeechHandler speechHandler = new SpeechHandler();

					speechHandler.Id = Guid.NewGuid().ToString();
					DateTimeOffset dt = DateTimeOffset.UtcNow;
					speechHandler.CreatedBy = userInfo.AccessId;
					speechHandler.Created = dt;
					speechHandler.Updated = dt;
					//TODO More cleanup and better UI
					if(string.IsNullOrWhiteSpace(model.UtteranceString))
					{
						return RedirectToAction(nameof(Create), ModelState);
					}

					IList<string> utterances = model.UtteranceString.Split(',').ToList();					
					foreach(string utterance in utterances)
					{
						string newUtterance = utterance;
						newUtterance = newUtterance.Trim().Replace(System.Environment.NewLine, "").Replace("\t", "").Replace("\r\n", "");
						speechHandler.Utterances.Remove(newUtterance);
						speechHandler.Utterances.Add(newUtterance);
					}
					speechHandler.Name = model.Name;
					speechHandler.ExactMatchesOnly = model.ExactMatchesOnly;
					speechHandler.WordMatchRule = model.WordMatchRule;
					speechHandler.Description = model.Description;

					await _cosmosDbService.ContainerManager.SpeechHandlerData.AddAsync(speechHandler);
					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Create), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating speech handler.", exception = ex.Message });
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
				
				SpeechHandler speechHandler = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetAsync(id);
			
				if (speechHandler == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{	SpeechHandlerViewModel speechHandlerViewModel = new SpeechHandlerViewModel();
					speechHandlerViewModel.Created = speechHandler.Created;
					speechHandlerViewModel.Description = speechHandler.Description;
					speechHandlerViewModel.Id = speechHandler.Id;
					speechHandlerViewModel.Name = speechHandler.Name;
					speechHandlerViewModel.ManagementAccess = speechHandler.ManagementAccess;
					speechHandlerViewModel.ExactMatchesOnly = speechHandler.ExactMatchesOnly;
					speechHandlerViewModel.WordMatchRule = speechHandler.WordMatchRule;
					speechHandlerViewModel.ItemType = speechHandler.ItemType;
					speechHandlerViewModel.Updated = speechHandler.Updated;
					speechHandlerViewModel.Utterances = speechHandler.Utterances;
					//TODO More cleanup and better UI
					speechHandlerViewModel.UtteranceString = String.Join(",", speechHandler.Utterances);
					speechHandlerViewModel.Created = speechHandler.Created;					
					ViewBag.WordMatchRules = new WordMatchRules().AllItems.OrderBy(x => x.Value);
					await SetViewBagData();
					return View(speechHandlerViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing speech handler.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(SpeechHandlerViewModel speechHandler)
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
					SpeechHandler loadedSpeechHandler = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetAsync(speechHandler.Id);
					loadedSpeechHandler.Name = speechHandler.Name;
					loadedSpeechHandler.Description = speechHandler.Description;
					loadedSpeechHandler.ExactMatchesOnly = speechHandler.ExactMatchesOnly;
					loadedSpeechHandler.WordMatchRule = speechHandler.WordMatchRule;
					//TODO More cleanup and better UI
					loadedSpeechHandler.Utterances = speechHandler.UtteranceString.Split(',').ToList();
					loadedSpeechHandler.Updated = DateTimeOffset.UtcNow;
					
					await _cosmosDbService.ContainerManager.SpeechHandlerData.UpdateAsync(loadedSpeechHandler);

					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Edit), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing speech handler.", exception = ex.Message });
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
			
				SpeechHandler speechHandler = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetAsync(id);
				if (speechHandler == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.CanBeDeleted = await CanBeDeleted(id, DeleteItem.SpeechHandler);
					await SetViewBagData();
					return View(speechHandler);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting speech handler.", exception = ex.Message });
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

				 await _cosmosDbService.ContainerManager.SpeechHandlerData.DeleteAsync(id);
				return RedirectToAction(nameof(Index));				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting speech handler.", exception = ex.Message });
			}
		}
	}
}