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
using System.IO;
using System.Net;
using System.Net.Mail;
using MistyRobotics.SDK.Messengers;

namespace EmailManager
{
    /// <summary>
    /// Library for sending emails
    /// </summary>
	public class EmailService
    {
		private IRobotMessenger _robot;
		private string _smtpHost;
		private int _smtpPort;

		public EmailService(IRobotMessenger robot, string smtpHost, int smtpPort)
		{
			_robot = robot;
			_smtpHost = smtpHost;
			_smtpPort = smtpPort;
		}

		/// <summary>
		/// Need an app password on a company account or setup our own smtp server then this should work
		/// </summary>
		/// <param name="subject"></param>
		/// <param name="senderEmail"></param>
		/// <param name="toContact"></param>
		/// <param name="body"></param>
		/// <returns></returns>
		public bool SendEmail(string subject, SenderEmail senderEmail, string toEmail, string body, byte[] attachment = null, string attachmentName = "Image.jpg")
		{
			try
			{
				if(senderEmail == null || senderEmail.Email == null ||
					string.IsNullOrWhiteSpace(toEmail) ||
					string.IsNullOrWhiteSpace(body))
				{
					_robot.SkillLogger.Log("Missing required email parameters.");
					return false;
				}

				using (MailMessage mail = new MailMessage())
				{
					mail.From = new MailAddress(senderEmail.Email);
					mail.To.Add(toEmail);
					mail.Subject = subject;
					mail.Body = "<h1>" + body + "</h1>";
					mail.IsBodyHtml = true;
					if(attachment != null && !string.IsNullOrWhiteSpace(attachmentName))
					{
						mail.Attachments.Add(new Attachment(new MemoryStream(attachment), attachmentName));
					}
					
					using (SmtpClient smtp = new SmtpClient(_smtpHost, _smtpPort))
					{
						smtp.Credentials = new NetworkCredential(senderEmail.Email, senderEmail.EmailPassword);
						smtp.EnableSsl = true;
						smtp.Send(mail);
					}
				}

				_robot.SkillLogger.Log($"{senderEmail.Email} sent an email to {toEmail}.");
				return true;
			}
			catch (SmtpException ex)
			{
				_robot.SkillLogger.Log("Smtp exception.", ex);				
			}
			catch (Exception ex)
			{
				_robot.SkillLogger.Log("Failed to send email.", ex);				
			}
			return false;
		}
	}
}
