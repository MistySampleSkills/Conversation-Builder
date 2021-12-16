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
using System.Threading;
using System.Threading.Tasks;
using Conversation.Common;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;

namespace Conversation.Common
{
	//TODO Move to common
	public interface IMistyState
	{
		//TODO Complete!
		event EventHandler<IFaceRecognitionEvent> FaceRecognitionEvent;
		event EventHandler<ICapTouchEvent> CapTouchEvent;
		event EventHandler<IBumpSensorEvent> BumperEvent;
		event EventHandler<IBatteryChargeEvent> BatteryChargeEvent;
		event EventHandler<IQrTagDetectionEvent> QrTagEvent;
		event EventHandler<IArTagDetectionEvent> ArTagEvent;
		event EventHandler<ITimeOfFlightEvent> TimeOfFlightEvent;
		event EventHandler<ISerialMessageEvent> SerialMessageEvent;
		event EventHandler<IActuatorEvent> LeftArmActuatorEvent;
		event EventHandler<IActuatorEvent> RightArmActuatorEvent;
		event EventHandler<IActuatorEvent> HeadPitchActuatorEvent;
		event EventHandler<IActuatorEvent> HeadYawActuatorEvent;
		event EventHandler<IActuatorEvent> HeadRollActuatorEvent;
		event EventHandler<IObjectDetectionEvent> ObjectEvent;
		event EventHandler<bool> KeyPhraseRecognitionOn;
		event EventHandler<IObjectDetectionEvent> PersonObjectEvent;
		event EventHandler<IObjectDetectionEvent> NonPersonObjectEvent;
		event EventHandler<IDriveEncoderEvent> DriveEncoder;

		event EventHandler<IUserEvent> ExternalEvent;
		event EventHandler<IUserEvent> SyncEvent;
		event EventHandler<IUserEvent> RobotCommand;

		event EventHandler<TriggerData> SpeechIntentEvent;
		event EventHandler<string> StartedSpeaking;
		event EventHandler<IAudioPlayCompleteEvent> StoppedSpeaking;
		event EventHandler<DateTime> StartedListening;
		event EventHandler<IVoiceRecordEvent> StoppedListening;		
		event EventHandler<IKeyPhraseRecognizedEvent> KeyPhraseRecognized;
		event EventHandler<IVoiceRecordEvent> CompletedProcessingVoice;
		event EventHandler<IVoiceRecordEvent> StartedProcessingVoice;
		
		void HandleSpeechIntentReceived(object sender, TriggerData triggerData);
		void HandleStartedSpeakingReceived(object sender, string triggerData);
		void HandleStoppedSpeakingReceived(object sender, IAudioPlayCompleteEvent triggerData);
		void HandleStartedListeningReceived(object sender, DateTime triggerData);
		void HandleStoppedListeningReceived(object sender, IVoiceRecordEvent triggerData);
		void HandleKeyPhraseRecognizedReceived(object sender, IKeyPhraseRecognizedEvent triggerData);
		void HandleCompletedProcessingVoiceReceived(object sender, IVoiceRecordEvent triggerData);
		void HandleStartedProcessingVoiceReceived(object sender, IVoiceRecordEvent triggerData);

		void HandleSyncEvent(object sender, IUserEvent userEvent);

		void HandleRobotCommand(object sender, IUserEvent userEvent);

		void HandleExternalEvent(object sender, IUserEvent userEvent);
		void HandleBatteryChargeEvent(object sender, IBatteryChargeEvent chargeEvent);

		//TODO Cleanup ownership of external events - BaseCharacter or MistyState
		event EventHandler<TriggerData> ValidTriggerReceived;
		event EventHandler<DateTime> ConversationStarted;
		event EventHandler<DateTime> ConversationEnded;
		event EventHandler<string> InteractionStarted;
		event EventHandler<string> InteractionEnded;

		void HandleValidTriggerReceived(object sender, TriggerData triggerData);
		void HandleConversationStarted(object sender, DateTime datetime);
		void HandleConversationEnded(object sender, DateTime datetime);
		void HandleInteractionStarted(object sender, string interactionName);
		void HandleInteractionEnded(object sender, string interactionName);

		Task<bool> Initialize();

		CharacterState GetCharacterState();
		void RegisterEvent(string trigger);
		void Dispose();

		//TODO
		//void UnregisterEvent(string trigger);
	}
}