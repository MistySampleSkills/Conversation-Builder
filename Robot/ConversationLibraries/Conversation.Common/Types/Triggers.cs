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
    /// <summary>
    /// Default trigger strings
    /// Matches the strings from Conversation builder UI
    /// </summary>
	public static class Triggers
	{
		//Should only be used for Stop Trigger event
		public const string None = "None";

		//Timed out with no successful (unhandled Unknowns) triggers
		//Can be handled, not the same as Interaction timeout which goes to the No response interaction or ends the conversation
		public const string Timeout = "Timeout";

		//User defined timer event, can be used for starting and stopping other events based upon time within the interaction
		public const string Timer = "Timer";

		//Common trigger types caused by interaction with robot
		public const string SpeechHeard = "SpeechHeard";
		public const string FaceRecognized = "FaceRecognized";
		public const string BumperPressed = "BumperPressed";
		public const string CapTouched = "CapTouched";
		public const string BumperReleased = "BumperReleased";
		public const string CapReleased = "CapReleased";
		public const string QrTagSeen = "QrTagSeen";
		public const string ArTagSeen = "ArTagSeen";
		public const string SerialMessage = "SerialMessage";
		public const string ObjectSeen = "ObjectSeen";
		public const string KeyPhraseRecognized = "KeyPhraseRecognized";

		//Trigger due to external event call into robot skill
		public const string ExternalEvent = "ExternalEvent";

		//To immediately trigger a start or stop or go to next animation after Misty speaks or plays audio
		public const string AudioCompleted = "AudioCompleted";

		public const string SyncEvent = "SyncEvent";

		public const string TimeOfFlightRange = "TimeOfFlightRange";		
	}	
}