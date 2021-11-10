using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using MistyRobotics.SDK.Messengers;
using Windows.Foundation;

namespace VisionTools.AzureCognitive
{
	/// <summary>
	/// Wrapper class for Azure Cognitive Speech Service
	/// </summary>
	public sealed class AzureVisionService : IVisionService
	{
		private IRobotMessenger _robot;
		private ComputerVisionClient _computerVisionClient;
		private SemaphoreSlim _computerVisionSemaphore = new SemaphoreSlim(1, 1);
		private AzureServiceAuthorization _servicesAuthorization;
		
		public AzureVisionService(AzureServiceAuthorization servicesAuthorization, IRobotMessenger robot)
		{
			_robot = robot;
			_servicesAuthorization = servicesAuthorization;

			_computerVisionClient = new ComputerVisionClient(
						new ApiKeyServiceClientCredentials(_servicesAuthorization.SubscriptionKey),
						new System.Net.Http.DelegatingHandler[] { });
			_computerVisionClient.Endpoint = _servicesAuthorization.Endpoint;
		}

		/// <summary>
		/// Analyze the image stream and return a description
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public IAsyncOperation<string> AnalyzeImageStream(object stream)
		{
			return AnalyzeImageInternal(stream as Stream).AsAsyncOperation();
		}
		
		private async Task<string> AnalyzeImageInternal(Stream stream)
		{
			_computerVisionSemaphore.Wait();
			try
			{
				ImageDescription imageDescription = await _computerVisionClient.DescribeImageInStreamAsync(stream);
				if (!imageDescription.Captions.Any())
				{
					return string.Empty;
				}
				else
				{
					return imageDescription.Captions.First().Text;
				}
			}
			catch (Exception ex)
			{
				string message = "Failed processing image.";
				_robot.SkillLogger.Log(message, ex);
				return message;
			}
			finally
			{
				_computerVisionSemaphore.Release();
			}
		}

		/// <summary>
		/// Analyze the image at the specified url and return a description
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public IAsyncOperation<string> AnalyzeImageUrl(string url)
		{
			return AnalyzeImageInternal(url).AsAsyncOperation();
		}
		
		private async Task<string> AnalyzeImageInternal(string url)
		{
			_computerVisionSemaphore.Wait();
			try
			{
				ImageDescription imageDescription = await _computerVisionClient.DescribeImageAsync(url, 1);
				if (!imageDescription.Captions.Any())
				{
					return string.Empty;
				}
				else
				{
					return imageDescription.Captions.First().Text;
				}
			}
			catch (Exception ex)
			{
				string message = "Failed processing image.";
				_robot.SkillLogger.Log(message, ex);
				return message;
			}
			finally
			{
				_computerVisionSemaphore.Release();
			}
		}
	}
}