// Created by SWAN DEV 2017
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Networking; // UNITY_2017_3_OR_NEWER

namespace SDev.Util
{
    public class WebRequestHandlerPlus : MonoBehaviour
    {
        private float _timeOut = 10f;
        private Action<bool, string> _onComplete;

        public static WebRequestHandlerPlus Create(string name = "")
        {
            return new GameObject("WebRequestHandlerPlus-" + name).AddComponent<WebRequestHandlerPlus>();
        }

        public void Request(string apiUrl, Action<bool, string> onComplete, Dictionary<string, string> queryKeyValuePairs = null, Dictionary<string, string> headerKeyValuePairs = null, float timeOut = 10f)
        {
            _timeOut = timeOut;

            if (queryKeyValuePairs != null && queryKeyValuePairs.Count > 0)
            {
                apiUrl += "?"; // query string begin

                var enumerator = queryKeyValuePairs.Keys.GetEnumerator();
                for (int i = 0; i < queryKeyValuePairs.Count; i++)
                {
                    enumerator.MoveNext();
                    string p = enumerator.Current; // parameter
                    string val = queryKeyValuePairs[p]; // value
                    apiUrl += (i > 0 ? "&" : "") + p + "=" + val;
#if UNITY_EDITOR
                    Debug.Log(i + " - Set Query string to URL: " + p + " = " + val);
#endif
                }
            }

            StartCoroutine(_CallApi(apiUrl, onComplete, headerKeyValuePairs));
        }

        private IEnumerator _CallApi(string apiUrl, Action<bool, string> onComplete, Dictionary<string, string> headerKeyValuePairs = null)
        {
            if (_timeOut > 0f) Invoke("_Clear", _timeOut);
            _onComplete = onComplete;

            using (UnityWebRequest uwr = UnityWebRequest.Get(apiUrl))
            {
                if (headerKeyValuePairs != null && headerKeyValuePairs.Count > 0)
                {
                    var enumerator = headerKeyValuePairs.Keys.GetEnumerator();
                    for (int i = 0; i < headerKeyValuePairs.Count; i++)
                    {
                        enumerator.MoveNext();
                        string key = enumerator.Current;
                        string val = headerKeyValuePairs[key];
                        uwr.SetRequestHeader(key, val);
#if UNITY_EDITOR
                        Debug.Log(i + " - Set UWR Header: " + key + " = " + val);
#endif
                    }
                }

                uwr.SendWebRequest();

                while (!uwr.isDone)
                {
                    yield return null;
                }

                if (_timeOut > 0f) CancelInvoke("_Clear");

#if UNITY_2020_1_OR_NEWER
                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError || uwr.result == UnityWebRequest.Result.DataProcessingError)
#else
                if (uwr.isNetworkError || uwr.isHttpError)
#endif
                {
                    Debug.Log("(WebRequestHandlerPlus) Error, API: " + apiUrl + ", Error: " + uwr.error);
                    _onComplete(false, "");
                    _Clear();
                }
                else if (uwr.isDone)
                {
                    _onComplete(true, uwr.downloadHandler.text);
                    _Clear();
                }
                else
                {
                    Debug.Log("(WebRequestHandlerPlus) Error, API: " + apiUrl);
                    _onComplete(false, "");
                    _Clear();
                }
            }
        }

        private void _Clear()
        {
            if (gameObject)
            {
                StopAllCoroutines();
                Destroy(gameObject);
            }
        }
    }
}
