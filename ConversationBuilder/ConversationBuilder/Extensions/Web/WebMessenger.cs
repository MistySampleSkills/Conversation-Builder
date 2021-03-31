using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConversationBuilder.Extensions
{
	/// <summary>
	/// Simple Http class to communicate with the outside world
	/// </summary>
	public class WebMessenger
	{

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
		/// Method to make a GET request to an external endpoint
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <returns></returns>
		public async Task<WebMessengerData> GetRequest(string endpoint)
		{
			return await GetInternalRequest(endpoint, null);
		}

		/// <summary>
		/// Method to make a GET request to an external endpoint using a Bearer Token
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="token">Bearer Token</param>
		/// <returns></returns>
		public async Task<WebMessengerData> SecureGetRequest(string endpoint, string token)
		{
			return await GetInternalRequest(endpoint, token);
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
				HttpStatusCode errorCode = ((HttpWebResponse)ex.Response)?.StatusCode ?? HttpStatusCode.InternalServerError;
				responseString = $"WebMessenger failed to connect to GET endpoint '{endpoint}' - Received status code: {errorCode} ";
				return new WebMessengerData { Response = responseString, HttpCode = (int)errorCode };
			}
			catch (Exception)
			{
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
		public async Task<WebMessengerData> SecurePostRequest(string endpoint, string data, string contentType, string token)
		{
			return await MakeRequest(endpoint, data, "POST", contentType, token);
		}

		/// <summary>
		/// Method to make an http POST request to an external endpoint
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <returns></returns>
		public async Task<WebMessengerData> PostRequest(string endpoint, string data, string contentType)
		{
			return await MakeRequest(endpoint, data, "POST", contentType, null);
		}

		/// <summary>
		/// Method to make an http DELETE request to an external endpoint using a Bearer Token
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <param name="token">Bearer Token</param>
		/// <returns></returns>
		public async Task<WebMessengerData> SecureDeleteRequest(string endpoint, string data, string contentType, string token)
		{
			return await MakeRequest(endpoint, data,  "DELETE", contentType, token);
		}

		/// <summary>
		/// Method to make an http DELETE request to an external endpoint
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <returns></returns>
		public async Task<WebMessengerData> DeleteRequest(string endpoint, string data, string contentType)
		{
			return await MakeRequest(endpoint, data, "DELETE", contentType, null);
		}

		/// <summary>
		/// Method to make an http PATCH request to an external endpoint using a Bearer Token
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <param name="token">Bearer Token</param>
		/// <returns></returns>
		public async Task<WebMessengerData> SecurePatchRequest(string endpoint, string data, string contentType, string token)
		{
			return await MakeRequest(endpoint, data, "PATCH", contentType, token);
		}

		/// <summary>
		/// Method to make an http PATCH request to an external endpoint
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <returns></returns>
		public async Task<WebMessengerData> PatchRequest(string endpoint, string data, string contentType)
		{
			return await MakeRequest(endpoint, data, "PATCH", contentType, null);
		}

		/// <summary>
		/// Method to make an http PUT request to an external endpoint using a Bearer Token
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <param name="token">Bearer Token</param>
		/// <returns></returns>
		public async Task<WebMessengerData> SecurePutRequest(string endpoint, string data, string contentType, string token)
		{
			return await MakeRequest(endpoint, data, "PUT", contentType, token);
		}

		/// <summary>
		/// Method to make an http PUT request to an external endpoint
		/// </summary>
		/// <param name="endpoint">the endpoint to call</param>
		/// <param name="data">data to send</param>
		/// <param name="contentType">the request content type</param>
		/// <returns></returns>
		public async Task<WebMessengerData> PutRequest(string endpoint, string data, string contentType)
		{
			return await MakeRequest(endpoint, data, "PUT", contentType, null);
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
				HttpStatusCode errorCode = ((HttpWebResponse)ex.Response)?.StatusCode ?? HttpStatusCode.InternalServerError;
				responseString = $"WebMessenger failed to connect to {requestMethod} endpoint '{endpoint}' using {contentType} content type - Received status code: {errorCode} ";
				return new WebMessengerData { Response = responseString, HttpCode = (int)errorCode };
			}
			catch (Exception ex)
			{
				responseString = $"WebMessenger failed to connect to {requestMethod} endpoint '{endpoint}' using {contentType} content type - Exception:{ex.Message}";
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
