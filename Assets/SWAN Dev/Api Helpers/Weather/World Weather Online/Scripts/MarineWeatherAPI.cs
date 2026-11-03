// Created by SWAN DEV 2021
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDev.WWO_API
{
    public class MarineWeatherAPI
    {
        public Data data;

        public class Data
        {
            public List<Request> request;
            public List<NearestArea> nearest_area;
            public List<Weather> weather;
        }

        [Serializable]
        public class MarineWeatherQuery
        {
            [Header("[ Required ]")]
            [Tooltip("Latitude and longitude. Comma separated longitude and latitude in degrees. For example, q=48.834,2.394")]
            public string q = "48.834,2.394";

            [Header("[ Optional ]")]
            [Tooltip("Specifies weather for a date (current or future). Valid values: today, tomorrow, or yyyy-MM-dd(e.g. 2021-05-15)")]
            public string date;

            [Tooltip("Whether to return weather forecast output.")]
            public WWOBool fx;

            [Tooltip("Specifies the weather forecast time interval in hours. Options are: 1 hour, 3 hourly, 6 hourly, 12 hourly (day/night) or 24 hourly (day average).")]
            public WeatherTimeInterval tp;

            [Tooltip("To return tide data information if available")]
            public WWOBool tide;

            [Tooltip("Returns weather description in other languages listed on Multilingual Support page : https://www.worldweatheronline.com/developer/api/multilingual.aspx")]
            public Language lang;

            [Tooltip("Include extra information.")]
            public ExtraParameter[] extra;
        }

        public class TideData
        {
            public string tideTime;
            public string tideHeight_mt;
            public string tideDateTime;
            public string tide_type;
        }

        public class Tide
        {
            public List<TideData> tide_data;
        }

        public class Hourly
        {
            public string time;
            public string tempC;
            public string tempF;
            public string windspeedMiles;
            public string windspeedKmph;
            public string winddirDegree;
            public string winddir16Point;
            public string weatherCode;
            public List<WeatherIconUrl> weatherIconUrl;
            public string precipMM;
            public string humidity;
            public string visibility;
            public string pressure;
            public string cloudcover;
            public string sigHeight_m;
            public string swellHeight_m;
            public string swellHeight_ft;
            public string swellDir;
            public string swellDir16Point;
            public string swellPeriod_secs;
            public string waterTemp_C;
            public string waterTemp_F;
            public string uvIndex;

            public string precipInches;
            public string visibilityMiles;
            public string pressureInches;
            public string HeatIndexC;
            public string HeatIndexF;
            public string DewPointC;
            public string DewPointF;
            public string WindChillC;
            public string WindChillF;
            public string WindGustMiles;
            public string WindGustKmph;
            public string FeelsLikeC;
            public string FeelsLikeF;

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
            public string maxtempC;
            public string maxtempF;
            public string mintempC;
            public string mintempF;
            public string uvIndex;
            public List<Astronomy> astronomy;
            public List<Hourly> hourly;
            public List<Tide> tides;
        }
    }
}
