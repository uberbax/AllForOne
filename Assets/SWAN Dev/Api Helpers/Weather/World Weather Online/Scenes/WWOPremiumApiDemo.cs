// Created by SWAN DEV 2021
using UnityEngine;
using UnityEngine.UI;

namespace SDev.WWO_API
{
	/// <summary>
	/// World Weather Online API demo.
	/// FAQ/Limitation: https://developer.worldweatheronline.com/api/faq.aspx
	/// Apply your API key: https://developer.worldweatheronline.com/signup.aspx
	/// </summary>
	public class WWOPremiumApiDemo : MonoBehaviour
	{
		[Header("[ WWO API Key & Query Objects ]")]
		public string m_PremiumApiKey = "Your_Premium_Api_Key";

		private string _resultText = "";

		private void _SetDisplay(string result)
		{
			m_Text_DemoInformation.gameObject.SetActive(false);
			m_InputField_Display.text = result;
		}

		private void _SetResult(string result)
		{
			string text = result.Substring(0, Mathf.Min(16000, result.Length));
#if UNITY_EDITOR
			Debug.Log(text);
#endif
			m_InputField_ResultText.text = text;
		}

		private TimeZoneAPI _timeZoneWWOResult;
		public TimeZoneAPI.TimeZoneQuery m_TimeZoneQuery;
		public void GetTimeZone()
		{
			// call the location Search method with input parameters
			WWOApiHelper api = new WWOApiHelper(m_PremiumApiKey);
			api.GetTimeZone(m_TimeZoneQuery, (success, timeZone) =>
			{
				if (success)
				{
					_timeZoneWWOResult = timeZone;

					_resultText = "GetTimeZone API, q=" + m_TimeZoneQuery.q;

				// printing a few values to show how to get the output
				_resultText += "\n\n Local Time: " + timeZone.data.time_zone[0].localtime;
					_resultText += "\n Time Offset: " + timeZone.data.time_zone[0].utcOffset;
					_resultText += "\n Zone: " + timeZone.data.time_zone[0].zone;

					if (timeZone.data.nearest_area != null && timeZone.data.nearest_area.Count > 0)
					{
						_resultText += "\n Nearest Area: " + timeZone.data.nearest_area[0].areaName;
					// .......
				}

					_SetDisplay(_resultText);
					_SetResult(api.cachedResult);
				}
			});
		}

		private LocalWeatherAPI _localWeatherResult;
		public LocalWeatherAPI.LocalWeatherQuery m_LocalWeatherQuery;
		public void GetLocalWeather()
		{
			// call the local weather method with input parameters
			WWOApiHelper api = new WWOApiHelper(m_PremiumApiKey);
			api.GetLocalWeather(m_LocalWeatherQuery, (success, localWeather) =>
			{
				if (success)
				{
					_localWeatherResult = localWeather;

					// printing a few values to show how to get the output
					_resultText = "GetLocalWeather API, Days=" + m_LocalWeatherQuery.num_of_days + ", q=" + m_LocalWeatherQuery.q + ", date=" + m_LocalWeatherQuery.date + "\n\nCurrent Condition:";

					for (int i = 0; i < localWeather.data.current_condition.Count; i++)
					{
						_resultText += "\n Cloud Cover: " + localWeather.data.current_condition[i].cloudcover;
						_resultText += "\n Humidity: " + localWeather.data.current_condition[i].humidity;
						_resultText += "\n Temp C: " + localWeather.data.current_condition[i].temp_C;
						_resultText += "\n Visibility: " + localWeather.data.current_condition[i].weatherDesc[0].value;
						_resultText += "\n Observation Time: " + localWeather.data.current_condition[i].observation_time;
						_resultText += "\n Pressue: " + localWeather.data.current_condition[i].pressure;

						// get weatherDesc by the language parameter in the query object
						//_resultText += "\n Weather Desc: " + localWeather.data.current_condition[i].weatherDesc[0].value; // default: English (always included)
						_resultText += "\n Weather Desc (" + m_LocalWeatherQuery.lang + "): " + localWeather.data.current_condition[i].GetWeatherDescs(m_LocalWeatherQuery.lang)[0].value;
					}

					// check null and list size before getting data that not always exist, depends on your query parameters
					if (localWeather.data.alerts != null && localWeather.data.alerts.alert != null && localWeather.data.alerts.alert.Count > 0)
					{
						_resultText += "\n\n alert[0].areas: " + localWeather.data.alerts.alert[0].areas;
						_resultText += "\n alert[0].desc: " + localWeather.data.alerts.alert[0].desc;
						_resultText += "\n alert[0].effective: " + localWeather.data.alerts.alert[0].effective;
						_resultText += "\n alert[0].expires: " + localWeather.data.alerts.alert[0].expires;
					}

					if (localWeather.data != null && localWeather.data.weather != null)
					{
						for (int i = 0; i < localWeather.data.weather.Count; i++)
						{
							_resultText += "\n\n Date: " + localWeather.data.weather[i].date.ToString();
							_resultText += "\n Sunrise: " + localWeather.data.weather[i].astronomy[0].sunrise.ToString() + "\n Sunset: " +
								localWeather.data.weather[i].astronomy[0].sunset.ToString();
							_resultText += "\n Temp. C Low:" + localWeather.data.weather[i].mintempC + " High: " + localWeather.data.weather[i].maxtempC;

							var weatherDescs = localWeather.data.weather[0].hourly[0].GetWeatherDescs(m_LocalWeatherQuery.lang);
							_resultText += "\n Weather Descs (" + m_LocalWeatherQuery.lang + ") : " + weatherDescs[0].value;
						}
					}

					// Reminder : More parameters available in the localWeather.data object, get the parameters as need....

					_SetDisplay(_resultText);
					_SetResult(api.cachedResult);
				}
			});
		}

		private LocalHistoryAPI _localHistoryResult;
		public LocalHistoryAPI.LocalHistoryQuery m_LocalHistoryQuery;
		public void GetLocalHistory()
		{
			// call the past weather method with input parameters
			WWOApiHelper api = new WWOApiHelper(m_PremiumApiKey);
			api.GetLocalHistory(m_LocalHistoryQuery, (success, localHistory) =>
			{
				if (success)
				{
					_localHistoryResult = localHistory;

					_resultText = "GetLocalHistory API, q=" + m_LocalHistoryQuery.q + ", date=" + m_LocalHistoryQuery.date + ", end date=" + m_LocalHistoryQuery.enddate;

				// printing a few values to show how to get the output
				if (_localHistoryResult.data != null && _localHistoryResult.data.weather != null)
					{
						for (int i = 0; i < _localHistoryResult.data.weather.Count; i++)
						{
							_resultText += "\n\n Date: " + _localHistoryResult.data.weather[i].date;
							_resultText += "\n Max Temp(C): " + _localHistoryResult.data.weather[i].maxtempC + ", Min Temp(C): " + _localHistoryResult.data.weather[i].mintempC;
							_resultText += "\n Max Temp(F): " + _localHistoryResult.data.weather[i].maxtempF + ", Min Temp(F): " + _localHistoryResult.data.weather[i].mintempF;

							_resultText += "\n Cloud Cover: " + _localHistoryResult.data.weather[i].hourly[0].cloudcover;

							var weatherDescs = _localHistoryResult.data.weather[i].hourly[0].GetWeatherDescs(m_LocalHistoryQuery.lang);
							_resultText += "\n Weather Descs (" + m_LocalHistoryQuery.lang + ") : " + weatherDescs[0].value;
						}
					}

				// Reminder : More parameters available in the localHistory.data object, get the parameters as need....

				_SetDisplay(_resultText);
					_SetResult(api.cachedResult);
				}
			});
		}

		private SearchLocationAPI _searchLocationResult;
		public SearchLocationAPI.SearchLocationQuery m_SearchLocationQuery;
		public void SearchLocation()
		{
			// call the location Search method with input parameters
			WWOApiHelper api = new WWOApiHelper(m_PremiumApiKey);
			api.SearchLocation(m_SearchLocationQuery, (success, searchLocation) =>
			{
				if (success && searchLocation.search_api != null && searchLocation.search_api.result != null)
				{
					_searchLocationResult = searchLocation;

					_resultText = "SearchLocation API, q=" + m_SearchLocationQuery.q;

					// printing a few values to show how to get the output
					for (int i = 0; i < searchLocation.search_api.result.Count; i++)
					{
						var location = searchLocation.search_api.result[i];
						if (location != null)
						{
							_resultText += "\n\nLocation (" + i + ")";
							_resultText += "\n Latitude: " + location.latitude;
							_resultText += "\n Longitude: " + location.longitude;
							if (location.areaName != null && location.areaName.Count > 0) _resultText += "\n Area Name: " + location.areaName[0].value;
							if (location.region != null && location.region.Count > 0) _resultText += "\n Region: " + location.region[0].value;
							if (location.country != null && location.country.Count > 0) _resultText += "\n Country: " + location.country[0].value;
							if (!string.IsNullOrEmpty(location.population)) _resultText += "\n Population: " + location.population;
							if (location.timezone != null) _resultText += "\n Timezone: Zone = " + location.timezone.zone + ", Offset = " + location.timezone.offset;
							if (location.weatherUrl != null && location.weatherUrl.Count > 0) _resultText += "\n Weather Url: " + location.weatherUrl[0].value;
						}
					}

					_SetDisplay(_resultText);
					_SetResult(api.cachedResult);
				}
			});
		}

		public MarineWeatherAPI _marineWeatherResult;
		public MarineWeatherAPI.MarineWeatherQuery m_MarineWeatherQuery;
		public void GetMarineWeather()
		{
			// call the location Search method with input parameters
			WWOApiHelper api = new WWOApiHelper(m_PremiumApiKey);
			api.GetMarineWeather(m_MarineWeatherQuery, (success, marineWeather) =>
			{
				if (success)
				{
					_marineWeatherResult = marineWeather;

					_resultText = "GetMarineWeather API, q=" + m_MarineWeatherQuery.q;

				// printing a few values to show how to get the output
				if (marineWeather.data != null && marineWeather.data.weather != null)
					{
						for (int i = 0; i < marineWeather.data.weather.Count; i++)
						{
							_resultText += "\n\n Date: " + marineWeather.data.weather[i].date;
							_resultText += "\n Max Temp(C): " + marineWeather.data.weather[i].maxtempC + ", Min Temp(C): " + marineWeather.data.weather[i].mintempC;
							_resultText += "\n Max Temp(F): " + marineWeather.data.weather[i].maxtempF + ", Min Temp(F): " + marineWeather.data.weather[i].mintempF;

							if (marineWeather.data.weather[i].hourly != null)
							{
								var weatherDescs = marineWeather.data.weather[i].hourly[0].GetWeatherDescs(m_MarineWeatherQuery.lang);
								_resultText += "\n Weather Descs (" + m_MarineWeatherQuery.lang + ") : " + weatherDescs[0].value;
							}
						}
					}

					_SetDisplay(_resultText);
					_SetResult(api.cachedResult);
				}
			});
		}

		public MarineHistoryAPI _marineHistoryResult;
		public MarineHistoryAPI.MarineHistoryQuery m_MarineHistoryQuery;
		public void GetMarineHistory()
		{
			// call the location Search method with input parameters
			WWOApiHelper api = new WWOApiHelper(m_PremiumApiKey);
			api.GetMarineHistory(m_MarineHistoryQuery, (success, marineHistory) =>
			{
				if (success)
				{
					_marineHistoryResult = marineHistory;

					_resultText = "GetMarineHistory API, q=" + m_MarineHistoryQuery.q + ", date=" + m_MarineHistoryQuery.date + ", end date=" + m_MarineHistoryQuery.enddate;

				// printing a few values to show how to get the output
				if (marineHistory.data != null && marineHistory.data.weather != null)
					{
						for (int i = 0; i < marineHistory.data.weather.Count; i++)
						{
							_resultText += "\n\n Date: " + marineHistory.data.weather[i].date;
							_resultText += "\n Max Temp(C): " + marineHistory.data.weather[i].maxtempC + ", Min Temp(C): " + marineHistory.data.weather[i].mintempC;
							_resultText += "\n Max Temp(F): " + marineHistory.data.weather[i].maxtempF + ", Min Temp(F): " + marineHistory.data.weather[i].mintempF;

							if (marineHistory.data.weather[i].hourly != null)
							{
								var weatherDescs = marineHistory.data.weather[i].hourly[0].GetWeatherDescs(m_MarineHistoryQuery.lang);
								_resultText += "\n Weather Descs (" + m_MarineHistoryQuery.lang + ") : " + weatherDescs[0].value;
							}
						}
					}

					_SetDisplay(_resultText);
					_SetResult(api.cachedResult);
				}
			});
		}

		public SkiWeatherAPI _skiWeatherResult;
		public SkiWeatherAPI.SkiWeatherQuery m_SkiWeatherQuery;
		public void GetSkiWeather()
		{
			// call the location Search method with input parameters
			WWOApiHelper api = new WWOApiHelper(m_PremiumApiKey);
			api.GetSkiWeather(m_SkiWeatherQuery, (success, skiWeather) =>
			{
				if (success)
				{
					_skiWeatherResult = skiWeather;

					_resultText = "GetSkiWeather API, q=" + m_SkiWeatherQuery.q; // + ", num_of_days=" + m_SkiWeatherQuery.num_of_days;

				// printing a few values to show how to get the output
				if (skiWeather.data != null && skiWeather.data.weather != null)
					{
						for (int i = 0; i < skiWeather.data.weather.Count; i++)
						{
							_resultText += "\n\n Date: " + skiWeather.data.weather[i].date;
						//_resultText += "\n Max Temp(C): " + skiWeather.data.weather[i].maxtempC + ", Min Temp(C): " + skiWeather.data.weather[i].mintempC;
						if (skiWeather.data.weather[i].top != null)
							{
								_resultText += "\n Top Max Temp(C): " + skiWeather.data.weather[i].top[0].maxtempC + ", Top Min Temp(C): " + skiWeather.data.weather[i].top[0].mintempC;
							}
							if (skiWeather.data.weather[i].mid != null)
							{
								_resultText += "\n Mid Max Temp(C): " + skiWeather.data.weather[i].mid[0].maxtempC + ", Mid Min Temp(C): " + skiWeather.data.weather[i].mid[0].mintempC;
							}
							if (skiWeather.data.weather[i].bottom != null)
							{
								_resultText += "\n Bot Max Temp(C): " + skiWeather.data.weather[i].bottom[0].maxtempC + ", Bot Min Temp(C): " + skiWeather.data.weather[i].bottom[0].mintempC;
							}

							if (skiWeather.data.weather[i].hourly != null)
							{
								for (int j = 0; j < skiWeather.data.weather[i].hourly.Count; j++)
								{
									var top = skiWeather.data.weather[i].hourly[j].top[0];
									var top_weatherDescs = top.GetWeatherDescs(m_SkiWeatherQuery.lang);
									_resultText += "\n(Hourly-" + j + ") Top  Temp(C): " + top.tempC + ", Temp(F): " + top.tempF + " (" + top_weatherDescs[0].value + ")";
									var mid = skiWeather.data.weather[i].hourly[j].mid[0];
									var mid_weatherDescs = mid.GetWeatherDescs(m_SkiWeatherQuery.lang);
									_resultText += "\n(Hourly-" + j + ") Mid  Temp(C): " + mid.maxtempC + ", Temp(F): " + mid.tempF + " (" + mid_weatherDescs[0].value + ")";
									var bot = skiWeather.data.weather[i].hourly[j].bottom[0];
									var bot_weatherDescs = bot.GetWeatherDescs(m_SkiWeatherQuery.lang);
									_resultText += "\n(Hourly-" + j + ") Bot  Temp(C): " + bot.maxtempC + ", Temp(F): " + bot.tempF + " (" + bot_weatherDescs[0].value + ")";
								}
							}
						}
					}

					_SetDisplay(_resultText);
					_SetResult(api.cachedResult);
				}
			});
		}

		public AstronomyAPI _astronomyResult;
		public AstronomyAPI.AstronomyQuery m_AstronomyQuery;
		public void GetAstronomy()
		{
			// call the location Search method with input parameters
			WWOApiHelper api = new WWOApiHelper(m_PremiumApiKey);
			api.GetAstronomy(m_AstronomyQuery, (success, astronomy) =>
			{
				if (success)
				{
					_astronomyResult = astronomy;

					_resultText = "GetAstronomy API, q=" + m_AstronomyQuery.q + ", date=" + m_AstronomyQuery.date;

				// printing a few values to show how to get the output
				if (astronomy.data != null && astronomy.data.time_zone != null)
					{
						for (int i = 0; i < astronomy.data.time_zone.Count; i++)
						{
							var timezone = astronomy.data.time_zone[i];
							_resultText += "\n\n[TimeZone] " + timezone.zone + "\n localtime: " + timezone.localtime + ", utc offset: " + timezone.utcOffset;
							_resultText += "\n sunrise: " + timezone.sunrise + ", sunset: " + timezone.sunset;
							_resultText += "\n moonrise: " + timezone.moonrise + ", moonset: " + timezone.moonset;
							_resultText += "\n moon_illumination: " + timezone.moon_illumination + ", moon_phase: " + timezone.moon_phase;
						}
					}

					if (astronomy.data.nearest_area != null)
					{
						for (int i = 0; i < astronomy.data.nearest_area.Count; i++)
						{
							var area = astronomy.data.nearest_area[i];
							_resultText += "\n\n[Nearest Area] " + (area.areaName == null || area.areaName.Count == 0 || area.areaName[0] == null ? "" : area.areaName[0].value)
								+ "\n region: " + (area.region == null || area.region.Count == 0 || area.region[0] == null ? "" : area.region[0].value)
								+ ", country: " + (area.country == null || area.country.Count == 0 || area.country[0] == null ? "" : area.country[0].value);
							_resultText += "\n latitude: " + area.latitude + ", longitude: " + area.longitude + ", distance_miles: " + area.distance_miles + ", population: " + area.population;
							if (area.weatherUrl != null && area.weatherUrl.Count > 0) _resultText += "\n weatherUrl: " + area.weatherUrl[0].value;
						}
					}

					_SetDisplay(_resultText);
					_SetResult(api.cachedResult);
				}
			});
		}

		[Header("[ UI Components ]")]
		public Text m_Text_DemoInformation;
		public InputField m_InputField_Display;
		public InputField m_InputField_ResultText;
	}
}
