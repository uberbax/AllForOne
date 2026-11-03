// Created by SWAN DEV 2021
using System;
using System.Collections.Generic;

namespace SDev.WWO_API
{
    /* Shared classes and enum for WorldWeatherOnline API */

    #region ----- Enum -----
    public enum WWOBool
    {
        NoSpecify = 0,

        Yes,
        No,
    }

    public enum DateFormat
    {
        NoSpecify = 0,

        Unix,
        ISO8601,
    }

    public enum WeatherTimeInterval
    {
        NoSpecify = 0,

        Hourly_1 = 1,
        Hourly_3 = 3,
        Hourly_6 = 6,
        Hourly_12 = 12,
        Hourly_24 = 24,
    }

    public enum LocationCategory
    {
        NoSpecify = 0,

        Ski,
        Cricket,
        Football,
        Golf,
        Fishing,
    }

    public enum Language
    {
        /// <summary>
        /// Return the weather description in the default language (English) only. Note that the default language is always included even another language is specified in the query object.
        /// </summary>
        NoSpecify = 0,

        ar, //Arabic
        bn, //Bengali
        bg, //Bulgarian
        zh, //Chinese Simplified
        zh_tw, //Chinese Traditional
        cs, //Czech
        da, //Danish
        nl, //Dutch
        fi, //Finnish
        fr, //French
        de, //German
        el, //Greek
        hi, //Hindi
        hu, //Hungarian
        it, //Italian
        ja, //Japanese
        jv, //Javanese
        ko, //Korean
        zh_cmn, //Mandarin
        mr, //Marathi
        pl, //Polish
        pt, //Portuguese
        pa, //Punjabi
        ro, //Romanian
        ru, //Russian
        sr, //Serbian
        si, //Sinhalese
        sk, //Slovak
        es, //Spanish
        sv, //Swedish
        ta, //Tamil
        te, //Telugu
        tr, //Turkish
        uk, //Ukrainian
        ur, //Urdu
        vi, //Vietnamese
        zh_wuu, //Wu (Shanghainese)
        zh_hsn, //Xiang
        zh_yue, //Yue (Cantonese)
        zu, //Zulu
    }
    #endregion

    #region ----- Shared Classes -----
    [Serializable]
    public class ExtraParameter
    {
        public string parameterName;
        public string parameterValue;
    }

    public class Request
    {
        public string type;
        public string query;
    }

    public class WeatherIconUrl
    {
        public string value;
    }

    public class WeatherUrl
    {
        public string value;
    }

    public class WeatherDesc
    {
        public string value;
    }

    public class Astronomy
    {
        public string sunrise;
        public string sunset;
        public string moonrise;
        public string moonset;
        public string moon_phase;
        public string moon_illumination;
    }

    public class AreaName
    {
        public string value;
    }

    public class Country
    {
        public string value;
    }

    public class Region
    {
        public string value;
    }

    public class NearestArea
    {
        /// <summary>
        /// Latitude in decimal degrees.
        /// </summary>
        public string latitude;
        /// <summary>
        /// Longitude in decimal degrees.
        /// </summary>
        public string longitude;
        public string population;
        public string distance_miles;
        public List<AreaName> areaName;
        public List<Country> country;
        public List<Region> region;
        public List<WeatherUrl> weatherUrl;
    }
    #endregion
}
