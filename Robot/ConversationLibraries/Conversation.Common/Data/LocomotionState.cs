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

namespace Conversation.Common
{
	public class LocomotionState
	{
		public double FrontLeftTOF { get; set; }

		public double FrontRightTOF { get; set; }

		public double FrontCenterTOF { get; set; }

		public double BackTOF { get; set; }

		public bool BackLeftBumpContacted { get; set; }

		public bool BackRightBumpContacted { get; set; }

		public bool FrontLeftBumpContacted { get; set; }

		public bool FrontRightBumpContacted { get; set; }

		public double FrontRightEdgeTOF { get; set; }

		public double FrontLeftEdgeTOF { get; set; }

		public double BackRightEdgeTOF { get; set; }

		public double BackLeftEdgeTOF { get; set; }

		public double LeftVelocity { get; set; }
		public double RightVelocity { get; set; }

		public double LeftDistanceSinceLastStop { get; set; }
		public double RightDistanceSinceLastStop { get; set; }

		public double LeftDistanceSinceWayPoint { get; set; }
		public double RightDistanceSinceWayPoint { get; set; }

		public double LeftDistanceSinceStart { get; set; }
		public double RightDistanceSinceStart { get; set; }

		public string[] MovementHistory { get; set; }

		public LocomotionStatus LocomotionStatus { get; set; }

		public LocomotionAction LocomotionAction { get; set; }

		public double RobotPitch { get; set; }
		public double RobotYaw { get; set; }
		public double RobotRoll { get; set; }

		public double XAcceleration { get; set; }
		public double YAcceleration { get; set; }
		public double ZAcceleration { get; set; }

		public double PitchVelocity { get; set; }
		public double RollVelocity { get; set; }
		public double YawVelocity { get; set; }

		public double? LockedHeading { get; set; }
	}
}