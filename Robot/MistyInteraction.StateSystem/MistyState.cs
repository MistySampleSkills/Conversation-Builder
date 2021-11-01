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
using MistyInteraction.Common;
using MistyRobotics.SDK.Events;

namespace MistyInteraction.StateSystem
{
	public class MistyState
	{
		//Commanded State Info
		public bool FlashLightOn { get; set; }
		public LEDTransitionAction LED { get; set; }
		public string Image { get; set; }
		public bool Speaking { get; set; }
		public string LastSaid { get; set; }
		public bool Listening { get; set; }
		public bool KeyPhraseRecognitionOn { get; set; }
		public bool ProcessingSpeech { get; set; }
		public int Volume { get; set; }

		//Event based state info
		public double HeadPitch { get; set; }
		public double HeadRoll { get; set; }
		public double HeadYaw { get; set; }

		public double RightArm { get; set; }
		public double LeftArm { get; set; }

		public double RobotPitch { get; set; }
		public double RobotRoll { get; set; }
		public double RobotYaw { get; set; }

		public double RobotPitchVelocity { get; set; }
		public double RobotRollVelocity { get; set; }
		public double RobotYawVelocity { get; set; }

		public double RobotXAcceleration { get; set; }
		public double RobotYAcceleration { get; set; }
		public double RobotZAcceleration { get; set; }

		public bool Chin { get; set; }
		public bool Scruff { get; set; }
		public bool FrontCap { get; set; }
		public bool BackCap { get; set; }
		public bool RightCap { get; set; }
		public bool LeftCap { get; set; }

		public bool FrontRightBumper { get; set; }
		public bool FrontLeftBumper { get; set; }
		public bool BackRightBumper { get; set; }
		public bool BackLeftBumper { get; set; }

		public string LastUserEventName { get; set; }

		public string LastFaceSeen { get; set; }

		public string LastObjectSeen { get; set; }

		public int LastArTagSeen { get; set; }
		public string LastQrTagSeen { get; set; }
		public string LastSerialMessageReceived { get; set; }

		public string LastHeard { get; set; }

		public string LastAudioPlayed { get; set; }

		public double? ChargePercent { get; set; }
		public bool IsCharging { get; set; }

		//General data
		public bool RepeatingAnimationScript { get; set; }
		public string AnimationScriptActionsComplete { get; set; }
		public string StartedAnimationScript { get; set; }
		public string CompletedAnimationScript { get; set; }
		public string Status { get; set; }

		public string RobotName { get; set; }
		public string CurrentInteraction { get; set; }
		public string CurrentAnimation { get; set; }
		public string DemoName { get; set; }
		public DateTime DemoStarted { get; set; }
		public string RoleName { get; set; }
		public string GroupName { get; set; }

		//Specific last event info		
		public TriggerData SpeechResponseEvent { get; set; }

		public KeyPhraseRecognizedEvent KeyPhraseRecognized { get; set; }

		public IMUEvent IMUEvent { get; set; }

		public FaceRecognitionEvent FaceRecognitionEvent { get; set; }

		public CapTouchEvent CapTouchEvent { get; set; }

		public BumpSensorEvent BumperEvent { get; set; }

		public UserEvent ExternalEvent { get; set; }

		public ObjectDetectionEvent ObjectEvent { get; set; }
		public BatteryChargeEvent BatteryChargeEvent { get; set; }

		public ActuatorEvent RightArmActuatorEvent { get; set; }
		public ActuatorEvent LeftArmActuatorEvent { get; set; }

		public ActuatorEvent HeadPitchActuatorEvent { get; set; }
		public ActuatorEvent HeadYawActuatorEvent { get; set; }
		public ActuatorEvent HeadRollActuatorEvent { get; set; }

		public VoiceRecordEvent VoiceRecordEvent { get; set; }
		public AudioPlayCompleteEvent AudioPlayCompleteEvent { get; set; }
		public KeyPhraseRecognizedEvent KeyPhraseRecognizedEvent { get; set; }

		public ArTagDetectionEvent ArTagEvent { get; set; }
		public QrTagDetectionEvent QrTagEvent { get; set; }
		public SerialMessageEvent SerialMessageEvent { get; set; }

		//Move out of this object so can fine tune?
		public LocomotionState LocomotionState { get; set; }
		public LocomotionAction LastLocomotionAction { get; set; }
		public LocomotionAction RunningLocomotionAction { get; set; }
	}
}