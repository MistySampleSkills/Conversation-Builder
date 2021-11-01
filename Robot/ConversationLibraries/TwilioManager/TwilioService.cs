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
using MistyRobotics.SDK.Messengers;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace TwilioManager
{
	/// <summary>
	/// Common SMS interface
	/// </summary>
	public interface ISMSService
	{
		/// <summary>
		/// Initialize the service connection
		/// </summary>
		/// <param name="accountId"></param>
		/// <param name="authToken"></param>
		/// <param name="senderPhoneNumber"></param>
		/// <returns></returns>
		bool Initialize(string accountId, string authToken, string senderPhoneNumber);

		/// <summary>
		/// Send a message to a phone
		/// </summary>
		/// <param name="toPhoneNumber"></param>
		/// <param name="message"></param>
		bool SendMessage(string toPhoneNumber, string message);

		/// <summary>
		/// Send a message with links to publicly available URIs
		/// </summary>
		/// <param name="toPhoneNumber"></param>
		/// <param name="message"></param>
		/// <param name="uris"></param>
		bool SendMessageWithMediaLinks(string toPhoneNumber, string message, IEnumerable<Uri> uris);
	}

	/// <summary>
	/// Provides twilio SMS service
	/// </summary>
	public sealed class TwilioService : ISMSService
	{
		/// <summary>
		/// Number for these messages to use as the sender's phone number
		/// </summary>
		private string _senderSmsPhone;

		/// <summary>
		/// Robot messenger instance used for skill logging
		/// </summary>
		private IRobotMessenger _misty;

		public TwilioService(IRobotMessenger misty)
		{
			_misty = misty;
		}

		/// <summary>
		/// Initialize the service connection
		/// </summary>
		/// <param name="accountId"></param>
		/// <param name="authToken"></param>
		/// <param name="senderPhoneNumber"></param>
		/// <returns></returns>
		public bool Initialize(string accountId, string authToken, string senderPhoneNumber)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(accountId) && !string.IsNullOrWhiteSpace(authToken) && !string.IsNullOrWhiteSpace(senderPhoneNumber))
				{
					TwilioClient.Init(accountId, authToken);
					_senderSmsPhone = CleanupPhoneNumber(senderPhoneNumber);
					return true;
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Failed to initialize Twilio connection.", ex);
			}
			return false;
		}
		
		/// <summary>
		/// Send a message to a phone
		/// </summary>
		/// <param name="toPhoneNumber"></param>
		/// <param name="message"></param>
		public bool SendMessage(string toPhoneNumber, string message)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(toPhoneNumber) || string.IsNullOrWhiteSpace(_senderSmsPhone))
				{
                    return false;
				}

				MessageResource.Create(
					body: message,
					from: new Twilio.Types.PhoneNumber(_senderSmsPhone),
					to: new Twilio.Types.PhoneNumber(CleanupPhoneNumber(toPhoneNumber))
				);
                
                return true;
            }
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Failed to send Twilio message.", ex);
				return false;
			}
		}

		/// <summary>
		/// Send a message with links to publicly available URIs
		/// </summary>
		/// <param name="toPhoneNumber"></param>
		/// <param name="message"></param>
		/// <param name="uris"></param>
		public bool SendMessageWithMediaLinks(string toPhoneNumber, string message, IEnumerable<Uri> uris)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(toPhoneNumber))
				{
					_misty.SkillLogger.Log("Missing receiver's phone number.");
					return false;
				}
				else if (string.IsNullOrWhiteSpace(_senderSmsPhone))
				{
					_misty.SkillLogger.Log("Missing sender's phone number.");
					return false;
				}

				uris = uris ?? new Uri[0];
				MessageResource.Create(
					body: message,
					from: new Twilio.Types.PhoneNumber(_senderSmsPhone),
					to: new Twilio.Types.PhoneNumber(CleanupPhoneNumber(toPhoneNumber)),
					mediaUrl: uris.ToList()
				);
                return true;
            }
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Failed to send Twilio message with links.", ex);
				return false;
			}
		}
		
		/// <summary>
		/// Put the number in twilio friendly format
		/// </summary>
		/// <param name="phoneNumber"></param>
		/// <returns></returns>
		private string CleanupPhoneNumber(string phoneNumber)
		{
			if(string.IsNullOrWhiteSpace(phoneNumber))
			{
				return "";
			}
			return $"+{phoneNumber.Replace("-", "").Replace(".", "").Replace("+", "").Replace(" ", "").Replace("_", "").Replace("#", "").Replace("(", "").Replace(")", "")}";
		}
	}
}