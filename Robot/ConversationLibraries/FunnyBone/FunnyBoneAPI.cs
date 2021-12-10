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

using SkillTools.Web;
using System.Threading.Tasks;

namespace FunnyBone
{
	public class FunnyBoneAPI
	{
		private WebMessenger _webMessenger = new WebMessenger();

		public async Task<SingleJokeFormat> GetDeveloperJoke()
		{
			try
			{
				WebMessengerData data = await _webMessenger.GetRequestAsync("https://v2.jokeapi.dev/joke/Programming,Pun?blacklistFlags=nsfw,religious,political,racist,sexist,explicit&type=single");
				SingleJokeFormat singleJokeFormat = Newtonsoft.Json.JsonConvert.DeserializeObject<SingleJokeFormat>(data.Response);
				return singleJokeFormat;
			}
			catch
			{
				return new SingleJokeFormat { Joke = "I had trouble getting a joke.", Error = true };
			}
		}
	
		public async Task<ChuckNorrisJokeFormat> GetChuckNorrisJoke(string [] limitTo = null, string [] exclude = null)
		{
			try
			{
				if (exclude == null)
				{
					exclude = new string[] { "explicit" };
				}

				string url = "http://api.icndb.com/jokes/random";
				bool toInclude = limitTo != null;
				bool toExclude = exclude != null;

				if (toExclude) url += string.Format("&exclude=[{0}]", string.Join(",", exclude));
				if (toInclude) url += string.Format("&limitTo=[{0}]", string.Join(",", limitTo));
				
				WebMessengerData data = await _webMessenger.GetRequestAsync(url);
				ChuckNorrisJokeFormat chuckNorrisJokeFormat = Newtonsoft.Json.JsonConvert.DeserializeObject<ChuckNorrisJokeFormat>(data.Response);
				return chuckNorrisJokeFormat;
			}
			catch
			{
				return new ChuckNorrisJokeFormat { Type = "error", Value = new ChuckNorrisJoke { Joke = "I had trouble getting a Chuck Norris joke.", Id = 0 } };
			}
		}
	}
}
