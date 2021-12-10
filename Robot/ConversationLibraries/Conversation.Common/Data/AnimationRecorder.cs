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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MistyRobotics.SDK.Messengers;
using Newtonsoft.Json;
using SkillTools.Web;
using Windows.Foundation;
using Windows.Storage;

namespace Conversation.Common
{
	public class ArmPayload
	{
		public int? LeftArmPosition { get; set; }
		public int? RightArmPosition { get; set; }
		public int? Duration { get; set; }

	}

	public class HeadPayload
	{
		public int? Pitch { get; set; }
		public int? Roll { get; set; }
		public int? Yaw { get; set; }
		public int? Duration { get; set; }
	}

	public class AnimationRecorder
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

		private string _puppetingList;

		private string[] _puppetingBots;

		private WebMessenger _webMessenger = new WebMessenger();

		private IRobotMessenger _misty;

		public AnimationRecorder(IRobotMessenger misty, string puppetingList)
		{
			_misty = misty;
			_puppetingList = puppetingList;
			if (!string.IsNullOrWhiteSpace(_puppetingList))
			{
				if (_puppetingList.Contains(";"))
				{
					_puppetingBots = _puppetingList.Split(";");
				}
				else
				{
					_puppetingBots = _puppetingList.Split(",");
				}
			}
		}

		/// <summary>
		/// Save the data in the dictionary to the file db
		/// And send out commands to any puppeted robots
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public async Task<bool> SaveRecording(string text)
		{
			try
			{
				await _semaphoreSlim.WaitAsync();

				if(_puppetingBots.Length > 0)
				{	
					foreach(string bot in _puppetingBots)
					{
						//Send command to other bots in the puppeting list
						string[] commands = text.Split(";");
						foreach (string command in commands)
						{
							string newCommand = command.ToLower().Trim();
							if (newCommand.StartsWith("arm"))
							{
								try
								{
									string[] fieldList = newCommand.Split(":");
									string[] fields = fieldList[1].Split(",");
									if (fields.Length >= 3)
									{
										var payload = new ArmPayload
										{
											LeftArmPosition = Convert.ToInt32(fields[0]),
											RightArmPosition = Convert.ToInt32(fields[1]),
											Duration = Convert.ToInt32(fields[2]) / 1000
										};

										_ = _webMessenger.PostRequest($@"http://{bot}/api/arms/set", JsonConvert.SerializeObject(payload), "application/json");
									}
								}
								catch (Exception ex)
								{
									_misty.SkillLogger.LogError($"Failed to puppet arm command on {bot}.", ex);
								}
								
							}
							else if (newCommand.StartsWith("head"))
							{
								try
								{
									string[] fieldList = newCommand.Split(":");
									string[] fields = fieldList[1].Split(",");
									if (fields.Length >= 4)
									{
										var payload = new HeadPayload
										{
											Pitch = Convert.ToInt32(fields[0]),
											Roll = Convert.ToInt32(fields[1]),
											Yaw = Convert.ToInt32(fields[2]),
											Duration = Convert.ToInt32(fields[3])/1000
										};
										_ = _webMessenger.PostRequest($@"http://{bot}/api/head", JsonConvert.SerializeObject(payload), "application/json");
									}
								}
								catch (Exception ex)
								{
									_misty.SkillLogger.LogError($"Failed to puppet head command on {bot}.", ex);
								}
							}

						}
					}
				}

				if (_dbFile != null)
				{
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