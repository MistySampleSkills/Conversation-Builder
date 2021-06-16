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
using Newtonsoft.Json;

namespace ConversationBuilder.Controllers
{
	[AuthorizeRoles(Roles.SiteAdministrator, Roles.Customer)]
	public class RobotsController : AdminToolController
	{
		public RobotsController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
			: base(cosmosDbService, userManager) { }

		public async Task<ActionResult> Index()
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				IEnumerable<Robot> robots = await _cosmosDbService.ContainerManager.RobotData.GetListAsync(1, 10000, userInfo.AccessId);

				await SetViewBagData();
				@ViewBag.Message = Convert.ToString(TempData["Message"]);
				TempData["Message"] = "";
				if (robots == null || robots.Count() == 0)
				{
					return View();
				}
				else
				{
					return View(robots.OrderBy(x => x.RobotName));
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing robot list.", exception = ex.Message });
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

				Robot robot = await _cosmosDbService.ContainerManager.RobotData.GetAsync(id);				
				await SetViewBagData();
				if (robot == null)
				{
					return RedirectToAction("Error", "Home", new { message = "Exception accessing robot details.", exception = "Cannot find details for robot." });
				}
				else
				{
					return View(robot);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception accessing robot details.", exception = ex.Message });
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

				Robot robot = new Robot();
				await SetViewBagData();
				return View(robot);
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating robot.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(Robot model)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				await SetViewBagData();
				if (ModelState.IsValid)
				{
					/*Robot existingRobot = await _cosmosDbService.ContainerManager.RobotData.GetBySerialNumberAsync(model.SerialNumber, null);
					if(existingRobot != null)
					{
						return RedirectToAction("Error", "Home", new { message = "This robot already exists." });
					}*/

					model.IsValidConfig = true;
					if (!string.IsNullOrWhiteSpace(model.RobotConfig))
					{
						try
						{
							JsonConvert.DeserializeObject<Dictionary<string, object>>(model.RobotConfig);
							TempData["Message"] = "";
						}
						catch
						{
							model.IsValidConfig = false;
							TempData["Message"] = "Warning: Invalid json configuration detected.";
						}
					}

					model.Id = Guid.NewGuid().ToString();
					DateTimeOffset dt = DateTimeOffset.UtcNow;
					model.Created = dt;
					model.Updated = dt;
					model.CreatedBy = userInfo.AccessId;

					await _cosmosDbService.ContainerManager.RobotData.AddAsync(model);

					return RedirectToAction(nameof(Index), new { model.Id });
				}
				else
				{
					return RedirectToAction(nameof(Create), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception creating robot.", exception = ex.Message });
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
				
				Robot robot = await _cosmosDbService.ContainerManager.RobotData.GetAsync(id);				
				await SetViewBagData();
				if (robot == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{			
					return View(robot);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing robot.", exception = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(Robot robot)
		{
			try
			{
				UserInformation userInfo = await GetUserInformation();
				if (userInfo == null)
				{
					return RedirectToAction("Error", "Home", new { message = UserNotFoundMessage });
				}

				await SetViewBagData();
				if (ModelState.IsValid)
				{
					robot.IsValidConfig = true;
					if (!string.IsNullOrWhiteSpace(robot.RobotConfig))
					{
						try
						{
							JsonConvert.DeserializeObject<Dictionary<string, object>>(robot.RobotConfig);
							TempData["Message"] = "";
						}
						catch
						{
							robot.IsValidConfig = false;
							TempData["Message"] = "Warning: Invalid json configuration detected in last update.";
						}
					}

					Robot loadedRobot = await _cosmosDbService.ContainerManager.RobotData.GetAsync(robot.Id);

					//Allow edit?
					loadedRobot.SerialNumber = robot.SerialNumber;		

					loadedRobot.IsProvisioned = robot.IsProvisioned;
					loadedRobot.AllowCrossRobotCommunication = robot.AllowCrossRobotCommunication;					
					loadedRobot.IP = robot.IP;
					loadedRobot.RobotName = robot.RobotName;
					loadedRobot.RobotConfig = robot.RobotConfig;
					loadedRobot.Updated = DateTimeOffset.UtcNow;
					loadedRobot.IsValidConfig = robot.IsValidConfig;
					loadedRobot.Notes = robot.Notes;
					await _cosmosDbService.ContainerManager.RobotData.UpdateAsync(loadedRobot);

					return RedirectToAction(nameof(Index));
				}
				else
				{
					return RedirectToAction(nameof(Edit), ModelState);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception editing robot.", exception = ex.Message });
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
			
				Robot robot = await _cosmosDbService.ContainerManager.RobotData.GetAsync(id);
				if (robot == null)
				{
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ViewBag.CanBeDeleted = true;
					await SetViewBagData();
					return View(robot);
				}
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting robot.", exception = ex.Message });
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

				 await _cosmosDbService.ContainerManager.RobotData.DeleteAsync(id);
				return RedirectToAction(nameof(Index));				
			}
			catch (Exception ex)
			{
				return RedirectToAction("Error", "Home", new { message = "Exception deleting robot.", exception = ex.Message });
			}
		}
	}
}