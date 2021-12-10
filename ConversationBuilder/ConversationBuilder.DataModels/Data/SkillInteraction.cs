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
	public class SkillInteraction
	{
		public string Id { get; set; }

		public string Name { get; set; }

		public string Animation { get; set; }
		
		public IDictionary<string, IList<TriggerActionOption>> TriggerMap { get; set; } = new Dictionary<string, IList<TriggerActionOption>>();

		public bool StartListening { get; set; } = false;

		public bool AllowKeyPhraseRecognition { get; set; }

		public bool AllowConversationTriggers { get; set; } = true;

		public bool AllowVoiceProcessingOverride { get; set; } = true;

		public double ListenTimeout { get; set; } = 6;

		public double SilenceTimeout { get; set; } = 6;

		public double InteractionFailedTimeout { get; set; } = 120; //2 minutes with no trigger response

		public string ConversationId { get; set; }

		public bool ConversationEntryPoint { get; set; }

		public IList<string> SkillMessages { get; set; } = new List<string>();
		
		public IList<string> AllowedUtterances { get; set; } = new List<string>();
		
		public bool UsePreSpeech { get; set; } = true;
		
		public string PreSpeechPhrases { get; set; }

		public string PreSpeechAnimation { get; set; }



		public string InitAnimation { get; set; }
		public string ListeningAnimation { get; set; }
		public string InitScript { get; set; }
		public string AnimationScript { get; set; }
		public string PreSpeechScript { get; set; }
		public string ListeningScript { get; set; }
		
	}
}