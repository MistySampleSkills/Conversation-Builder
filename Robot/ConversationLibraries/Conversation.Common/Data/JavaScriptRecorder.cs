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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MistyRobotics.SDK.Messengers;
using SkillTools.Web;
using Windows.Storage;

namespace Conversation.Common
{
	public class JavaScriptRecorder
	{
		/// <summary>
		/// Folder containing data
		/// </summary>
		private const string SkillDBFolderName = "SkillData";

		/// <summary>
		/// Path to data folder
		/// </summary>
		private const string SDKFolderLocation = @"C:\Data\Misty\SDK";

		/// <summary>
		/// Reference to db file
		/// </summary>
		private StorageFile _dbFile;

		/// <summary>
		/// Reference to db folder
		/// </summary>
		private StorageFolder _databaseFolder;

		/// <summary>
		/// Semaphore to control db access
		/// </summary>
		private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

		/// <summary>
		/// DB file name
		/// </summary>
		private string _fileSafeDBName;

		private WebMessenger _webMessenger = new WebMessenger();

		private IRobotMessenger _misty;

		public JavaScriptRecorder(IRobotMessenger misty)
		{
			_misty = misty;
		}

		/// <summary>
		/// Save the data in the dictionary to the file db
		/// And send out commands to any puppeted robots
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public async Task<bool> SaveRecording(string text, bool addNewline = true)
		{
			try
			{
				await _semaphoreSlim.WaitAsync();

				if (_dbFile != null)
				{
					if(addNewline)
					{
						text += Environment.NewLine;
					}
					File.AppendAllText(GetDbPath(), text);
					return true;
				}
				return false;
			}
			catch
			{
				_dbFile = null;
				return false;
			}
			finally
			{
				_semaphoreSlim.Release();
			}
		}

		/// <summary>
		/// Get the path to the storage folder
		/// </summary>
		/// <returns></returns>
		private string GetDbPath()
		{
			return _databaseFolder.Path + "\\" + _fileSafeDBName;
		}


		/// <summary>
		/// Attempts to create data store at C:\Data\Misty\SDK\SkillData
		/// If the path is not accessible, this is probably a robot with an older FFU
		/// So will attempt to put it in C:Data\Users\DefaultAccount\Documents\SkillData
		/// </summary>
		/// <returns></returns>
		public async Task<bool> CreateDataStore(string name)
		{
			if (_dbFile == null || !_dbFile.IsAvailable)
			{
				StorageFolder sdkFolder;
				try
				{
					sdkFolder = await StorageFolder.GetFolderFromPathAsync(SDKFolderLocation);
					_databaseFolder = await sdkFolder.CreateFolderAsync(SkillDBFolderName, CreationCollisionOption.OpenIfExists);
				}
				catch
				{
					try
					{
						//If old FFU build, \Misty\SDK won't exist, so save in \Users\DefaultAccount\Documents
						_databaseFolder = await KnownFolders.DocumentsLibrary.CreateFolderAsync(SkillDBFolderName, CreationCollisionOption.OpenIfExists);
					}
					catch
					{
						_dbFile = null;
						return false;
					}
				}

				try
				{
					Regex invalidCharacters = new Regex(@"[\\/:*?""<>|]");
					string fileSafeSkillName = invalidCharacters.Replace(name.Replace(" ", "_"), "");
					_fileSafeDBName = $"{fileSafeSkillName}.txt";

					if ((_dbFile = (StorageFile)await _databaseFolder.TryGetItemAsync(_fileSafeDBName)) == null)
					{
						_dbFile = await _databaseFolder.CreateFileAsync(_fileSafeDBName, CreationCollisionOption.ReplaceExisting);
					}
				}
				catch (Exception)
				{
					_dbFile = null;
					return false;
				}
			}

			return _dbFile != null;
		}
	}
}