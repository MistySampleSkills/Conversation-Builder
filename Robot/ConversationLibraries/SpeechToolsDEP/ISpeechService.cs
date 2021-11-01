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
using Windows.Foundation;

namespace SpeechTools
{
	public sealed class SpeechToTextData
	{
		public string Text { get; set; }

		public TimeSpan Duration { get; set; }

		public IDictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
	}

	public sealed class TextToSpeechData
	{
		public byte[] AudioData { get; set; }

		public TimeSpan Duration { get; }
	}

	public interface ISpeechService
	{
		/// <summary>
		/// If the service is authorized
		/// </summary>
		bool Authorized { get; }

		/// <summary>
		/// The Speaking voice used by Misty
		/// </summary>
		string SpeakingVoice { get; set; }

		/// <summary>
		/// The language the speaker is using
		/// </summary>
		string SpokenLanguage { get; set; }

		/// <summary>
		/// The language to translate into, can be the same language
		/// </summary>
		string TranslatedLanguage { get; set; }

		/// <summary>
		/// Misty's speaking profanity setting, defaults to allow cursing
		/// </summary>
		string ProfanitySetting { get; set; }

		/// <summary>
		/// Using an audio stream, get the translation of that audio file
		/// </summary>
		/// <param name="audioData"></param>
		/// <returns></returns>
		IAsyncOperation<SpeechToTextData> TranslateAudioStream(IEnumerable<byte> audioData);

		/// <summary>
		/// Using an audio file in either the LocalFolder (or the Assets folder for testing)
		/// get the translation of that audio file
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="useAssetFolder"></param>
		/// <returns></returns>
		IAsyncOperation<SpeechToTextData> TranslateAudioFile(string filename, bool useAssetFolder);

		/// <summary>
		/// Return an audio file for the passed in text
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		IAsyncOperation<TextToSpeechData> TextToSpeechFile(string text);

		/// <summary>
		/// Tell Misty to speak
		/// </summary>
		/// <param name="text"></param>
		/// <param name="fileName"></param>
		/// <param name="overrideVolume"></param>
		/// <param name="useSSML"></param>
		/// <param name="trimAudioSilence"></param>
		IAsyncOperation<bool> Speak(string text, string fileName, int overrideVolume, bool useSSML, int trimAudioSilenceMs);

		/// <summary>
		/// Tell Misty to speak at the current volume
		/// </summary>
		/// <param name="text"></param>
		/// <param name="fileName"></param>
		/// <param name="useSSML"></param>
		/// <param name="trimAudioSilenceMs"></param>
		IAsyncOperation<bool> Speak(string text, string fileName, bool useSSML, int trimAudioSilenceMs);
	}
}