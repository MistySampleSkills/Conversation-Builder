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

using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using Windows.Storage;

namespace SkillTools.AssetTools
{
	/// <summary>
	/// Asset wrapper to help with asset specific capabilities
	/// </summary>
	public interface IAssetWrapper
	{
		/// <summary>
		/// Current Image List 
		/// May be slightly incorrect if new images have been pushed to robot from another source
		/// </summary>
		IList<ImageDetails> ImageList { get; }

		/// Current Audio List 
		/// May be slightly incorrect if new audio files have been pushed to robot from another source
		IList<AudioDetails> AudioList { get; }

		/// Current video List 
		/// May be slightly incorrect if new videos have been pushed to robot from another source
		IList<VideoDetails> VideoList { get; }

		/// <summary>
		/// Will request the asset lists from the robot and update the skills info
		/// </summary>
		/// <returns></returns>
		Task RefreshAssetLists();

		/// <summary>
		/// Simple method wrapper to help with displaying system images
		/// </summary>
		/// <param name="systemImage"></param>
		void ShowSystemImage(SystemImage systemImage);
		
		/// <summary>
		/// Simple method wrapper to help with displaying system images
		/// </summary>
		/// <param name="systemImage"></param>
		/// <param name="layer"></param>
		void ShowSystemImage(SystemImage systemImage, string layer);

		/// <summary>
		/// Simple method wrapper to help with playing system audio
		/// </summary>
		void PlaySystemSound(SystemSound sound);

		/// <summary>
		/// Simple wrapper method to help with playing system audio
		/// </summary>
		void PlaySystemSound(SystemSound sound, int volume);

		/// <summary>
		/// Attempts to load image, video and audio assets in the project's 'Assets/SkillAssets' folder, or the overridden storage folder, to the robot's system
		/// </summary>
		/// <param name="forceReload">force the system to upload all assets, whether they exist or not</param>
		/// <param name="assetFolder">pass in to override the default location</param>
		/// <returns></returns>
		Task LoadAssets(bool forceReload = false, StorageFolder assetFolder = null);
	}
}