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
using MistyRobotics.SDK.Events;

namespace Conversation.Common
{
	public class TriggerEvent
	{
		public string EventName { get; set; }
		public string Source { get; set; }
		public string Originator { get; set; }
		public DateTimeOffset Created { get; set; }		
		public string Text { get; set; }
		public string TriggerFilter { get; set; }
		public string Trigger { get; set; }		
		public bool OverrideIntent { get; set; }

		public static TriggerEvent GetMatchTriggerData(IUserEvent userEvent)
		{
			return GetTriggerData(userEvent);
		}

		public static TriggerEvent GetCheckTriggerData(IUserEvent userEvent)
		{
			return GetTriggerData(userEvent, "LatestTriggerCheck");
		}
		
		private static TriggerEvent GetTriggerData(IUserEvent userEvent, string triggerType = "LatestTriggerMatch")
		{
			try
			{ 
				TriggerEvent triggerEvent = new TriggerEvent();
				triggerEvent.EventName = userEvent.EventName;
				triggerEvent.Source = userEvent.Source;
				triggerEvent.Originator = userEvent.EventOriginator.ToString();
				triggerEvent.Created = userEvent.Created;

				if (userEvent.TryGetPayload(out IDictionary<string, object> payload) && !string.IsNullOrWhiteSpace(triggerType))
				{
					if(payload.TryGetValue(triggerType, out object matchData))
					{
						KeyValuePair<DateTime, TriggerData> triggerData = Newtonsoft.Json.JsonConvert.DeserializeObject< KeyValuePair<DateTime, TriggerData>>(Convert.ToString(matchData));

						if(triggerData.Value != null)
						{
							triggerEvent.Text = triggerData.Value.Text;
							triggerEvent.TriggerFilter = triggerData.Value.TriggerFilter;
							triggerEvent.Trigger = triggerData.Value.Trigger;
							triggerEvent.OverrideIntent = triggerData.Value.OverrideIntent;						
						}
					}
				}

				return triggerEvent;
			}
			catch
			{
				return null;
			}
		}		
	}
}