using Windows.Foundation;

namespace VisionTools
{
	/// <summary>
	/// Current integrated services
	/// </summary>
	public enum ServiceType
	{
		Unknown,
		AzureCognitive
		//Moar!
	}

	/// <summary>
	/// Common vision service interface
	/// </summary>
	public interface IVisionService
	{
		IAsyncOperation<string> AnalyzeImageStream(object stream);

		IAsyncOperation<string> AnalyzeImageUrl(string url);
	}
}