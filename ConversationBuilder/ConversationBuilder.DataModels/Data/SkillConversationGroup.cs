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
	public class SkillConversationGroup
	{
		/// <summary>
		/// Unique id for mapping
		/// </summary>
		public string Id { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }
		
		public string RobotName { get; set; } //use for talking about self and Hey commands if using azure

		public string StartupConversation { get; set; }

		public string KeyPhraseRecognizedAudio { get; set; }

		public string CharacterConfiguration { get; set; }

		public IList<SkillAuthorization> SkillAuthorizations { get; set; } = new List<SkillAuthorization>();
		public IList<SkillMessage> SkillMessages { get; set; } = new List<SkillMessage>();
		public IList<GenericDataStore> GenericDataStores { get; set; } = new List<GenericDataStore> ();
			
		public IList<SkillConversation> Conversations { get; set; } = new List<SkillConversation>();

		public IDictionary<string, UtteranceData> IntentUtterances = new Dictionary<string, UtteranceData>();
		
		public IDictionary<string, ConversationMappingDetail> ConversationMappings { get; set; } = new Dictionary<string, ConversationMappingDetail>();

		public bool AnimationCreationMode { get; set; }
		public double AnimationCreationDebounceSeconds { get; set; } = .25;
		public bool IgnoreArmCommands { get; set; }

		public bool CreateJavaScriptTemplate { get; set; }
		public bool IgnoreHeadCommands { get; set; }
		public bool RetranslateTTS { get; set; }
		public bool SmoothRecording { get; set; }
		public string PuppetingList { get; set; }
	}

}
