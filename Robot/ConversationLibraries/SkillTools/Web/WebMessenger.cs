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
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace SkillTools.Web
{
	/// <summary>
	/// Simple Http class to communicate with the outside world
	/// </summary>
	public class WebMessenger
	{
		private const string LoggingStartString = "Misty Robotics [••] WebMessenger : ";

		/// <summary>
		/// Initializes a new instance of WebMessenger
		/// </summary>
		public WebMessenger()
		{
#if DEBUG
			ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
#endif
		}

		/// <summary>
		/// With moved code, replace old async operation functions?
		/// </summary>
		/// <param name="endpoint"></param>
		/// <returns></returns>
		public Task<WebMessengerData> GetRequestAsync(string endpoint)
		{
			return GetInternalRequest(endpoint, null);
		}

		/// <summary>
		/// Method to make a GET request to an external endpoint
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <returns></returns>
		public IAsyncOperation<WebMessengerData> GetRequest(string endpoint)
		{
			return GetInternalRequest(endpoint, null).AsAsyncOperation();
		}

		/// <summary>
		/// Method to make a GET request to an external endpoint using a Bearer Token
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="token">Bearer Token</param>
		/// <returns></returns>
		public IAsyncOperation<WebMessengerData> SecureGetRequest(string endpoint, string token)
		{
			return GetInternalRequest(endpoint, token).AsAsyncOperation();
		}

		private async Task<WebMessengerData> GetInternalRequest(string endpoint, string token)
		{
			StreamReader readStream = null;
			HttpWebResponse response = null;
			string responseString = "";

			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
				if (request == null)
				{
					return new WebMessengerData { Response = "Trouble creating request.", HttpCode = 503 };
				}

				if (!string.IsNullOrWhiteSpace(token))
				{
					request.PreAuthenticate = true;
					request.Headers.Add("Authorization", "Bearer " + token);
				}
				// Set some reasonable limits on resources used by this request
				//request.MaximumAutomaticRedirections = 4;
				//request.MaximumResponseHeadersLength = 4;

				// Set credentials to use for this request.
				request.Credentials = CredentialCache.DefaultCredentials;				
				response = (HttpWebResponse)await request.GetResponseAsync();

				HttpStatusCode responseCode;
				if (response == null)
				{
					responseCode = HttpStatusCode.ServiceUnavailable;
					responseString = "Endpoint does not appear to be accepting requests.";
				}
				else
				{
					Stream receiveStream = response.GetResponseStream();
					responseCode = response?.StatusCode ?? HttpStatusCode.InternalServerError;
					readStream = new StreamReader(receiveStream, Encoding.UTF8);
					responseString = readStream.ReadToEnd();
				}

				return new WebMessengerData { Response = responseString, HttpCode = Convert.ToInt32(responseCode) };
			}
			catch (WebException ex)
			{
				string dateTimeLogString = $"{LoggingStartString} {DateTime.Now.ToString("MM/dd/yy hh:mm:ss.fff tt")}";
				HttpStatusCode errorCode = ((HttpWebResponse)ex.Response)?.StatusCode ?? HttpStatusCode.InternalServerError;
				responseString = $"WebMessenger failed to connect to GET endpoint '{endpoint}' - Received status code: {errorCode} ";
				Console.WriteLine($"{dateTimeLogString} {responseString}");
				return new WebMessengerData { Response = responseString, HttpCode = (int)errorCode };
			}
			catch (Exception ex)
			{
				string dateTimeLogString = $"{LoggingStartString} {DateTime.Now.ToString("MM/dd/yy hh:mm:ss.fff tt")}";
				responseString = $"{dateTimeLogString} WebMessenger failed to connect to GET endpoint '{endpoint}' - Exception:{ex.Message}";
				Console.WriteLine(responseString);
				return new WebMessengerData { Response = responseString, HttpCode = (int)HttpStatusCode.InternalServerError };
			}
			finally
			{
				response?.Dispose();
				readStream?.Dispose();
			}
		}

		/// <summary>
		/// Method to make an http POST request to an external endpoint using a Bearer Token
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <param name="token">Bearer Token</param>
		/// <returns></returns>
		public IAsyncOperation<WebMessengerData> SecurePostRequest(string endpoint, string data, string contentType, string token)
		{
			return MakeRequest(endpoint, data, "POST", contentType, token).AsAsyncOperation();
		}

		/// <summary>
		/// Method to make an http POST request to an external endpoint
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <returns></returns>
		public IAsyncOperation<WebMessengerData> PostRequest(string endpoint, string data, string contentType)
		{
			return MakeRequest(endpoint, data, "POST", contentType, null).AsAsyncOperation();
		}

		/// <summary>
		/// Method to make an http DELETE request to an external endpoint using a Bearer Token
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <param name="token">Bearer Token</param>
		/// <returns></returns>
		public IAsyncOperation<WebMessengerData> SecureDeleteRequest(string endpoint, string data, string contentType, string token)
		{
			return MakeRequest(endpoint, data,  "DELETE", contentType, token).AsAsyncOperation();
		}

		/// <summary>
		/// Method to make an http DELETE request to an external endpoint
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <returns></returns>
		public IAsyncOperation<WebMessengerData> DeleteRequest(string endpoint, string data, string contentType)
		{
			return MakeRequest(endpoint, data, "DELETE", contentType, null).AsAsyncOperation();
		}

		/// <summary>
		/// Method to make an http PATCH request to an external endpoint using a Bearer Token
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <param name="token">Bearer Token</param>
		/// <returns></returns>
		public IAsyncOperation<WebMessengerData> SecurePatchRequest(string endpoint, string data, string contentType, string token)
		{
			return MakeRequest(endpoint, data, "PATCH", contentType, token).AsAsyncOperation();
		}

		/// <summary>
		/// Method to make an http PATCH request to an external endpoint
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <returns></returns>
		public IAsyncOperation<WebMessengerData> PatchRequest(string endpoint, string data, string contentType)
		{
			return MakeRequest(endpoint, data, "PATCH", contentType, null).AsAsyncOperation();
		}

		/// <summary>
		/// Method to make an http PUT request to an external endpoint using a Bearer Token
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <param name="token">Bearer Token</param>
		/// <returns></returns>
		public IAsyncOperation<WebMessengerData> SecurePutRequest(string endpoint, string data, string contentType, string token)
		{
			return MakeRequest(endpoint, data, "PUT", contentType, token).AsAsyncOperation();
		}

		/// <summary>
		/// Method to make an http PUT request to an external endpoint
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <returns></returns>
		public IAsyncOperation<WebMessengerData> PutRequest(string endpoint, string data, string contentType)
		{
			return MakeRequest(endpoint, data, "PUT", contentType, null).AsAsyncOperation();
		}

		private async Task<WebMessengerData> MakeRequest(string endpoint, string data, string requestMethod, string contentType, string token)
		{
			WebResponse response = null;
			Stream dataStream = null;
			string responseString = "";

			requestMethod = string.IsNullOrWhiteSpace(requestMethod) ? "POST" : requestMethod;
			data = string.IsNullOrWhiteSpace(data) ? "{}" : data;
			contentType = string.IsNullOrWhiteSpace(contentType) ? "application/json" : contentType;
			try
			{
				HttpStatusCode responseCode;
				// Create a request using a URL that can receive a post.   
				WebRequest request = WebRequest.Create(endpoint);
				if(request == null)
				{
					return new WebMessengerData { Response = "Trouble creating request.", HttpCode = 503 };
				}

				if(!string.IsNullOrWhiteSpace(token))
				{
					request.PreAuthenticate = true;
					request.Headers.Add("Authorization", "Bearer " + token);
				}

				request.Method = requestMethod;
				request.Credentials = CredentialCache.DefaultCredentials;

				byte[] byteArray = Encoding.UTF8.GetBytes(data);

				// Set the ContentType property of the WebRequest.  
				request.ContentType = contentType;
				// Set the ContentLength property of the WebRequest.  
				//request.ContentLength = byteArray.Length;

				dataStream = await request.GetRequestStreamAsync();
				dataStream.Write(byteArray, 0, byteArray.Length);
				// Close the Stream object.  
				dataStream.Close();

				// Get the response.  
				response = (HttpWebResponse)await request.GetResponseAsync();

				if (response == null)
				{
					responseCode = HttpStatusCode.ServiceUnavailable;
					responseString = "Endpoint does not appear to be accepting requests.";
				}
				else
				{
					responseCode = ((HttpWebResponse)response)?.StatusCode ?? HttpStatusCode.InternalServerError;
					using (dataStream = response.GetResponseStream())
					{
						StreamReader reader = new StreamReader(dataStream);
						responseString = reader.ReadToEnd();
					}
				}

				return new WebMessengerData { Response = responseString, HttpCode = (int)responseCode };
			}
			catch (WebException ex)
			{
				string dateTimeLogString = $"{LoggingStartString} {DateTime.Now.ToString("MM/dd/yy hh:mm:ss.fff tt")}";
				HttpStatusCode errorCode = ((HttpWebResponse)ex.Response)?.StatusCode ?? HttpStatusCode.InternalServerError;
				responseString = $"WebMessenger failed to connect to {requestMethod} endpoint '{endpoint}' using {contentType} content type - Received status code: {errorCode} ";
				Console.WriteLine($"{dateTimeLogString} {responseString}");
				return new WebMessengerData { Response = responseString, HttpCode = (int)errorCode };
			}
			catch (Exception ex)
			{
				string dateTimeLogString = $"{LoggingStartString} {DateTime.Now.ToString("MM/dd/yy hh:mm:ss.fff tt")}";
				responseString = $"WebMessenger failed to connect to {requestMethod} endpoint '{endpoint}' using {contentType} content type - Exception:{ex.Message}";
				Console.WriteLine($"{dateTimeLogString} {responseString}");
				return new WebMessengerData { Response = responseString, HttpCode = (int)HttpStatusCode.InternalServerError };
			}
			finally
			{
				response?.Dispose();
				dataStream?.Dispose();
			}
		}
	}
}
