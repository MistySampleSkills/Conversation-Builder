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
	public class HeadManager : BaseManager, IHeadManager
	{
		public event EventHandler<IActuatorEvent> HeadPitchActuatorEvent;
		public event EventHandler<IActuatorEvent> HeadYawActuatorEvent;
		public event EventHandler<IActuatorEvent> HeadRollActuatorEvent;		
		public event EventHandler<IObjectDetectionEvent> ObjectEvent;

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
		private bool _tick = false;
		private int _lookAroundThrottle = 0;
		
		public HeadManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters)
			: base(misty, parameters, characterParameters)
		{
			Robot.UnregisterEvent("GenericODEvent", null);
			Robot.UnregisterEvent("ODEventForObjectFollow", null);			
			Robot.UnregisterEvent("HeadYaw", null);
			Robot.UnregisterEvent("HeadPitch", null);

			_currentHeadRequest = new HeadLocation(null, null, null);

			//Person object, used for following face
			List<ObjectValidation> personValidations = new List<ObjectValidation>();
			personValidations.Add(new ObjectValidation { Name = ObjectFilter.Description, Comparison = ComparisonOperator.Equal, ComparisonValue = "person" });
			LogEventDetails(Robot.RegisterObjectDetectionEvent(ObjectDetectionCallback, (int)Math.Abs(CharacterParameters.ObjectDetectionDebounce * 1000), true, personValidations, "ODEventForFace", null));
			
			List<ObjectValidation> objectValidations = new List<ObjectValidation>();
			objectValidations.Add(new ObjectValidation { Name = ObjectFilter.Description, Comparison = ComparisonOperator.NotEqual, ComparisonValue = "person" });

			LogEventDetails(Robot.RegisterObjectDetectionEvent(ObjectDetectionCallback, 0, true, objectValidations, "GenericODEvent", null));

			//Head Actuators for following actions.
			IList<ActuatorPositionValidation> actuatorYawValidations = new List<ActuatorPositionValidation>();
			actuatorYawValidations.Add(new ActuatorPositionValidation(ActuatorPositionFilter.SensorName, ComparisonOperator.Equal, ActuatorPosition.HeadYaw));
			LogEventDetails(Robot.RegisterActuatorEvent(ActuatorCallback, (int)Math.Abs(CharacterParameters.ObjectDetectionDebounce *1000), true, actuatorYawValidations, "HeadYaw", null));

			IList<ActuatorPositionValidation> actuatorPitchValidations = new List<ActuatorPositionValidation>();
			actuatorPitchValidations.Add(new ActuatorPositionValidation(ActuatorPositionFilter.SensorName, ComparisonOperator.Equal, ActuatorPosition.HeadPitch));
			LogEventDetails(Robot.RegisterActuatorEvent(ActuatorCallback, (int)Math.Abs(CharacterParameters.ObjectDetectionDebounce *1000), true, actuatorPitchValidations, "HeadPitch", null));
			
			Robot.StartObjectDetector(characterParameters.PersonConfidence, 0, characterParameters.TrackHistory, null);
		}

		/// <summary>
		/// Handle head actions for this character
		/// </summary>
		public void HandleHeadAction(AnimationRequest animationRequest, ConversationData conversation)
		{
			if (!string.IsNullOrWhiteSpace(animationRequest.HeadLocation))
			{
				HeadLocation headLocation = conversation.HeadLocations.FirstOrDefault(x => x.Id == animationRequest.HeadLocation);
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
                        lock(_timerLock)
                        {
                            _moveHeadTimer?.Dispose();
                        }

						if (_currentHeadRequest.MovementDuration != null && _currentHeadRequest.MovementDuration > 0)
						{
							Robot.MoveHead(_currentHeadRequest.MaxPitch, _currentHeadRequest.MaxRoll, _currentHeadRequest.MaxYaw, null, (int)Math.Abs((double)_currentHeadRequest.MovementDuration), AngularUnit.Degrees, null);
						}
						else if (_currentHeadRequest.MovementVelocity != null && _currentHeadRequest.MovementVelocity > 0)
						{
							Robot.MoveHead(_currentHeadRequest.MaxPitch, _currentHeadRequest.MaxRoll, _currentHeadRequest.MaxYaw, (int)Math.Abs((int)_currentHeadRequest.MovementVelocity), null, AngularUnit.Degrees, null);
						}

					}
					else
					{
						if (_currentHeadRequest.FollowFace || !string.IsNullOrWhiteSpace(_currentHeadRequest.FollowObject))
						{
							_headMovingContinuously = true;
                            lock (_timerLock)
                            {
                                _moveHeadTimer?.Dispose();
                                if (!_isDisposed)
                                {
                                    _moveHeadTimer = new Timer(MoveHeadCallback, null, (int)Math.Abs(_currentHeadRequest.DelayBetweenMovements * 1000), (int)Math.Abs(_currentHeadRequest.DelayBetweenMovements * 1000));
                                }
                            }
						}
					}
				}
			}
		}
	
		private void ObjectDetectionCallback(IObjectDetectionEvent objEvent)
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
				ObjectEvent?.Invoke(this, objEvent);
			}
			catch (Exception ex)
			{
				Robot.SkillLogger.Log("Failed processing face event.", ex);
			}
		}

		private void MoveHeadCallback(object timerData)
		{
			if(_headMovingContinuously)
			{
				_tick = !_tick;

				bool _lookAroundFailover = false;
				if(_currentHeadRequest.FollowFace)
				{
					if(_lastPersonEvent != null)
					{
						FindAndFollowPerson(_lastPersonEvent);
						return;
					}
					else if(_currentHeadRequest.StartLookAroundOnLostObject != null && _currentHeadRequest.StartLookAroundOnLostObject > 0)
					{
						//Look around if lost it for too long
						if(_followedObjectLastSeen < DateTime.Now.AddSeconds(-(double)_currentHeadRequest.StartLookAroundOnLostObject))
						{
							_lookAroundFailover = true;
						}
					}
				}
				else if(!string.IsNullOrWhiteSpace(_currentHeadRequest.FollowObject))
				{
					if(_lastObjectEvent != null && _lastObjectEvent.Description.ToLower() == _currentHeadRequest.FollowObject.ToLower())
					{
						FindAndFollowObject(_lastObjectEvent);
						return;
					}
					else if (_currentHeadRequest.StartLookAroundOnLostObject != null && _currentHeadRequest.StartLookAroundOnLostObject > 0)
					{
						//Look around if lost it for too long
						if (_followedObjectLastSeen < DateTime.Now.AddSeconds(-(double)_currentHeadRequest.StartLookAroundOnLostObject))
						{
							_lookAroundFailover = true;
						}
					}
				}
				
				//Else, look around using ranges provided
				if (_lookAroundFailover ||(!_currentHeadRequest.FollowFace && string.IsNullOrWhiteSpace(_currentHeadRequest.FollowObject)))
				{
					//Ew, stinky code
					//Improve following object/face and lost face movement
					if(_lookAroundFailover && _lookAroundThrottle < 2)
					{
						_lookAroundThrottle++;
						return;
					}
					_lookAroundThrottle = 0;
                    
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
						if(_currentHeadRequest.MinPitch == _currentHeadRequest.MaxPitch)
						{
							newPitch = _currentHeadRequest.MinPitch;
						}
						else
						{
							double? minPitch = _currentHeadRequest.MinPitch < _currentHeadRequest.MaxPitch ? _currentHeadRequest.MinPitch : _currentHeadRequest.MaxPitch;
							double? maxPitch = _currentHeadRequest.MinPitch < _currentHeadRequest.MaxPitch ? _currentHeadRequest.MaxPitch : _currentHeadRequest.MinPitch;

							if(_currentHeadRequest.RandomRange)
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
						if(_currentHeadRequest.MinRoll == _currentHeadRequest.MaxRoll)
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
						Robot.MoveHead(newPitch, newRoll, newYaw, null, (int)Math.Abs((double)_currentHeadRequest.MovementDuration), AngularUnit.Degrees, null);
					}
					else if (_currentHeadRequest.MovementVelocity != null && _currentHeadRequest.MovementVelocity > 0)
					{
						Robot.MoveHead(newPitch, newRoll, newYaw, (int)Math.Abs((int)_currentHeadRequest.MovementVelocity), null, AngularUnit.Degrees, null);
					}
				}
			}			
		}
		
		private void ActuatorCallback(IActuatorEvent actuatorEvent)
		{
			switch(actuatorEvent.SensorPosition)
			{
				case ActuatorPosition.HeadPitch:
					_lastActuatorPitch = actuatorEvent.ActuatorValue;
					HeadPitchActuatorEvent?.Invoke(this, actuatorEvent);
					break;
				case ActuatorPosition.HeadYaw:
					_lastActuatorYaw = actuatorEvent.ActuatorValue;
					HeadYawActuatorEvent?.Invoke(this, actuatorEvent);
					break;
				case ActuatorPosition.HeadRoll:
					HeadRollActuatorEvent?.Invoke(this, actuatorEvent);
					break;
			}
		}

		private bool _finding = false;

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

				if(pitch == null && yaw == null)
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
				if(_finding)
				{
					return;
				}
				_finding = true;
				double? yaw = null;
				double? pitch = null;

				if (_lastActuatorYaw != null)
				{
					_lastYaw = _lastActuatorYaw;
				}

				if (_lastActuatorPitch != null)
				{
					_lastPitch = _lastActuatorPitch;
				}

				// 0 is Left 320 is Right  - Convert it ==>  L:1 to R:-1
				double widthOfHuman = objEvent.ImageLocationRight - objEvent.ImageLocationLeft;
				double xError = (160.0 - ((objEvent.ImageLocationLeft + objEvent.ImageLocationRight) / 2.0)) / 160.0;

				// Use this for human tracking
				double yError = (160.0 - 1.2 * objEvent.ImageLocationTop + 0.2 * objEvent.ImageLocationBottom) / 160.0;

				// Target is to get move Misty's head to get X and Y Errors to be close to 0.0
				// Head moves only if error is greater than threshold ; Error range is 1 to -1
				double threshold = Math.Max(0.2, (321.0 - widthOfHuman) / 1000.0);

				// Higher the number higher the damping - 0 damping at 1.0
				double damperGain = 7.0;

				if (Math.Abs(xError) > threshold) //use threshold?
				{
					yaw = (_lastYaw + xError * (RobotConstants.MaximumYawDegreesExclusive - RobotConstants.MinimumYawDegreesInclusive) / damperGain);

					if (yaw < RobotConstants.MinimumYawDegreesInclusive)
					{
						yaw = RobotConstants.MinimumYawDegreesInclusive;
					}
					if (yaw >= RobotConstants.MaximumYawDegreesExclusive)
					{
						yaw = RobotConstants.MaximumYawDegreesExclusive-1;
					}
				}

				if (Math.Abs(yError) >= threshold)
				{
					pitch = _lastPitch - yError * ((RobotConstants.MaximumPitchDegreesExclusive - RobotConstants.MinimumPitchDegreesInclusive) / 3.0) - (RobotConstants.MaximumPitchDegreesExclusive + RobotConstants.MinimumPitchDegreesInclusive);
					if(objEvent.Description == "person")
					{
						pitch = pitch + CharacterParameters.FacePitchOffset;
					}
						
					if (pitch < RobotConstants.MinimumPitchDegreesInclusive)
					{
						pitch = RobotConstants.MinimumPitchDegreesInclusive;
					}
					if (pitch >= RobotConstants.MaximumPitchDegreesExclusive)
					{
						pitch = RobotConstants.MaximumPitchDegreesExclusive-1;
					}
				}

				if (pitch != null || yaw != null)
				{
					if (_currentHeadRequest.MovementDuration != null && _currentHeadRequest.MovementDuration > 0)
					{
						Robot.MoveHead(pitch, null, yaw, null, _currentHeadRequest.MovementDuration, AngularUnit.Degrees, null);
					}
					else if (_currentHeadRequest.MovementVelocity != null && _currentHeadRequest.MovementVelocity > 0)
					{
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
				_finding = false;
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
					    Robot.UnregisterAllEvents(null);
					    _headMovingContinuously = false;
                    
                            _moveHeadTimer?.Dispose();
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
 