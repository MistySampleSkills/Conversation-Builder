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
	public class TriggersController : AdminToolController
	{
		public TriggersController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
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
				int totalCount = await _cosmosDbService.ContainerManager.TriggerDetailData.GetCountAsync();
				IList<TriggerDetail> triggers = await _cosmosDbService.ContainerManager.TriggerDetailData.GetListAsync(startItem, totalItems);
				foreach(TriggerDetail trigger in triggers.Where(x => x.Trigger == "SpeechHeard" && !string.IsNullOrWhiteSpace(x.TriggerFilter)))
				{
					//TODO performance
					//get the speech name for display
					SpeechHandler speechHandler = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetAsync(trigger.TriggerFilter);
					if(speechHandler != null) //legacy data will not match
					{
						trigger.TriggerFilter = speechHandler.Name;
					}
				}

				SetFilterAndPagingViewData(1, null, totalCount, totalItems);
				if (triggers == null)
				{
					return View();
				}
				else
				{
					ViewBag.Emotions = (new DefaultEmotions()).AllItems;
					ViewBag.Interactions = await InteractionList();
					return View(triggers.OrderBy(x => x.Name));
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing trigger list.", exception = ex.Message });
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

				
				TriggerDetail trigger =  await _cosmosDbService.ContainerManager.TriggerDetailData.GetAsync(id);
				if (trigger == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{			
					ViewBag.Emotions = (new DefaultEmotions()).AllItems;
					ViewBag.Interactions = await InteractionList();
					
					IDictionary<string, string> triggerFilters = (new TriggerFilters()).AllItems.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
					
					//Deal with legacy mappings of speech handlers
					IList<SpeechHandler> speechHandlers = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetListAsync(1, 1000);//TODO
					foreach(var speechHandler in speechHandlers)
					{
						triggerFilters.Add(new KeyValuePair<string, string>(speechHandler.Id, "SpeechHeard: " + speechHandler.Name));
					}

					ViewBag.TriggerFilterName = await GetTriggerFilterDisplayName(trigger.Trigger, trigger.TriggerFilter, triggerFilters);
					ViewBag.StartingTriggerFilterName = await GetTriggerFilterDisplayName(trigger.StartingTrigger, trigger.StartingTriggerFilter, triggerFilters);
					ViewBag.StoppingTriggerFilterName = await GetTriggerFilterDisplayName(trigger.StoppingTrigger, trigger.StoppingTriggerFilter, triggerFilters);
					
					/*if(trigger.Trigger == "SpeechHeard" && !string.IsNullOrWhiteSpace(trigger.TriggerFilter))
					{
						if(Guid.TryParse(trigger.TriggerFilter, out Guid result))
						{
							SpeechHandler speechHandler = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetAsync(trigger.TriggerFilter);
							if(speechHandler != null)
							{
								ViewBag.TriggerFilterName = speechHandler.Name;
							}
						}
						else
						{
							ViewBag.TriggerFilterName = trigger.TriggerFilter;
						}
					}
					else
					{
						ViewBag.TriggerFilterName = trigger.TriggerFilter;
					}*/

					await SetViewBagData();
					return View(trigger);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing trigger details.", exception = ex.Message });
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

				TriggerDetailViewModel triggerViewModel = new TriggerDetailViewModel {};
				triggerViewModel.Id = Guid.NewGuid().ToString();
				ViewBag.Interactions = await InteractionList();
				ViewBag.Triggers = (new Triggers()).AllItems.OrderBy(x => x.Value);
				var triggerFilters = (new TriggerFilters()).AllItems.OrderBy(x => x.Value).ToList();
				
				IList<SpeechHandler> speechHandlers = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetListAsync(1, 1000);//TODO
				foreach(var speechHandler in speechHandlers)
				{
					triggerFilters.Add(new KeyValuePair<string, string>(speechHandler.Id, "SpeechHeard: " + speechHandler.Name));
				}

				ViewBag.TriggerFilters = triggerFilters;
				ViewBag.Emotions = (new DefaultEmotions()).AllItems.OrderByDescending(x => x.Value);

				await SetViewBagData();
				return View(triggerViewModel);				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating trigger.", exception = ex.Message });
			}
		}
		
		private TriggerDetail ManageTriggerView(TriggerDetailViewModel model)
		{
			TriggerDetail intentDetail = new TriggerDetail();
			intentDetail.Name = model.Name;
			intentDetail.Trigger = model.Trigger;
			intentDetail.ItemType = model.ItemType;

			if(!string.IsNullOrWhiteSpace(model.UserDefinedTriggerFilter))
			{
				intentDetail.TriggerFilter = model.UserDefinedTriggerFilter;
			}
			else
			{
				if(string.IsNullOrWhiteSpace(model.TriggerFilter) || model.TriggerFilter.ToLower() == "triggerfilter" || model.TriggerFilter.ToLower() == "none")
				{
					intentDetail.TriggerFilter = "";	
				}
				else
				{
					intentDetail.TriggerFilter = model.TriggerFilter;	
				}
			}

			if(!string.IsNullOrWhiteSpace(model.UserDefinedStartingTriggerFilter))
			{
				intentDetail.StartingTriggerFilter = model.UserDefinedStartingTriggerFilter;
			}
			else
			{
				if(string.IsNullOrWhiteSpace(model.StartingTriggerFilter) || model.StartingTriggerFilter.ToLower() == "none")
				{
					intentDetail.StartingTriggerFilter = "";
				}
				else
				{
					intentDetail.StartingTriggerFilter = model.StartingTriggerFilter;	
				}
			}

			if(!string.IsNullOrWhiteSpace(model.UserDefinedStoppingTriggerFilter))
			{
				intentDetail.StoppingTriggerFilter = model.UserDefinedStoppingTriggerFilter;
			}
			else
			{
				if(string.IsNullOrWhiteSpace(model.StoppingTriggerFilter) || model.StoppingTriggerFilter.ToLower() == "none")
				{
					intentDetail.StoppingTriggerFilter = "";
				}
				else
				{
					intentDetail.StoppingTriggerFilter = model.StoppingTriggerFilter;	
				}
			}

			intentDetail.StartingTriggerDelay = model.StartingTriggerDelay;
			intentDetail.StartingTrigger = model.StartingTrigger;
			intentDetail.StoppingTriggerDelay = model.StoppingTriggerDelay;
			intentDetail.StoppingTrigger = model.StoppingTrigger;
			
			intentDetail.Id = model.Id;
			return intentDetail;
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(TriggerDetailViewModel model)
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

					TriggerDetail intentDetail = ManageTriggerView(model);
					DateTimeOffset dt = DateTimeOffset.UtcNow;
					intentDetail.CreatedBy = userInfo.AccessId;
					intentDetail.Created = dt;
					intentDetail.Updated = dt;
					
					await _cosmosDbService.ContainerManager.TriggerDetailData.AddAsync(intentDetail);
					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Create), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating trigger.", exception = ex.Message });
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
				
				TriggerDetail triggerDetail = await _cosmosDbService.ContainerManager.TriggerDetailData.GetAsync(id);
				if (triggerDetail == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					TriggerDetailViewModel triggerDetailViewModel = new TriggerDetailViewModel {};
					triggerDetailViewModel.Id = triggerDetail.Id;
					triggerDetailViewModel.Name = triggerDetail.Name;
					triggerDetailViewModel.Trigger = triggerDetail.Trigger;
					triggerDetailViewModel.ManagementAccess = triggerDetail.ManagementAccess;
					triggerDetailViewModel.ItemType = triggerDetail.ItemType;

					IDictionary<string, string> triggerFilters = (new TriggerFilters()).AllItems.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
					
					//Deal with legacy mappings of speech handlers
					IList<SpeechHandler> speechHandlers = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetListAsync(1, 1000);//TODO
					foreach(var speechHandler in speechHandlers)
					{
						triggerFilters.Add(new KeyValuePair<string, string>(speechHandler.Id, "SpeechHeard: " + speechHandler.Name));
					}

					string displayName = await GetTriggerFilterDisplayName(triggerDetail.Trigger, triggerDetail.TriggerFilter, triggerFilters);
					
					if(triggerFilters.Any(x => x.Value == displayName))
					{
						triggerDetailViewModel.TriggerFilter = triggerDetail.TriggerFilter;
						triggerDetailViewModel.UserDefinedTriggerFilter = "";
					}
					else
					{
						triggerDetailViewModel.UserDefinedTriggerFilter = triggerDetail.TriggerFilter;
					}

					string startingDisplayName = await GetTriggerFilterDisplayName(triggerDetail.StartingTrigger, triggerDetail.StartingTriggerFilter, triggerFilters);
					if(triggerFilters.Any(x => x.Value == startingDisplayName))
					{
						triggerDetailViewModel.StartingTriggerFilter = triggerDetail.StartingTriggerFilter;
						triggerDetailViewModel.UserDefinedStartingTriggerFilter = "";
					}
					else
					{
						triggerDetailViewModel.UserDefinedStartingTriggerFilter = triggerDetail.StartingTriggerFilter;
					}

					string stoppingDisplayName = await GetTriggerFilterDisplayName(triggerDetail.StoppingTrigger, triggerDetail.StoppingTriggerFilter, triggerFilters);
					if(triggerFilters.Any(x => x.Value == stoppingDisplayName))
					{
						triggerDetailViewModel.StoppingTriggerFilter = triggerDetail.StoppingTriggerFilter;
						triggerDetailViewModel.UserDefinedStoppingTriggerFilter = "";
					}
					else
					{
						triggerDetailViewModel.UserDefinedStoppingTriggerFilter = triggerDetail.StoppingTriggerFilter;
					}

					triggerDetailViewModel.StartingTriggerDelay = triggerDetail.StartingTriggerDelay;
					triggerDetailViewModel.StartingTrigger = triggerDetail.StartingTrigger;
					triggerDetailViewModel.StoppingTriggerDelay = triggerDetail.StoppingTriggerDelay;
					triggerDetailViewModel.StoppingTrigger = triggerDetail.StoppingTrigger;
					
					triggerDetailViewModel.Created = triggerDetail.Created;
				
					ViewBag.Interactions = await InteractionList();					
					ViewBag.Triggers = (new Triggers()).AllItems.OrderBy(x => x.Value);
			
					ViewBag.TriggerFilters = triggerFilters;
					ViewBag.Emotions = (new DefaultEmotions()).AllItems.OrderByDescending(x => x.Value);

					await SetViewBagData();
					return View(triggerDetailViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing trigger.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(TriggerDetailViewModel model)
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
					TriggerDetail intentDetail = ManageTriggerView(model);	
					intentDetail.Created = model.Created;
					intentDetail.ManagementAccess = model.ManagementAccess;
					intentDetail.Name = model.Name;
					
					intentDetail.Updated = DateTimeOffset.UtcNow;
					await _cosmosDbService.ContainerManager.TriggerDetailData.UpdateAsync(intentDetail);

					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Edit), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing trigger.", exception = ex.Message });
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
			
				TriggerDetail trigger = await _cosmosDbService.ContainerManager.TriggerDetailData.GetAsync(id);
				if (trigger == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{															
					ViewBag.CanBeDeleted = await CanBeDeleted(id, DeleteItem.Trigger);
					ViewBag.Emotions = (new DefaultEmotions()).AllItems;
					ViewBag.Interactions = await InteractionList();
					await SetViewBagData();
					return View(trigger);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting trigger.", exception = ex.Message });
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

				 await _cosmosDbService.ContainerManager.TriggerDetailData.DeleteAsync(id);
				return RedirectToAction(nameof(Index));				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting trigger.", exception = ex.Message });
			}
		}
	}
}