﻿/**********************************************************************
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
using Conversation.Common;
using MistyRobotics.SDK.Messengers;

namespace MistyInteraction
{
	public class TimeManager : BaseManager, ITimeManager
	{
		public TimeManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters)
			: base(misty, parameters, characterParameters) { }
		
		public TimeObject GetTimeObject()
		{
			DateTime now = DateTime.Now.ToLocalTime();
			TimeObject timeObject = new TimeObject
			{
				Timestamp = now,
				Description = TimeDescription.Unknown
			};

			timeObject.IsPm = false;
			int hour = now.Hour;

			if (hour >= 4 && hour < 12)
			{
				timeObject.Description = TimeDescription.Morning;
			}
			else if (hour >= 12 && hour < 17)
			{
				timeObject.Description = TimeDescription.Afternoon;
			}
			else if (hour >= 17 && hour < 21)
			{
				timeObject.Description = TimeDescription.Evening;
			}
			else
			{
				timeObject.Description = TimeDescription.Night;
			}

			if (hour >= 12)
			{
				timeObject.IsPm = true;
				hour = hour - 12;
			}

			if (hour == 0)
			{
				hour = 12;
			}

			timeObject.SpokenDay = now.DayOfWeek;

			//get proper minute string
			int minute = now.Minute;
			string minuteString = "";
			if (minute > 0 && minute < 10)
			{
				minuteString = " oh " + minute;
			}
			else if(minute == 0)
			{
				minuteString = "";
			}
			else			
			{
				minuteString = minute.ToString();
			}

			//get time
			timeObject.SpokenTime = hour + " " + minuteString + " " + (timeObject.IsPm ? "P.M." : "A.M.");

			return timeObject;
		}

		//This implementation doesn't use dispose yet, but following manager pattern
		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing) { }

				_isDisposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
	}
}