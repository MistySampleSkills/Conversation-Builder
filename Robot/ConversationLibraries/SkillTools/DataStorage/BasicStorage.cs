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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Foundation;
using Windows.Storage;

namespace SkillTools.DataStorage
{
	/// <summary>
	/// Basic data store for long terms storage of skill data
	/// </summary>
	public abstract class BasicStorage : ISkillStorage
	{
		/// <summary>
		/// Folder containing data
		/// </summary>
		protected const string SkillDBFolderName = "SkillData";

		/// <summary>
		/// Path to data folder
		/// </summary>
		protected const string SDKFolderLocation = @"C:\Data\Misty\SDK";

		/// <summary>
		/// Reference to db file
		/// </summary>
		protected StorageFile _dbFile;

		/// <summary>
		/// Reference to db folder
		/// </summary>
		protected StorageFolder _databaseFolder;

		/// <summary>
		/// Semaphore to control db access
		/// </summary>
		protected SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

		/// <summary>
		/// DB file name
		/// </summary>
		protected string _fileSafeDBName;

		/// <summary>
		/// DB password
		/// </summary>
		protected string _password;

		internal SecurityController _securityController = new SecurityController();

		/// <summary>
		/// Method used to load the data from data storage
		/// </summary>
		/// <returns></returns>
		public IAsyncOperation<ConcurrentDictionary<string, object>> LoadDataAsync()
		{
			return LoadDataInternalAsync().AsAsyncOperation();
		}

		private async Task<ConcurrentDictionary<string, object>> LoadDataInternalAsync()
		{
			ConcurrentDictionary<string, object> data = new ConcurrentDictionary<string, object>();
			try
			{
				await _semaphoreSlim.WaitAsync();

				if (_dbFile == null)
				{
					await CreateDataStore();
				}

				if (_dbFile != null)
				{
					string dataString = File.ReadAllText(GetDbPath());

					if (string.IsNullOrWhiteSpace(dataString))
					{
						//This indicates new file or no data in existing db, grant access
						return new ConcurrentDictionary<string, object>();
					}

					if (!string.IsNullOrWhiteSpace(_password))
					{
						dataString = _securityController.Decrypt(_password, dataString);
						if (dataString == null)
						{
							//This indicates bad parse with password, deny access
							return null;
						}
					}

					data = JsonConvert.DeserializeObject<ConcurrentDictionary<string, object>>(dataString, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
				}
			}
			catch
			{
				_dbFile = null;
				return null;
			}
			finally
			{
				_semaphoreSlim.Release();
			}

			return data;
		}

		/// <summary>
		/// Call to remove the current data file for this skill
		/// </summary>
		/// <returns></returns>
		public IAsyncOperation<bool> DeleteSkillDatabaseAsync()
		{
			return DeleteSkillDatabaseInternalAsync().AsAsyncOperation();
		}
		
		private async Task<bool> DeleteSkillDatabaseInternalAsync()
		{
			try
			{
				await _semaphoreSlim.WaitAsync();

				if (_dbFile != null)
				{
					File.Delete(GetDbPath());
					return true;
				}
				return false;
			}
			catch
			{
				return false;
			}
			finally
			{
				_semaphoreSlim.Release();
			}
		}

		/// <summary>
		/// Method used to store the data into the long term skill data storage
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public IAsyncOperation<bool> SaveDataAsync(IDictionary<string, object> data)
		{
			return SaveDataInternalAsync(data).AsAsyncOperation();
		}

		/// <summary>
		/// Save the data in the dictionary to the file db
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private async Task<bool> SaveDataInternalAsync(IDictionary<string, object> data)
		{
			try
			{
				await _semaphoreSlim.WaitAsync();

				if (_dbFile != null && data != null && data.Count > 0)
				{
					string dataString;
					if (!string.IsNullOrWhiteSpace(_password))
					{
						dataString = _securityController.Encrypt(_password, JsonConvert.SerializeObject(data));
					}
					else
					{
						dataString = JsonConvert.SerializeObject(data);
					}

					File.WriteAllText(GetDbPath(), dataString);
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
		protected string GetDbPath()
		{
			return _databaseFolder.Path + "\\" + _fileSafeDBName;
		}

		/// <summary>
		/// Attempts to create data store at C:\Data\Misty\SDK\SkillData
		/// If the path is not accessible, this is probably a robot with an older FFU
		/// So will attempt to put it in C:Data\Users\DefaultAccount\Documents\SkillData
		/// </summary>
		/// <returns></returns>
		protected async Task<bool> CreateDataStore()
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