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
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ConversationBuilder.DataModels
{
	public class HeadLocation : IEditableData
	{

		[Required]
		public string Name { get; set; }
		public string Id { get; set; }
		public string ItemType { get; set; } = DataItemType.HeadLocation.ToString();

		[Display(Name = "Min Pitch")]
		public double? MinPitch { get; set; }
		
		[Display(Name = "Min Roll")]
		public double? MinRoll { get; set; }
		
		[Display(Name = "Min Yaw")]
		public double? MinYaw { get; set; }
		
		[Display(Name = "Max Pitch")]
		public double? MaxPitch { get; set; }
		
		[Display(Name = "Max Roll")]
		public double? MaxRoll { get; set; }
		
		[Display(Name = "Max Yaw")]
		public double? MaxYaw { get; set; }
		
		[Display(Name = "Follow faces?")]
		public bool FollowFace { get; set; } = true;
		
		[Display(Name = "Name of object to follow")]
		public string FollowObject { get; set; }
		
		[Display(Name = "Delay in seconds of not seeing an expected face/object, before looking around")]
		public double? StartLookAroundOnLostObject { get; set; }
		
		[Display(Name = "Duration of movement in seconds - use this or Velocity")]
		public double? MovementDuration { get; set; }
		
		[Display(Name = "Delay in seconds between repeated movements")]
		public double DelayBetweenMovements { get; set; }
		
		[Display(Name = "Velocity (0 - 100) - use this or Duration")]
		public int? MovementVelocity { get; set; }
		
		[Display(Name = "Look randomly within range if not following.")]
		public bool RandomRange { get; set; } = true;
		public DateTimeOffset Created { get; set; }
		
		public DateTimeOffset Updated { get; set; }

		[Display(Name = "Management Access (beta)")]
		public string ManagementAccess { get; set; } = "Shared";

		public string CreatedBy { get; set; }
	}
}
