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
	//TODO 
	[ApiController]
	//[Authorize(Roles = Roles.MistyRobot, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[Route("api/conversations")]
	public class ConversationsApiController : AdminToolController
	{
		public ConversationsApiController(ICosmosDbService cosmosDbService, UserManager<ApplicationUser> userManager)
			: base(cosmosDbService, userManager) { }

		[HttpGet]
		[Route("group")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<SkillParameters>> ConversationGroup(string id, string accountId, string key)
		{			
			try
			{
				ConversationGroup conversationGroup = await _cosmosDbService.ContainerManager.ConversationGroupData.GetAsync(id);
				if(conversationGroup == null)
				{
					return BadRequest("Failed accessing conversation group data, please check the id.");
				}
				else if(string.IsNullOrWhiteSpace(conversationGroup.RequestAccess)/*old data*/ || conversationGroup.RequestAccess == "None")
				{
					return NotFound("Conversation group is not available at this time.");
					//it exists but isn't available					
				}
				else if(conversationGroup.RequestAccess == "Public")
				{
					SkillParameters skillParameters = await GenerateSkillConfiguration(id);					
					string skillConfiguration = Newtonsoft.Json.JsonConvert.SerializeObject(skillParameters);
					return Ok(skillConfiguration);

				}
				else if(!string.IsNullOrWhiteSpace(accountId) && !string.IsNullOrWhiteSpace(key))
				{
					IList<SkillAuthorization> skillAuthorizations = conversationGroup.SkillAuthorizations;

					if(skillAuthorizations != null && skillAuthorizations.Any(x => x.AccountId == accountId && x.Key == key))
					{	
						SkillParameters skillParameters = await GenerateSkillConfiguration(id);	
						string skillConfiguration = Newtonsoft.Json.JsonConvert.SerializeObject(skillParameters);
						return Ok(skillConfiguration);
					}
				}
				
				return Unauthorized();
				
			}
			catch
			{
				return BadRequest("Failed accessing conversation group data.");
			}
		}

		[HttpGet]
		[Route("skills/auth")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult> Authorize(string id, string accountId, string key)
		{			
			try
			{
				SkillMessage skillMessage = await _cosmosDbService.ContainerManager.SkillMessageData.GetAsync(id);
				if(skillMessage == null)
				{
					return BadRequest("Failed accessing skill message data, please check the id.");
				}
				else if(string.IsNullOrWhiteSpace(skillMessage.RequestAccess)/*old data*/ || skillMessage.RequestAccess == "None")
				{			
					return NotFound("Skill message is not available at this time.");
					//it exists but isn't available					
				}
				else if(skillMessage.RequestAccess == "Public")
				{
					return Ok();
				}
				else if(!string.IsNullOrWhiteSpace(accountId) && !string.IsNullOrWhiteSpace(key))
				{
					IList<SkillAuthorization> skillAuthorizations = skillMessage.SkillAuthorizations;
					if(skillAuthorizations != null && skillAuthorizations.Any(x => x.AccountId == accountId && x.Key == key))
					{	
						return Ok();
					}
				}
				
				return Unauthorized();
				
			}
			catch
			{
				return BadRequest("Failed accessing conversation group data.");
			}
		}
	}
}