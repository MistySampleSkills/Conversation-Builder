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
	public class LEDTransitionActionsController : AdminToolController
	{
		public LEDTransitionActionsController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
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
				int totalCount = await _cosmosDbService.ContainerManager.LEDTransitionActionData.GetCountAsync();
				IList<LEDTransitionAction> ledTransitionActions = await _cosmosDbService.ContainerManager.LEDTransitionActionData.GetListAsync(startItem, totalItems);
				SetFilterAndPagingViewData(1, null, totalCount, totalItems);

				if (ledTransitionActions == null)
				{
					return View();
				}
				else
				{
					return View(ledTransitionActions.OrderBy(x => x.Name));
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing lEDTransitionAction list.", exception = ex.Message });
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

				LEDTransitionAction lEDTransitionAction =  await _cosmosDbService.ContainerManager.LEDTransitionActionData.GetAsync(id);
				if (lEDTransitionAction == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{				
					LEDTransitionViewModel lEDTransitionViewModel = new LEDTransitionViewModel();
					lEDTransitionViewModel.Id = lEDTransitionAction.Id;
					lEDTransitionViewModel.Name = lEDTransitionAction.Name;
					lEDTransitionViewModel.Pattern = lEDTransitionAction.Pattern;
					lEDTransitionViewModel.PatternTime = lEDTransitionAction.PatternTime;
					lEDTransitionViewModel.Updated = lEDTransitionAction.Updated;
					lEDTransitionViewModel.Created = lEDTransitionAction.Created;
					lEDTransitionViewModel.ManagementAccess = lEDTransitionAction.ManagementAccess;
					lEDTransitionViewModel.Updated = DateTimeOffset.UtcNow;
					lEDTransitionViewModel.StartingRGB = $"rgba({lEDTransitionAction.Red},{lEDTransitionAction.Green},{lEDTransitionAction.Blue})";
					lEDTransitionViewModel.EndingRGB = $"rgba({lEDTransitionAction.Red2},{lEDTransitionAction.Green2},{lEDTransitionAction.Blue2})";

					await SetViewBagData();
					return View(lEDTransitionViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing lEDTransitionAction details.", exception = ex.Message });
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
				LEDTransitionViewModel lEDTransitionAction = new LEDTransitionViewModel {};
				await SetViewBagData();
				return View(lEDTransitionAction);				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating lEDTransitionAction.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(LEDTransitionViewModel model)
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
					LEDTransitionAction ledTransitionAction = new LEDTransitionAction();

					string startRgbString = model.StartingRGB ?? "rgba(0,0,0,1)";
					startRgbString = startRgbString.Split("(")[1].Split(")")[0];
					var rgbArray = startRgbString.Split(","); 

					ledTransitionAction.Red = Convert.ToByte(rgbArray[0]);
					ledTransitionAction.Green = Convert.ToByte(rgbArray[1]);
					ledTransitionAction.Blue = Convert.ToByte(rgbArray[2]);

					string endRgbString = model.EndingRGB ?? "rgba(0,0,0,1)";
					endRgbString = endRgbString.Split("(")[1].Split(")")[0];
					var endRgbArray = endRgbString.Split(","); 

					ledTransitionAction.Red2 = Convert.ToByte(endRgbArray[0]);
					ledTransitionAction.Green2 = Convert.ToByte(endRgbArray[1]);
					ledTransitionAction.Blue2 = Convert.ToByte(endRgbArray[2]);

					ledTransitionAction.Id = Guid.NewGuid().ToString();
					DateTimeOffset dt = DateTimeOffset.UtcNow;
					ledTransitionAction.Name = model.Name;
					ledTransitionAction.Pattern = model.Pattern;
					ledTransitionAction.PatternTime = model.PatternTime;
					ledTransitionAction.CreatedBy = userInfo.AccessId;
					ledTransitionAction.Created = dt;
					ledTransitionAction.Updated = dt;

					await _cosmosDbService.ContainerManager.LEDTransitionActionData.AddAsync(ledTransitionAction);
					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Create), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating lEDTransitionAction.", exception = ex.Message });
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
				
				LEDTransitionAction lEDTransitionAction = await _cosmosDbService.ContainerManager.LEDTransitionActionData.GetAsync(id);
				
				if (lEDTransitionAction == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					LEDTransitionViewModel lEDTransitionViewModel = new LEDTransitionViewModel();
					lEDTransitionViewModel.Id = lEDTransitionAction.Id;
					lEDTransitionViewModel.Name = lEDTransitionAction.Name;
					lEDTransitionViewModel.Pattern = lEDTransitionAction.Pattern;
					lEDTransitionViewModel.PatternTime = lEDTransitionAction.PatternTime;
					lEDTransitionViewModel.Updated = lEDTransitionAction.Updated;
					lEDTransitionViewModel.Created = lEDTransitionAction.Created;
					lEDTransitionViewModel.ManagementAccess = lEDTransitionAction.ManagementAccess;
					lEDTransitionViewModel.Updated = DateTimeOffset.UtcNow;
					lEDTransitionViewModel.StartingRGB = $"rgba({lEDTransitionAction.Red},{lEDTransitionAction.Green},{lEDTransitionAction.Blue})";
					lEDTransitionViewModel.EndingRGB = $"rgba({lEDTransitionAction.Red2},{lEDTransitionAction.Green2},{lEDTransitionAction.Blue2})";

					ViewBag.Emotions = (new DefaultEmotions()).AllItems;
					await SetViewBagData();
					return View(lEDTransitionViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing lEDTransitionAction.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(LEDTransitionViewModel lEDTransitionAction)
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
					LEDTransitionAction loadedLEDTransitionAction = await _cosmosDbService.ContainerManager.LEDTransitionActionData.GetAsync(lEDTransitionAction.Id);
					loadedLEDTransitionAction.Name = lEDTransitionAction.Name;
					loadedLEDTransitionAction.Pattern = lEDTransitionAction.Pattern;
					loadedLEDTransitionAction.PatternTime= lEDTransitionAction.PatternTime;
					
					string startRgbString = lEDTransitionAction.StartingRGB ?? "rgba(0,0,0,1)";
					startRgbString = startRgbString.Split("(")[1].Split(")")[0];
					var rgbArray = startRgbString.Split(","); 

					loadedLEDTransitionAction.Red = Convert.ToByte(rgbArray[0]);
					loadedLEDTransitionAction.Green = Convert.ToByte(rgbArray[1]);
					loadedLEDTransitionAction.Blue = Convert.ToByte(rgbArray[2]);

					string endRgbString = lEDTransitionAction.EndingRGB ?? "rgba(0,0,0,1)";
					endRgbString = endRgbString.Split("(")[1].Split(")")[0];
					var endRgbArray = endRgbString.Split(","); 

					loadedLEDTransitionAction.Red2 = Convert.ToByte(endRgbArray[0]);
					loadedLEDTransitionAction.Green2 = Convert.ToByte(endRgbArray[1]);
					loadedLEDTransitionAction.Blue2 = Convert.ToByte(endRgbArray[2]);

					loadedLEDTransitionAction.ManagementAccess = lEDTransitionAction.ManagementAccess;
					loadedLEDTransitionAction.Updated = DateTimeOffset.UtcNow;			
					await _cosmosDbService.ContainerManager.LEDTransitionActionData.UpdateAsync(loadedLEDTransitionAction);

					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Edit), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing lEDTransitionAction.", exception = ex.Message });
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
			
				LEDTransitionAction lEDTransitionAction = await _cosmosDbService.ContainerManager.LEDTransitionActionData.GetAsync(id);
				if (lEDTransitionAction == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.CanBeDeleted = await CanBeDeleted(id, DeleteItem.LedTransitionAction);
					await SetViewBagData();
					LEDTransitionViewModel lEDTransitionViewModel = new LEDTransitionViewModel();
					lEDTransitionViewModel.Id = lEDTransitionAction.Id;
					lEDTransitionViewModel.Name = lEDTransitionAction.Name;
					lEDTransitionViewModel.Pattern = lEDTransitionAction.Pattern;
					lEDTransitionViewModel.PatternTime = lEDTransitionAction.PatternTime;
					lEDTransitionViewModel.Updated = lEDTransitionAction.Updated;
					lEDTransitionViewModel.Created = lEDTransitionAction.Created;
					lEDTransitionViewModel.ManagementAccess = lEDTransitionAction.ManagementAccess;
					lEDTransitionViewModel.Updated = DateTimeOffset.UtcNow;
					lEDTransitionViewModel.StartingRGB = $"rgba({lEDTransitionAction.Red},{lEDTransitionAction.Green},{lEDTransitionAction.Blue}";
					lEDTransitionViewModel.EndingRGB = $"rgba({lEDTransitionAction.Red2},{lEDTransitionAction.Green2},{lEDTransitionAction.Blue2}";

					return View(lEDTransitionViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting lEDTransitionAction.", exception = ex.Message });
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

				 await _cosmosDbService.ContainerManager.LEDTransitionActionData.DeleteAsync(id);
				return RedirectToAction(nameof(Index));				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting lEDTransitionAction.", exception = ex.Message });
			}
		}
	}
}