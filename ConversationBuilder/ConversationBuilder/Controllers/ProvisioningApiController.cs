using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ConversationBuilder.Data.Cosmos;
using ConversationBuilder.DataModels;
using ConversationBuilder.Models;
using ConversationBuilder.ViewModels;
using ConversationBuilder.Extensions;

namespace ConversationBuilder.Controllers
{
	[ApiController]
	[AllowAnonymous]
	[Route("api/provisioning")]
	public class ProvisioningApiController : ControllerBase
	{
		private readonly ICosmosDbService _dbService;
		private readonly UserManager<ApplicationUser> _userManager;

		public ProvisioningApiController(ICosmosDbService dbService, UserManager<ApplicationUser> userManager)
		{
			_dbService = dbService;
			_userManager = userManager;
		}

	/*	[HttpPost]
		[Route("provision")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ProvisioningModel>> Provision(string serialNumber, string deviceId)
		{
			if (string.IsNullOrEmpty(serialNumber) || string.IsNullOrEmpty(deviceId))
			{
				return BadRequest("Supplied arguments are invalid.");
			}

			try
			{
				serialNumber.Trim();				
				Robot robot = await _dbService.ContainerManager.RobotData.GetBySerialNumberAsync(serialNumber, false);
			
				if (robot == null || robot.DeviceId != deviceId)
				{
					return NotFound("Could not find the robot or it is already provisioned to an organization.");
				}

				string userName = RobotUserNameBuilder.BuildUserName(serialNumber);
				ApplicationUser robotUser = new ApplicationUser
				{
					Email = userName,
					Id = Guid.NewGuid().ToString(),
					UserName = userName
				};

				//TODO Hacky reprovisioning code, needs testing!
				//Do we want this?
				var user = await _userManager.FindByEmailAsync(robotUser.Email);
				if (user != null)
				{
					//Robot is being reprovisioned
					//Delete old info and add new
					await _userManager.RemoveFromRoleAsync(user, Roles.MistyRobot);
					await _userManager.DeleteAsync(user);
				}

				string robotPassword = GenerateRandomPassword();
				IdentityResult identityResult = await _userManager.CreateAsync(robotUser, robotPassword);

				if (identityResult.Succeeded)
				{
					IdentityResult roleResult = await _userManager.AddToRoleAsync(robotUser, Roles.MistyRobot);

					if (roleResult.Succeeded)
					{
						string code = await _userManager.GenerateEmailConfirmationTokenAsync(robotUser);
						await _userManager.ConfirmEmailAsync(robotUser, code);

						robot.IsProvisioned = true;
						robot.Updated = DateTimeOffset.UtcNow;
						robot.CreatedBy	 = $"Robot:{serialNumber}";

						//TODO This updates the robot association, should it?
						//only allowed if assigned in original admin... more cleanup to do
						await _dbService.ContainerManager.RobotData.UpdateAsync(robot);

						ProvisioningModel model = new ProvisioningModel
						{
							Password = robotPassword,
							OrganizationId = robot.OrganizationId
						};
						return Ok(model);
					}
					else
					{
						return BadRequest("Robot already exists in this role.");
					}
				}
				else
				{
					return BadRequest("Robot already exists in system.");
				}
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed processing provisioning request. '{ex.Message}.");
			}
		}*/
		
		private static string GenerateRandomPassword()
		{
			const int MinLength = 24;
			bool hasUppercaseLetter = false;
			bool hasLowercaseLetter = false;
			bool hasDigit = false;

			const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			const string lowercase = "abcdefghijklmnopqrstuvwxyz";
			const string digits = "1234567890";
			const string validSelections = uppercase + lowercase + digits;

			StringBuilder builder = new StringBuilder();
			Random rng = new Random();
			int length = 0;

			while (length < MinLength || !hasDigit || !hasUppercaseLetter || !hasLowercaseLetter)
			{
				char selected = validSelections[rng.Next(validSelections.Length)];
				builder.Append(selected);
				length++;

				if (uppercase.Contains(selected))
				{
					hasUppercaseLetter = true;
				}

				if (lowercase.Contains(selected))
				{
					hasLowercaseLetter = true;
				}

				if (digits.Contains(selected))
				{
					hasDigit = true;
				}
			}

			return builder.ToString();
		}
	}
}