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

using System.Text.RegularExpressions;
using MistyRobotics.Common.Data;

namespace SkillTools.DataStorage
{
	/// <summary>
	/// Basic data store for long terms storage of skill data
	/// IMPORTANT! These are helper/example data storage classes and are readable and minimally encrypted
	/// If you need better security, you can try the EncryptedStorage class or you can need to write your own
	/// </summary>
	public sealed class SkillStorage : BasicStorage
	{
		private static SkillStorage _skillDB = null;

		private SkillStorage(string name)
		{
			CreateSafeName(name);
		}

		private SkillStorage(INativeRobotSkill skill)
		{
			CreateSafeName(skill.Name);
		}

		private void CreateSafeName(string name)
		{
			Regex invalidCharacters = new Regex(@"[\\/:*?""<>|]");
			string fileSafeSkillName = invalidCharacters.Replace(name.Replace(" ", "_"), "");
			_fileSafeDBName = $"{fileSafeSkillName}.txt";
		}

		/// <summary>
		/// Method used to get a reference to the skill specific data store
		/// IMPORTANT! These are helper data storage classes and this version is readable and NOT encrypted
		/// If you need security, you can try the EncryptedStorage class or you may need to write your own.
		/// </summary>
		/// <param name="skill"></param>
		/// <returns></returns>
		public static ISkillStorage GetDatabase(INativeRobotSkill skill)
		{
			if (_skillDB == null)
			{
				_skillDB = new SkillStorage(skill);
			}
			return _skillDB;
		}

		/// <summary>
		/// Allow more dbs per skill by name
		/// </summary>
		/// <param name="dbName"></param>
		/// <returns></returns>
		public static ISkillStorage GetDatabase(string dbName)
		{
			return new SkillStorage(dbName);
		}
	}
}
