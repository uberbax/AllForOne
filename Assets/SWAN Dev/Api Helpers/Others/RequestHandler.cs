// Created by SWAN DEV 2017
using System;
using System.Net;
using System.IO;

namespace SDev.Util
{
	public class RequestHandler
	{
		public static void Process(string url, Action<bool, string> onComplete, float timeOut = 10)
		{
			WebRequestHandler.Create("Default").Request(url,
				(success, result) =>
				{
					onComplete(success, result);
				},
				timeOut
			);
		}

		public static string Process(string url)
		{
#if UNITY_EDITOR
			UnityEngine.Debug.Log("Http Web Request");
#endif
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			HttpWebResponse HttpWResp = (HttpWebResponse)request.GetResponse();
			Stream streamResponse = HttpWResp.GetResponseStream();

			// And read it out
			StreamReader reader = new StreamReader(streamResponse);
			string response = reader.ReadToEnd();

			reader.Close();
			reader.Dispose();

#if UNITY_EDITOR
			UnityEngine.Debug.Log("response: \n" + response);
#endif
			return response;
		}
	}
}
