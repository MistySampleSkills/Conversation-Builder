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
using System.Threading.Tasks;
using Conversation.Common;
using MistyRobotics.SDK.Events;

namespace SpeechTools
{
	public interface ISpeechManager
	{
		event EventHandler<string> StartedSpeaking;
		event EventHandler<IAudioPlayCompleteEvent> StoppedSpeaking;
		event EventHandler<DateTime> StartedListening;
		event EventHandler<IVoiceRecordEvent> StoppedListening;
		event EventHandler<TriggerData> SpeechIntent;
		event EventHandler<bool> KeyPhraseRecognitionOn;
		event EventHandler<IKeyPhraseRecognizedEvent> KeyPhraseRecognized;
		event EventHandler<IAudioPlayCompleteEvent> PreSpeechCompleted;
		event EventHandler<IVoiceRecordEvent> CompletedProcessingVoice;
		event EventHandler<IVoiceRecordEvent> StartedProcessingVoice;
		event EventHandler<string> UserDataAnimationScript;

		Task<bool> Initialize();
		int Volume { get; set; }
		Task Speak(AnimationRequest currentAnimation, Interaction currentInteraction, bool backgroundSpeech);
		void SetAllowedUtterances(IList<string> allowedUtterances);
		Task<bool> UpdateKeyPhraseRecognition(Interaction _currentInteraction, bool hasAudio);
		void AbortListening(string audioName);		
		bool TryToPersonalizeData(string text, AnimationRequest animationRequest, Interaction interaction, out string newText);

		string MakeTextBasedFileName(string text);
		void SetAudioTrim(int trimMs);
		void SetMaxSilence(int silenceTimeout);
		void SetMaxListen(int listenTimeout);
		void SetSpeechRate(double rate);
		void SetSpeakingStyle(string speakingStyle);
		void SetLanguage(string language);
		void SetVoice(string voice);
		void SetPitch(string pitch);
		//hacky
		bool HandleExternalSpeech(string text = null);
		bool CancelSpeechProcessing();

		void AddValidIntent(object sender, KeyValuePair<string, TriggerData> triggerData);

		void Dispose();
		
		//TODO Move prespeech into speech manager
		//void UpdatePrespeech(string prespeech);
	}
}
 