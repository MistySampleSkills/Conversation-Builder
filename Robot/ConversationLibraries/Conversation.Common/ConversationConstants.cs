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

namespace Conversation.Common
{
	public class ConversationConstants
	{
		//Speech data keys
		public readonly static string SpeechRegion = "SpeechRegion";
		public readonly static string SpeechEndpoint = "SpeechEndpoint";
		public readonly static string SpeakingVoice = "SpeakingVoice";
		public readonly static string TranslatedLanguage = "TranslatedLanguage";
		public readonly static string SpokenLanguage = "SpokenLanguage";
		public readonly static string ProfanitySetting = "ProfanitySetting";
		public readonly static string SpeechRecognitionService = "SpeechRecognitionService";
		public readonly static string TextToSpeechService = "TextToSpeechService";
		public readonly static string SpeakingGender = "SpeakingGender";
		public readonly static string RetranslateTTS = "RetranslateTTS";
		public readonly static string RecognizeKeyPhrase = "RecognizeKeyPhrase";
		public readonly static string AzureSpeechSettings = "AzureSpeechSettings";
		public readonly static string GoogleSpeechSettings = "GoogleSpeechSettings";
		public readonly static string SpeechConfiguration = "SpeechConfiguration";
		
		//Follow Face Keys
		public readonly static string FollowFaceDebounce = "FollowFaceDebounce";
		public readonly static string FacePitchOffset = "FacePitchOffset";
		
		//TODO Face or OD here?
		public readonly static string PersonConfidence = "PersonConfidence";
		public readonly static string TrackHistory = "TrackHistory";
		public readonly static string ObjectDetectionDebounce = "ObjectDetectionDebounce";

		//CapTouch triggers
		public readonly static string ChinTouchTrigger = "Chin";
		public readonly static string ScruffTouchTrigger = "Scruff";
		public readonly static string RightCapTouchTrigger = "RightCap";
		public readonly static string LeftCapTouchTrigger = "LeftCap";
		public readonly static string BackCapTouchTrigger = "BackCap";
		public readonly static string FrontCapTouchTrigger = "FrontCap";
		public readonly static string AnyCapTouched = "";
		
		//Face triggers
		public readonly static string SeeKnownFaceTrigger = "SeenKnownFace";
		public readonly static string SeeUnknownFaceTrigger = "SeenUnknownFace";
		public readonly static string SeeNewFaceTrigger = "SeenNewFace";
		public readonly static string SeeAnyFaceTrigger = "";

		//Sound triggers
		public readonly static string HeardUnknownTrigger = "HeardUnknownSpeech";
		public readonly static string HeardKnownTrigger = "HeardKnownIntent";
		public readonly static string HeardNothingTrigger = "HeardNothing";
		public readonly static string HeardAnythingTrigger = "";

		//Conversation keys
		public readonly static string ConversationGroupData = "ConversationGroupData";
		public readonly static string CharacterName = "CharacterName";

		//General keys
		public readonly static string LogLevel = "LogLevel";
		public readonly static string UnknownPersonFaceLabel = "unknown person";
		public readonly static string ConversationGroup = "ConversationGroup";
		public readonly static string Payload = "Payload";		
		public readonly static string IgnoreCallback = "ignorecallback_";
		public readonly static string ReloadAssets = "ReloadAssets";
		public readonly static string CharacterSkillParameters = "CharacterSkillParameters";
		public readonly static string AddLocaleToAudioNames = "AddLocaleToAudioNames";
		public readonly static string StreamInteraction = "StreamInteraction";
		public readonly static string LogInteraction = "LogInteraction";
		public readonly static string InterruptAudio = "InterruptAudio";
		public readonly static string Character = "Character";
		public readonly static string HeardSpeechToScreen = "HeardSpeechToScreen";
		public readonly static string DisplaySpoken = "DisplaySpoken"; 
		public readonly static string StartVolume = "StartVolume";
		public readonly static string LargePrint = "LargePrint";
		public readonly static string ShowListeningIndicator = "ShowListeningIndicator";
		public readonly static string ShowSpeakingIndicator = "ShowSpeakingIndicator";
		public readonly static string SendInteractionUIEvents = "SendInteractionUIEvents";
		public readonly static string UsePreSpeech = "UsePreSpeech";
		public readonly static string PreSpeechPhrases = "PreSpeechPhrases";
		public readonly static string PreSpeechAnimation = "PreSpeechAnimation";
		public readonly static string SpeakingImage = "SpeakingImage";
		public readonly static string ProcessingImage = "ProcessingImage";
		public readonly static string ListeningImage = "ListeningImage";		
	}
}