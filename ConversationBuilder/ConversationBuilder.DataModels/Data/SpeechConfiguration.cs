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
using System.ComponentModel.DataAnnotations;

namespace ConversationBuilder.DataModels
{
	/// <summary>
	/// Speech services information
	/// </summary>
	public sealed class SpeechConfiguration : IEditableData
	{
		public string Id { get; set; }

		[Required]
		public string Name { get; set; }

		public string ItemType { get; set; } = DataItemType.SpeechConfiguration.ToString();

		[Display(Name = "Speech Recognition Subscription Key")]
		public string SpeechRecognitionSubscriptionKey { get; set; }

		[Display(Name = "Text To Speech Subscription Key")]
		public string TextToSpeechSubscriptionKey { get; set; }

		[Required]
		[Display(Name = "Speech Recognition Service")]
		public string SpeechRecognitionService { get; set; } = "Azure";
		
		[Required]
		[Display(Name = "Text To Speech Service")]
		public string TextToSpeechService { get; set; } = "Misty";

		/// <summary>
		/// Speaking language, the language the speaker is using
		/// </summary>
		[Display(Name = "Spoken Language of the person")]
		public string SpokenLanguage { get; set; }

		/// <summary>
		/// Speaking voice, works better if proper voice for translated language
		/// </summary>		
		[Display(Name = "Misty's Speaking Voice")]
		public string SpeakingVoice { get; set; }

		/// <summary>
		/// Text to speech Endpoint
		/// </summary>		
		[Display(Name = "Text To Speech Endpoint")]
		public string TextToSpeechEndpoint { get; set; }

		/// <summary>
		/// Speech To Text Endpoint
		/// </summary>		
		[Display(Name = "Speech Recognition Endpoint")]
		public string SpeechRecognitionEndpoint { get; set; }
		
		/// <summary>
		/// Azure Cognitive Region
		/// </summary>
		[Display(Name = "Speech Recognition Region (Azure)")]
		public string SpeechRecognitionRegion { get; set; }

		/// <summary>
		/// <summary>
		/// Azure Cognitive Region
		/// </summary>
		[Display(Name = "Text to Speech Region (Azure)")]
		public string TextToSpeechRegion { get; set; }

		/// <summary>
		/// Google Speaking gender
		/// </summary>
		[Display(Name = "SpeakingGender (Google)")]
		public string SpeakingGender { get; set; }

		/// <summary>
		/// Translated language, works better if proper language for speaking voice
		/// Currently only available for azure
		/// </summary>
		[Display(Name = "Translated Language (Azure)")]
		public string TranslatedLanguage { get; set; }

		/// <summary>
		/// Azure Setting to allow profanity
		/// 
		/// </summary>
		[Display(Name = "Profanity Setting (Azure)")]
		public string ProfanitySetting { get; set; }
		
		public DateTimeOffset Created { get; set; }
		
		public DateTimeOffset Updated { get; set; }

				
		[Display(Name = "Management Access (beta)")]
		public string ManagementAccess { get; set; } = "Public";
		public string CreatedBy { get; set; }
	}
}