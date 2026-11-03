// Created by SWAN DEV 2021
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDev.WWO_API
{
    public class SearchLocationAPI
    {
        public SearchApi search_api;

        public class SearchApi
        {
            public List<Result> result;
        }

        [Serializable]
        public class SearchLocationQuery
        {
            [Header("[ Required ]")]
            [Tooltip("'q' parameter may have one of the following values to describe location: " +
                "1. City or town name (e.g. New+York, New+york,ny, London,united+kingdom); " +
                "2. IP address (e.g. 101.25.32.325); " +
                "3. UK or Canada Postal Code or US Zipcode (e.g. SW1, 90201); " +
                "4. Latitude and longitude (e.g. 48.834,2.394)")]
            public string q = "New+York";

            [Header("[ Optional ]")]
            [Tooltip("The number of results to return. Premium API: Default 10, maximum 50; Free API: Default 3, maximum 3.")]
            public int num_of_results = 3;

            [Tooltip("Change the current condition observation_time field format in the output.")]
            public DateFormat date_format;

            [Tooltip("Whether to return offset hours from GMT for each location.")]
            public WWOBool timezone;

            [Tooltip("Whether to only search for popular locations, such as major cities.")]
            public WWOBool popular;

            [Tooltip("Returns nearest locations for the type of category provided.")]
            public LocationCategory wct;

            [Tooltip("Include extra information.")]
            public ExtraParameter[] extra;
        }

        public class Timezone
        {
            /// <summary>
            /// Hour offset from GMT.
            /// </summary>
            public string offset;
            public string zone;
        }

        public class Result
        {
            public List<AreaName> areaName;
            public List<Country> country;
            public List<Region> region;
            /// <summary>
            /// The location's latitude, in degrees.
            /// </summary>
            public string latitude;
            /// <summary>
            /// The location's longitude, in degrees.
            /// </summary>
            public string longitude;
            /// <summary>
            /// The location's population (if available). This value will be "0" If not available.
            /// </summary>
            public string population;
            /// <summary>
            /// The URL to the location's weather.
            /// </summary>
            public List<WeatherUrl> weatherUrl;
            /// <summary>
            /// The location's timezone offset from GMT.
            /// </summary>
            public Timezone timezone;
        }
    }
}