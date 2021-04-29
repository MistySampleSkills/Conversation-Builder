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
	public class Conversation : IEditableData
	{

		public string Id { get; set; }

		public string ItemType { get; set; } = DataItemType.Conversation.ToString();

		[Required]
		public string Name { get; set; }
		public string Description { get; set; }

		[Display(Name = "Starting Interaction")]		
		public string StartupInteraction { get; set; }
		
		[Display(Name = "No Trigger Interaction - called if an interaction timeout happens.")]		
		public string NoTriggerInteraction { get; set; }

		[Required]
		[Display(Name = "Starting emotion (beta)")]		
		public string StartingEmotion { get; set; } = DefaultEmotions.Joy;

		public IList<string> Interactions { get; set; } = new List<string>();

		public IList<string> Animations { get; set; } = new List<string>();

		public IList<string> Triggers { get; set; } = new List<string>();

		//public IList<string> SpeechHandlers = new List<string>();

		public IList<string> GenericDataStores = new List<string>();
		
		[Display(Name = "Initiate Skills At Conversation Start")]		
		public bool InitiateSkillsAtConversationStart { get; set; }
		public IList<string> SkillMessages { get; set; } = new List<string>();

		public IDictionary<string, IList<TriggerActionOption>> ConversationTriggerMap { get; set; } = new Dictionary<string, IList<TriggerActionOption>>();

		public IDictionary<string,string> InteractionAnimations { get; set; } = new Dictionary<string, string>();

		//key is interaction id
		public IDictionary<string, EntryMap> ConversationEntryPoints { get; set; } = new Dictionary<string, EntryMap>();

		//key is trigger action option id
		public IDictionary<string, DepartureMap> ConversationDeparturePoints { get; set; } = new Dictionary<string, DepartureMap>();
		
		public DateTimeOffset Created { get; set; }
		
		public DateTimeOffset Updated { get; set; }

		[Required]
		[Display(Name = "Management Access (beta)")]		
		public string ManagementAccess { get; set; } = "Public";
		public string CreatedBy { get; set; }
	}
}