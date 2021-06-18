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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conversation.Common;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;

namespace MistyCharacter
{
	public enum LocomotionStatus
	{
		Unknown,
		Initialized,
		Starting,
		Stopped,
		ScriptDriving,
		WaypointDriving,
		RecalculatingRoute,
		PathBlocked,
		Wandering
	}

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

	public enum LocomotionCommand
	{
		Drive,
		Heading,
		Turn,
		Arc,
		Waypoint,
		Wander,
		Return,//ToLastWaypoint
		Stop,
		Halt,
		TurnHeading,
	}

	public class LocomotionAction
	{
		public AnimationRequest AnimationRequest { get; set; }
		public ConversationData Conversation { get; set; }

		public LocomotionCommand Action { get; set; }
		public double? DistanceMeters { get; set; }
		public int? TimeMs { get; set; }
		public double? Velocity { get; set; }
		public double? Degrees { get; set; }
		public double? Heading { get; set; }
		public double? Radius { get; set; }
		public bool Reverse { get; set; }

		public bool AllowRerouting { get; set; } = true;
	}

	public class LocomotionManager : BaseManager, ILocomotionManager
	{
		public event EventHandler<LocomotionAction> StartedLocomotionAction;
		public event EventHandler<LocomotionAction> CompletedLocomotionAction;
		public event EventHandler<LocomotionAction> LocomotionFailed;
		public event EventHandler<LocomotionAction> LocomotionStopped;

		public event EventHandler<LocomotionAction> ReachedDestination;
		public event EventHandler<LocomotionAction> PassingWaypoint;
		public event EventHandler<LocomotionAction> TryingNewRoute;
		public event EventHandler<IIMUEvent> IMUEvent;

		public LocomotionState CurrentLocomotionState { get; private set; } = new LocomotionState();
		
		public LocomotionManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters)
			: base(misty, parameters, characterParameters)
		{
			CurrentLocomotionState.LocomotionStatus = LocomotionStatus.Unknown;
		}

		public override Task<bool> Initialize()
		{
			RegisterEvents();
			CurrentLocomotionState.LocomotionStatus = LocomotionStatus.Initialized;
			return Task.FromResult(true);
		}
		
		/// <summary>
		/// Handle loco actions for this character
		/// </summary>
		public async Task HandleLocomotionAction(LocomotionAction locomotionAction)
		{
			try
			{
				CurrentLocomotionState.LocomotionAction = locomotionAction;
				CurrentLocomotionState.LocomotionStatus = LocomotionStatus.Starting;

				StartedLocomotionAction?.Invoke(this, locomotionAction);
				//based upon request, do stuff and other things

				switch (locomotionAction.Action)
				{
					case LocomotionCommand.Stop:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.Stopped;
						await Robot.StopAsync();
						LocomotionStopped?.Invoke(this, locomotionAction);
						break;
					case LocomotionCommand.Halt:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.Stopped;
						await Robot.HaltAsync(new List<MotorMask> { MotorMask.AllMotors });
						LocomotionStopped?.Invoke(this, locomotionAction);
						break;
					case LocomotionCommand.Turn:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.ScriptDriving;
						await Turn((double)locomotionAction.Degrees, (int)locomotionAction.TimeMs);
						break;
					case LocomotionCommand.TurnHeading:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.ScriptDriving;
						await TurnHeading((double)locomotionAction.Heading, (int)locomotionAction.TimeMs);
						break;
					case LocomotionCommand.Arc:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.ScriptDriving;
						await Arc((double)locomotionAction.Degrees, (double)locomotionAction.Radius, (int)locomotionAction.TimeMs, locomotionAction.Reverse);
						break;
					case LocomotionCommand.Heading:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.ScriptDriving;
						await Heading((double)locomotionAction.Heading, (double)locomotionAction.DistanceMeters, (int)locomotionAction.TimeMs, locomotionAction.Reverse);
						break;
					case LocomotionCommand.Drive:
						CurrentLocomotionState.LocomotionStatus = LocomotionStatus.ScriptDriving;
						await Drive((double)locomotionAction.DistanceMeters, (int)locomotionAction.TimeMs, locomotionAction.Reverse);
						break;
				}

				CompletedLocomotionAction?.Invoke(this, locomotionAction);
			}
			catch
			{
				//TODO
				LocomotionFailed?.Invoke(this, locomotionAction);
			}
		}

		private async Task Turn(double degrees, int timeMs)
		{
			await Robot.DriveArcAsync(CurrentLocomotionState.RobotYaw + degrees, 0, timeMs, false);
		}

		private async Task TurnHeading(double heading, int timeMs)
		{
			await Robot.DriveArcAsync(heading, 0, timeMs, false);
		}

		private async Task Arc(double degrees, double radius, int timeMs, bool reverse = false)
		{
			await Robot.DriveArcAsync(CurrentLocomotionState.RobotYaw + degrees, radius, timeMs, reverse);
		}
		
		private async Task Heading(double heading, double distance, int timeMs, bool reverse = false)
		{
			await Robot.DriveHeadingAsync(heading, distance, timeMs, reverse);
		}

		private async Task Drive(double distance, int timeMs, bool reverse = false)
		{
			await Robot.DriveHeadingAsync(CurrentLocomotionState.RobotYaw, distance, timeMs, reverse);
		}
		
		private void EncoderCallback(IDriveEncoderEvent encoderEvent)
		{
			CurrentLocomotionState.LeftDistanceSinceLastStop = encoderEvent.LeftDistance;
			CurrentLocomotionState.RightDistanceSinceLastStop = encoderEvent.RightDistance;
			CurrentLocomotionState.LeftVelocity = encoderEvent.LeftVelocity;
			CurrentLocomotionState.RightVelocity = encoderEvent.RightVelocity;			
		}
		
		private void RegisterEvents()
		{
			//Register Bump Sensors with a callback
			Robot.RegisterBumpSensorEvent(BumpCallback, 0, true, null, null, null);

			//Front Right Time of Flight
			List<TimeOfFlightValidation> tofFrontRightValidations = new List<TimeOfFlightValidation>();
			tofFrontRightValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.FrontRight });
			Robot.RegisterTimeOfFlightEvent(TOFFRRangeCallback, 0, true, tofFrontRightValidations, "FrontRight", null);

			//Front Left Time of Flight
			List<TimeOfFlightValidation> tofFrontLeftValidations = new List<TimeOfFlightValidation>();
			tofFrontLeftValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.FrontLeft });
			Robot.RegisterTimeOfFlightEvent(TOFFLRangeCallback, 0, true, tofFrontLeftValidations, "FrontLeft", null);

			//Front Center Time of Flight
			List<TimeOfFlightValidation> tofFrontCenterValidations = new List<TimeOfFlightValidation>();
			tofFrontCenterValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.FrontCenter });
			Robot.RegisterTimeOfFlightEvent(TOFCRangeCallback, 0, true, tofFrontCenterValidations, "FrontCenter", null);

			//Back Time of Flight
			List<TimeOfFlightValidation> tofBackValidations = new List<TimeOfFlightValidation>();
			tofBackValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.Back });
			Robot.RegisterTimeOfFlightEvent(TOFBRangeCallback, 0, true, tofBackValidations, "Back", null);

			//Setting debounce a little higher to avoid too much traffic
			//Firmware will do the actual stop for edge detection
			List<TimeOfFlightValidation> tofFrontRightEdgeValidations = new List<TimeOfFlightValidation>();
			tofFrontRightEdgeValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.DownwardFrontRight });
			Robot.RegisterTimeOfFlightEvent(FrontEdgeCallback, 1000, true, tofFrontRightEdgeValidations, "FREdge", null);

			List<TimeOfFlightValidation> tofFrontLeftEdgeValidations = new List<TimeOfFlightValidation>();
			tofFrontLeftEdgeValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.DownwardFrontLeft });
			Robot.RegisterTimeOfFlightEvent(FrontEdgeCallback, 1000, true, tofFrontLeftEdgeValidations, "FLEdge", null);

			IList<DriveEncoderValidation> driveValidations = new List<DriveEncoderValidation>();
			LogEventDetails(Robot.RegisterDriveEncoderEvent(EncoderCallback, 250, true, driveValidations, "DriveEncoder", null));

			LogEventDetails(Robot.RegisterIMUEvent(IMUCallback, 50, true, null, "IMU", null));

		}

		private bool TryGetAdjustedDistance(ITimeOfFlightEvent tofEvent, out double distance)
		{
			distance = 0;
			//   0 = valid range data
			// 101 = sigma fail - lower confidence but most likely good
			// 104 = Out of bounds - Distance returned is greater than distance we are confident about, but most likely good
			if (tofEvent.Status == 0 || tofEvent.Status == 101 || tofEvent.Status == 104)
			{
				distance = tofEvent.DistanceInMeters;
			}
			else if (tofEvent.Status == 102)
			{
				//102 generally indicates nothing substantial is in front of the robot so the TOF is returning the floor as a close distance
				//So ignore the distance returned and just set to 2 meters
				distance = 2;
			}
			else
			{
				//TOF returning uncertain data or really low confidence in distance, ignore value 
				return false;
			}
			return true;
		}

		private void IMUCallback(IIMUEvent imuEvent)
		{
			CurrentLocomotionState.RobotPitch = imuEvent.Pitch;
			CurrentLocomotionState.RobotYaw = imuEvent.Yaw;
			CurrentLocomotionState.RobotRoll = imuEvent.Roll;
			CurrentLocomotionState.XAcceleration = imuEvent.XAcceleration;
			CurrentLocomotionState.YAcceleration = imuEvent.YAcceleration;
			CurrentLocomotionState.ZAcceleration = imuEvent.ZAcceleration;
			CurrentLocomotionState.PitchVelocity = imuEvent.PitchVelocity;
			CurrentLocomotionState.RollVelocity = imuEvent.RollVelocity;
			CurrentLocomotionState.YawVelocity = imuEvent.YawVelocity;
		}

		private void TOFFLRangeCallback(ITimeOfFlightEvent tofEvent)
		{
			if (TryGetAdjustedDistance(tofEvent, out double distance))
			{
				CurrentLocomotionState.FrontLeftTOF = distance;
			}
		}

		private void TOFFRRangeCallback(ITimeOfFlightEvent tofEvent)
		{
			if (TryGetAdjustedDistance(tofEvent, out double distance))
			{
				CurrentLocomotionState.FrontRightTOF = distance;
			}
		}

		private void TOFCRangeCallback(ITimeOfFlightEvent tofEvent)
		{
			if (TryGetAdjustedDistance(tofEvent, out double distance))
			{
				CurrentLocomotionState.FrontCenterTOF = distance;
			}
		}

		private void TOFBRangeCallback(ITimeOfFlightEvent tofEvent)
		{
			if (TryGetAdjustedDistance(tofEvent, out double distance))
			{
				CurrentLocomotionState.BackTOF = distance;
			}
		}

		private void BumpCallback(IBumpSensorEvent bumpEvent)
		{
			switch (bumpEvent.SensorPosition)
			{
				case BumpSensorPosition.FrontRight:
					if (bumpEvent.IsContacted)
					{
						CurrentLocomotionState.FrontRightBumpContacted = true;
					}
					else
					{
						CurrentLocomotionState.FrontRightBumpContacted = false;
					}
					break;
				case BumpSensorPosition.FrontLeft:
					if (bumpEvent.IsContacted)
					{
						CurrentLocomotionState.FrontLeftBumpContacted = true;
					}
					else
					{
						CurrentLocomotionState.FrontLeftBumpContacted = false;
					}
					break;
				case BumpSensorPosition.BackRight:
					if (bumpEvent.IsContacted)
					{
						CurrentLocomotionState.BackRightBumpContacted = true;
					}
					else
					{
						CurrentLocomotionState.BackRightBumpContacted = false;
					}
					break;
				case BumpSensorPosition.BackLeft:
					if (bumpEvent.IsContacted)
					{
						CurrentLocomotionState.BackLeftBumpContacted = true;
					}
					else
					{
						CurrentLocomotionState.BackLeftBumpContacted = false;
					}
					break;
			}
		}

		private void FrontEdgeCallback(ITimeOfFlightEvent edgeEvent)
		{
			switch (edgeEvent.SensorPosition)
			{
				case TimeOfFlightPosition.DownwardFrontRight:
					CurrentLocomotionState.FrontRightEdgeTOF = edgeEvent.DistanceInMeters;
					break;
				case TimeOfFlightPosition.DownwardFrontLeft:
					CurrentLocomotionState.FrontLeftEdgeTOF = edgeEvent.DistanceInMeters;
					break;
			}
		}

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
           
            if (!_isDisposed)
			{
				if (disposing)
				{
			 	}

				_isDisposed = true;
            }
        }

        public void Dispose()
		{
			Dispose(true);
		}
	}
}
 
 