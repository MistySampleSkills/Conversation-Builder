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
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Events;

namespace MistyCharacter
{
	/// <summary>
	/// //Deprecated
	/// </summary>
	public class ArmManager : BaseManager, IArmManager
	{
		private Timer _moveArmsTimer;
		private bool _armsMovingContinuously;
		private Random _random = new Random();
		private ArmLocation _currentArmRequest = new ArmLocation();
		private bool _tick = false;
        private object _timerLock = new object();

        public ArmManager(IRobotMessenger misty, IDictionary<string, object> parameters, CharacterParameters characterParameters)
		: base(misty, parameters, characterParameters) {}

		public void StopMovement()
		{
			_moveArmsTimer?.Dispose();
		}

		private void MoveArmsCallback(object timerData)
		{
			if (_armsMovingContinuously)
			{
				_tick = !_tick;
				double? minLeft = null;
				double? maxLeft = null;
				double? newLeft = null;
				if (_currentArmRequest.MinLeftArm != null && _currentArmRequest.MaxLeftArm != null)
				{
					minLeft = _currentArmRequest.MinLeftArm < _currentArmRequest.MaxLeftArm ? _currentArmRequest.MinLeftArm : _currentArmRequest.MaxLeftArm;
					maxLeft = _currentArmRequest.MinLeftArm < _currentArmRequest.MaxLeftArm ? _currentArmRequest.MaxLeftArm : _currentArmRequest.MinLeftArm;
				}
				else
				{
					if (_currentArmRequest.MinLeftArm == null && _currentArmRequest.MaxLeftArm != null)
					{
						newLeft = _currentArmRequest.MaxLeftArm;
					}
					else if (_currentArmRequest.MaxLeftArm == null && _currentArmRequest.MinLeftArm != null)
					{
						newLeft = _currentArmRequest.MinLeftArm;
					}
					else
					{
						//both null, uh oh
						newLeft = null;
					}
				}

				double? minRight = null;
				double? maxRight = null;
				double? newRight = null;
				if (_currentArmRequest.MinRightArm != null && _currentArmRequest.MaxRightArm != null)
				{
					minRight = _currentArmRequest.MinRightArm < _currentArmRequest.MaxRightArm ? _currentArmRequest.MinRightArm : _currentArmRequest.MaxRightArm;
					maxRight = _currentArmRequest.MinRightArm < _currentArmRequest.MaxRightArm ? _currentArmRequest.MaxRightArm : _currentArmRequest.MinRightArm;
				}
				else
				{
					if (_currentArmRequest.MinRightArm == null && _currentArmRequest.MaxRightArm != null)
					{
						newRight = _currentArmRequest.MaxRightArm;
					}
					else if (_currentArmRequest.MaxRightArm == null && _currentArmRequest.MinRightArm != null)
					{
						newRight = _currentArmRequest.MinRightArm;
					}
					else
					{
						//both null, uh oh
						newRight = null;
					}
				}
				
				if (_currentArmRequest.RandomRange)
				{
					newLeft = _random.Next((int)minLeft, (int)maxLeft);
					newRight = _random.Next((int)minRight, (int)maxRight);
				}
				else if(_tick)
				{
					newLeft = (int)minLeft;
					newRight = (int)minRight;
				}
				else //tock
				{
					newLeft = (int)maxLeft;
					newRight = (int)maxRight;
				}


				if (_currentArmRequest.MovementDuration != null && _currentArmRequest.MovementDuration > 0)
				{
					Robot.MoveArms(newLeft ?? 90, newRight ?? 90, null, null, (int)Math.Abs((double)_currentArmRequest.MovementDuration), AngularUnit.Degrees, null);
				}
				else if (_currentArmRequest.MovementVelocity != null && _currentArmRequest.MovementVelocity > 0)
				{
					Robot.MoveArms(newLeft ?? 90, newRight ?? 90, (int)Math.Abs((int)_currentArmRequest.MovementVelocity), (int)Math.Abs((int)_currentArmRequest.MovementVelocity), null, AngularUnit.Degrees, null);
				}
			}
		}

		public void HandleArmAction(AnimationRequest animationRequest, ConversationData conversation)
		{
			if(!string.IsNullOrWhiteSpace(animationRequest.ArmLocation))
			{
				ArmLocation armLocation = conversation.ArmLocations.FirstOrDefault(x => x.Id == animationRequest.ArmLocation);
				if(armLocation != null)
				{
					_currentArmRequest = armLocation;
					if (armLocation.MaxLeftArm == null && armLocation.MinLeftArm == null && armLocation.MaxRightArm == null && armLocation.MinRightArm == null)
					{
						//do nuthin
						return;
					}

					if (armLocation.MaxLeftArm != null && armLocation.MaxLeftArm >= RobotConstants.MaximumArmDegreesExclusive)
					{
						_currentArmRequest.MaxLeftArm = RobotConstants.MaximumArmDegreesExclusive - 1;
					}

					if (armLocation.MaxRightArm != null && armLocation.MaxRightArm >= RobotConstants.MaximumArmDegreesExclusive)
					{
						_currentArmRequest.MaxRightArm = RobotConstants.MaximumArmDegreesExclusive - 1;
					}

					if (armLocation.MinRightArm != null && armLocation.MinRightArm < RobotConstants.MinimumArmDegreesInclusive)
					{
						_currentArmRequest.MinRightArm = RobotConstants.MinimumArmDegreesInclusive;
					}

					if (armLocation.MinLeftArm != null && armLocation.MinLeftArm < RobotConstants.MinimumArmDegreesInclusive)
					{
						_currentArmRequest.MinLeftArm = RobotConstants.MinimumArmDegreesInclusive;
					}

					if (_currentArmRequest.MaxLeftArm == _currentArmRequest.MinLeftArm &&
						_currentArmRequest.MaxRightArm == _currentArmRequest.MinRightArm)
					{
						_armsMovingContinuously = false;
                        lock (_timerLock)
                        {
                            _moveArmsTimer?.Dispose();
                        }
						
						if (_currentArmRequest.MovementDuration != null && _currentArmRequest.MovementDuration > 0)
						{
							Robot.MoveArms((double)_currentArmRequest.MaxLeftArm, (double)_currentArmRequest.MaxRightArm, null, null, (int)Math.Abs((double)_currentArmRequest.MovementDuration), AngularUnit.Degrees, null);
						}
						else if (_currentArmRequest.MovementVelocity != null && _currentArmRequest.MovementVelocity > 0)
						{
							Robot.MoveArms((double)_currentArmRequest.MaxLeftArm, (double)_currentArmRequest.MaxRightArm, (int)Math.Abs((int)_currentArmRequest.MovementVelocity), (int)Math.Abs((int)_currentArmRequest.MovementVelocity), null, AngularUnit.Degrees, null);
						}
					}
					else
					{
						if(!_armsMovingContinuously)
						{
							_armsMovingContinuously = true;
							lock (_timerLock)
							{
								_moveArmsTimer?.Dispose();
								if (!_isDisposed)
								{
									_moveArmsTimer = new Timer(MoveArmsCallback, null, (int)Math.Abs(_currentArmRequest.DelayBetweenMovements * 1000), (int)Math.Abs(_currentArmRequest.DelayBetweenMovements * 1000));
								}
							}
						}
					}
				}
			}
		}
		
		private bool _isDisposed = false;

		protected void Dispose(bool disposing)
		{
            lock (_timerLock)
            {
                if (!_isDisposed)
                {
                    if (disposing)
					{
						_armsMovingContinuously = false;
						_moveArmsTimer?.Dispose();
						//Robot.UnregisterAllEvents(null);
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
 