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
using MistyRobotics.SDK.Events;

namespace Conversation.Common
{
	public class CharacterState
	{
		public CharacterState() { }

		public CharacterState(CharacterState state)
		{
			AnimationLED = state.AnimationLED;
			ArTagEvent = state.ArTagEvent;
			Audio = state.Audio;
			BatteryChargeEvent = state.BatteryChargeEvent;
			BumpEvent = state.BumpEvent;
			CapTouchEvent = state.CapTouchEvent;
			ExternalEvent = state.ExternalEvent;
			FaceRecognitionEvent = state.FaceRecognitionEvent;
			FlashLightOn = state.FlashLightOn;
			HeadPitchActuatorEvent = state.HeadPitchActuatorEvent;
			HeadRollActuatorEvent = state.HeadRollActuatorEvent;
			HeadYawActuatorEvent = state.HeadYawActuatorEvent;
			Image = state.Image;
			KeyPhraseRecognitionOn = state.KeyPhraseRecognitionOn;
			LatestTriggerMatched = state.LatestTriggerMatched;
			LatestTriggerChecked = state.LatestTriggerChecked;
			LeftArmActuatorEvent = state.LeftArmActuatorEvent;
			Listening = state.Listening;
			ObjectEvent = state.ObjectEvent;
			NonPersonObjectEvent = state.NonPersonObjectEvent;
			QrTagEvent = state.QrTagEvent;
			RightArmActuatorEvent = state.RightArmActuatorEvent;
			Saying = state.Saying;
			SerialMessageEvent = state.SerialMessageEvent;
			Speaking = state.Speaking;
			SpeechResponseEvent = state.SpeechResponseEvent;
			RightDistance = state.RightDistance;
			DriveEncoder = state.DriveEncoder;
			LeftDistance = state.LeftDistance;
			LeftVelocity = state.LeftVelocity;
			AnimationEmotion = state.AnimationEmotion;
			CurrentMood = state.CurrentMood;
		}

		//Commanded State Info
		public bool FlashLightOn { get; set; }
		public LEDTransitionAction AnimationLED { get; set; }
		public string Image { get; set; }
		public bool Speaking { get; set; }
		public string Saying { get; set; }
		public bool Listening { get; set; }		
		public bool KeyPhraseRecognitionOn { get; set; }
		public string Audio { get; set; }
		public bool Spoke { get; set; }
		public bool KnownFaceSeen { get; set; }
		public bool UnknownFaceSeen { get; set; }

		public string LastKnownFaceSeen { get; set; }

		public KeyValuePair<DateTime, TriggerData> LatestTriggerChecked { get; set; }
		public KeyValuePair<DateTime, TriggerData> LatestTriggerMatched { get; set; }

		//Last event of the type
		public TriggerData SpeechResponseEvent { get; set; }
		public KeyPhraseRecognizedEvent KeyPhraseRecognized { get; set; }
		public DriveEncoderEvent DriveEncoder { get; set; }
		public DriveEncoderEvent LeftVelocity { get; set; }
		public DriveEncoderEvent RightDistance { get; set; }
		public DriveEncoderEvent LeftDistance { get; set; }
		public QrTagDetectionEvent QrTagEvent { get; set; }
		public FaceRecognitionEvent FaceRecognitionEvent { get; set; }
		public BumpSensorEvent BumpEvent { get; set; }
		public CapTouchEvent CapTouchEvent { get; set; }
		public ArTagDetectionEvent ArTagEvent { get; set; }
		public SerialMessageEvent SerialMessageEvent { get; set; }
		public UserEvent ExternalEvent { get; set; }
		public ObjectDetectionEvent ObjectEvent { get; set; }
		public ObjectDetectionEvent NonPersonObjectEvent { get; set; }
		public BatteryChargeEvent BatteryChargeEvent { get; set; }
		public ActuatorEvent RightArmActuatorEvent { get; set; }
		public ActuatorEvent LeftArmActuatorEvent { get; set; }
		public ActuatorEvent HeadPitchActuatorEvent { get; set; }
		public ActuatorEvent HeadYawActuatorEvent { get; set; }
		public ActuatorEvent HeadRollActuatorEvent { get; set; }

		public string AnimationEmotion { get; set; } = Emotions.Calmness;
		public string CurrentMood { get; set; } = Emotions.Calmness;
	}
}