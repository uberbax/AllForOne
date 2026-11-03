// Created by SWAN DEV 2021
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDev.WWO_API
{
    public class AstronomyAPI
    {
        public Data data;

        public class Data
        {
            public List<Request> request;
            public List<NearestArea> nearest_area;
            public List<TimeZone> time_zone;
        }

        [Serializable]
        public class AstronomyQuery
        {
            [Header("[ Required ]")]
            [Tooltip("'q' parameter may have one of the following values to describe location: " +
                "1. City or town name (e.g. New+York, New+york,ny, London,united+kingdom); " +
                "2. IP address (e.g. 101.25.32.325); " +
                "3. UK or Canada Postal Code or US Zipcode (e.g. SW1, 90201); " +
                "4. Latitude and longitude (e.g. 48.834,2.394)")]
            public string q = "New+York";

            [Tooltip("Specifies a date for which astronomy data is required (current or future). Valid values: today, tomorrow, or yyyy-MM-dd(e.g. 2021-05-15)")]
            public string date;

            [Header("[ Optional ]")]
            [Tooltip("Whether to return the nearest weather point for which the weather data is returned for a given postcode, zipcode and lat/lon values.")]
            public WWOBool includelocation;

            [Tooltip("Whether output contains CSV comments.")]
            public WWOBool show_comments;

            [Tooltip("Include extra information. See note here: https://www.worldweatheronline.com/developer/api/docs/local-city-town-weather-api.aspx#extraparameter")]
            public ExtraParameter[] extra;
        }

        public class TimeZone
        {
            public string localtime;
            public string utcOffset;
            public string zone;
            public string sunrise;
            public string sunset;
            public string moonrise;
            public string moonset;
            public string moon_phase;
            public string moon_illumination;
        }
    }
}
