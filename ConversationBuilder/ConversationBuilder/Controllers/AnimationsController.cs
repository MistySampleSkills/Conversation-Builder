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
	public class AnimationsController : AdminToolController
	{
		public AnimationsController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
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
				int totalCount = await _cosmosDbService.ContainerManager.AnimationData.GetCountAsync();
				IList<Animation> animations = await _cosmosDbService.ContainerManager.AnimationData.GetListAsync(startItem, totalItems);
				SetFilterAndPagingViewData(1, null, totalCount, totalItems);

				if (animations == null)
				{
					return View();
				}
				else
				{
					return View(animations.OrderBy(x => x.Name));
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing animation list.", exception = ex.Message });
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

				
				Animation animation =  await _cosmosDbService.ContainerManager.AnimationData.GetAsync(id);
				if (animation == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{		
					ViewBag.HeadLocationName = "";	
					if(!string.IsNullOrWhiteSpace(animation.HeadLocation))
					{
						HeadLocation headLocation =  await _cosmosDbService.ContainerManager.HeadLocationData.GetAsync(animation.HeadLocation);
						if(headLocation != null)
						{
							ViewBag.HeadLocationName = headLocation.Name;
						}
					}

					ViewBag.ArmLocationName = "";	
					if(!string.IsNullOrWhiteSpace(animation.ArmLocation))
					{
						ArmLocation armLocation =  await _cosmosDbService.ContainerManager.ArmLocationData.GetAsync(animation.ArmLocation);
						if(armLocation != null)
						{
							ViewBag.ArmLocationName = armLocation.Name;
						}
					}

					ViewBag.LedTransitionActionName = "";	
					if(!string.IsNullOrWhiteSpace(animation.LEDTransitionAction))
					{
						LEDTransitionAction ledAction =  await _cosmosDbService.ContainerManager.LEDTransitionActionData.GetAsync(animation.LEDTransitionAction);
						if(ledAction != null)
						{
							ViewBag.LedTransitionActionName = ledAction.Name;
						}
					}
					
					await SetViewBagData();
					return View(animation);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing animation details.", exception = ex.Message });
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

				ViewBag.ArmLocations = await ArmLocationList();
				ViewBag.HeadLocations = await HeadLocationList();
				ViewBag.LEDTransitionActions = await LEDTransitionActionList();
			
				ViewBag.Emotions = (new DefaultEmotions()).AllItems;

				Animation animation = new Animation {};
				await SetViewBagData();
				return View(animation);				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating animation.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(Animation model)
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

					await _cosmosDbService.ContainerManager.AnimationData.AddAsync(model);
					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Create), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating animation.", exception = ex.Message });
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
				
				Animation animation = await _cosmosDbService.ContainerManager.AnimationData.GetAsync(id);
				if (animation == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.ArmLocations = await ArmLocationList();
					ViewBag.HeadLocations = await HeadLocationList();
					ViewBag.LEDTransitionActions = await LEDTransitionActionList();

					ViewBag.Emotions = (new DefaultEmotions()).AllItems;
					await SetViewBagData();
					return View(animation);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing animation.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(Animation animation)
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
					Animation loadedAnimation = await _cosmosDbService.ContainerManager.AnimationData.GetAsync(animation.Id);
					loadedAnimation.Name = animation.Name;
					loadedAnimation.Speak = animation.Speak;
					loadedAnimation.Emotion = animation.Emotion;
					loadedAnimation.AudioFile = animation.AudioFile;
					loadedAnimation.Silence = animation.Silence;
					loadedAnimation.ImageFile = animation.ImageFile;
					loadedAnimation.SetFlashlight = animation.SetFlashlight;
					loadedAnimation.TrimAudioSilence = animation.TrimAudioSilence;
					loadedAnimation.TrimAudioSilence = animation.TrimAudioSilence;
					loadedAnimation.HeadActionDelay = animation.HeadActionDelay;
					loadedAnimation.ArmActionDelay = animation.ArmActionDelay;
					loadedAnimation.HeadLocation = animation.HeadLocation;
					loadedAnimation.ManagementAccess = animation.ManagementAccess;
					loadedAnimation.ArmLocation = animation.ArmLocation;
					loadedAnimation.LEDTransitionAction = animation.LEDTransitionAction;
					loadedAnimation.Volume = animation.Volume;
					loadedAnimation.SpeechRate = animation.SpeechRate;
					loadedAnimation.OverrideVoice   = animation.OverrideVoice;
					loadedAnimation.SpeakingStyle  = animation.SpeakingStyle;
					loadedAnimation.AnimationScript = animation.AnimationScript;
					loadedAnimation.Updated = DateTimeOffset.UtcNow;
					
					await _cosmosDbService.ContainerManager.AnimationData.UpdateAsync(loadedAnimation);

					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Edit), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing animation.", exception = ex.Message });
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
			
				Animation animation = await _cosmosDbService.ContainerManager.AnimationData.GetAsync(id);
				if (animation == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.HeadLocationName = "";	
					if(!string.IsNullOrWhiteSpace(animation.HeadLocation))
					{
						HeadLocation headLocation =  await _cosmosDbService.ContainerManager.HeadLocationData.GetAsync(animation.HeadLocation);
						if(headLocation != null)
						{
							ViewBag.HeadLocationName = headLocation.Name;
						}
					}

					ViewBag.ArmLocationName = "";	
					if(!string.IsNullOrWhiteSpace(animation.ArmLocation))
					{
						ArmLocation armLocation =  await _cosmosDbService.ContainerManager.ArmLocationData.GetAsync(animation.ArmLocation);
						if(armLocation != null)
						{
							ViewBag.ArmLocationName = armLocation.Name;
						}
					}

					ViewBag.LedTransitionActionName = "";	
					if(!string.IsNullOrWhiteSpace(animation.LEDTransitionAction))
					{
						LEDTransitionAction ledAction =  await _cosmosDbService.ContainerManager.LEDTransitionActionData.GetAsync(animation.LEDTransitionAction);
						if(ledAction != null)
						{
							ViewBag.LedTransitionActionName = ledAction.Name;
						}
					}

					ViewBag.CanBeDeleted = await CanBeDeleted(id, DeleteItem.Animation);
					await SetViewBagData();
					return View(animation);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting animation.", exception = ex.Message });
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

				 await _cosmosDbService.ContainerManager.AnimationData.DeleteAsync(id);
				return RedirectToAction(nameof(Index));				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting animation.", exception = ex.Message });
			}
		}
	}
}