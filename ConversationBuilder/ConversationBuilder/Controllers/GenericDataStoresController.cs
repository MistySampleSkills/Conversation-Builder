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
	public class GenericDataStoresController : AdminToolController
	{
		public GenericDataStoresController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
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
				int totalCount = await _cosmosDbService.ContainerManager.GenericDataStoreData.GetCountAsync();
				IList<GenericDataStore> genericDataStores = await _cosmosDbService.ContainerManager.GenericDataStoreData.GetListAsync(startItem, totalItems);
				SetFilterAndPagingViewData(1, null, totalCount, totalItems);

				if (genericDataStores == null)
				{
					return View();
				}
				else
				{
					return View(genericDataStores.OrderBy(x => x.Name));
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing genericDataStore list.", exception = ex.Message });
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

				
				GenericDataStore genericDataStore =  await _cosmosDbService.ContainerManager.GenericDataStoreData.GetAsync(id);
				if (genericDataStore == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{				
					await SetViewBagData();
					return View(genericDataStore);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing genericDataStore details.", exception = ex.Message });
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

				ViewBag.Emotions = (new DefaultEmotions()).AllItems;					
				ViewBag.WordMatchRules = new WordMatchRules().AllItems.OrderBy(x => x.Value);
				GenericDataStore genericDataStore = new GenericDataStore {};
				await SetViewBagData();
				return View(genericDataStore);				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating genericDataStore.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(GenericDataStore model)
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

					await _cosmosDbService.ContainerManager.GenericDataStoreData.AddAsync(model);
					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Create), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating genericDataStore.", exception = ex.Message });
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
				
				GenericDataStore genericDataStore = await _cosmosDbService.ContainerManager.GenericDataStoreData.GetAsync(id);
				if (genericDataStore == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.Emotions = (new DefaultEmotions()).AllItems;					
					ViewBag.WordMatchRules = new WordMatchRules().AllItems.OrderBy(x => x.Value);
					await SetViewBagData();
					return View(genericDataStore);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing genericDataStore.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(GenericDataStore genericDataStore)
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
					GenericDataStore loadedGenericDataStore = await _cosmosDbService.ContainerManager.GenericDataStoreData.GetAsync(genericDataStore.Id);
					loadedGenericDataStore.Name = genericDataStore.Name;
					loadedGenericDataStore.Description = genericDataStore.Description;
					loadedGenericDataStore.ManagementAccess = genericDataStore.ManagementAccess;
					loadedGenericDataStore.TreatKeyAsUtterance = genericDataStore.TreatKeyAsUtterance;
					loadedGenericDataStore.ExactMatchesOnly = genericDataStore.ExactMatchesOnly;
					loadedGenericDataStore.WordMatchRule = genericDataStore.WordMatchRule;
					loadedGenericDataStore.Updated = DateTimeOffset.UtcNow;
					await _cosmosDbService.ContainerManager.GenericDataStoreData.UpdateAsync(loadedGenericDataStore);

					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Edit), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing genericDataStore.", exception = ex.Message });
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
			
				GenericDataStore genericDataStore = await _cosmosDbService.ContainerManager.GenericDataStoreData.GetAsync(id);
				if (genericDataStore == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.CanBeDeleted = await CanBeDeleted(id, DeleteItem.GenericDataStore);
					await SetViewBagData();
					return View(genericDataStore);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting genericDataStore.", exception = ex.Message });
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

				 await _cosmosDbService.ContainerManager.GenericDataStoreData.DeleteAsync(id);
				return RedirectToAction(nameof(Index));				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting genericDataStore.", exception = ex.Message });
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
			
				GenericDataStore genericDataStore = await _cosmosDbService.ContainerManager.GenericDataStoreData.GetAsync(id);
				if(genericDataStore == null)
				{
					return View();
				}
				else
				{
					GenericDataViewModel genericDataViewModel = new GenericDataViewModel();
					genericDataViewModel.Data = genericDataStore.Data;
					genericDataViewModel.Description = genericDataStore.Description;
					genericDataViewModel.TreatKeyAsUtterance = genericDataStore.TreatKeyAsUtterance;
					genericDataViewModel.ExactMatchesOnly = genericDataStore.ExactMatchesOnly;
					genericDataViewModel.WordMatchRule = genericDataStore.WordMatchRule;
					genericDataViewModel.Name = genericDataStore.Name;
					genericDataViewModel.Id = genericDataStore.Id;
					return View(genericDataViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing the generic data list.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> AddData(GenericDataViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				GenericDataStore genericDataStore = await _cosmosDbService.ContainerManager.GenericDataStoreData.GetAsync(model.Id);				
				
				if(genericDataStore != null && !string.IsNullOrWhiteSpace(model.Key))
				{
					GenericData genericData = new GenericData();
					genericData.Id = Guid.NewGuid().ToString();
					genericData.Key = model.Key;
					genericData.Value = model.Value;
					genericData.Image = model.Image;
					genericData.ScreenText = model.ScreenText;
					genericDataStore.Data.Remove(genericData.Id);
					genericDataStore.Data.Add(genericData.Id, genericData);
					await _cosmosDbService.ContainerManager.GenericDataStoreData.UpdateAsync(genericDataStore);

					return RedirectToAction("Manage", new {id = genericDataStore.Id});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the generic data store group."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the generic data store group.", exception = ex.Message });
			}
		}

		
		public async Task<ActionResult> EditData(string dataStoreId, string dataId)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}
				
				GenericDataStore genericDataStore = await _cosmosDbService.ContainerManager.GenericDataStoreData.GetAsync(dataStoreId);
				if (genericDataStore == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					GenericDataViewModel genericDataViewModel = new GenericDataViewModel();
					KeyValuePair<string, GenericData> genericData = genericDataStore.Data.FirstOrDefault(x => x.Value.Id == dataId);
					if(genericData.Value != null)
					{
						await SetViewBagData();
						genericDataViewModel.Name = genericDataStore.Name;
						genericDataViewModel.Id = genericDataStore.Id;
						genericDataViewModel.DataId = genericData.Value.Id;
						genericDataViewModel.Key = genericData.Value.Key;
						genericDataViewModel.Value = genericData.Value.Value;
						genericDataViewModel.Image = genericData.Value.Image;
						genericDataViewModel.ScreenText = genericData.Value.ScreenText;

						return View(genericDataViewModel);
					}
					return RedirectToAction("Error", "Home", new { message = "Exception editing the generic data."});
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing the generic data.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> EditData(GenericDataViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				GenericDataStore genericDataStore = await _cosmosDbService.ContainerManager.GenericDataStoreData.GetAsync(model.Id);				
				
				if(genericDataStore != null)
				{
					//Update to use newly added Id vs key
					KeyValuePair<string, GenericData> genericData = genericDataStore.Data.FirstOrDefault(x => x.Value.Id == model.DataId);
					if(genericData.Value != null)
					{
						genericData.Value.Key = model.Key;
						genericData.Value.Value = model.Value;
						genericData.Value.Image = model.Image;
						genericData.Value.ScreenText = model.ScreenText;
						genericDataStore.Data.Remove(model.DataId);
						genericDataStore.Data.Add(model.DataId, genericData.Value);
						await _cosmosDbService.ContainerManager.GenericDataStoreData.UpdateAsync(genericDataStore);
						return RedirectToAction("Manage", new {id = genericDataStore.Id});
					}
				}
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the generic data store group."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the generic data store group.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> RemoveData(string dataStoreId, string dataId)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				GenericDataStore genericDataStore = await _cosmosDbService.ContainerManager.GenericDataStoreData.GetAsync(dataStoreId);				
				
				if(genericDataStore != null)
				{
					genericDataStore.Data.Remove(dataId);
					await _cosmosDbService.ContainerManager.GenericDataStoreData.UpdateAsync(genericDataStore);

					return RedirectToAction("Manage", new {id = genericDataStore.Id});
				}
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the generic data store group."});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the generic data store group.", exception = ex.Message });
			}
		}
	}
}