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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ConversationBuilder.Data.Cosmos;
using ConversationBuilder.DataModels;
using ConversationBuilder.ViewModels;
using ConversationBuilder.Extensions;

namespace ConversationBuilder.Controllers
{
	[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
	public class ConversationSpeechHandlersController : AdminToolController
	{
		public ConversationSpeechHandlersController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
			: base(cosmosDbService, userManager) { }

		public async Task<ActionResult> Index(string conversationId, int startItem = 1, int totalItems = 100)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				await SetViewBagData();
				int totalCount = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetCountAsync();
				IList<SpeechHandler> speechHandlers = await _cosmosDbService.ContainerManager.SpeechHandlerData.GetListAsync(startItem, 1000);//TODO
				IList<SpeechHandler> filteredSpeechHandlers = new List<SpeechHandler>();
				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(conversationId);				
				foreach(var speechHandler in conversation.SpeechHandlers)
				{
					SpeechHandler selectedSpeechHandler  = speechHandlers.FirstOrDefault(x => x.Id == speechHandler);
					if(selectedSpeechHandler != null && !filteredSpeechHandlers.Contains(selectedSpeechHandler))
					{
						filteredSpeechHandlers.Add(selectedSpeechHandler);
					}
				}

				SetFilterAndPagingViewData(1, null, totalCount, totalItems);
				if (speechHandlers == null)
				{
					return View();
				}
				else
				{
					ViewBag.SpeechHandlers = await SpeechHandlers();
					ConversationSpeechHandlerViewModel speechHandlerViewModel = new ConversationSpeechHandlerViewModel();
					speechHandlerViewModel.ConversationId = conversationId;
					speechHandlerViewModel.ConversationName = conversation.Name;
					speechHandlerViewModel.SpeechHandlers = filteredSpeechHandlers.OrderBy(x => x.Name).ToList();
					return View(speechHandlerViewModel);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing speech handler list.", exception = ex.Message });
			}
		}
		
		public async Task<ActionResult> Add(ConversationSpeechHandlerViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.ConversationId);				
				if(!conversation.SpeechHandlers.Contains(model.Handler))
				{
					conversation.SpeechHandlers.Add(model.Handler);
				}

				await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);

				return RedirectToAction("Index", new {conversationId = conversation.Id});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception adding to the conversation speech handler list.", exception = ex.Message });
			}
		}

		public async Task<ActionResult> Remove(ConversationSpeechHandlerViewModel model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				Conversation conversation = await _cosmosDbService.ContainerManager.ConversationData.GetAsync(model.ConversationId);
				conversation.SpeechHandlers.Remove(model.Handler);
				await _cosmosDbService.ContainerManager.ConversationData.UpdateAsync(conversation);

				return RedirectToAction("Index", new {conversationId = conversation.Id});
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception removing from the conversation speech handler list.", exception = ex.Message });
			}
		}
	}
}