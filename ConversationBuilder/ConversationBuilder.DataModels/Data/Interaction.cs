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
	public class Interaction : IEditableData
	{
		public string Id { get; set; }

		public string ConversationId { get; set; }

		public string ItemType { get; set; } = DataItemType.Interaction.ToString();

		public string Name { get; set; }

		[Display(Name = "Default Animation")]
		public string Animation { get; set; }

		public IList<string> SkillMessages { get; set; } = new List<string>();
		public IDictionary<string, IList<TriggerActionOption>> TriggerMap { get; set; } = new Dictionary<string, IList<TriggerActionOption>>();
		
		
		[Display(Name = "Interaction timeout (seconds)")]
		public double InteractionFailedTimeout { get; set; } = 120; //2 minutes with no trigger response

		
		[Display(Name = "Listen to speaker timeout")]
		[JsonProperty(PropertyName = "ListenTimeout")]
		/// <summary>
		/// The max length a person can talk after listening starts
		/// Max is 10000ms (10 seconds) at this time
		/// </summary>
		public double ListenTimeout { get; set; } = 6;

		[Display(Name = "Wait for speaking silence timeout in seconds")]
		[JsonProperty(PropertyName = "SilenceTimeout")]
		/// <summary>
		/// How long to wait through silence before ending listening
		/// Max is 10000ms (10 seconds) at this time
		/// </summary>
		public double SilenceTimeout { get; set; } = 6;

		[Display(Name = "Start Listening immediately after speech/audio in seconds")]
		[JsonProperty(PropertyName = "StartListening")]
		/// <summary>
		/// If Start Listening is set to true, starts capturing speech after she speaks/plays audio
		/// This also gets set if the interaction anticipates a speech intent
		/// </summary>
		public bool StartListening { get; set; } = true;

		[Display(Name = "Allow Key Phrase Recognition")]
		[JsonProperty(PropertyName = "AllowKeyPhraseRecognition")]		
		/// <summary>
		/// Will update the conversation based Key Phrase rec if set to true
		/// </summary>
		public bool AllowKeyPhraseRecognition { get; set; }

		[Display(Name = "Allow Conversation Triggers")]
		[JsonProperty(PropertyName = "AllowConversationTriggers")]		
		/// <summary>
		/// Allows the conversation trigger handling to be checked if the interaction doesn't handle a trigger
		/// </summary>
		public bool AllowConversationTriggers { get; set; } = true;

		[Display(Name = "Allow VoiceProcessing Override")]
		[JsonProperty(PropertyName = "AllowVoiceProcessingOverride")]		
		/// <summary>
		/// If true, will interrupt speech processing to throw timeout events when they happen
		/// otherwise, will pause to see if voice processes a successful trigger before throwing timeout
		/// Currently experimental
		/// </summary>
		public bool AllowVoiceProcessingOverride { get; set; } = true;

		[Display(Name = "Conversation Entry Point")]
		public bool ConversationEntryPoint { get; set; }

		public DateTimeOffset Created { get; set; }
		
		public DateTimeOffset Updated { get; set; }
		public string CreatedBy { get; set; }
	}
}