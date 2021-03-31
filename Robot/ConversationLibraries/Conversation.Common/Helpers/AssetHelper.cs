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

using System.IO;
using System.Text;
using SkillTools.AssetTools;

namespace Conversation.Common
{
	public class AssetHelper
	{
		public static bool AreEqualAudioFilenames(string audioFilename1, string audioFilename2, bool includeLocale)
		{
			if(string.IsNullOrWhiteSpace(audioFilename1) || string.IsNullOrWhiteSpace(audioFilename2))
			{
				return false;
			}

			audioFilename1 = audioFilename1.ToLower();
			audioFilename2 = audioFilename2.ToLower();

			if(!includeLocale)
			{
				return audioFilename1 == audioFilename2 ||
				$"{audioFilename1}.wav" == audioFilename2 ||
				audioFilename1 == $"{audioFilename2}.wav";
			}

			return audioFilename1.EndsWith(audioFilename2) ||
				$"{audioFilename1}.wav".EndsWith(audioFilename2) ||
				audioFilename1.EndsWith($"{audioFilename2}.wav");
		}

		public static string AddMissingWavExtension(string audioFilename)
		{
			if(string.IsNullOrWhiteSpace(audioFilename))
			{
				return null;
			}

			if (Path.GetExtension(audioFilename).Contains("."))
			{
				return audioFilename;
			}
			return $"{audioFilename}.wav";
		}

		/// <summary>
		/// Make a unique filename based upon the text of the message
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string MakeFileName(string text)
		{
			if(string.IsNullOrWhiteSpace(text))
			{
				return null;
			}

			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
			{
				byte[] inputBytes = Encoding.ASCII.GetBytes(text);
				byte[] hashBytes = md5.ComputeHash(inputBytes);

				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < hashBytes.Length; i++)
				{
					sb.Append(hashBytes[i].ToString("X2"));
				}
				return sb.ToString();
			}
		}

		public static string GetFileNameFromAsset(SystemImage image)
		{
			return $"e_{image}.jpg";
		}

		public static string GetFileNameFromAsset(SystemSound sound)
		{
			return $"s_{sound}.wav";
		}
	}
}