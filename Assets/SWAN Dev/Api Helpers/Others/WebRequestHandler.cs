// Created by SWAN DEV 2017
using System.Collections;
using UnityEngine;
using System;

#if UNITY_2017_3_OR_NEWER
using UnityEngine.Networking;
#endif

namespace SDev.Util
{
    public class WebRequestHandler : MonoBehaviour
    {
        private float _timeOut = 10f;
        private Action<bool, string> _onComplete;

        public static WebRequestHandler Create(string name = "")
        {
            return new GameObject("WebRequestHandler-" + name).AddComponent<WebRequestHandler>();
        }

        public void Request(string apiUrl, Action<bool, string> onComplete, float timeOut = 10f)
        {
            _timeOut = timeOut;
            StartCoroutine(_CallApi(apiUrl, onComplete));
        }

        private IEnumerator _CallApi(string apiUrl, Action<bool, string> onComplete)
        {
            if (_timeOut > 0f) Invoke("_Clear", _timeOut);
            _onComplete = onComplete;

#if UNITY_2017_3_OR_NEWER

            using (UnityWebRequest uwr = UnityWebRequest.Get(apiUrl))
            {
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
                    Debug.Log("(WebRequestHandler) Error during call API: " + apiUrl + ", Error: " + uwr.error);
                    _onComplete(false, "");
                    _Clear();
                }
                else if (uwr.isDone)
                {
                    onComplete(true, uwr.downloadHandler.text);
                    _Clear();
                }
                else
                {
                    Debug.Log("(WebRequestHandler) Error during call API: " + apiUrl);
                    _onComplete(false, "");
                    _Clear();
                }

            }

#else

            WWW www = new WWW(apiUrl);
            yield return www;

            if (_timeOut > 0f) CancelInvoke("_Clear");

            if (www.error == null)
            {
                _onComplete(true, www.text);
                www.Dispose();
                www = null;
                _Clear();
            }
            else
            {
                Debug.Log("(WebRequestHandler, WWW) Error during call API: " + apiUrl + ", Error: " + www.error);
                _onComplete(false, "");
                www.Dispose();
                www = null;
                _Clear();
            }

#endif
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
