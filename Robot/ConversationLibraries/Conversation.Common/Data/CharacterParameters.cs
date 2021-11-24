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

using MistyRobotics.Common.Types;
using System.Collections.Generic;

namespace Conversation.Common
{
	public enum InitializationStatus
	{
		Unknown,
		Warning,
		Error,
		Waiting,
		Success
	}

	public sealed class CharacterParameters
	{
		public AzureSpeechParameters AzureSpeechRecognitionParameters { get; set; }

		public GoogleSpeechParameters GoogleSpeechRecognitionParameters { get; set; }

		public AzureSpeechParameters AzureTTSParameters { get; set; }

		public GoogleSpeechParameters GoogleTTSParameters { get; set; }
		
		public SpeechConfiguration SpeechConfiguration { get; set; }
		
		public string TextToSpeechService { get; set; } = "misty";
		public string SpeechRecognitionService { get; set; } = "vosk";
		
		public int FacePitchOffset { get; set; }
		public double ObjectDetectionDebounce { get; set; } = 0.333;

		public bool AddLocaleToAudioNames { get; set; } = true;

		public SkillLogLevel LogLevel { get; set; } = SkillLogLevel.Warning;

		public double PersonConfidence { get; set; } = 0.6;
		public int TrackHistory { get; set; } = 2;

		public ConversationGroup ConversationGroup { get; set; }

		public bool LogInteraction { get; set; }
		public bool StreamInteraction { get; set; }
		
		public bool ShowListeningIndicator { get; set; } = true;

		public bool ShowSpeakingIndicator { get; set; } = true;
		public bool SendInteractionUIEvents { get; set; } = true;
		public bool HeardSpeechToScreen { get; set; }
		public bool DisplaySpoken { get; set; }
		public bool LargePrint { get; set; }
		
		public int? StartVolume { get; set; }

		public string Payload { get; set; }

		public string InitializationStatusMessage { get; set; }

		public InitializationStatus InitializationErrorStatus { get; set; }

		public string RobotIp { get; set; }

		public string SpeakingImage { get; set; }
		public string ListeningImage { get; set; }
		public string ProcessingImage { get; set; }

		public bool UsePreSpeech { get; set; }


		public IList<string> PreSpeechList { get; set; }

		private string _phrases;
		public string PreSpeechPhrases
		{
			get
			{
				return _phrases;
			}
			set
			{
				_phrases = value;
				PreSpeechList = _phrases.Split(";");
			}
		}
		
		public IList<Robot> Robots { get; set; } = new List<Robot>();

		//TODO
		public IList<Recipe> Recipes { get; set; } = new List<Recipe>();

		public bool AnimationCreationMode { get; set; } = false;
		public double AnimationCreationDebounceSeconds { get; set; } = .25;
		public bool IgnoreArmCommands { get; set; } = false;
		public bool IgnoreHeadCommands { get; set; } = false;

		public bool RetranslateTTS { get; set; }
		public bool SmoothRecording { get; set; } = false;

		public string PuppetingList { get; set; }
	}
}