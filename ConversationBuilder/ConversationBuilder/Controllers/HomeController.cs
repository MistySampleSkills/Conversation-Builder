﻿/**********************************************************************
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

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ConversationBuilder.Models;
using ConversationBuilder.Data.Cosmos;
using ConversationBuilder.Extensions;

namespace ConversationBuilder.Controllers
{
	public class HomeController : AdminToolController
	{
		public HomeController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
			: base(cosmosDbService, userManager) { }

		[AllowAnonymous]
		public async Task<IActionResult> Index()
		{
			UserInformation userInfo = await GetUserInformation();
			if (userInfo == null)
			{
				return View();
			}
			await SetViewBagData();
			return View();
		}

		[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
		public async Task<IActionResult> UserPage()
		{
			await SetViewBagData();
			return View();
		}

		[AuthorizeRoles(Roles.SiteAdministrator)]
		public async Task<IActionResult> AdminPage()
		{
			await SetViewBagData();
			return View();
		}

		[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
		public async Task<IActionResult> Advanced()
		{
			await SetViewBagData(); 
			return View();
		}

		[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
		public async Task<IActionResult> ActionLibrary()
		{
			await SetViewBagData();
			return View();
		}

		[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
		public async Task<IActionResult> Speech()
		{
			await SetViewBagData();
			return View();
		}

		[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
		public async Task<IActionResult> TriggerConstruction()
		{
			await SetViewBagData();
			return View();
		}

		[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
		public async Task<IActionResult> Conversations()
		{
			await SetViewBagData();
			return View();
		}

		[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
		public async Task<IActionResult> Administrative()
		{
			await SetViewBagData();
			return View();
		}

		[AllowAnonymous]
		public async Task<IActionResult> About()
		{
			await SetViewBagData();
			ViewData["Message"] = "Your application description page.";

			return View();
		}

		[AllowAnonymous]
		public async Task<IActionResult> Contact()
		{
			await SetViewBagData();
			ViewData["Message"] = "Your contact page.";

			return View();
		}

		[AllowAnonymous]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public async Task<IActionResult> Error(string message = "", string exception = "")
		{
			await SetViewBagData();
			return View(
				new ErrorViewModel 
				{ 
					RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, 
					Message = message,
					Exception = exception
				});
		}
	}
}
