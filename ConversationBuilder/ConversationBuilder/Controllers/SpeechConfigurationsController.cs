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

namespace ConversationBuilder.Controllers
{
	[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
	public class SpeechConfigurationsController : AdminToolController
	{
		public SpeechConfigurationsController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
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
				int totalCount = await _cosmosDbService.ContainerManager.SpeechConfigurationData.GetCountAsync();
				IList<SpeechConfiguration> speechConfigurations = await _cosmosDbService.ContainerManager.SpeechConfigurationData.GetListAsync(startItem, totalItems);
				SetFilterAndPagingViewData(1, null, totalCount, totalItems);

				if (speechConfigurations == null)
				{
					return View();
				}
				else
				{
					return View(speechConfigurations.OrderBy(x => x.Name));
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing speechConfiguration list.", exception = ex.Message });
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

				
				SpeechConfiguration speechConfiguration =  await _cosmosDbService.ContainerManager.SpeechConfigurationData.GetAsync(id);
				if (speechConfiguration == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{				
					await SetViewBagData();
					return View(speechConfiguration);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing speechConfiguration details.", exception = ex.Message });
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
				SpeechConfiguration speechConfiguration = new SpeechConfiguration {};
				await SetViewBagData();
				return View(speechConfiguration);				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating speechConfiguration.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(SpeechConfiguration model)
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

					await _cosmosDbService.ContainerManager.SpeechConfigurationData.AddAsync(model);
					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Create), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating speechConfiguration.", exception = ex.Message });
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
				
				SpeechConfiguration speechConfiguration = await _cosmosDbService.ContainerManager.SpeechConfigurationData.GetAsync(id);
				if (speechConfiguration == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.Emotions = (new DefaultEmotions()).AllItems;
					await SetViewBagData();
					return View(speechConfiguration);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing speechConfiguration.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(SpeechConfiguration speechConfiguration)
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
					SpeechConfiguration loadedSpeechConfiguration = await _cosmosDbService.ContainerManager.SpeechConfigurationData.GetAsync(speechConfiguration.Id);
					loadedSpeechConfiguration.Name = speechConfiguration.Name;
					loadedSpeechConfiguration.ProfanitySetting = speechConfiguration.ProfanitySetting;
					loadedSpeechConfiguration.SpeechRecognitionRegion = speechConfiguration.SpeechRecognitionRegion;
					loadedSpeechConfiguration.TextToSpeechRegion = speechConfiguration.TextToSpeechRegion;
					loadedSpeechConfiguration.SpeakingGender = speechConfiguration.SpeakingGender;
					loadedSpeechConfiguration.SpeakingVoice = speechConfiguration.SpeakingVoice;
					loadedSpeechConfiguration.SpokenLanguage= speechConfiguration.SpokenLanguage;
					loadedSpeechConfiguration.SpeechRecognitionEndpoint= speechConfiguration.SpeechRecognitionEndpoint;					
					loadedSpeechConfiguration.TextToSpeechEndpoint= speechConfiguration.TextToSpeechEndpoint;
					loadedSpeechConfiguration.SpeechRecognitionSubscriptionKey= speechConfiguration.SpeechRecognitionSubscriptionKey;
					loadedSpeechConfiguration.TextToSpeechSubscriptionKey= speechConfiguration.TextToSpeechSubscriptionKey;
					loadedSpeechConfiguration.TranslatedLanguage= speechConfiguration.TranslatedLanguage;
					loadedSpeechConfiguration.TextToSpeechService = speechConfiguration.TextToSpeechService;
					loadedSpeechConfiguration.SpeechRecognitionService = speechConfiguration.SpeechRecognitionService;
					loadedSpeechConfiguration.ManagementAccess = speechConfiguration.ManagementAccess;
					loadedSpeechConfiguration.Updated = DateTimeOffset.UtcNow;		
					await _cosmosDbService.ContainerManager.SpeechConfigurationData.UpdateAsync(loadedSpeechConfiguration);

					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Edit), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing speechConfiguration.", exception = ex.Message });
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
			
				SpeechConfiguration speechConfiguration = await _cosmosDbService.ContainerManager.SpeechConfigurationData.GetAsync(id);
				if (speechConfiguration == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.CanBeDeleted = await CanBeDeleted(id, DeleteItem.SpeechConfiguration);
					await SetViewBagData();
					return View(speechConfiguration);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting speechConfiguration.", exception = ex.Message });
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

				 await _cosmosDbService.ContainerManager.SpeechConfigurationData.DeleteAsync(id);
				return RedirectToAction(nameof(Index));				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting speechConfiguration.", exception = ex.Message });
			}
		}
	}
}