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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ConversationBuilder.Data.Cosmos;
using ConversationBuilder.DataModels;
using ConversationBuilder.Extensions;

namespace ConversationBuilder.Controllers
{
	[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
	public class UserConfigurationsController : AdminToolController
	{
		public UserConfigurationsController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
			: base(cosmosDbService, userManager) { }

		public async Task<ActionResult> Details()
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				UserConfiguration userConfiguration =  await _cosmosDbService.ContainerManager.UserConfigurationData.GetAsync(userInfo.AccessId);
				await SetViewBagData();
				if (userConfiguration == null)
				{
					userConfiguration = new UserConfiguration();
					//first time viewing profile, create it
					userConfiguration.Id = userInfo.AccessId;
					userConfiguration.OverrideCssFile = "lite";
					DateTimeOffset dt = DateTimeOffset.UtcNow;
					userConfiguration.CreatedBy = userInfo.AccessId;
					userConfiguration.Created = dt;
					userConfiguration.Updated = dt;

					await _cosmosDbService.ContainerManager.UserConfigurationData.AddAsync(userConfiguration);

					return View(userConfiguration);
				}
				else
				{				
					return View(userConfiguration);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing userConfiguration details.", exception = ex.Message });
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

				UserConfiguration userConfiguration = new UserConfiguration {};
				ViewBag.Themes = new Themes().AllItems;
				await SetViewBagData();
				return View(userConfiguration);				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating userConfiguration.", exception = ex.Message });
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
				
				UserConfiguration userConfiguration = await _cosmosDbService.ContainerManager.UserConfigurationData.GetAsync(id);
				if (userConfiguration == null)
				{
					//first time viewing profile? create it, should have prolly been to details first
					userConfiguration.Id = userInfo.AccessId;
					DateTimeOffset dt = DateTimeOffset.UtcNow;
					userConfiguration.CreatedBy = userInfo.AccessId;
					userConfiguration.Created = dt;
					userConfiguration.Updated = dt;
					
					ViewBag.Themes = new Themes().AllItems;

					await _cosmosDbService.ContainerManager.UserConfigurationData.AddAsync(userConfiguration);

					return View(userConfiguration);
				}
				else
				{
					ViewBag.SpeechConfigurations = await SpeechConfigurationList();
					
					ViewBag.Themes = new Themes().AllItems;
					await SetViewBagData();
					return View(userConfiguration);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing userConfiguration.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(UserConfiguration userConfiguration)
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
					UserConfiguration loadedUserConfiguration = await _cosmosDbService.ContainerManager.UserConfigurationData.GetAsync(userConfiguration.Id);
					loadedUserConfiguration.Id = userConfiguration.Id;
					loadedUserConfiguration.Updated = DateTimeOffset.UtcNow;
					loadedUserConfiguration.OverrideCssFile = userConfiguration.OverrideCssFile;
					loadedUserConfiguration.ShowBetaItems = userConfiguration.ShowBetaItems;
					loadedUserConfiguration.UserName = userConfiguration.UserName;
					loadedUserConfiguration.Updated = DateTimeOffset.UtcNow;
					
					await _cosmosDbService.ContainerManager.UserConfigurationData.UpdateAsync(loadedUserConfiguration);

					return RedirectToAction(nameof(Details));
				}
				else
				{
					return RedirectToAction(nameof(Edit), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing userConfiguration.", exception = ex.Message });
			}
		}
	}
}