// Created by SWAN DEV 2021
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDev.WWO_API
{
    public class TimeZoneAPI
    {
        public Data data;

        public class Data
        {
            /// <summary>
            /// Contains information about the request.
            /// </summary>
            public List<Request> request;
            /// <summary>
            /// Contains the information about the nearest data point in our database from which the weather data has been taken.
            /// </summary>
            public List<NearestArea> nearest_area;
            /// <summary>
            /// Contains information about the location's time zone.
            /// </summary>
            public List<TimeZone> time_zone;
        }

        [Serializable]
        public class TimeZoneQuery
        {
            [Header("[ Required ]")]
            [Tooltip("'q' parameter may have one of the following values to describe location: " +
                "1. City or town name (e.g. New+York, New+york,ny, London,united+kingdom); " +
                "2. IP address (e.g. 101.25.32.325); " +
                "3. UK or Canada Postal Code or US Zipcode (e.g. SW1, 90201); " +
                "4. Latitude and longitude (e.g. 48.834,2.394)")]
            public string q = "New+York";

            [Header("[ Optional ]")]
            [Tooltip("Include extra information.")]
            public ExtraParameter[] extra;
        }

        public class TimeZone
        {
            /// <summary>
            /// The current local time.
            /// </summary>
            public string localtime;
            /// <summary>
            /// UTC offset from GMT in hour and minute.
            /// </summary>
            public string utcOffset;
            public string zone;
        }
    }
}
