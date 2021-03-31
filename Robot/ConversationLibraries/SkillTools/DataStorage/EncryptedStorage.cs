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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;

namespace SkillTools.DataStorage
{
	/// <summary>
	/// Basic data store for long terms storage of skill data
	/// IMPORTANT! These are helper data storage classes and are readable and minimally encrypted
	/// If you need better security, you may need to write your own
	/// </summary>
	public sealed class EncryptedStorage : BasicStorage
	{
		/// <summary>
		/// Method used to get a reference to the data store
		/// IMPORTANT! These are helper data storage classes and are not locked and are simply encrypted.
		/// If you need real security, you may need to write your own.
		/// Returns null if could not create file or one exists and the password is incorrect on an existing data store with data.
		/// Data is encrypted using the password, but the file is not encrypted.  The password encryption is applied when data is stored and retrieved.
		/// </summary>
		/// <param name="databaseIdentifier"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public static IAsyncOperation<ISkillStorage> GetDatabase(string databaseIdentifier, string password)
		{
			return GetDatabaseInternal(databaseIdentifier, password).AsAsyncOperation();
		}

		private static async Task<ISkillStorage> GetDatabaseInternal(string databaseIdentifier, string password)
		{
			EncryptedStorage skillDB = new EncryptedStorage(databaseIdentifier, password);

			//See if we can read the data store with that auth info
			IDictionary<string, object> existingData = await skillDB.LoadDataAsync();
			if (existingData == null)
			{
				//Could not decrypt or failed to parse an existing file, don't give reference to the file...
				return null;
			}
			return skillDB;
		}

		/// <summary>
		/// IMPORTANT! These are helper data storage classes and are readable and simply encrypted.
		/// If you need real security, you may need to write your own.
		/// </summary>
		/// <param name="databaseIdentifier"></param>
		/// <param name="password"></param>
		private EncryptedStorage(string databaseIdentifier, string password)
		{
			_password = password;
			Regex invalidCharacters = new Regex(@"[\\/:*?""<>|]");
			string fileSafeSkillName = invalidCharacters.Replace(databaseIdentifier.Replace(" ", "_"), "");
			_fileSafeDBName = $"{fileSafeSkillName}.txt";
		}
	}
}