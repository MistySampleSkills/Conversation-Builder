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
			TimeOfFlightEvent = state.TimeOfFlightEvent;
			Audio = state.Audio;
			BatteryChargeEvent = state.BatteryChargeEvent;
			Scruff = state.Scruff;
			Chin = state.Chin;
			FrontCap = state.FrontCap;
			BackCap = state.BackCap;
			LeftCap = state.LeftCap;
			RightCap = state.RightCap;
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
			LastTrigger = state.LastTrigger;
			LastSaid = state.LastSaid;
			LastHeard = state.LastHeard;
			DisplayedScreenText = state.DisplayedScreenText;
			LeftArmActuatorEvent = state.LeftArmActuatorEvent;
			Listening = state.Listening;
			ObjectEvent = state.ObjectEvent;
			NonPersonObjectEvent = state.NonPersonObjectEvent;
			QrTagEvent = state.QrTagEvent;
			RightArmActuatorEvent = state.RightArmActuatorEvent;
			Saying = state.Saying;
			SerialMessageEvent = state.SerialMessageEvent;
			Speaking = state.Speaking;
			AnimationEmotion = state.AnimationEmotion;
			CurrentMood = state.CurrentMood;
			
			LocomotionState = state.LocomotionState;
		}

		//Commanded State Info
		//TODO Pull out Locomotion state?
		public LocomotionState LocomotionState { get; set; } = new LocomotionState();

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
		public string LastTrigger { get; set; }
		
		public string LastSaid { get; set; }
		public string LastHeard { get; set; }
		public string DisplayedScreenText { get; set; }


		public KeyValuePair<DateTime, TriggerData> LatestTriggerChecked { get; set; }

		private KeyValuePair<DateTime, TriggerData> _lastTriggerMatchKVP;
		public KeyValuePair<DateTime, TriggerData> LatestTriggerMatched
		{
			get
			{
				return _lastTriggerMatchKVP;
			}

			set
			{
				_lastTriggerMatchKVP = value;
				LastTrigger = $"{_lastTriggerMatchKVP.Value.Trigger}:{_lastTriggerMatchKVP.Value.TriggerFilter}";
			}
		}

		//Last event of the type
		public TriggerData SpeechResponseEvent { get; set; } = new TriggerData("", "", "");
		public KeyPhraseRecognizedEvent KeyPhraseRecognized { get; set; } = new KeyPhraseRecognizedEvent();

		public QrTagDetectionEvent QrTagEvent { get; set; } = new QrTagDetectionEvent();
		public FaceRecognitionEvent FaceRecognitionEvent { get; set; } = new FaceRecognitionEvent();

		public CapTouchEvent Scruff { get; set; } = new CapTouchEvent();
		public CapTouchEvent Chin { get; set; } = new CapTouchEvent();
		public CapTouchEvent FrontCap{ get; set; } = new CapTouchEvent();
		public CapTouchEvent BackCap { get; set; } = new CapTouchEvent();
		public CapTouchEvent LeftCap { get; set; } = new CapTouchEvent();
		public CapTouchEvent RightCap { get; set; } = new CapTouchEvent();

		public ArTagDetectionEvent ArTagEvent { get; set; } = new ArTagDetectionEvent();
		public TimeOfFlightEvent TimeOfFlightEvent { get; set; } = new TimeOfFlightEvent();
		public SerialMessageEvent SerialMessageEvent { get; set; } = new SerialMessageEvent();
		public UserEvent ExternalEvent { get; set; } = new UserEvent();
		public ObjectDetectionEvent ObjectEvent { get; set; } = new ObjectDetectionEvent();
		public ObjectDetectionEvent NonPersonObjectEvent { get; set; } = new ObjectDetectionEvent();
		public BatteryChargeEvent BatteryChargeEvent { get; set; } = new BatteryChargeEvent();
		public ActuatorEvent RightArmActuatorEvent { get; set; } = new ActuatorEvent();
		public ActuatorEvent LeftArmActuatorEvent { get; set; } = new ActuatorEvent();
		public ActuatorEvent HeadPitchActuatorEvent { get; set; } = new ActuatorEvent();
		public ActuatorEvent HeadYawActuatorEvent { get; set; } = new ActuatorEvent();
		public ActuatorEvent HeadRollActuatorEvent { get; set; } = new ActuatorEvent();

		public string AnimationEmotion { get; set; } = Emotions.Calmness;
		public string CurrentMood { get; set; } = Emotions.Calmness;
	}
}