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
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ConversationBuilder.DataModels
{
	public class ConversationGroup : IEditableData
	{
		public string Id { get; set; }

		public string ItemType { get; set; } = DataItemType.ConversationGroup.ToString();
		
		[Required]
		[Display(Name = "Name")]
		public string Name { get; set; }

		public string Description { get; set; }

		[Display(Name = "Robot name")]		
		public string RobotName { get; set; }

		[Display(Name = "Starting conversation")]		
		public string StartupConversation { get; set; }

		public IList<string> Conversations { get; set; } = new List<string>();

		public IList<string> SkillMessages { get; set; } = new List<string>();

		public string RobotIp { get; set; }

		public bool ValidConfiguration  { get; set; }

		[Display(Name = "Character Configuration")]	
		public string CharacterConfiguration { get; set; }
	
		[Display(Name = "Key Phrase recognized alert audio (beta)")]	
		public string KeyPhraseRecognizedAudio { get; set; }
		
		[Required]
		[Display(Name = "Management Access (beta)")]	
		public string ManagementAccess { get; set; } = "Shared";

		[Required]
		[Display(Name = "Configuration Request Access (beta)")]	
		public string RequestAccess { get; set; } = "Public";
		
		public IList<SkillAuthorization> SkillAuthorizations { get; set; } = new List<SkillAuthorization>();

		public DateTimeOffset Created { get; set; }
		
		public DateTimeOffset Updated { get; set; }
		public string CreatedBy { get; set; }

		//Key is departure as there should only be one for these mappings
		public IDictionary<string, ConversationMappingDetail> ConversationMappings { get; set; } = new Dictionary<string, ConversationMappingDetail>();
	}

	public class ConversationMappingDetail
	{
		public EntryMap EntryMap {get; set;}
		public DepartureMap DepartureMap {get; set;}
	}

	public class EntryMap
	{
		public string DisplayName {get; set;}
		public string ConversationId {get; set;}
		public string InteractionId {get; set;}
	}

	public class DepartureMap
	{
		public string DisplayName {get; set;}
		public string TriggerOptionId {get; set;}
		public string TriggerId {get; set;}
		public string ConversationId {get; set;}
		public string InteractionId {get; set;}
		public string AnimationId {get; set;}
	}
}