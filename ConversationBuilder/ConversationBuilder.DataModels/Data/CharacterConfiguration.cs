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
using Newtonsoft.Json;

namespace ConversationBuilder.DataModels
{
	public sealed class CharacterConfiguration : IEditableData
	{
		public string Id { get; set; }

		[Required]
		public string Name { get; set; }
		public string ItemType { get; set; } = DataItemType.CharacterConfiguration.ToString();

		[Display(Name = "Speech configuration to use in conversation")]		
		public string SpeechConfiguration { get; set; }

		[Display(Name = "Face pitch offset, used to help align Misty for follow face")]		
		public int FacePitchOffset { get; set; }

		[Display(Name = "Object Detection Debounce - affects follow speeds and performance")]		
		public double ObjectDetectionDebounce { get; set; } = 0.333;
		
		[Required]
		[Display(Name = "Log Level")]		
		public string LogLevel { get; set; } = LogLevels.Info;

		[Display(Name = "Object Confidence")]
		public double PersonConfidence { get; set; } = 0.6;
		
		[Display(Name = "Log Interaction, check to log a lot of data for testing (beta)")]
		public bool LogInteraction { get; set; }
		
		[Display(Name = "Stream Interaction, check to publish conversation checkpoints to websockets (beta)")]
		public bool StreamInteraction { get; set; }
		
		[Display(Name = "Display heard speech on the screen (beta)")]
		public bool HeardSpeechToScreen { get; set; }
		
		[Display(Name = "Display spoken words on screen (beta)")]
		public bool DisplaySpoken { get; set; }		
		
		[Display(Name = "Large Print, check to show the words Misty says in large print (beta)")]
		public bool LargePrint { get; set; }
		
		[Display(Name = "Show Listening Indicator, check to show the listening/speaking indicator on the screen")]
		public bool ShowListeningIndicator { get; set; } = true;

		[Display(Name = "Starting volume of Misty")]
		public int? StartVolume { get; set; }		

		[Display(Name = "Use prespeech for speech intent processing")]
		public bool UsePreSpeech { get; set; }

		[Display(Name = "PreSpeech Phrases")]
		public string PreSpeechPhrases { get; set; }

		[Display(Name = "Only change if using a different, home-made Misty Conversation Skill")]
		public string Skill { get; set; }
		
		[Display(Name = "Character template to use (beta)")]
		public string Character { get; set; }
		
		[Display(Name = "Optional Character Payload to send to trigger skills")]
		public string Payload { get; set; }
		
		public DateTimeOffset Created { get; set; }
		
		public DateTimeOffset Updated { get; set; }
		
		[Required]
		[Display(Name = "Management Access (beta)")]
		public string ManagementAccess { get; set; } = "Public";
		public string CreatedBy { get; set; }
	}
}