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
using System.Threading.Tasks;
using Conversation.Common;
using MistyRobotics.SDK.Events;

namespace MistyCharacter
{
	public interface IBaseCharacter
	{
		/// <summary>
		/// Triggered when a conversation is started
		/// </summary>
		event EventHandler<DateTime> ConversationStarted;

		/// <summary>
		/// Triggered when a conversation is ended
		/// </summary>
		event EventHandler<DateTime> ConversationEnded;

		/// <summary>
		/// Triggered when an interaction is started
		/// </summary>
		event EventHandler<DateTime> InteractionStarted;

		/// <summary>
		/// Triggered when an interaction is ended
		/// </summary>
		event EventHandler<DateTime> InteractionEnded;

		/// <summary>
		/// Triggered when voice data has been processed
		/// </summary>
		event EventHandler<IVoiceRecordEvent> CompletedProcessingVoice;
		
		/// <summary>
		/// Triggered when voice data processing has started
		/// </summary>
		event EventHandler<IVoiceRecordEvent> StartedProcessingVoice;

		/// <summary>
		/// Triggered when any face recognition event occurs
		/// It may or may not be handled as a trigger depending on the interaction settings
		/// </summary>
		event EventHandler<IFaceRecognitionEvent> FaceRecognitionEvent;

		/// <summary>
		/// Triggered when any cap tuch event occurs
		/// It may or may not be handled as a trigger depending on the interaction settings
		/// </summary>
		event EventHandler<ICapTouchEvent> CapTouchEvent;

		/// <summary>
		/// Triggered when any bumper event occurs
		/// It may or may not be handled as a trigger depending on the interaction settings
		/// </summary>
		event EventHandler<IBumpSensorEvent> BumperEvent;

		/// <summary>
		/// Triggered when any battery charge event occurs
		/// </summary>
		event EventHandler<IBatteryChargeEvent> BatteryChargeEvent;

		/// <summary>
		/// Triggered when any QR Tag event occurs
		/// It may or may not be handled as a trigger depending on the interaction settings
		/// </summary>
		event EventHandler<IQrTagDetectionEvent> QrTagEvent;

		/// <summary>
		/// Triggered when any AR Tag event occurs
		/// It may or may not be handled as a trigger depending on the interaction settings
		/// </summary>
		event EventHandler<IArTagDetectionEvent> ArTagEvent;
		
		/// <summary>
		/// Triggered when a TOF event occurs
		/// It may or may not be handled as a trigger depending on the interaction settings
		/// </summary>
		event EventHandler<ITimeOfFlightEvent> TimeOfFlightEvent;

		/// <summary>
		/// Triggered when any serial messge event occurs
		/// It may or may not be handled as a trigger depending on the interaction settings
		/// </summary>
		event EventHandler<ISerialMessageEvent> SerialMessageEvent;

		/// <summary>
		/// Triggered when any external event occurs
		/// It may or may not be handled as a trigger depending on the interaction settings
		/// </summary>
		event EventHandler<IUserEvent> ExternalEvent;

		/// <summary>
		/// Triggered when any object event occurs
		/// It may or may not be handled as a trigger depending on the interaction settings
		/// </summary>
		event EventHandler<IObjectDetectionEvent> ObjectEvent;

		/// <summary>
		/// Triggered when any speech intent event occurs
		/// It may or may not be handled as a trigger depending on the interaction settings
		/// </summary>
		event EventHandler<TriggerData> SpeechIntentEvent;

		/// <summary>
		/// Triggered when an intent is handled
		/// </summary>
		event EventHandler<TriggerData> ResponseEventReceived;

		/// <summary>
		/// Triggered when any face recognition event occurs
		/// It may or may not be handled as a trigger depending on the interaction settings
		/// </summary>
		event EventHandler<string> StartedSpeaking;

		/// <summary>
		/// Triggered when any face recognition event occurs
		/// </summary>
		event EventHandler<IAudioPlayCompleteEvent> StoppedSpeaking;

		/// <summary>
		/// Triggered when any face recognition event occurs
		/// </summary>
		event EventHandler<DateTime> StartedListening;

		/// <summary>
		/// Triggered when any face recognition event occurs
		/// </summary>
		event EventHandler<IVoiceRecordEvent> StoppedListening;

		/// <summary>
		/// Triggered when any face recognition event occurs
		/// </summary>
		event EventHandler<bool> KeyPhraseRecognitionOn;

		/// <summary>
		/// Current state of robot and character
		/// </summary>
		CharacterState CharacterState { get; }

		/// <summary>
		/// The state of robot and character at the end of the last interaction
		/// </summary>
		CharacterState PreviousState { get; }

		/// <summary>
		/// The state of robot and character at the start of this interaction
		/// </summary>
		CharacterState StateAtAnimationStart { get; }

		/// <summary>
		/// The current interaction being performed
		/// </summary>
		Interaction CurrentInteraction { get; }
		
		/// <summary>
		/// If we are currently waiting for override intent
		/// </summary>
		bool WaitingForOverrideTrigger { get; }		

		/// <summary>
		/// Restarts normal intent handling for conversation
		/// Be aware that if all the intents have passed now, you may need to trigger an intent manually
		/// </summary>
		void RestartTriggerHandling();

		/// <summary>
		/// Stop normal intent handling
		/// </summary>
		/// <param name="ignoreTriggeringEvents">if true, also ignores the triggering of new events and stopping of old</param>
		void PauseTriggerHandling(bool ignoreTriggeringEvents = true);

		/// <summary>
		/// Restart this interaction
		/// </summary>
		void RestartCurrentInteraction(bool interruptAudio = true);

		/// <summary>
		/// Simulate your own event
		/// </summary>
		/// <param name="responseEvent">the simluated trigger</param>
		/// <param name="setAsOverrideEvent">if not true, will only trigger intent starting and stopping if intent handling paused</param>
		void SimulateTrigger(TriggerData responseEvent, bool setAsOverrideEvent = true);

		/// <summary>
		/// Change Misty's default volume
		/// </summary>
		/// <param name="volume"></param>
		void ChangeVolume(int volume);
		
		/// <summary>
		/// Refresh image and audio asset lists for the easy access asset lists
		/// Should only be necessary if new assets are added to the robot while the skill is running
		/// </summary>
		/// <returns></returns>
		Task RefreshAssetLists();

		/// <summary>
		/// Start a conversation
		/// </summary>
		/// <param name="conversationId"></param>
		/// <param name="interactionId"></param>
		/// <returns></returns>
		Task<bool> StartConversation(string conversationId = null, string interactionId = null);

		/// <summary>
		/// Stop the conversation
		/// </summary>
		/// <param name="speak"></param>
		void StopConversation(string speak = null);

		/// <summary>
		/// Must be called after character creation, before use.
		/// </summary>
		/// <returns></returns>
		Task<bool> Initialize();

		/// <summary>
		/// Dispose of the character to prevent background tasks from continuing after cancellation
		/// </summary>
		void Dispose();
	}
}
 