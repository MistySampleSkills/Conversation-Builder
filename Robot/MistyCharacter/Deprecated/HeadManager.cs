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
using Conversation.Common;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;

namespace MistyCharacter
{
	/// <summary>
	/// //Deprecated, should go away!
	/// </summary>
	public class HeadManager : BaseManager, IHeadManager
	{
		private IObjectDetectionEvent _lastObjectEvent;
		private IObjectDetectionEvent _lastPersonEvent;
        private object _timerLock = new object();
		private Timer _moveHeadTimer;
		private Random _random = new Random();
		private double? _lastPitch = 0;
		private double? _lastYaw = 0;
		private double? _lastActuatorYaw;
		private double? _lastActuatorPitch;
		private bool _headMovingContinuously;
		private object _findFaceLock = new object();
		private HeadLocation _currentHeadRequest = new HeadLocation(null, null, null);
		private DateTime _followedObjectLastSeen = DateTime.Now;
		private DateTime _faceLastSeen = DateTime.Now;
		private DateTime _lastHandledFaceTime = DateTime.Now;
		private DateTime _lastMovementCommand = DateTime.Now;
		private bool _tick = false;
		int? _currentElevation = null;
		int? _currentBearing = null;
		private bool _finding = false;
		private bool _findingPersonObject = false;
		bool _handlingMove = false;

		public HeadManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters)
			: base(misty, parameters, characterParameters)
		{
			_currentHeadRequest = new HeadLocation(null, null, null);
		}
		public void StopMovement()
		{
			_moveHeadTimer?.Dispose();
		}

		public void HandleHeadAction(HeadLocation headLocation)
		{
			if (headLocation != null)
			{
				_currentHeadRequest = headLocation;
				if (_currentHeadRequest.MaxPitch == null && _currentHeadRequest.MinPitch == null &&
					_currentHeadRequest.MaxYaw == null && _currentHeadRequest.MinYaw == null &&
					_currentHeadRequest.MinRoll == null && _currentHeadRequest.MaxRoll == null)
				{
					//do nuthin
					return;
				}

				if (_currentHeadRequest.MaxPitch != null && _currentHeadRequest.MaxPitch >= RobotConstants.MaximumPitchDegreesExclusive)
				{
					_currentHeadRequest.MaxPitch = RobotConstants.MaximumPitchDegreesExclusive - 1;
				}

				if (_currentHeadRequest.MaxYaw != null && _currentHeadRequest.MaxYaw >= RobotConstants.MaximumYawDegreesExclusive)
				{
					_currentHeadRequest.MaxYaw = RobotConstants.MaximumYawDegreesExclusive - 1;
				}

				if (_currentHeadRequest.MaxRoll != null && _currentHeadRequest.MaxRoll >= RobotConstants.MaximumRollDegreesExclusive)
				{
					_currentHeadRequest.MaxRoll = RobotConstants.MaximumRollDegreesExclusive - 1;
				}

				if (_currentHeadRequest.MinPitch != null && _currentHeadRequest.MinPitch < RobotConstants.MinimumPitchDegreesInclusive)
				{
					_currentHeadRequest.MinPitch = RobotConstants.MinimumPitchDegreesInclusive;
				}

				if (_currentHeadRequest.MinYaw != null && _currentHeadRequest.MinYaw < RobotConstants.MinimumYawDegreesInclusive)
				{
					_currentHeadRequest.MinYaw = RobotConstants.MinimumYawDegreesInclusive;
				}

				if (_currentHeadRequest.MinRoll != null && _currentHeadRequest.MinRoll < RobotConstants.MinimumRollDegreesInclusive)
				{
					_currentHeadRequest.MinRoll = RobotConstants.MinimumRollDegreesInclusive;
				}

				if (!_currentHeadRequest.FollowFace && string.IsNullOrWhiteSpace(_currentHeadRequest.FollowObject) &&
					_currentHeadRequest.MinRoll == _currentHeadRequest.MaxRoll &&
					_currentHeadRequest.MinPitch == _currentHeadRequest.MaxPitch &&
					_currentHeadRequest.MinYaw == _currentHeadRequest.MaxYaw)
				{
					_headMovingContinuously = false;
					lock (_timerLock)
					{
						_moveHeadTimer?.Dispose();
					}

					if (_currentHeadRequest.MovementDuration != null && _currentHeadRequest.MovementDuration > 0)
					{
						_lastMovementCommand = DateTime.Now;
						Robot.MoveHead(_currentHeadRequest.MaxPitch, _currentHeadRequest.MaxRoll, _currentHeadRequest.MaxYaw, null, (int)Math.Abs((double)_currentHeadRequest.MovementDuration), AngularUnit.Degrees, null);
					}
					else if (_currentHeadRequest.MovementVelocity != null && _currentHeadRequest.MovementVelocity > 0)
					{
						_lastMovementCommand = DateTime.Now;
						Robot.MoveHead(_currentHeadRequest.MaxPitch, _currentHeadRequest.MaxRoll, _currentHeadRequest.MaxYaw, (int)Math.Abs((int)_currentHeadRequest.MovementVelocity), null, AngularUnit.Degrees, null);
					}
				}
				else
				{
					if (_currentHeadRequest.FollowFace || !string.IsNullOrWhiteSpace(_currentHeadRequest.FollowObject) && !_headMovingContinuously)
					{
						_headMovingContinuously = true;
						lock (_timerLock)
						{
							_moveHeadTimer?.Dispose();
							if (!_isDisposed)
							{
								//Dealing with ye-olde options
								double? headDelay = _currentHeadRequest.FollowRefresh != null && _currentHeadRequest.FollowRefresh > 0 ? _currentHeadRequest.FollowRefresh : (_currentHeadRequest.DelayBetweenMovements);
								if (headDelay != null && headDelay != 0)
								{
									_moveHeadTimer = new Timer(MoveHeadCallback, null, (int)Math.Abs((double)headDelay * 1000), (int)Math.Abs((double)headDelay * 1000));
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Handle head actions for this character
		/// </summary>
		public void HandleHeadAction(AnimationRequest animationRequest, ConversationData conversation)
		{
			if (!string.IsNullOrWhiteSpace(animationRequest.HeadLocation))
			{
				HeadLocation headLocation = conversation.HeadLocations.FirstOrDefault(x => x.Id == animationRequest.HeadLocation);
				HandleHeadAction(headLocation);
			}
		}

		public void HandleFaceRecognitionEvent(object sender, IFaceRecognitionEvent faceRecognitionEvent)
		{
			
			if (faceRecognitionEvent.Bearing >= -1 && faceRecognitionEvent.Bearing <= 1 && faceRecognitionEvent.Elevation >= -1 && faceRecognitionEvent.Elevation <= 1)
			{
				return;
			}
				
			_faceLastSeen = DateTime.Now;
			_currentElevation = faceRecognitionEvent.Elevation;
			_currentBearing = faceRecognitionEvent.Bearing;			
		}
		
		public void HandleObjectDetectionEvent(object sender, IObjectDetectionEvent objEvent)
		{
			try
			{
				if(objEvent.Description == "person")
				{
					_followedObjectLastSeen = DateTime.Now;
					_lastPersonEvent = objEvent;
					_lastObjectEvent = null;
				}
				else if (objEvent.Description.ToLower() == _currentHeadRequest?.FollowObject?.ToLower())
				{
					_followedObjectLastSeen = DateTime.Now;
					_lastObjectEvent = objEvent;
					_lastPersonEvent = null;
				}
				else
				{
					_lastObjectEvent = null;
					_lastPersonEvent = null;
				}
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log("Failed processing face event.", ex);
			}
		}

		//TODO Lotso cleanup
		private void MoveHeadCallback(object timerData)
		{
			try
			{
				if(_lastHandledFaceTime == _faceLastSeen)
				{
					return;
				}
				_lastHandledFaceTime = _faceLastSeen;

				if (_handlingMove)
				{
					return;
				}
				_handlingMove = true;

				if (_headMovingContinuously)
				{
					_tick = !_tick;

					bool _lookAroundFailover = false;
					if (_currentHeadRequest.FollowFace)
					{
						if (_currentElevation != null && _currentBearing != null)
						{
							
							Robot.MoveHead(_currentElevation + CharacterParameters.FacePitchOffset, _random.Next(0, 5), _currentBearing, null, (int)Math.Abs(CharacterParameters.ObjectDetectionDebounce), AngularUnit.Degrees, null);
							_currentElevation = null;
							_currentBearing = null;
						}
						else if (_lastPersonEvent != null)
						{
							FindAndFollowPerson(_lastPersonEvent);
							return;
						}
						//else if (_currentHeadRequest.StartLookAroundOnLostObject != null && _currentHeadRequest.StartLookAroundOnLostObject > 0)
						//{
						//	//Look around if lost it for too long
						//	if (_followedObjectLastSeen < DateTime.Now.AddSeconds(-(double)_currentHeadRequest.StartLookAroundOnLostObject) &&
						//		_lastMovementCommand < DateTime.Now.AddSeconds(-_currentHeadRequest.DelayBetweenMovements))
						//	{
						//		_lookAroundFailover = true;
						//	}
						//}
					}
					else if (!string.IsNullOrWhiteSpace(_currentHeadRequest.FollowObject))
					{
						if (_lastObjectEvent != null && _lastObjectEvent.Description.ToLower() == _currentHeadRequest.FollowObject.ToLower())
						{
							FindAndFollowObject(_lastObjectEvent);
							return;
						}
						//else if (_currentHeadRequest.StartLookAroundOnLostObject != null && _currentHeadRequest.StartLookAroundOnLostObject > 0)
						//{
						//	//Look around if lost it for too long
						//	if (_followedObjectLastSeen < DateTime.Now.AddSeconds(-(double)_currentHeadRequest.StartLookAroundOnLostObject) &&
						//		_lastMovementCommand < DateTime.Now.AddSeconds(-_currentHeadRequest.DelayBetweenMovements))
						//	{
						//		_lookAroundFailover = true;
						//	}
						//}
					}

					//Else, look around using ranges provided
					if (_lookAroundFailover || (!_currentHeadRequest.FollowFace && string.IsNullOrWhiteSpace(_currentHeadRequest.FollowObject)))
					{
						//if the Min and Max of a movement match or one of them is null, go to that point exactly
						//if both are null, don't move that axis
						//if different, 
						//then check if this is an exact range movement
						// if it is, start to go there (whether you make it depends on the duration/velocity before the next movement is started
						// if not, pick a random range and try to go there...

						//Goto location if given range
						double? newPitch = null;
						if (_currentHeadRequest.MinPitch != null && _currentHeadRequest.MaxPitch != null)
						{
							if (_currentHeadRequest.MinPitch == _currentHeadRequest.MaxPitch)
							{
								newPitch = _currentHeadRequest.MinPitch;
							}
							else
							{
								double? minPitch = _currentHeadRequest.MinPitch < _currentHeadRequest.MaxPitch ? _currentHeadRequest.MinPitch : _currentHeadRequest.MaxPitch;
								double? maxPitch = _currentHeadRequest.MinPitch < _currentHeadRequest.MaxPitch ? _currentHeadRequest.MaxPitch : _currentHeadRequest.MinPitch;

								if (_currentHeadRequest.RandomRange)
								{
									newPitch = _random.Next((int)minPitch, (int)maxPitch);
								}
								else if (_tick)
								{
									newPitch = minPitch;
								}
								else
								{
									newPitch = maxPitch;
								}
							}
						}
						else if (_currentHeadRequest.MinPitch != null && _currentHeadRequest.MaxPitch == null)
						{
							newPitch = _currentHeadRequest.MinPitch;
						}
						else if (_currentHeadRequest.MinPitch == null && _currentHeadRequest.MaxPitch != null)
						{
							newPitch = _currentHeadRequest.MaxPitch;
						}

						double? newRoll = null;
						if (_currentHeadRequest.MinRoll != null && _currentHeadRequest.MaxRoll != null)
						{
							if (_currentHeadRequest.MinRoll == _currentHeadRequest.MaxRoll)
							{
								newRoll = _currentHeadRequest.MinRoll;
							}
							else
							{
								double? minRoll = _currentHeadRequest.MinRoll < _currentHeadRequest.MaxRoll ? _currentHeadRequest.MinRoll : _currentHeadRequest.MaxRoll;
								double? maxRoll = _currentHeadRequest.MinRoll < _currentHeadRequest.MaxRoll ? _currentHeadRequest.MaxRoll : _currentHeadRequest.MinRoll;
								newRoll = _random.Next((int)minRoll, (int)maxRoll);
							}
						}
						else if (_currentHeadRequest.MinRoll != null && _currentHeadRequest.MaxRoll == null)
						{
							newRoll = _currentHeadRequest.MinRoll;
						}
						else if (_currentHeadRequest.MinRoll == null && _currentHeadRequest.MaxRoll != null)
						{
							newRoll = _currentHeadRequest.MaxRoll;
						}

						double? newYaw = null;
						if (_currentHeadRequest.MinYaw != null && _currentHeadRequest.MaxYaw != null)
						{
							if (_currentHeadRequest.MinYaw == _currentHeadRequest.MaxYaw)
							{
								newYaw = _currentHeadRequest.MinYaw;
							}
							else
							{
								double? minYaw = _currentHeadRequest.MinYaw < _currentHeadRequest.MaxYaw ? _currentHeadRequest.MinYaw : _currentHeadRequest.MaxYaw;
								double? maxYaw = _currentHeadRequest.MinYaw < _currentHeadRequest.MaxYaw ? _currentHeadRequest.MaxYaw : _currentHeadRequest.MinYaw;
								newYaw = _random.Next((int)minYaw, (int)maxYaw);
							}
						}
						else if (_currentHeadRequest.MinYaw != null && _currentHeadRequest.MaxYaw == null)
						{
							newYaw = _currentHeadRequest.MinYaw;
						}
						else if (_currentHeadRequest.MinYaw == null && _currentHeadRequest.MaxYaw != null)
						{
							newYaw = _currentHeadRequest.MaxYaw;
						}

						if (_currentHeadRequest.MovementDuration != null && _currentHeadRequest.MovementDuration > 0)
						{
							_lastMovementCommand = DateTime.Now;
							Robot.MoveHead(newPitch, newRoll, newYaw, null, (int)Math.Abs((double)_currentHeadRequest.MovementDuration), AngularUnit.Degrees, null);
						}
						else if (_currentHeadRequest.MovementVelocity != null && _currentHeadRequest.MovementVelocity > 0)
						{
							_lastMovementCommand = DateTime.Now;
							Robot.MoveHead(newPitch, newRoll, newYaw, (int)Math.Abs((int)_currentHeadRequest.MovementVelocity), null, AngularUnit.Degrees, null);
						}
					}
				}
			}
			catch
			{
				
			}
			finally
			{
				_handlingMove = false;
			}		
		}
		
		public void HandleActuatorEvent(object sender, IActuatorEvent actuatorEvent)
		{
			switch(actuatorEvent.SensorPosition)
			{
				case ActuatorPosition.HeadPitch:
					_lastActuatorPitch = actuatorEvent.ActuatorValue;
					break;
				case ActuatorPosition.HeadYaw:
					_lastActuatorYaw = actuatorEvent.ActuatorValue;
					break;
			}
		}

		protected void FindAndFollowObject(IObjectDetectionEvent objEvent)
		{
			try
			{
				if(_finding)
				{
					return;
				}
				_finding = true;
				double? yaw = Math.Abs(objEvent.Yaw) > 0.02 ? (double?)(_lastYaw ?? 0) - (objEvent.Yaw * 10) : null;
				double? pitch = Math.Abs(objEvent.Pitch) > 0.01 ? (double?)(_lastPitch ?? 0) + (objEvent.Pitch * 10) : null;			

				if (pitch == null && yaw == null)
				{
					return;
				}

				if (pitch < RobotConstants.MinimumPitchDegreesInclusive)
				{
					pitch = RobotConstants.MinimumPitchDegreesInclusive;
				}
				if (pitch >= RobotConstants.MaximumPitchDegreesExclusive)
				{
					pitch = RobotConstants.MaximumPitchDegreesExclusive - 1;
				}

				if (yaw < RobotConstants.MinimumYawDegreesInclusive)
				{
					yaw = RobotConstants.MinimumYawDegreesInclusive;
				}
				if (yaw >= RobotConstants.MaximumYawDegreesExclusive)
				{
					yaw = RobotConstants.MaximumYawDegreesExclusive - 1;
				}

				_lastMovementCommand = DateTime.Now;
				Robot.MoveHead(pitch, null, yaw, null, _currentHeadRequest.MovementDuration, AngularUnit.Degrees, null);

				_lastYaw = yaw;
				_lastPitch = pitch;

			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log("Failed processing FindAndFollowObject request.", ex);
			}
			finally
			{
				_finding = false;
			}
		}

		/// <summary>
		/// Borrowed from CPs JS skill
		/// </summary>
		/// <param name="objEvent"></param>
		protected void FindAndFollowPerson(IObjectDetectionEvent objEvent)
		{
			try
			{
				if (_findingPersonObject)
				{
					return;
				}
				_findingPersonObject = true;
				double? yaw = objEvent.Yaw*10;
				double? pitch = objEvent.Pitch*10;
			
				if (pitch != null || yaw != null)
				{
					if (_currentHeadRequest.MovementDuration != null && _currentHeadRequest.MovementDuration > 0)
					{
						_lastMovementCommand = DateTime.Now;
						Robot.MoveHead(pitch, null, yaw, null, _currentHeadRequest.MovementDuration, AngularUnit.Degrees, null);
					}
					else if (_currentHeadRequest.MovementVelocity != null && _currentHeadRequest.MovementVelocity > 0)
					{
						_lastMovementCommand = DateTime.Now;
						Robot.MoveHead(pitch, null, yaw, Math.Abs((int)_currentHeadRequest.MovementVelocity), null, AngularUnit.Degrees, null);
					}
				}
					
				_lastPitch = pitch ?? _lastPitch;
				_lastYaw = yaw ?? _lastYaw;
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log("Failed processing FindAndFollowPerson request.", ex);
			}
			finally
			{
				_findingPersonObject = false;
			}
		}
		
		
		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
            lock (_timerLock)
            {
                if (!_isDisposed)
			    {
				    if (disposing)
				    {
						_moveHeadTimer?.Dispose();
						Robot.UnregisterAllEvents(null);
					    _headMovingContinuously = false;                    	
             	    }

				    _isDisposed = true;
			    }
            }
        }

        public void Dispose()
		{
			Dispose(true);
		}
	}
}
 