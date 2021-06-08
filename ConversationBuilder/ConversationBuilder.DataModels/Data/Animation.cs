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
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ConversationBuilder.DataModels
{
	public class Animation : IEditableData
	{
		[JsonProperty(PropertyName = "Id")]
		/// <summary>
		/// Unique id for mapping
		/// </summary>
		public string Id { get; set; }

		[Required]
		[Display(Name = "Item Type")]
		[JsonProperty(PropertyName = "ItemType")]
		/// <summary>
		/// Data item type
		/// </summary>
		public string ItemType { get; set; } = DataItemType.Animation.ToString();

		[Display(Name = "Trim Audio Silence")]
		[JsonProperty(PropertyName = "TrimAudioSilence")]
		/// <summary>
		/// Set to greater than 0 to trim the end of the audio when created
		/// TODO Update to remove empty ending audio and not use timer, then remove this
		/// </summary>
		public double TrimAudioSilence { get; set; } = 0.5;

		[Display(Name = "Head Location")]
		[JsonProperty(PropertyName = "HeadLocation")]
		/// <summary>
		/// 
		/// </summary>
		public string HeadLocation { get; set; }

		[Display(Name = "Arm Location")]
		[JsonProperty(PropertyName = "ArmLocation")]
		/// <summary>
		/// 
		/// </summary>
		public string ArmLocation { get; set; }
		
		[Required]
		[Display(Name = "Name")]
		[JsonProperty(PropertyName = "Name")]
		/// <summary>
		/// Name for the animation if you want to track it
		/// </summary>
		public string Name { get; set; }

		[Display(Name = "Speech Rate (Azure only)")]
		[JsonProperty(PropertyName = "SpeechRate")]
		/// <summary>
		/// Change the speaking rate.
		/// 1.0 is normal rate
		/// </summary>
		public double SpeechRate { get; set; } = 1.0;


		[Display(Name = "Override Voice (Azure and Google only)")]
		[JsonProperty(PropertyName = "OverrideVoice")]
		/// <summary>
		/// Pass in to speak in a different voice for this animation
		/// Otherwise will use current character voice
		/// </summary>
		public string OverrideVoice { get; set; }


		[Display(Name = "Speaking Style (Azure only)")]
		[JsonProperty(PropertyName = "SpeakingStyle")]
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

		[Required]
		[Display(Name = "Emotion of the animation")]
		[JsonProperty(PropertyName = "Emotion")]
		/// <summary>
		/// Emotional action to perform
		/// Emotional action drives default of all minor movements and changes and should provide a complete experience.
		/// Most fields provided here are to allow overrides when necessary.
		/// </summary>
		public string Emotion { get; set; } = DefaultEmotions.Joy;

		[Display(Name = "Speak")]
		[JsonProperty(PropertyName = "Speak")]
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

		[Display(Name = "Audio File to play")]
		[JsonProperty(PropertyName = "AudioFile")]
		/// <summary>
		/// Play this audio file, if speak and audio file populated, plays audio file first
		/// If neither are populated, plays default audio for emotion, if any
		/// </summary>
		public string AudioFile { get; set; }

		[Display(Name = "Silence")]
		[JsonProperty(PropertyName = "Silence")]
		/// <summary>
		/// No sounds or speaking, including stopping speaking if misty is currently doing so
		/// </summary>
		public bool Silence { get; set; }

		[Display(Name = "Image File to display on the screen")]
		[JsonProperty(PropertyName = "ImageFile")]
		/// <summary>
		/// Show this image file
		/// If not populated, displays image for emotion
		/// </summary>
		public string ImageFile { get; set; }

		[Display(Name = "Flashlight On")]
		[JsonProperty(PropertyName = "SetFlashlight")]
		/// <summary>
		/// Set to true to turn on and false to turn off with the request
		/// </summary>
		public bool SetFlashlight { get; set; }

		[Display(Name = "New Volume")]
		[JsonProperty(PropertyName = "Volume")]
		/// <summary>
		/// Sets volume
		/// Null uses current default
		/// </summary>
		public int? Volume { get; set; } = null;

		[Display(Name = "Delay before starting new head movements")]
		[JsonProperty(PropertyName = "HeadActionDelay")]
		/// <summary>
		/// Head action delay before starting
		/// </summary>
		public double HeadActionDelay { get; set; } = 0.0;

		[Display(Name = "Delay before starting new arm actions")]
		[JsonProperty(PropertyName = "ArmActionDelay")]
		/// <summary>
		/// Arm action delay before starting
		/// </summary>
		public double ArmActionDelay { get; set; } = 0.0;
		
		[Display(Name = "Delay before starting new LED transitions")]
		[JsonProperty(PropertyName = "LEDActionDelay")]
		/// <summary>
		/// LED action delay before starting
		/// </summary>
		public double LEDActionDelay { get; set; } = 0.0;
		
		/// <summary>
		/// Overrides the LED color for the animation request
		/// </summary>
		[Display(Name = "LED Transition Action")]
		[JsonProperty(PropertyName = "LEDTransitionAction")]
		public string LEDTransitionAction{ get; set; }	

		[Display(Name = "AnimationScript")]
		[JsonProperty(PropertyName = "AnimationScript")]
		/// <summary>
		/// Show this image file
		/// If not populated, displays image for emotion
		/// </summary>
		public string AnimationScript { get; set; }

		[Display(Name = "Created")]
		[JsonProperty(PropertyName = "Created")]
		public DateTimeOffset Created { get; set; }

		[Display(Name = "CreatedBy")]
		[JsonProperty(PropertyName = "Created By")]
		public string CreatedBy { get; set; }
		
		[Display(Name = "Updated")]
		[JsonProperty(PropertyName = "Updated")]
		public DateTimeOffset Updated { get; set; }

		public string ManagementAccess { get; set; } = "Public";
	}
}