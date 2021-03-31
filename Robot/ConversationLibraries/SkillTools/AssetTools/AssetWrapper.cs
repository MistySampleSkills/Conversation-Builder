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
using System.Linq;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using Windows.Storage.Streams;
using Windows.Storage;

namespace SkillTools.AssetTools
{
	/// <summary>
	/// Utility class to manage and use image, audio, and video assets
	/// </summary>
	public sealed class AssetWrapper : IAssetWrapper
	{
		private IRobotMessenger _misty;
		
		/// <summary>
		/// Misty's image list as per last check
		/// </summary>
		public IList<ImageDetails> ImageList { get; private set; } = new List<ImageDetails>();

		/// <summary>
		/// Misty's audio list as per last check
		/// </summary>
		public IList<AudioDetails> AudioList { get; private set; } = new List<AudioDetails>();

		/// <summary>
		/// Misty's video list as per last check
		/// </summary>
		public IList<VideoDetails> VideoList { get; private set; } = new List<VideoDetails>();

		/// <summary>
		/// Attempts to refresh the assets in the list with what is on the robot
		/// </summary>
		/// <returns></returns>
		public async Task RefreshAssetLists()
		{
			try
			{
				IAudioServiceEnabledResponse audioEnabledResponse = await _misty.AudioServiceEnabledAsync();

				if (audioEnabledResponse == null || audioEnabledResponse.Status != ResponseStatus.Success || !audioEnabledResponse.Data)
				{
					IRobotCommandResponse enableResponse = await _misty.EnableAudioServiceAsync();
					if(enableResponse.Status != ResponseStatus.Success)
					{
						//Cannot get audio status, will still attempt to load assets
						_misty.SkillLogger.Log($"Unable to determine audio service status. Attempting asset load.");
					}
				}

				//Get the current assets on the robot
				IGetAudioListResponse audioListResponse = await _misty.GetAudioListAsync();
				if (audioListResponse != null && audioListResponse.Status == ResponseStatus.Success && audioListResponse.Data.Count() > 0)
				{
					AudioList = audioListResponse.Data;
				}

				IGetImageListResponse imageListResponse = await _misty.GetImageListAsync();
				if (imageListResponse != null  && imageListResponse.Status == ResponseStatus.Success && imageListResponse.Data.Count() > 0)
				{
					ImageList = imageListResponse.Data;
				}

				IGetVideoListResponse videoListResponse = await _misty.GetVideoListAsync();
				if (videoListResponse != null && videoListResponse.Status == ResponseStatus.Success && videoListResponse.Data.Count() > 0)
				{
					VideoList = videoListResponse.Data;
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log($"Failed to fully refresh asset list.", ex);
			}
		}

		/// <summary>
		/// Asset Wrapper constructor
		/// </summary>
		/// <param name="robotMessenger"></param>
		public AssetWrapper(IRobotMessenger robotMessenger)
		{
			_misty = robotMessenger;
		}

		/// <summary>
		/// Simple method wrapper to help with displaying system images
		/// </summary>
		/// <param name="systemImage"></param>
		public void ShowSystemImage(SystemImage systemImage)
		{
			_misty.DisplayImage($"e_{systemImage.ToString()}.jpg", null, false, null);
		}

		/// <summary>
		/// Simple method wrapper to help with displaying system images
		/// </summary>
		/// <param name="systemImage"></param>
		/// <param name="layer"></param>
		public void ShowSystemImage(SystemImage systemImage, string layer)
		{
			_misty.DisplayImage($"e_{systemImage.ToString()}.jpg", layer, false, null);
		}
		
		/// <summary>
		/// Simple method wrapper to help with playing system audio
		/// </summary>
		public void PlaySystemSound(SystemSound sound)
		{
			_misty.PlayAudio($"s_{sound.ToString()}.wav", null, null);
		}

		/// <summary>
		/// Simple wrapper method to help with playing system audio
		/// </summary>
		public void PlaySystemSound(SystemSound sound, int volume)
		{
			_misty.PlayAudio($"s_{sound.ToString()}.wav", volume, null);
		}

		/// <summary>
		/// Attempts to load image, video and audio assets in the project's 'Assets/SkillAssets' folder, or the overridden storage folder, to the robot's system
		/// </summary>
		/// <param name="forceReload">force the system to upload all assets, whether they exist or not</param>
		/// <param name="assetFolder">pass in to override the default location</param>
		/// <returns></returns>
		public async Task LoadAssets(bool forceReload = false, StorageFolder assetFolder = null)
		{
			try
			{
				await RefreshAssetLists();

				StorageFolder skillAssetFolder = null;
				if(assetFolder == null)
				{
					StorageFolder firstFolder = (await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFoldersAsync()).FirstOrDefault(x => x.DisplayName == "Assets");
					//Load the assets in the Assets/SkillAssets folder to the robot if they are missing or if ReloadAssets is passed in
					if (firstFolder != null)
					{
						StorageFolder secondFolder = (await firstFolder.GetFoldersAsync()).FirstOrDefault(x => x.DisplayName == "SkillAssets");
						if(secondFolder == null)
						{
							_misty.SkillLogger.Log($"No asset data to upload in Assets/SkillAssets.");
							return;
						}
						else
						{
							skillAssetFolder = secondFolder;
						}
					}
				}
				else
				{
					skillAssetFolder = assetFolder;
				}
				
				IList<StorageFile> assetFileList = (await skillAssetFolder?.GetFilesAsync()).ToList() ?? new List<StorageFile>();
				foreach (StorageFile storageFile in assetFileList)
				{
					if (forceReload ||
						(!AudioList.Any(x => x.Name == storageFile.Name) &&
						!ImageList.Any(x => x.Name == storageFile.Name) &&
						!VideoList.Any(x => x.Name == storageFile.Name)))
					{
						StorageFile file = await skillAssetFolder.GetFileAsync(storageFile.Name);
						IRandomAccessStreamWithContentType stream = await file.OpenReadAsync();
						byte[] contents = new byte[stream.Size];
						await stream.AsStream().ReadAsync(contents, 0, contents.Length);

						if (storageFile.Name.EndsWith(".mp3") ||
							storageFile.Name.EndsWith(".wav") ||
							storageFile.Name.EndsWith(".wma") ||
							storageFile.Name.EndsWith(".aac"))
						{
							if ((await _misty.SaveAudioAsync(storageFile.Name, contents, false, true)).Status == ResponseStatus.Success)
							{
								AudioList.Add(new AudioDetails { Name = storageFile.Name, SystemAsset = false });
								_misty.SkillLogger.LogInfo($"Uploaded audio asset '{storageFile.Name}'");
							}
							else
							{
								_misty.SkillLogger.Log($"Failed to upload audio asset '{storageFile.Name}'");
							}
						}
						else if (storageFile.Name.EndsWith(".mp4") ||
							storageFile.Name.EndsWith(".wmv"))
						{
							if ((await _misty.SaveVideoAsync(storageFile.Name, contents, false, true)).Status == ResponseStatus.Success)
							{
								VideoList.Add(new VideoDetails { Name = storageFile.Name, SystemAsset = false });
								_misty.SkillLogger.LogInfo($"Uploaded video asset '{storageFile.Name}'");
							}
							else
							{
								_misty.SkillLogger.Log($"Failed to upload video asset '{storageFile.Name}'");
							}
						}
						else if (storageFile.Name.EndsWith(".jpg") ||
							storageFile.Name.EndsWith(".jpeg") ||
							storageFile.Name.EndsWith(".png") ||
							storageFile.Name.EndsWith(".gif"))
						{
							if ((await _misty.SaveImageAsync(storageFile.Name, contents, false, true, 0, 0)).Status == ResponseStatus.Success)
							{
								ImageList.Add(new ImageDetails { Name = storageFile.Name, SystemAsset = false });
								_misty.SkillLogger.LogInfo($"Uploaded image asset '{storageFile.Name}'");
							}
							else
							{
								_misty.SkillLogger.Log($"Failed to upload image asset '{storageFile.Name}'");
							}
						}
						else
						{
							_misty.SkillLogger.Log($"Unknown extension for asset '{storageFile.Name}', could not load to robot.");
						}
					}
				}
			}
			catch(Exception ex)
			{
				_misty.SkillLogger.Log("Error loading assets.", ex);

			}
		}
	}
}