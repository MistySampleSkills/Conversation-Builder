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

namespace ConversationBuilder.DataModels
{
	public class SkillConversation
	{
		public string Id { get; set; }

		public string Name { get; set; }
		public string Description { get; set; }
		
		public string StartupInteraction { get; set; }

		public string NoTriggerInteraction { get; set; }

		public string StartingEmotion { get; set; } = DefaultEmotions.None;

		public bool InitiateSkillsAtConversationStart {get; set;}

		public IList<SkillInteraction> Interactions { get; set; } = new List<SkillInteraction>();

		public IList<Animation> Animations { get; set; } = new List<Animation> ();
		
		public IList<HeadLocation> HeadLocations { get; set; } = new List<HeadLocation> ();
		public IList<ArmLocation> ArmLocations { get; set; } = new List<ArmLocation> ();
		
		public IDictionary<string,string> InteractionAnimations { get; set; } = new Dictionary<string, string>();
		public IDictionary<string,string> InteractionPreSpeechAnimations { get; set; } = new Dictionary<string, string>();





//TODO
		public IDictionary<string,string> InteractionListeningAnimations { get; set; } = new Dictionary<string, string>();
		public IDictionary<string,string> InteractionInitAnimations { get; set; } = new Dictionary<string, string>();

		
		public IDictionary<string,string> InteractionInitScripts { get; set; } = new Dictionary<string, string>();
		public IDictionary<string,string> InteractionPreSpeechScripts { get; set; } = new Dictionary<string, string>();
		public IDictionary<string,string> InteractionListeningScripts { get; set; } = new Dictionary<string, string>();
		public IDictionary<string,string> InteractionScripts { get; set; } = new Dictionary<string, string>();
		

		public IList<SkillMessage> SkillMessages { get; set; } = new List<SkillMessage> ();
		public IList<SkillMessage> AllowedUtterancess { get; set; } = new List<SkillMessage> ();

		public IList<TriggerDetail> Triggers { get; set; } = new List<TriggerDetail>();
		public IList<LEDTransitionAction> LEDTransitionActions { get; set; } = new List<LEDTransitionAction>();
		public IDictionary<string, IList<TriggerActionOption>> ConversationTriggerMap { get; set; } = new Dictionary<string, IList<TriggerActionOption>>();

		public IDictionary<string, EntryMap> ConversationEntryPoints { get; set; } = new Dictionary<string, EntryMap>();

		//key is trigger action option id
		public IDictionary<string, DepartureMap> ConversationDeparturePoints { get; set; } = new Dictionary<string, DepartureMap>();		
	}
}