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
	public class AnimationRequest
	{
		public AnimationRequest() { }

		public AnimationRequest(string id, string emotion)
		{
			Id = id;
			Emotion = emotion;
			Name = emotion.ToString();
		}

		public AnimationRequest(string id, string emotion, string name, string speak = null)
		{
			Id = id;
			Emotion = emotion;
			Speak = speak;			
		}
		
		
		public AnimationRequest(AnimationRequest state)
		{
			TrimAudioSilence = state.TrimAudioSilence;
			Id = state.Id;
			HeadLocation = state.HeadLocation;
			ArmLocation = state.ArmLocation;
			Name = state.Name;
			SpeechRate = state.SpeechRate;
			OverrideVoice = state.OverrideVoice;
			SpeakingStyle = state.SpeakingStyle;
			Emotion = state.Emotion;
			Speak = state.Speak;
			AudioFile = state.AudioFile;
			Silence = state.Silence;
			ImageFile = state.ImageFile;
			SetFlashlight = state.SetFlashlight;
			Volume = state.Volume;
			SpeakFileName = state.SpeakFileName;
			ArmActionDelay = state.ArmActionDelay;
			HeadActionDelay = state.HeadActionDelay;
			LEDActionDelay = state.LEDActionDelay;
			AnimationScript = state.AnimationScript;
			LEDTransitionAction = state.LEDTransitionAction;
		}

		/// <summary>
		/// File name to save this audio text to speech to, without an extension.  Saved as a .wav file.
		/// if empty, uses default tts name that will be overwrittten on next speach action
		/// Must be populated to avoid tts translations every time or to use a distinct audio file
		/// name in an audio callback check
        /// TODO Cleanup, this is no longer a user field to set in the animation and is only used with Google and Azure TTS
		/// </summary>
		public string SpeakFileName { get; set; }
		
        /// <summary>
        /// Overrides the emotional default audio so this animation makes no sound
        /// </summary>
		public bool Silence { get; set; }
		
		/// <summary>
		/// Set to greater than 0 to trim the end of the audio when created
		/// TODO Update to remove empty ending audio and not use timer, then remove this
		/// </summary>
		public double TrimAudioSilence { get; set; } = 0.5;

		/// <summary>
		/// Mapping Id
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Override head location used with HeadAction Manual
		/// </summary>
		public string HeadLocation { get; set; }

		/// <summary>
		/// Override arm location used with ArmAction Manual
		/// </summary>
		public string ArmLocation { get; set; }

		/// <summary>
		/// Name for the animation if you want to track it
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Change the speaking rate.
		/// 1.0 is normal rate
		/// </summary>
		public double SpeechRate { get; set; } = 1.0;

		/// <summary>
		/// Pass in to speak in a different voice for this animation
		/// Otherwise will use current character voice
		/// </summary>
		public string OverrideVoice { get; set; }

		/// <summary>
		/// You can adjust the speaking style of three Neural voices.
		/// If you set these, it can override other speech settings like rate.
		/// 
		/// en-US-AriaNeural
		/// style="newscast"    Expresses a formal and professional tone for narrating news
		/// style="customerservice"	Expresses a friendly and helpful tone for customer support
		/// style="chat"	Expresses a casual and relaxed tone
		/// style="cheerful"	Expresses a positive and happy tone
		/// style="empathetic"	Expresses a sense of caring and understanding
		/// 
		/// zh-CN-XiaoxiaoNeural
		/// style="newscast"    Expresses a formal and professional tone for narrating news
		/// style="customerservice"	Expresses a friendly and helpful tone for customer support
		/// style="assistant"	Expresses a warm and relaxed tone for digital assistants
		/// style="lyrical"	Expresses emotions in a melodic and sentimental way
		/// 
		/// zh-CN-YunyangNeural 
		/// style="customerservice" Expresses a friendly and helpful tone for customer support		
		/// </summary>
		public string SpeakingStyle { get; set; }

		/// <summary>
		/// Emotional action to perform
		/// Emotional action drives default of all minor movements and changes and should provide a complete experience.
		/// Most fields provided here are to allow overrides when necessary.
		/// </summary>
		public string Emotion { get; set; }

		/// <summary>
		/// If the robot should say something during performance
		/// If speak and audio file populated, plays audio file
		/// If empty or null audio file and NULL Speak, plays default audio for emotion, if any
		/// If empty or null audio file and Empty ("") Speak, is silent
		/// If text is in the form of an SSML statement, it will be processed as such and will ignore the current 
		/// SpeechRate, OverrideStyle, and SpeakingStyle fields as those should be added to the SSML.
		/// 
		/// SSML example: 
		/// Speak = "<?xml version=\"1.0\" encoding=\"utf-8\"?> <speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">" +
		/// "<voice name=\"en-US-GuyNeural\"><prosody rate=\"2.0\">" +
		/// "California, the most populous US state and the first to implement a statewide lockdown to combat the coronavirus outbreak, is setting daily records this week for new cases as officials urge caution and dangle enforcement threats to try to curb the spikes." +
		/// "The virus is spreading at private gatherings in homes, and more young people are testing positive, Gov.Gavin Newsom said Wednesday.Infections at some prisons are raising concerns." +
		/// "</prosody></voice></speak>"
		/// </summary>
		public string Speak { get; set; }
		
		/// <summary>
		/// The max length a person can talk after listening starts
		/// Max is 10000ms (10 seconds) at this time
		/// </summary>
		public double ListenTimeout { get; set; } = 6.0;

		/// <summary>
		/// How long to wait through silence before ending listening
		/// Max is 10000ms (10 seconds) at this time
		/// </summary>
		public double SilenceTimeout { get; set; } = 6.0;

		/// <summary>
		/// Play this audio file, if speak and audio file populated, plays audio file first
		/// If neither are populated, plays default audio for emotion, if any
		/// </summary>
		public string AudioFile { get; set; }

		/// <summary>
		/// Show this image file
		/// If not populated, displays image for emotion
		/// </summary>
		public string ImageFile { get; set; }

		/// <summary>
		/// Set to true to turn on and false to turn off with the request
		/// </summary>
		public bool SetFlashlight { get; set; }

		/// <summary>
		/// Sets volume
		/// Null uses current default
		/// </summary>
		public int? Volume { get; set; } = null;
		
		/// <summary>
		/// Delay before starting arm action within the animation		
		/// </summary>
		public double? ArmActionDelay { get; set; } = 0.0;

		/// <summary>
		/// Delay before starting head action within the animation
		/// </summary>
		public double? HeadActionDelay { get; set; } = 0.0;

        /// <summary>
        /// Delay before starting LED action within the animation
        /// </summary>
        public double? LEDActionDelay { get; set; } = 0.0;

        /// <summary>
        /// LED transition action to perform during animation
        /// </summary>
        public string LEDTransitionAction { get; set; }

		/// <summary>
		/// Optional animation script
		/// </summary>
		public string AnimationScript { get; set; }		
	}
}