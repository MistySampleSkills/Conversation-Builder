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
using SpeechTools;
using TimeManager;

namespace MistyCharacter
{
    /// <summary>
    /// Configuration used to optionally override default managers when calling through character templates
    /// </summary>
	public class ManagerConfiguration
	{
		public ManagerConfiguration() { }

		public ManagerConfiguration(ISpeechManager speechmanager = null, ITimeManager timeManager = null, IEmotionManager emotionManager = null, ISpeechIntentManager speechIntentManager = null, IAnimationManager animationManager = null)
		{
			SpeechManager = speechmanager;		
			TimeManager = timeManager;
			EmotionManager = emotionManager;
			SpeechIntentManager = speechIntentManager;
			AnimationManager = animationManager;
		}

		/// <summary>
		/// Deprecated!
		/// </summary>
		/// <param name="speechmanager"></param>
		/// <param name="timeManager"></param>
		/// <param name="armManager"></param>
		/// <param name="headManager"></param>
		/// <param name="emotionManager"></param>
		/// <param name="speechIntentManager"></param>
		/// <param name="animationManager"></param>
		/// <param name="locomotionManager"></param>
		[Obsolete("Arm Manager and Head Manager are deprecated, please use Animation Manager instead.")]
		public ManagerConfiguration(ISpeechManager speechmanager = null, ITimeManager timeManager = null, IArmManager armManager = null, IHeadManager headManager = null, IEmotionManager emotionManager = null, ISpeechIntentManager speechIntentManager = null, IAnimationManager animationManager = null, ILocomotionManager locomotionManager = null)
		{
			SpeechManager = speechmanager;
			ArmManager = armManager;
			HeadManager = headManager;
			TimeManager = timeManager;
			EmotionManager = emotionManager;
			SpeechIntentManager = speechIntentManager;
			AnimationManager = animationManager;
			LocomotionManager = locomotionManager;
		}

		public ISpeechManager SpeechManager { get; set; }
		public IArmManager ArmManager { get; set; }
		public IHeadManager HeadManager { get; set; }
		public ITimeManager TimeManager { get; set; }
		public IEmotionManager EmotionManager { get; set; }
		public ISpeechIntentManager SpeechIntentManager { get; set; }
		public IAnimationManager AnimationManager { get; set; }
		public ILocomotionManager LocomotionManager { get; set; }
	}
}