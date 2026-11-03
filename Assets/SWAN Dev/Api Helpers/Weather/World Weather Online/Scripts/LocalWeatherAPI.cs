// Created by SWAN DEV 2021
using System;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json;

namespace SDev.WWO_API
{
    public class LocalWeatherAPI
    {
        public Data data;

        public class Data
        {
            public List<Request> request;
            public List<NearestArea> nearest_area;
            public List<TimeZone> time_zone;
            public List<CurrentCondition> current_condition;
            public List<Weather> weather;
            public List<ClimateAverage> ClimateAverages;
            public Alerts alerts;
        }

        [Serializable]
        public class LocalWeatherQuery
        {
            [Header("[ Required ]")]
            [Tooltip("'q' parameter may have one of the following values to describe location: " +
                "1. City or town name (e.g. New+York, New+york,ny, London,united+kingdom); " +
                "2. IP address (e.g. 101.25.32.325); " +
                "3. UK or Canada Postal Code or US Zipcode (e.g. SW1, 90201); " +
                "4. Latitude and longitude (e.g. 48.834,2.394)")]
            public string q = "New+York";

            [Tooltip("Number of days of forecast.")]
            public int num_of_days = 3;

            [Header("[ Optional ]")]
            [Tooltip("Specifies weather for a date (current or future). Valid values: today, tomorrow, or yyyy-MM-dd(e.g. 2021-05-15)")]
            public string date;

            [Tooltip("Change the current condition observation_time field format in the output.")]
            public DateFormat date_format;

            [Tooltip("Whether to return weather forecast output.")]
            public WWOBool fx;

            [Tooltip("Whether to return current weather conditions output.")]
            public WWOBool cc;

            [Tooltip("Whether to return monthly climate average data.")]
            public WWOBool mca;

            [Tooltip("Returns 24 hourly weather information in a 3 hourly interval response.")]
            public WWOBool fx24;

            [Tooltip("Whether to return the nearest weather point for which the weather data is returned for a given postcode, zipcode and lat/lon values.")]
            public WWOBool includelocation;

            [Tooltip("Specifies the weather forecast time interval in hours. Options are: 1 hour, 3 hourly, 6 hourly, 12 hourly (day/night) or 24 hourly (day average).")]
            public WeatherTimeInterval tp;

            [Tooltip("Whether output contains CSV/TAB comments.")]
            public WWOBool show_comments;

            [Tooltip("Whether to return the timezone information i.e. local time and offset hour and minute for the location.")]
            public WWOBool showlocaltime;

            [Tooltip("Returns series of GIF images for rain, snow and pressure map and surface temperature map.")]
            public WWOBool showmap;

            [Tooltip("Whether to return the air quality data information.")]
            public WWOBool aqi;

            [Tooltip("Whether to return the weather alerts.")]
            public WWOBool alerts;

            [Tooltip("Returns weather description in other languages listed on Multilingual Support page : https://www.worldweatheronline.com/developer/api/multilingual.aspx")]
            public Language lang;

            [Tooltip("Include extra information. See note here: https://www.worldweatheronline.com/developer/api/docs/local-city-town-weather-api.aspx#extraparameter")]
            public ExtraParameter[] extra;
        }

        public class TimeZone
        {
            public string localtime;
            public string utcOffset;
            public string zone;
        }

        public class AirQuality
        {
            public string co;
            public string o3;
            public string no2;
            public string so2;
            public string pm2_5;
            public string pm10;
            [JsonProperty("us-epa-index")] public string us_epa_index;
            [JsonProperty("gb-defra-index")] public string gb_defra_index;
        }

        public class CurrentCondition
        {
            public string observation_time; //DateTime
            public string temp_C;
            public string temp_F;
            public string weatherCode;
            public List<WeatherIconUrl> weatherIconUrl;
            public string windspeedMiles;
            public string windspeedKmph;
            public string winddirDegree;
            public string winddir16Point;
            public string precipMM;
            public string precipInches;
            public string humidity;
            public string visibility;
            public string visibilityMiles;
            public string pressure;
            public string pressureInches;
            public string cloudcover;
            public string FeelsLikeC;
            public string FeelsLikeF;
            public string uvIndex;
            public AirQuality air_quality;

            public List<WeatherDesc> weatherDesc;
            // Language
            public List<WeatherDesc> lang_ar;
            public List<WeatherDesc> lang_bn;
            public List<WeatherDesc> lang_bg;
            public List<WeatherDesc> lang_zh;
            public List<WeatherDesc> lang_zh_tw;
            public List<WeatherDesc> lang_cs;
            public List<WeatherDesc> lang_da;
            public List<WeatherDesc> lang_nl;
            public List<WeatherDesc> lang_fi;
            public List<WeatherDesc> lang_fr;
            public List<WeatherDesc> lang_de;
            public List<WeatherDesc> lang_el;
            public List<WeatherDesc> lang_hi;
            public List<WeatherDesc> lang_hu;
            public List<WeatherDesc> lang_it;
            public List<WeatherDesc> lang_ja;
            public List<WeatherDesc> lang_jv;
            public List<WeatherDesc> lang_ko;
            public List<WeatherDesc> lang_zh_cmn;
            public List<WeatherDesc> lang_mr;
            public List<WeatherDesc> lang_pl;
            public List<WeatherDesc> lang_pt;
            public List<WeatherDesc> lang_pa;
            public List<WeatherDesc> lang_ro;
            public List<WeatherDesc> lang_ru;
            public List<WeatherDesc> lang_sr;
            public List<WeatherDesc> lang_si;
            public List<WeatherDesc> lang_sk;
            public List<WeatherDesc> lang_es;
            public List<WeatherDesc> lang_sv;
            public List<WeatherDesc> lang_ta;
            public List<WeatherDesc> lang_te;
            public List<WeatherDesc> lang_tr;
            public List<WeatherDesc> lang_uk;
            public List<WeatherDesc> lang_ur;
            public List<WeatherDesc> lang_vi;
            public List<WeatherDesc> lang_zh_wuu;
            public List<WeatherDesc> lang_zh_hsn;
            public List<WeatherDesc> lang_zh_yue;
            public List<WeatherDesc> lang_zu;

            public List<WeatherDesc> GetWeatherDescs(Language queryLanguage)
            {
                List<WeatherDesc> result = weatherDesc;
                if (queryLanguage == Language.NoSpecify) return result;

                switch (queryLanguage)
                {
                    case Language.ar:
                        result = lang_ar;
                        break;
                    case Language.bg:
                        result = lang_bg;
                        break;
                    case Language.bn:
                        result = lang_bn;
                        break;
                    case Language.cs:
                        result = lang_cs;
                        break;
                    case Language.da:
                        result = lang_da;
                        break;
                    case Language.de:
                        result = lang_de;
                        break;
                    case Language.el:
                        result = lang_el;
                        break;
                    case Language.es:
                        result = lang_es;
                        break;
                    case Language.fi:
                        result = lang_fi;
                        break;
                    case Language.fr:
                        result = lang_fr;
                        break;
                    case Language.hi:
                        result = lang_hi;
                        break;
                    case Language.hu:
                        result = lang_hu;
                        break;
                    case Language.it:
                        result = lang_it;
                        break;
                    case Language.ja:
                        result = lang_ja;
                        break;
                    case Language.jv:
                        result = lang_jv;
                        break;
                    case Language.ko:
                        result = lang_ko;
                        break;
                    case Language.mr:
                        result = lang_mr;
                        break;
                    case Language.nl:
                        result = lang_nl;
                        break;
                    case Language.pa:
                        result = lang_pa;
                        break;
                    case Language.pl:
                        result = lang_pl;
                        break;
                    case Language.pt:
                        result = lang_pt;
                        break;
                    case Language.ro:
                        result = lang_ro;
                        break;
                    case Language.ru:
                        result = lang_ru;
                        break;
                    case Language.si:
                        result = lang_si;
                        break;
                    case Language.sk:
                        result = lang_sk;
                        break;
                    case Language.sr:
                        result = lang_sr;
                        break;
                    case Language.sv:
                        result = lang_sv;
                        break;
                    case Language.ta:
                        result = lang_ta;
                        break;
                    case Language.te:
                        result = lang_te;
                        break;
                    case Language.tr:
                        result = lang_tr;
                        break;
                    case Language.uk:
                        result = lang_uk;
                        break;
                    case Language.ur:
                        result = lang_ur;
                        break;
                    case Language.vi:
                        result = lang_vi;
                        break;
                    case Language.zh:
                        result = lang_zh;
                        break;
                    case Language.zh_cmn:
                        result = lang_zh_cmn;
                        break;
                    case Language.zh_hsn:
                        result = lang_zh_hsn;
                        break;
                    case Language.zh_tw:
                        result = lang_zh_tw;
                        break;
                    case Language.zh_wuu:
                        result = lang_zh_wuu;
                        break;
                    case Language.zh_yue:
                        result = lang_zh_yue;
                        break;
                    case Language.zu:
                        result = lang_zu;
                        break;
                }
                return result;
            }
        }

        public class Hourly
        {
            public string time;
            public string tempC;
            public string tempF;
            public string FeelsLikeC;
            public string FeelsLikeF;
            public string HeatIndexC;
            public string HeatIndexF;
            public string DewPointC;
            public string DewPointF;
            public string WindChillC;
            public string WindChillF;
            public string windspeedMiles;
            public string windspeedKmph;
            public string windspeedKnots;
            public string windspeedMeterSec;
            public string WindGustMiles;
            public string WindGustKmph;
            public string winddirDegree;
            public string winddir16Point;
            public string weatherCode;
            public List<WeatherIconUrl> weatherIconUrl;
            public string precipMM;
            public string precipInches;
            public string humidity;
            public string visibility;
            public string visibilityMiles;
            public string pressure;
            public string pressureInches;
            public string cloudcover;
            public string uvIndex;

            public string chanceofrain;
            public string chanceofremdry;
            public string chanceofwindy;
            public string chanceofovercast;
            public string chanceofsunshine;
            public string chanceoffrost;
            public string chanceofhightemp;
            public string chanceoffog;
            public string chanceofsnow;
            public string chanceofthunder;
            public AirQuality air_quality;

            public List<WeatherDesc> weatherDesc;
            // Language
            public List<WeatherDesc> lang_ar;
            public List<WeatherDesc> lang_bn;
            public List<WeatherDesc> lang_bg;
            public List<WeatherDesc> lang_zh;
            public List<WeatherDesc> lang_zh_tw;
            public List<WeatherDesc> lang_cs;
            public List<WeatherDesc> lang_da;
            public List<WeatherDesc> lang_nl;
            public List<WeatherDesc> lang_fi;
            public List<WeatherDesc> lang_fr;
            public List<WeatherDesc> lang_de;
            public List<WeatherDesc> lang_el;
            public List<WeatherDesc> lang_hi;
            public List<WeatherDesc> lang_hu;
            public List<WeatherDesc> lang_it;
            public List<WeatherDesc> lang_ja;
            public List<WeatherDesc> lang_jv;
            public List<WeatherDesc> lang_ko;
            public List<WeatherDesc> lang_zh_cmn;
            public List<WeatherDesc> lang_mr;
            public List<WeatherDesc> lang_pl;
            public List<WeatherDesc> lang_pt;
            public List<WeatherDesc> lang_pa;
            public List<WeatherDesc> lang_ro;
            public List<WeatherDesc> lang_ru;
            public List<WeatherDesc> lang_sr;
            public List<WeatherDesc> lang_si;
            public List<WeatherDesc> lang_sk;
            public List<WeatherDesc> lang_es;
            public List<WeatherDesc> lang_sv;
            public List<WeatherDesc> lang_ta;
            public List<WeatherDesc> lang_te;
            public List<WeatherDesc> lang_tr;
            public List<WeatherDesc> lang_uk;
            public List<WeatherDesc> lang_ur;
            public List<WeatherDesc> lang_vi;
            public List<WeatherDesc> lang_zh_wuu;
            public List<WeatherDesc> lang_zh_hsn;
            public List<WeatherDesc> lang_zh_yue;
            public List<WeatherDesc> lang_zu;

            public List<WeatherDesc> GetWeatherDescs(Language queryLanguage)
            {
                List<WeatherDesc> result = weatherDesc;
                if (queryLanguage == Language.NoSpecify) return result;

                switch (queryLanguage)
                {
                    case Language.ar:
                        result = lang_ar;
                        break;
                    case Language.bg:
                        result = lang_bg;
                        break;
                    case Language.bn:
                        result = lang_bn;
                        break;
                    case Language.cs:
                        result = lang_cs;
                        break;
                    case Language.da:
                        result = lang_da;
                        break;
                    case Language.de:
                        result = lang_de;
                        break;
                    case Language.el:
                        result = lang_el;
                        break;
                    case Language.es:
                        result = lang_es;
                        break;
                    case Language.fi:
                        result = lang_fi;
                        break;
                    case Language.fr:
                        result = lang_fr;
                        break;
                    case Language.hi:
                        result = lang_hi;
                        break;
                    case Language.hu:
                        result = lang_hu;
                        break;
                    case Language.it:
                        result = lang_it;
                        break;
                    case Language.ja:
                        result = lang_ja;
                        break;
                    case Language.jv:
                        result = lang_jv;
                        break;
                    case Language.ko:
                        result = lang_ko;
                        break;
                    case Language.mr:
                        result = lang_mr;
                        break;
                    case Language.nl:
                        result = lang_nl;
                        break;
                    case Language.pa:
                        result = lang_pa;
                        break;
                    case Language.pl:
                        result = lang_pl;
                        break;
                    case Language.pt:
                        result = lang_pt;
                        break;
                    case Language.ro:
                        result = lang_ro;
                        break;
                    case Language.ru:
                        result = lang_ru;
                        break;
                    case Language.si:
                        result = lang_si;
                        break;
                    case Language.sk:
                        result = lang_sk;
                        break;
                    case Language.sr:
                        result = lang_sr;
                        break;
                    case Language.sv:
                        result = lang_sv;
                        break;
                    case Language.ta:
                        result = lang_ta;
                        break;
                    case Language.te:
                        result = lang_te;
                        break;
                    case Language.tr:
                        result = lang_tr;
                        break;
                    case Language.uk:
                        result = lang_uk;
                        break;
                    case Language.ur:
                        result = lang_ur;
                        break;
                    case Language.vi:
                        result = lang_vi;
                        break;
                    case Language.zh:
                        result = lang_zh;
                        break;
                    case Language.zh_cmn:
                        result = lang_zh_cmn;
                        break;
                    case Language.zh_hsn:
                        result = lang_zh_hsn;
                        break;
                    case Language.zh_tw:
                        result = lang_zh_tw;
                        break;
                    case Language.zh_wuu:
                        result = lang_zh_wuu;
                        break;
                    case Language.zh_yue:
                        result = lang_zh_yue;
                        break;
                    case Language.zu:
                        result = lang_zu;
                        break;
                }
                return result;
            }
        }

        public class Weather
        {
            public string date;
            public List<Astronomy> astronomy;
            public string maxtempC;
            public string maxtempF;
            public string mintempC;
            public string mintempF;
            public string avgtempC;
            public string avgtempF;
            public string totalSnow_cm;
            public string sunHour;
            public string uvIndex;
            public AirQuality air_quality;
            public List<Hourly> hourly;
        }

        public class Month
        {
            public string index;
            public string name;

            public string avgMinTemp;
            public string avgMinTemp_F;
            public string avgMaxTemp;
            public string avgMaxTemp_F;

            public string absMinTemp;
            public string absMinTemp_F;
            public string absMaxTemp;
            public string absMaxTemp_F;

            public string avgDailyRainfall;
            public string avgMonthlyRainfall;

            public string avgDryDays;
            public string avgSnowDays;
            public string avgFogDays;
        }

        public class ClimateAverage
        {
            public List<Month> month;
        }

        public class Alert
        {
            public string headline;
            public string msgType;
            public string severity;
            public string urgency;
            public string areas;
            public string category;
            public string certainty;
            [JsonProperty("event")] public string _event;
            public string note;
            public string effective;  //DateTime
            public string expires;    //DateTime
            public string desc;
            public string instruction;
        }

        public class Alerts
        {
            public List<Alert> alert;
        }
    }
}
