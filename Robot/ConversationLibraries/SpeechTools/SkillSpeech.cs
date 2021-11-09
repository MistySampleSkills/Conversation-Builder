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
using System.IO;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;

namespace SpeechTools
{
	public class SkillSpeech
	{
		private VoiceInformation _voice = SpeechSynthesizer.DefaultVoice;
		double _pitch = 1.0;
		double _rate = 1.0;

		public void SetPitch(double pitch)
		{
			_pitch = pitch;
		}

		public void SetRate(double rate)
		{
			_rate = rate;
		}

		public bool SetVoice(string voice)
		{
			var voices = SpeechSynthesizer.AllVoices;
			foreach (VoiceInformation voiceInfo in voices)
			{
				if (voiceInfo.DisplayName.Contains(voice))
				{
					_voice = voiceInfo;
					return true;
				}
			}
			return false;
		}
		

		public async Task<Stream> TextToStream(string text)
		{
			using (var synth = new SpeechSynthesizer())
			{
				synth.Options.SpeakingRate = _rate;
				synth.Options.AudioPitch = _pitch;
				synth.Voice = _voice;
				var test = synth.Options;
				SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(text);
				return stream.AsStream();
			}
		}
		public async Task<Stream> SsmlToStream(string text)
		{
			using (var synth = new SpeechSynthesizer())
			{
				var voices = SpeechSynthesizer.AllVoices;
				foreach (VoiceInformation voice in voices)
				{
					string testX = voice.DisplayName;
					if(testX.Contains("Zira"))
					{
						synth.Voice = voice;
					}
				}
				var defaultVoice = SpeechSynthesizer.DefaultVoice;
				var test = synth.Options;
				SpeechSynthesisStream stream = await synth.SynthesizeSsmlToStreamAsync(text);
				return stream.AsStream();
			}
		}
	}
}
 