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

namespace Conversation.Weather.OpenWeather.IpStack.Models
{
	public class IpAddressDetails
	{
		/// <summary>
		/// Returns the requested IP address.
		/// </summary>
		public string ip { get; set; }

		/// <summary>
		/// Returns the hostname the requested IP resolves to, only returned if Hostname Lookup is enabled.
		/// </summary>
		public string Hostname { get; set; }

		/// <summary>
		/// Returns the IP address type IPv4 or IPv6.
		/// </summary>
		public string type { get; set; }

		/// <summary>
		/// Returns the 2-letter continent code associated with the IP.
		/// </summary>
		public string continent_code { get; set; }

		/// <summary>
		/// Returns the name of the continent associated with the IP.
		/// </summary>
		public string continent_name { get; set; }

		/// <summary>
		/// Returns the 2-letter country code associated with the IP.
		/// </summary>
		public string country_code { get; set; }

		/// <summary>
		/// Returns the name of the country associated with the IP.
		/// </summary>
		public string country_name { get; set; }

		/// <summary>
		/// Returns the region code of the region associated with the IP.
		/// </summary>
		public string region_code { get; set; }

		/// <summary>
		/// Returns the name of the region associated with the IP.
		/// </summary>
		public string region_name { get; set; }

		/// <summary>
		/// Returns the name of the city associated with the IP.
		/// </summary>
		public string city { get; set; }

		/// <summary>
		/// Returns the ZIP code associated with the IP.
		/// </summary>
		public string zip { get; set; }

		/// <summary>
		/// Returns the latitude value associated with the IP.
		/// </summary>
		public double latitude { get; set; }

		/// <summary>
		/// Returns the longitude value associated with the IP.
		/// </summary>
		public double longitude { get; set; }

		/// <summary>
		/// Returns multiple location-related objects.
		/// </summary>
		public Location Location { get; set; }

		/// <summary>
		/// Returns an object containing timezone-related data.
		/// </summary>
		public TimeZone TimeZone { get; set; }

		/// <summary>
		/// Returns an object containing currency-related data.
		/// </summary>
		public Currency Currency { get; set; }

		/// <summary>
		/// Returns an object containing connection-related data.
		/// </summary>
		public Connection Connection { get; set; }

		/// <summary>
		/// Returns an object containing security-related data.
		/// </summary>
		public Security Security { get; set; }
	}
}
