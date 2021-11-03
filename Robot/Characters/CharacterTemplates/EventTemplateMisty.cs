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
using Conversation.Common;
using MistyCharacter;
using MistyRobotics.SDK.Messengers;

namespace MistyConversation
{
    /// <summary>
    /// Character with optional event listening example built off of the BaseCharacter
    /// Shows most of the events available to a developer
    /// Experimental, concepts may change or be deprecated
    /// </summary>
    public class EventTemplateMisty : BaseCharacter
	{
		public EventTemplateMisty(IRobotMessenger misty, IDictionary<string, object> originalParameters, ManagerConfiguration managerConfiguration = null)
			: base(misty, originalParameters,
				  new ManagerConfiguration  //update managers as desired
				(
					null,
					null,
					null,
					null,
					null
				 ))
		{
			RegisterOptionalEvents();
			UpdateEmotionDefaults();
		}

		#region Example of updating default emotions

		/// <summary>
		/// Override the emotion defaults for this character if desired
		/// </summary>
		private void UpdateEmotionDefaults()
		{
			//TODO Remove and reset as desired, or create a whole new Emotion Manager
			foreach(KeyValuePair<string, AnimationRequest> emotionanimation in EmotionAnimations)
			{
				//do what ya want
			}
		}

		#endregion

		#region Optional event registration

		private void RegisterOptionalEvents()
		{
			//Handled trigger event
			ValidTriggerReceived += CuriousMisty_ResponseEventReceived;

			//Other events...
			SpeechIntentEvent += CuriousMisty_SpeechIntent;
			FaceRecognitionEvent += CuriousMisty_FaceRecognitionEvent;
			CapTouchEvent += CuriousMisty_CapTouchEvent;
			BumperEvent += CuriousMisty_BumperEvent;
			BatteryChargeEvent += CuriousMisty_BatteryChargeEvent;
			QrTagEvent += CuriousMisty_QrTagEvent;
			ArTagEvent += CuriousMisty_ArTagEvent;
			SerialMessageEvent += CuriousMisty_SerialMessageEvent;
			ExternalEvent += CuriousMisty_ExternalEvent;
			ObjectEvent += CuriousMisty_ObjectEvent;
			StartedSpeaking += CuriousMisty_StartedSpeaking;
			StoppedSpeaking += CuriousMisty_StoppedSpeaking;
			StartedListening += CuriousMisty_StartedListening;
			StoppedListening += CuriousMisty_StoppedListening;
			KeyPhraseRecognitionOn += CuriousMisty_KeyPhraseRecognitionOn;
			ConversationStarted += CuriousMisty_ConversationStarted;
			ConversationEnded += CuriousMisty_ConversationEnded;
			InteractionStarted += CuriousMisty_InteractionStarted;
			InteractionEnded += CuriousMisty_InteractionEnded;
			StartedProcessingVoice += CuriousMisty_StartedProcessingVoice;
			CompletedProcessingVoice += CuriousMisty_CompletedProcessingVoice;
			DriveEncoder += CuriousMisty_DriveEncoder;			
		}

		#endregion

		#region Extra event handling as desired for Curious Misty

		private void CuriousMisty_ResponseEventReceived(object sender, TriggerData e)
		{
			//Already handled through interaction mapping, add something here if ya want
		}

		private void CuriousMisty_SpeechIntent(object sender, TriggerData e)
		{
			//Do something
		}

		private void CuriousMisty_InteractionEnded(object sender, DateTime e)
		{
			//Do something
		}

		private void CuriousMisty_InteractionStarted(object sender, DateTime e)
		{
			//Do something
		}

		private void CuriousMisty_ConversationEnded(object sender, DateTime e)
		{
			//Do something
		}

		private void CuriousMisty_ConversationStarted(object sender, DateTime e)
		{
			//Do something
		}

		private void CuriousMisty_FaceRecognitionEvent(object sender, MistyRobotics.SDK.Events.IFaceRecognitionEvent e)
		{
			//Do something
		}
		
		private void CuriousMisty_ObjectEvent(object sender, MistyRobotics.SDK.Events.IObjectDetectionEvent e)
		{
			//Do something?
		}

		private void CuriousMisty_ExternalEvent(object sender, MistyRobotics.SDK.Events.IUserEvent e)
		{
			//Do something?
		}

		private void CuriousMisty_SerialMessageEvent(object sender, MistyRobotics.SDK.Events.ISerialMessageEvent e)
		{
			//Do something?
		}

		private void CuriousMisty_ArTagEvent(object sender, MistyRobotics.SDK.Events.IArTagDetectionEvent e)
		{
			//Do something?
		}

		private void CuriousMisty_QrTagEvent(object sender, MistyRobotics.SDK.Events.IQrTagDetectionEvent e)
		{
			//Do something?
		}

		private void CuriousMisty_BumperEvent(object sender, MistyRobotics.SDK.Events.IBumpSensorEvent e)
		{
			//Do something?
		}

		private void CuriousMisty_CapTouchEvent(object sender, MistyRobotics.SDK.Events.ICapTouchEvent e)
		{
			//Do something?
		}

		private void CuriousMisty_BatteryChargeEvent(object sender, MistyRobotics.SDK.Events.IBatteryChargeEvent e)
		{
			//Do something?
		}
		private void CuriousMisty_KeyPhraseRecognitionOn(object sender, bool e)
		{
			//Do something?
		}

		private void CuriousMisty_StoppedListening(object sender, MistyRobotics.SDK.Events.IVoiceRecordEvent e)
		{
			//Do something?
		}

		private void CuriousMisty_StartedListening(object sender, DateTime e)
		{
			//Do something?		
		}

		private void CuriousMisty_StoppedSpeaking(object sender, MistyRobotics.SDK.Events.IAudioPlayCompleteEvent e)
		{
			//Do something?
		}

		private void CuriousMisty_StartedSpeaking(object sender, string e)
		{
			//Do something?		
		}
		
		private void CuriousMisty_DriveEncoder(object sender, MistyRobotics.SDK.Events.IDriveEncoderEvent e)
		{
			//Do something?
		}
		
		private void CuriousMisty_StartedProcessingVoice(object sender, MistyRobotics.SDK.Events.IVoiceRecordEvent e)
		{
			//Do something?
		}

		private void CuriousMisty_CompletedProcessingVoice(object sender, MistyRobotics.SDK.Events.IVoiceRecordEvent e)
		{
			//Do something?
		}
		
		#endregion

		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					base.Dispose();
				}

				_isDisposed = true;
			}
		}

		public new void Dispose()
		{
			Dispose(true);
		}

		#endregion
	}
}