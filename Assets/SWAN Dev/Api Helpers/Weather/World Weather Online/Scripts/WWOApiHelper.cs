// Created by SWAN DEV 2021
using System;

using SDev.Util;
using Newtonsoft.Json;

namespace SDev.WWO_API
{
	/// <summary>
	/// World Weather Online API Helper/Manager.
	/// FAQ/Limitation: https://developer.worldweatheronline.com/api/faq.aspx || 
	/// Apply your API key: https://developer.worldweatheronline.com/signup.aspx
	/// </summary>
	public class WWOApiHelper
	{
		public string ApiBaseURL = "https://api.worldweatheronline.com/premium/v1/";
		public string PremiumApiKey = "YOUR_WWO_API_KEY"; //<-- This is a test Api Key, please apply your key for production app.

		public string cachedResult;

		public WWOApiHelper(string premiumApiKey)
		{
			PremiumApiKey = premiumApiKey;
		}

		private void _AppendExtraParameter(ref string apiURL, ExtraParameter[] extra)
		{
			if (extra != null && extra.Length > 0)
			{
				for (int i = 0; i < extra.Length; i++)
				{
					if (extra[i] != null && !string.IsNullOrEmpty(extra[i].parameterName) && !string.IsNullOrEmpty(extra[i].parameterValue))
					{
						apiURL += "&" + extra[i].parameterName + "=" + extra[i].parameterValue;
					}
				}
			}
		}

		/// <summary>
		/// The Time Zone REST API method retrieves current local time and UTC offset hour and minute for a specified location.
		/// </summary>
		public void GetTimeZone(TimeZoneAPI.TimeZoneQuery query, Action<bool, TimeZoneAPI> onComplete)
		{
			// create URL based on query paramters
			string apiURL = ApiBaseURL + "tz.ashx?format=json&key=" + PremiumApiKey + "&q=" + query.q;

			RequestHandler.Process(apiURL, (success, jsonText) =>
			{
				if (success) // Success
			{
				//_WriteToTextFile(jsonText, "GetTimeZone");
				cachedResult = jsonText;
				// serialize the json output and parse in the helper class
				TimeZoneAPI result = JsonConvert.DeserializeObject<TimeZoneAPI>(jsonText);
					onComplete(true, result);
				}
				else // Fail
			{
					onComplete(false, null);
				}
			});
		}

		/// <summary>
		/// The Location search REST API method takes a query value and returns information about the location, including area name, country, latitude/longitude, population,
		/// and a URL for the weather information. Note that the Free API returns 30 results and the Premium API returns 100 results.
		/// </summary>
		public void SearchLocation(SearchLocationAPI.SearchLocationQuery query, Action<bool, SearchLocationAPI> onComplete)
		{
			// create URL based on query paramters
			string apiURL = ApiBaseURL + "search.ashx?format=json&key=" + PremiumApiKey + "&query=" + query.q;
			if (query.num_of_results > 0) apiURL += "&num_of_results=" + query.num_of_results;
			if (query.timezone != WWOBool.NoSpecify) apiURL += "&timezone=" + query.timezone.ToString().ToLower();
			if (query.popular != WWOBool.NoSpecify) apiURL += "&popular=" + query.popular.ToString().ToLower();
			if (query.wct != LocationCategory.NoSpecify) apiURL += "&wct=" + query.wct.ToString().ToLower();

			RequestHandler.Process(apiURL, (success, jsonText) =>
			{
				if (success) // Success
			{
				//_WriteToTextFile(jsonText, "SearchLocation");
				cachedResult = jsonText;
				// serialize the json output and parse in the helper class
				SearchLocationAPI result = JsonConvert.DeserializeObject<SearchLocationAPI>(jsonText);
					onComplete(true, result);
				}
				else // Fail
			{
					onComplete(false, null);
				}
			});
		}

		/// <summary>
		/// The Local Weather REST API (also called City and Town Weather API) method allows you to access current weather conditions,
		/// the next 14 days of accurate and reliable weather forecast, Air Quality Data, Weather Alerts and Monthly Climate Averages for over 4 million cities and towns worldwide.
		/// The Local Weather API returns weather elements such as temperature, precipitation (rainfall), weather description, weather icon, wind speed, etc.
		/// </summary>
		public void GetLocalWeather(LocalWeatherAPI.LocalWeatherQuery query, Action<bool, LocalWeatherAPI> onComplete)
		{
			// create URL based on query paramters
			string apiURL = ApiBaseURL + "weather.ashx?format=json&key=" + PremiumApiKey + "&q=" + query.q + "&num_of_days=" + query.num_of_days;
			if (!string.IsNullOrEmpty(query.date)) apiURL += "&date=" + query.date;
			if (query.date_format != DateFormat.NoSpecify) apiURL += "&date_format=" + query.date_format.ToString().ToLower();
			if (query.tp != WeatherTimeInterval.NoSpecify) apiURL += "&tp=" + query.tp.ToString().ToLower();
			if (query.fx != WWOBool.NoSpecify) apiURL += "&fx=" + query.fx.ToString().ToLower();
			if (query.cc != WWOBool.NoSpecify) apiURL += "&cc=" + query.cc.ToString().ToLower();
			if (query.mca != WWOBool.NoSpecify) apiURL += "&mca=" + query.mca.ToString().ToLower();
			if (query.fx24 != WWOBool.NoSpecify) apiURL += "&fx24=" + query.fx24.ToString().ToLower();
			if (query.includelocation != WWOBool.NoSpecify) apiURL += "&includelocation=" + query.includelocation.ToString().ToLower();
			if (query.show_comments != WWOBool.NoSpecify) apiURL += "&show_comments=" + query.show_comments.ToString().ToLower();
			if (query.showlocaltime != WWOBool.NoSpecify) apiURL += "&showlocaltime=" + query.showlocaltime.ToString().ToLower();
			if (query.showmap != WWOBool.NoSpecify) apiURL += "&showmap=" + query.showmap.ToString().ToLower();
			if (query.aqi != WWOBool.NoSpecify) apiURL += "&aqi=" + query.aqi.ToString().ToLower();
			if (query.alerts != WWOBool.NoSpecify) apiURL += "&alerts=" + query.alerts.ToString().ToLower();
			if (query.lang != Language.NoSpecify) apiURL += "&lang=" + query.lang.ToString().ToLower();
			_AppendExtraParameter(ref apiURL, query.extra);

			RequestHandler.Process(apiURL, (success, jsonText) =>
			{
				if (success) // Success
			{
				//_WriteToTextFile(jsonText, "GetLocalWeather");
				cachedResult = jsonText;
				// serialize the json output and parse in the helper class
				LocalWeatherAPI result = JsonConvert.DeserializeObject<LocalWeatherAPI>(jsonText);
					onComplete(true, result);
				}
				else // Fail
			{
					onComplete(false, null);
				}
			});
		}

		/// <summary>
		/// The Local Historical or Past Weather API (also known as City and Town Historical Weather API) allows you to access weather conditions from 1st July 2008 up until the present time.
		/// The API returns weather elements such as temperature, precipitation (rainfall), weather description, weather icon and wind speed.
		/// </summary>
		public void GetLocalHistory(LocalHistoryAPI.LocalHistoryQuery query, Action<bool, LocalHistoryAPI> onComplete)
		{
			// create URL based on query paramters
			string apiURL = ApiBaseURL + "past-weather.ashx?format=json&key=" + PremiumApiKey + "&q=" + query.q + "&date=" + query.date;
			if (!string.IsNullOrEmpty(query.enddate)) apiURL += "&enddate=" + query.enddate;
			if (query.includelocation != WWOBool.NoSpecify) apiURL += "&includelocation=" + query.includelocation.ToString().ToLower();
			if (query.tp != WeatherTimeInterval.NoSpecify) apiURL += "&tp=" + query.tp.ToString().ToLower();
			if (query.lang != Language.NoSpecify) apiURL += "&lang=" + query.lang.ToString().ToLower();
			_AppendExtraParameter(ref apiURL, query.extra);

			RequestHandler.Process(apiURL, (success, jsonText) =>
			{
				if (success) // Success
			{
				//_WriteToTextFile(jsonText, "GetLocalHistory");
				cachedResult = jsonText;
				// serialize the json output and parse in the helper class
				LocalHistoryAPI result = JsonConvert.DeserializeObject<LocalHistoryAPI>(jsonText);
					onComplete(true, result);
				}
				else // Fail
			{
					onComplete(false, null);
				}
			});
		}

		/// <summary>
		/// The Premium Marine Weather REST API method allows you to access today's live marine/sailing weather forecast for a given longitude and latitude,
		/// as well as up to 7 days of forecast. The Marine Weather API returns weather elements such as temperature, precipitation (rainfall),
		/// weather description, weather icon, wind speed, Tide data, significant wave height, swell height, swell direction and swell period.
		/// </summary>
		public void GetMarineWeather(MarineWeatherAPI.MarineWeatherQuery query, Action<bool, MarineWeatherAPI> onComplete)
		{
			// create URL based on query paramters
			string apiURL = ApiBaseURL + "marine.ashx?format=json&key=" + PremiumApiKey + "&q=" + query.q;
			if (!string.IsNullOrEmpty(query.date)) apiURL += "&date=" + query.date;
			if (query.fx != WWOBool.NoSpecify) apiURL += "&fx=" + query.fx.ToString().ToLower();
			if (query.tp != WeatherTimeInterval.NoSpecify) apiURL += "&tp=" + query.tp.ToString().ToLower();
			if (query.tide != WWOBool.NoSpecify) apiURL += "&tide=" + query.tide.ToString().ToLower();
			if (query.lang != Language.NoSpecify) apiURL += "&lang=" + query.lang.ToString().ToLower();
			_AppendExtraParameter(ref apiURL, query.extra);

			RequestHandler.Process(apiURL, (success, jsonText) =>
			{
				if (success) // Success
			{
				//_WriteToTextFile(jsonText, "GetMarineWeather");
				cachedResult = jsonText;
				// serialize the json output and parse in the helper class
				MarineWeatherAPI result = JsonConvert.DeserializeObject<MarineWeatherAPI>(jsonText);
					onComplete(true, result);
				}
				else // Fail
			{
					onComplete(false, null);
				}
			});
		}

		/// <summary>
		/// The Premium Historical Marine Weather REST API method allows you to access marine data since 1st Jan, 2015 for a given longitude and latitude,
		/// as well as tide data. The Historical Marine Weather API returns weather elements such as temperature, precipitation (rainfall), weather description,
		/// weather icon, wind speed, Tide data, significant wave height, swell height, swell direction and swell period.
		/// </summary>
		public void GetMarineHistory(MarineHistoryAPI.MarineHistoryQuery query, Action<bool, MarineHistoryAPI> onComplete)
		{
			// create URL based on query paramters
			string apiURL = ApiBaseURL + "past-marine.ashx?format=json&key=" + PremiumApiKey + "&q=" + query.q + "&date=" + query.date;
			if (!string.IsNullOrEmpty(query.enddate)) apiURL += "&enddate=" + query.enddate;
			if (query.tide != WWOBool.NoSpecify) apiURL += "&tide=" + query.tide.ToString().ToLower();
			if (query.lang != Language.NoSpecify) apiURL += "&lang=" + query.lang.ToString().ToLower();
			_AppendExtraParameter(ref apiURL, query.extra);

			RequestHandler.Process(apiURL, (success, jsonText) =>
			{
				if (success) // Success
			{
				//_WriteToTextFile(jsonText, "GetMarineHistory");
				cachedResult = jsonText;
				// serialize the json output and parse in the helper class
				MarineHistoryAPI result = JsonConvert.DeserializeObject<MarineHistoryAPI>(jsonText);
					onComplete(true, result);
				}
				else // Fail
			{
					onComplete(false, null);
				}
			});
		}

		/// <summary>
		/// The Premium Ski and Mountain Weather REST API method allows you to access 7 day of accurate and reliable weather forecast for Top, Middle and Bottom Elevations.
		/// The Ski and Mountain Weather API returns weather elements for each Top, Middle and Bottom elevations such as Chance of snow, expected snow fall,
		/// freeze level, temperature, precipitation (rainfall), weather description, weather icon and wind speed and much more.
		/// </summary>
		public void GetSkiWeather(SkiWeatherAPI.SkiWeatherQuery query, Action<bool, SkiWeatherAPI> onComplete)
		{
			// create URL based on query paramters
			string apiURL = ApiBaseURL + "ski.ashx?format=json&key=" + PremiumApiKey + "&q=" + query.q; // + "&num_of_days=" + query.num_of_days;
			if (!string.IsNullOrEmpty(query.date)) apiURL += "&date=" + query.date;
			if (query.includelocation != WWOBool.NoSpecify) apiURL += "&includelocation=" + query.includelocation.ToString().ToLower();
			if (query.lang != Language.NoSpecify) apiURL += "&lang=" + query.lang.ToString().ToLower();
			_AppendExtraParameter(ref apiURL, query.extra);

			RequestHandler.Process(apiURL, (success, jsonText) =>
			{
				if (success) // Success
			{
				//_WriteToTextFile(jsonText, "GetSkiWeather");
				cachedResult = jsonText;
				// serialize the json output and parse in the helper class
				SkiWeatherAPI result = JsonConvert.DeserializeObject<SkiWeatherAPI>(jsonText);
					onComplete(true, result);
				}
				else // Fail
			{
					onComplete(false, null);
				}
			});
		}

		/// <summary>
		/// The Premium Astronomy and Lunar REST API method allows you to access astronomy information for any given date.
		/// </summary>
		public void GetAstronomy(AstronomyAPI.AstronomyQuery query, Action<bool, AstronomyAPI> onComplete)
		{
			// create URL based on query paramters
			string apiURL = ApiBaseURL + "astronomy.ashx?format=json&key=" + PremiumApiKey + "&q=" + query.q + "&date=" + query.date;
			if (query.includelocation != WWOBool.NoSpecify) apiURL += "&includelocation=" + query.includelocation.ToString().ToLower();
			if (query.show_comments != WWOBool.NoSpecify) apiURL += "&show_comments=" + query.show_comments.ToString().ToLower();
			_AppendExtraParameter(ref apiURL, query.extra);

			RequestHandler.Process(apiURL, (success, jsonText) =>
			{
				if (success) // Success
			{
				//_WriteToTextFile(jsonText, "GetAstronomy");
				cachedResult = jsonText;
				// serialize the json output and parse in the helper class
				AstronomyAPI result = JsonConvert.DeserializeObject<AstronomyAPI>(jsonText);
					onComplete(true, result);
				}
				else // Fail
			{
					onComplete(false, null);
				}
			});
		}

		/// <summary>
		/// Write to file for debug purpose.
		/// </summary>
		private void _WriteToTextFile(string text, string filename)
		{
			string saveDir = System.IO.Path.Combine(UnityEngine.Application.dataPath, "_Debug_WWOApiHelper");
			if (!System.IO.Directory.Exists(saveDir)) System.IO.Directory.CreateDirectory(saveDir);
			System.IO.File.WriteAllText(System.IO.Path.Combine(saveDir, filename + ".txt"), text);
		}
	}
}
