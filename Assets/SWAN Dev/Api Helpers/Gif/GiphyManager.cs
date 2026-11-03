// Created by SwanDEV 2017

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

#if UNITY_2017_3_OR_NEWER
using UnityEngine.Networking;
#endif

#if !USE_BUILD_IN_JSON
using Newtonsoft.Json;
#endif

namespace SDev.GiphyAPI
{
    /// <summary>
    /// Giphy API Helper/Manager.
    /// Developers, Docs, Apply your API key: https://developers.giphy.com
    /// </summary>
    public class GiphyManager : MonoBehaviour
    {
        public enum Rating
        {
            None = 0,
            Y,
            G,
            PG,
            PG_13,
            R
        }

        //lang - specify default country for regional content; format is 2-letter ISO 639-1 language code
        public enum Language
        {
            None = 0,
            en, //English
            es, //Spanish
            pt, //Portuguese
            id, //Indonesian
            fr, //French
            ar, //Arabic
            tr, //Turkish
            th, //Thai
            vi, //Vietnamese
            de, //German
            it, //Italian
            ja, //Japanese
            ru, //Russian
            ko, //Korean
            pl, //Polish
            nl, //Dutch
            ro, //Romanian
            hu, //Hungarian
            sv, //Swedish
            cs, //Czech
            hi, //Hindi
            bn, //Bengali
            da, //Danish
            fa, //Farsi
            tl, //Filipino
            fi, //Finnish
            iw, //Hebrew
            ms, //Malay
            no, //Norwegian
            uk, //Ukrainian
            CN, //Chinese Simplified
            TW, //Chinese Traditional
        }

        #region ------- GIF API Variables  -------

        //----- Apply your gif channel & api keys here: https://developers.giphy.com/ -----//
        /// <summary>
        /// Your user name on Giphy.
        /// </summary>
        [Header("[ Giphy Channel ]")]
        [Tooltip("Your user name on Giphy.")]
        public string m_GiphyUserName = "";

        /// <summary>
        /// Your API key associated with your app on Giphy (for normal Gif API and Sticker API)
        /// </summary>
        [Tooltip("Your API key associated with your app on Giphy (for normal Gif API and Sticker API)")]
        public string m_GiphyApiKey = "";

        /// <summary>
        /// Your upload API key associated with your app on Giphy (for Upload API, you can request a production key from Giphy if need)
        /// </summary>
        [Tooltip("Your upload API key associated with your app on Giphy (for Upload API, you can request a production key from Giphy if need)")]
        public string m_GiphyUploadApiKey = "";


        [Header("[ Giphy API URL ]")]
        public string m_ApiBaseUrl = "https://api.giphy.com/v1/";
        public string m_UploadApiUrl = "https://upload.giphy.com/v1/gifs";


        /// <summary>
        /// Number of results to return, maximum 100. Default: 25
        /// </summary>
        [Header("[ Optional-Settings ]")]
        [Tooltip("Number of results to return, maximum 100. Default: 25")]
        public int m_ResultLimit = 10;

        /// <summary>
        /// Results offset, default: 0
        /// </summary>
        [Tooltip("Results offset, default: 0")]
        public int m_ResultOffset = 0;

        /// <summary>
        /// Limit results to those rated GIFs (y,g, pg, pg-13 or r)
        /// </summary>
        [Tooltip("Limit results to those rated GIFs (y,g, pg, pg-13 or r)")]
        public Rating m_Rating = Rating.None;

        /// <summary>
        /// Language use with GIFs Search API and Stickers Search API, specify default country for regional content. Default: en
        /// </summary>
        [Tooltip("Language use with GIFs Search API and Stickers Search API, specify default country for regional content. Default: en")]
        public Language m_Language = Language.None;

        /// <summary>
        /// An ID/proxy for a specific user. This is a unique user ID provides by Giphy. You can call the GenerateRandomIdForUser method to get the ID for each user.
        /// </summary>
        [Tooltip("An ID/proxy for a specific user. This is a unique user ID provides by Giphy. You can call the GenerateRandomIdForUser method to get the ID for each user.")]
        public string m_RandomId;

        /// <summary>
        /// An url attach to the GIF Info>SOURCE field, for the uploaded GIF (e.g. Your website/The page on which this GIF was found)
        /// </summary>
        [Header("[ Optional-Promotion ]")]
        [Tooltip("An url attach to the GIF Info>SOURCE field, for the uploaded GIF (e.g. Your website/The page on which this GIF was found)")]
        public string m_Source_Post_Url = "";

        #endregion


        private string _FullJsonResponseText = "Full Json Response Text";
        public string FullJsonResponseText
        {
            get
            {
                return _FullJsonResponseText;
            }
            set
            {
                _FullJsonResponseText = value;

                //string json = value.Replace("\\", "");
                //Debug.Log(json);
            }
        }

        private static GiphyManager _instance = null;
        public static GiphyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("[GiphyManager]").AddComponent<GiphyManager>();
                }
                return _instance;
            }
        }

        public void CheckAppendRandomId(ref string url)
        {
            if (!string.IsNullOrEmpty(m_RandomId)) url += "&random_id=" + m_RandomId;
        }

        public void SetChannelAuthentication(string userName, string apiKey = "", string uploadApiKey = "")
        {
            m_GiphyUserName = userName;
            m_GiphyApiKey = apiKey;
            m_GiphyUploadApiKey = uploadApiKey;
        }

        private bool HasUserName
        {
            get
            {
                bool hasUserName = !string.IsNullOrEmpty(m_GiphyUserName);
                if (!hasUserName)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("Giphy User Name is required if you use a production api key.");
#endif
                }
                return true;
            }
        }

        private bool HasApiKey
        {
            get
            {
                bool hasApiKey = !string.IsNullOrEmpty(m_GiphyApiKey);
                if (!hasApiKey)
                {
                    Debug.LogWarning("Giphy API Key is required!");
                }

                return hasApiKey;
            }
        }

        private bool HasUploadApiKey
        {
            get
            {
                bool hasApiKey = !string.IsNullOrEmpty(m_GiphyUploadApiKey);
                if (!hasApiKey)
                {
                    Debug.LogWarning("Giphy Upload API Key is required!");
                }

                return hasApiKey;
            }
        }

        void Start()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

        void Update()
        {
#if UNITY_2017_3_OR_NEWER
            if (uwrUpload != null)
            {
                if (_onUploadProgress != null)
                {
                    _onUploadProgress(uwrUpload.uploadProgress);
                }
            }
#else
        if (wwwUpload != null)
        {
            if (_onUploadProgress != null)
            {
                _onUploadProgress(wwwUpload.uploadProgress);
            }
        }
#endif
        }

        #region ------- Upload GIF API -------
#if UNITY_2017_3_OR_NEWER
        private UnityWebRequest uwrUpload;
#else
    private WWW wwwUpload;
#endif
        private Action<float> _onUploadProgress;

        /// <summary>
        /// Upload a specified GIF with its filePath to Giphy.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="onComplete">On complete.</param>
        /// <param name="onProgress">On progress.</param>
        public void Upload(string filePath, Action<GiphyUpload> onComplete, Action<float> onProgress = null, Action onFail = null)
        {
            if (!HasUploadApiKey || !HasUserName) return;
            StartCoroutine(_Upload(filePath, null, onComplete, onProgress, onFail));
        }

        /// <summary>
        /// Upload a specified GIF with its filePath to Giphy. 
        /// And add tag(s) to the GIF that allows it to be searched by browsing/searching.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="tags">Tags.</param>
        /// <param name="onComplete">On complete.</param>
        /// <param name="onProgress">On progress.</param>
        public void Upload(string filePath, List<string> tags, Action<GiphyUpload> onComplete, Action<float> onProgress = null, Action onFail = null)
        {
            if (!HasUploadApiKey || !HasUserName) return;
            StartCoroutine(_Upload(filePath, tags, onComplete, onProgress, onFail));
        }

        private IEnumerator _Upload(string filePath, List<string> tags, Action<GiphyUpload> onComplete, Action<float> onProgress = null, Action onFail = null)
        {
            //string url = m_UploadApi;
            string url = m_UploadApiUrl + "?api_key=" + m_GiphyUploadApiKey;// + (string.IsNullOrEmpty(m_GiphyUserName)? "":"&username=" + m_GiphyUserName);

            _onUploadProgress = onProgress;

            string tagsStr = "";
            if (tags != null && tags.Count > 0)
            {
                foreach (string hTag in tags)
                {
                    if (!string.IsNullOrEmpty(hTag)) tagsStr += hTag + ",";
                }
                if (!string.IsNullOrEmpty(tagsStr)) tagsStr = tagsStr.Substring(0, tagsStr.Length - 1);
            }

            byte[] bytes = File.ReadAllBytes(filePath);

            WWWForm postForm = new WWWForm();
            postForm.AddBinaryData("file", bytes);
            postForm.AddField("api_key", m_GiphyUploadApiKey);
            postForm.AddField("username", m_GiphyUserName);
            if (!string.IsNullOrEmpty(tagsStr)) postForm.AddField("tags", tagsStr);
            if (!string.IsNullOrEmpty(m_Source_Post_Url)) postForm.AddField("source_post_url", m_Source_Post_Url);

#if UNITY_EDITOR
            Debug.Log("UserName: " + m_GiphyUserName + " | Upload Api Key: " + m_GiphyUploadApiKey +
                " | Api Key: " + m_GiphyApiKey + " | tags: " + tagsStr + " | m_Source_Post_Url: " + m_Source_Post_Url);
#endif

#if UNITY_2017_3_OR_NEWER

            uwrUpload = UnityWebRequest.Post(url, postForm);
            yield return uwrUpload.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
            if (uwrUpload.result == UnityWebRequest.Result.ConnectionError || uwrUpload.result == UnityWebRequest.Result.ProtocolError || uwrUpload.result == UnityWebRequest.Result.DataProcessingError)
#else
            if (uwrUpload.isNetworkError || uwrUpload.isHttpError)
#endif
            {
                if (onFail != null) onFail();
                Debug.LogWarning("Error during upload: " + uwrUpload.error + "\nResult: " + uwrUpload.downloadHandler.text);
            }
            else if (uwrUpload.isDone)
            {
                FullJsonResponseText = uwrUpload.downloadHandler.text;
#if USE_BUILD_IN_JSON
            GiphyUpload uploadResponse = JsonUtility.FromJson<GiphyUpload>(w.downloadHandler.text);
#else
                GiphyUpload uploadResponse = JsonConvert.DeserializeObject<GiphyUpload>(uwrUpload.downloadHandler.text);
#endif
                onComplete(uploadResponse);
            }
            else
            {
                if (onFail != null) onFail();
                Debug.LogWarning("Fail to upload!");
            }

            if (uwrUpload != null)
            {
                uwrUpload.Dispose();
                uwrUpload = null;
            }

#else

        wwwUpload = new WWW(url, postForm);
        wwwUpload.threadPriority = ThreadPriority.High;

        yield return wwwUpload;
        if (!string.IsNullOrEmpty(wwwUpload.error))
        {
            if (onFail != null) onFail();
            Debug.Log("Error during upload: " + wwwUpload.error + "\n" + wwwUpload.text);
        }
        else
        {
            FullJsonResponseText = wwwUpload.text;
#if USE_BUILD_IN_JSON
            GiphyUpload uploadResponse = JsonUtility.FromJson<GiphyUpload>(wwwUpload.text);
#else
            GiphyUpload uploadResponse = JsonConvert.DeserializeObject<GiphyUpload>(wwwUpload.text);
#endif
            onComplete(uploadResponse);
        }

        if (wwwUpload != null)
        {
            wwwUpload.Dispose();
            wwwUpload = null;
        }

#endif

        }
        #endregion

        #region ------- Other API --------
        /// <summary>
        /// GIPHY Random ID Endpoint allows GIPHY to generate a unique ID you can assign to each new user in your app.
        /// (** To get the most out of Random ID, we recommend sending the random_id param with all compatible endpoints.
        /// This lets us adjust the API response to your users’ preferences and improve their GIF experience while maintaining their privacy.)
        /// </summary>
        public void GenerateRandomIdForUser(Action<GiphyUserRandomId> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;
            string url = m_ApiBaseUrl + "randomid?api_key=" + m_GiphyApiKey;

            StartCoroutine(_LoadRoutine(url, (text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
#if USE_BUILD_IN_JSON
                GiphyUserRandomId response = JsonUtility.FromJson<GiphyUserRandomId>(text);
#else
                    GiphyUserRandomId response = JsonConvert.DeserializeObject<GiphyUserRandomId>(text);
#endif
                    if (onComplete != null) onComplete(response);
                }
                else
                {
                    if (onFail != null) onFail();
                }
            }));
        }

        /// <summary>
        /// Providers users a list of Gif categories on the GIPHY network.
        /// </summary>
        public void GetGifCategories(Action<GiphyGifCategories> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;
            string url = m_ApiBaseUrl + "gifs/categories?api_key=" + m_GiphyApiKey;

            StartCoroutine(_LoadRoutine(url, (text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
                    //File.WriteAllText(Path.Combine(Application.dataPath, "!!GiphyGifCates.txt"), text);
#if USE_BUILD_IN_JSON
                GiphyGifCategories response = JsonUtility.FromJson<GiphyGifCategories>(text);
#else
                    GiphyGifCategories response = JsonConvert.DeserializeObject<GiphyGifCategories>(text);
#endif
                    if (onComplete != null) onComplete(response);
                }
                else
                {
                    if (onFail != null) onFail();
                }
            }));
        }

        /// <summary>
        /// Providers users a list of valid terms that completes the given tag on the GIPHY network.
        /// </summary>
        /// <param name="tag">The incomplete tag term (by the user).</param>
        /// <param name="limit">The maximum number of objects to return. (Default: 5)</param>
        /// <param name="offset">Specifies the starting position of the results. (Default: 0)</param>
        public void GetAutocompleteTerms(string tag, int limit, int offset, Action<GiphyAutocomplete> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;
            string url = m_ApiBaseUrl + "gifs/search/tags?api_key=" + m_GiphyApiKey + "&q=" + tag;
            if (limit > 0) url += "&limit=" + limit;
            if (offset > 0) url += "&offset=" + offset;

            StartCoroutine(_LoadRoutine(url, (text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
                    Debug.Log("GetAutocompleteTerms :\n" + text);
#if USE_BUILD_IN_JSON
                GiphyAutocomplete response = JsonUtility.FromJson<GiphyAutocomplete>(text);
#else
                    GiphyAutocomplete response = JsonConvert.DeserializeObject<GiphyAutocomplete>(text);
#endif
                    if (onComplete != null) onComplete(response);
                }
                else
                {
                    if (onFail != null) onFail();
                }
            }));
        }

        /// <summary>
        /// Channel Search endpoint returns all the GIPHY channels matching the query term.
        /// </summary>
        /// <param name="keyword">Accepts term to search through GIPHY’s channels.</param>
        /// <param name="limit">The maximum number of objects to return. (Default: 25)</param>
        /// <param name="offset">Specifies the starting position of the results. (Default: 0)</param>
        public void SearchChannels(string keyword, int limit, int offset, Action<GiphySearchChannels> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;
            string url = m_ApiBaseUrl + "channels/search?api_key=" + m_GiphyApiKey + "&q=" + keyword;
            if (limit > 0) url += "&limit=" + limit;
            if (offset > 0) url += "&offset=" + offset;

            StartCoroutine(_LoadRoutine(url, (text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
                    File.WriteAllText(Path.Combine(Application.dataPath, "!!GiphySearchChannels.txt"), text);
#if USE_BUILD_IN_JSON
                GiphySearchChannels response = JsonUtility.FromJson<GiphySearchChannels>(text);
#else
                    GiphySearchChannels response = JsonConvert.DeserializeObject<GiphySearchChannels>(text);
#endif
                    if (onComplete != null) onComplete(response);
                }
                else
                {
                    if (onFail != null) onFail();
                }
            }));
        }

        /// <summary>
        /// Providers users a list of tag terms related to the given tag on the GIPHY network.
        /// </summary>
        /// <param name="tag">Tag term.</param>
        public void SearchSuggestions(string tag, Action<GiphySearchSuggestion> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;
            string url = m_ApiBaseUrl + "tags/related/" + tag + "?api_key=" + m_GiphyApiKey;

            StartCoroutine(_LoadRoutine(url, (text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
                    Debug.Log("SearchSuggestions :\n" + text);
#if USE_BUILD_IN_JSON
                GiphySearchSuggestion response = JsonUtility.FromJson<GiphySearchSuggestion>(text);
#else
                    GiphySearchSuggestion response = JsonConvert.DeserializeObject<GiphySearchSuggestion>(text);
#endif
                    if (onComplete != null) onComplete(response);
                }
                else
                {
                    if (onFail != null) onFail();
                }
            }));
        }

        /// <summary>
        /// Provides users a list of the most popular trending search terms on the GIPHY network.
        /// </summary>
        public void GetTrendingSearchTerms(Action<GiphyTrendingSearchTerms> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;
            string url = m_ApiBaseUrl + "trending/searches?api_key=" + m_GiphyApiKey;

            StartCoroutine(_LoadRoutine(url, (text) =>
            {
                Debug.Log("GetTrendingSearchTerms :\n" + text);
                if (!string.IsNullOrEmpty(text))
                {
#if USE_BUILD_IN_JSON
                GiphyTrendingSearchTerms response = JsonUtility.FromJson<GiphyTrendingSearchTerms>(text);
#else
                    GiphyTrendingSearchTerms response = JsonConvert.DeserializeObject<GiphyTrendingSearchTerms>(text);
#endif
                    if (onComplete != null) onComplete(response);
                }
                else
                {
                    if (onFail != null) onFail();
                }
            }));
        }
        #endregion

        #region ------- Normal GIF API --------
        /// <summary>
        /// Returns a GIF given that GIF's unique ID
        /// </summary>
        /// <param name="giphyGifId">Giphy GIF identifier.</param>
        public void GetById(string giphyGifId, Action<GiphyGetById> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;

            if (!string.IsNullOrEmpty(giphyGifId))
            {
                string url = m_ApiBaseUrl + "gifs/" + giphyGifId + "?api_key=" + m_GiphyApiKey;
                CheckAppendRandomId(ref url);

                StartCoroutine(_LoadRoutine(url, (text) =>
                {
                    if (!string.IsNullOrEmpty(text))
                    {
#if USE_BUILD_IN_JSON
                    GiphyGetById response = JsonUtility.FromJson<GiphyGetById>(text);
#else
                        GiphyGetById response = JsonConvert.DeserializeObject<GiphyGetById>(text);
#endif
                        if (onComplete != null) onComplete(response);
                    }
                    else
                    {
                        if (onFail != null) onFail();
                    }
                }));
            }
            else
            {
                Debug.LogWarning("GIF id is empty!");
            }
        }

        /// <summary>
        /// Returns an array of GIFs given that GIFs' unique IDs
        /// </summary>
        /// <param name="giphyGifIds">Giphy GIF identifiers.</param>
        public void GetByIds(List<string> giphyGifIds, Action<GiphyGetByIds> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;

            string giphyGifIdsStr = "";
            foreach (string id in giphyGifIds)
            {
                if (!string.IsNullOrEmpty(id)) giphyGifIdsStr += id + ",";
            }

            if (!string.IsNullOrEmpty(giphyGifIdsStr))
            {
                giphyGifIdsStr = giphyGifIdsStr.Substring(0, giphyGifIdsStr.Length - 1);

                string url = m_ApiBaseUrl + "gifs?ids=" + giphyGifIdsStr + "&api_key=" + m_GiphyApiKey;
                CheckAppendRandomId(ref url);

                StartCoroutine(_LoadRoutine(url, (text) =>
                {
                    if (!string.IsNullOrEmpty(text))
                    {
#if USE_BUILD_IN_JSON
                    GiphyGetByIds response = JsonUtility.FromJson<GiphyGetByIds>(text);
#else
                        GiphyGetByIds response = JsonConvert.DeserializeObject<GiphyGetByIds>(text);
#endif
                        if (onComplete != null) onComplete(response);
                    }
                    else
                    {
                        if (onFail != null) onFail();
                    }
                }));
            }
            else
            {
                Debug.LogWarning("GIF ids is empty!");
            }
        }

        /// <summary>
        /// Search all GIPHY GIFs for a word or phrase. Punctuation will be stripped and ignored.
        /// </summary>
        /// <param name="keyWords">Key words.</param>
        public void Search(List<string> keyWords, Action<GiphySearch> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;

            string keyWordsStr = "";
            foreach (string k in keyWords)
            {
                keyWordsStr += k + "+";
            }
            keyWordsStr = keyWordsStr.Substring(0, keyWordsStr.Length - 1);

            string url = m_ApiBaseUrl + "gifs/search?q=" + keyWordsStr + "&api_key=" + m_GiphyApiKey;
            if (m_ResultLimit > 0) url += "&limit=" + m_ResultLimit;
            if (m_ResultOffset > 0) url += "&offset=" + m_ResultOffset;
            if (m_Rating != Rating.None) url += "&rating=" + m_Rating.ToString().ToUpper();
            if (m_Language != Language.None) url += "&lang=" + _GetLanguageString(m_Language);
            CheckAppendRandomId(ref url);

            StartCoroutine(_LoadRoutine(url, (text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
#if USE_BUILD_IN_JSON
                GiphySearch response = JsonUtility.FromJson<GiphySearch>(text);
#else
                    GiphySearch response = JsonConvert.DeserializeObject<GiphySearch>(text);
#endif
                    if (onComplete != null) onComplete(response);
                }
                else
                {
                    if (onFail != null) onFail();
                }
            }));
        }

        /// <summary>
        /// Get a random GIF.
        /// </summary>
        public void Random(Action<GiphyRandom> onComplete, Action onFail = null)
        {
            _Random(null, onComplete, onFail);
        }

        /// <summary>
        /// Get a random GIF related to the word or phrase entered : tag.
        /// </summary>
        /// <param name="hTag">Tag: the GIF tag to limit randomness by.</param>
        public void Random(string hTag, Action<GiphyRandom> onComplete, Action onFail = null)
        {
            _Random(hTag, onComplete, onFail);
        }

        private void _Random(string hTag, Action<GiphyRandom> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;

            string url = m_ApiBaseUrl + "gifs/random?api_key=" + m_GiphyApiKey;
            if (!string.IsNullOrEmpty(hTag)) url += "&tag=" + hTag;
            if (m_Rating != Rating.None) url += "&rating=" + m_Rating.ToString().ToUpper();
            CheckAppendRandomId(ref url);

            StartCoroutine(_LoadRoutine(url, (text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
#if USE_BUILD_IN_JSON
                GiphyRandom response = JsonUtility.FromJson<GiphyRandom>(text);
#else
                    GiphyRandom response = JsonConvert.DeserializeObject<GiphyRandom>(text);
#endif
                    if (onComplete != null) onComplete(response);
                }
                else
                {
                    if (onFail != null) onFail();
                }
            }));
        }

        /// <summary>
        /// The translate API draws on search, but uses the GIPHY special sauce to handle translating from one vocabulary to another. 
        /// In this case, words and phrases to GIFs. The result is Random even for the same term.
        /// </summary>
        /// <param name="term">Search term.</param>
        /// <param name="weirdness">Value from 0-10 which makes results weirder as you go up the scale.</param>
        public void Translate(string term, int weirdness, Action<GiphyTranslate> onComplete, Action onFail = null)
        {
            if (!string.IsNullOrEmpty(term))
            {
                string url = m_ApiBaseUrl + "gifs/translate?api_key=" + m_GiphyApiKey + "&s=" + term;
                if (weirdness >= 0) url += "&weirdness=" + weirdness;
                CheckAppendRandomId(ref url);

                StartCoroutine(_LoadRoutine(url, (text) =>
                {
                    if (!string.IsNullOrEmpty(text))
                    {
#if USE_BUILD_IN_JSON
                    GiphyTranslate response = JsonUtility.FromJson<GiphyTranslate>(text);
#else
                        GiphyTranslate response = JsonConvert.DeserializeObject<GiphyTranslate>(text);
#endif
                        if (onComplete != null) onComplete(response);
                    }
                    else
                    {
                        if (onFail != null) onFail();
                    }
                }));
            }
            else
            {
                Debug.LogWarning("Search term is empty!");
            }
        }

        /// <summary>
        /// Fetch GIFs currently trending online. Hand curated by the GIPHY editorial team. 
        /// The data returned mirrors the GIFs showcased on the GIPHY homepage.
        /// </summary>
        public void Trending(Action<GiphyTrending> onComplete, Action onFail = null)
        {
            string url = m_ApiBaseUrl + "gifs/trending?api_key=" + m_GiphyApiKey;
            if (m_ResultLimit > 0) url += "&limit=" + m_ResultLimit;
            if (m_ResultOffset > 0) url += "&offset=" + m_ResultOffset;
            if (m_Rating != Rating.None) url += "&rating=" + m_Rating.ToString().ToUpper();
            CheckAppendRandomId(ref url);

            StartCoroutine(_LoadRoutine(url, (text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
#if USE_BUILD_IN_JSON
                GiphyTrending response = JsonUtility.FromJson<GiphyTrending>(text);
#else
                    GiphyTrending response = JsonConvert.DeserializeObject<GiphyTrending>(text);
#endif
                    if (onComplete != null) onComplete(response);
                }
                else
                {
                    if (onFail != null) onFail();
                }
            }));
        }
        #endregion


        #region -------- Sticker GIF API --------
        private string _GetLanguageString(Language lang)
        {
            string langStr = "";
            switch (lang)
            {
                case Language.None:
                    //Do nothing
                    break;

                case Language.CN:
                    langStr = "zh-CN";
                    break;

                case Language.TW:
                    langStr = "zh-TW";
                    break;

                default:
                    langStr = lang.ToString().ToLower();
                    break;
            }
            return langStr;
        }

        /// <summary>
        /// GIPHY Search gives you instant access to their library of millions of GIFs and Stickers by entering a word or phrase. 
        /// </summary>
        /// <param name="keyWords">Search term(s).</param>
        public void Search_Sticker(List<string> keyWords, Action<GiphyStickerSearch> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;

            string keyWordsStr = "";
            foreach (string k in keyWords)
            {
                keyWordsStr += k + "+";
            }
            keyWordsStr = keyWordsStr.Substring(0, keyWordsStr.Length - 1);

            string url = m_ApiBaseUrl + "stickers/search?q=" + keyWordsStr + "&api_key=" + m_GiphyApiKey;
            if (m_ResultLimit > 0) url += "&limit=" + m_ResultLimit;
            if (m_ResultOffset > 0) url += "&offset=" + m_ResultOffset;
            if (m_Rating != Rating.None) url += "&rating=" + m_Rating.ToString().ToUpper();
            if (m_Language != Language.None) url += "&lang=" + _GetLanguageString(m_Language);
            CheckAppendRandomId(ref url);

            StartCoroutine(_LoadRoutine(url, (text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
#if USE_BUILD_IN_JSON
                GiphyStickerSearch searchResponse = JsonUtility.FromJson<GiphyStickerSearch>(text);
#else
                    GiphyStickerSearch searchResponse = JsonConvert.DeserializeObject<GiphyStickerSearch>(text);
#endif
                    if (onComplete != null) onComplete(searchResponse);
                }
                else
                {
                    if (onFail != null) onFail();
                }
            }));
        }

        /// <summary>
        /// Get a random Sticker.
        /// </summary>
        public void Random_Sticker(Action<GiphyStickerRandom> onComplete, Action onFail = null)
        {
            _Random_Sticker(null, onComplete, onFail);
        }

        /// <summary>
        /// Get a random Sticker related to the word or phrase entered : tag.
        /// </summary>
        /// <param name="hTag">Tag: the GIF tag to limit randomness by.</param>
        public void Random_Sticker(string hTag, Action<GiphyStickerRandom> onComplete, Action onFail = null)
        {
            _Random_Sticker(hTag, onComplete, onFail);
        }

        private void _Random_Sticker(string hTag, Action<GiphyStickerRandom> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;

            string url = m_ApiBaseUrl + "stickers/random?api_key=" + m_GiphyApiKey;
            if (!string.IsNullOrEmpty(hTag)) url += "&tag=" + hTag;
            if (m_Rating != Rating.None) url += "&rating=" + m_Rating.ToString().ToUpper();
            CheckAppendRandomId(ref url);

            StartCoroutine(_LoadRoutine(url, (text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
#if USE_BUILD_IN_JSON
                GiphyStickerRandom searchResponse = JsonUtility.FromJson<GiphyStickerRandom>(text);
#else
                    GiphyStickerRandom searchResponse = JsonConvert.DeserializeObject<GiphyStickerRandom>(text);
#endif
                    if (onComplete != null) onComplete(searchResponse);
                }
                else
                {
                    if (onFail != null) onFail();
                }
            }));
        }

        /// <summary>
        /// GIPHY Translate converts words and phrases to the perfect Sticker using GIPHY's special sauce algorithm.
        /// </summary>
        /// <param name="term">Search term.</param>
        /// <param name="weirdness">Value from 0-10 which makes results weirder as you go up the scale.</param>
        public void Translate_Sticker(string term, int weirdness, Action<GiphyStickerTranslate> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;

            if (!string.IsNullOrEmpty(term))
            {
                string url = m_ApiBaseUrl + "stickers/translate?api_key=" + m_GiphyApiKey + "&s=" + term;
                if (weirdness >= 0) url += "&weirdness=" + weirdness;
                CheckAppendRandomId(ref url);

                StartCoroutine(_LoadRoutine(url, (text) =>
                {
                    if (!string.IsNullOrEmpty(text))
                    {
#if USE_BUILD_IN_JSON
                    GiphyStickerTranslate searchResponse = JsonUtility.FromJson<GiphyStickerTranslate>(text);
#else
                        GiphyStickerTranslate searchResponse = JsonConvert.DeserializeObject<GiphyStickerTranslate>(text);
#endif
                        if (onComplete != null) onComplete(searchResponse);
                    }
                    else
                    {
                        if (onFail != null) onFail();
                    }
                }));
            }
            else
            {
                Debug.LogWarning("Search term is empty!");
            }
        }

        /// <summary>
        /// GIPHY Trending returns a list of the most relevant and engaging content each and every day. 
        /// </summary>
        public void Trending_Sticker(Action<GiphyStickerTrending> onComplete, Action onFail = null)
        {
            if (!HasApiKey || !HasUserName) return;

            string url = m_ApiBaseUrl + "stickers/trending?api_key=" + m_GiphyApiKey;
            if (m_ResultLimit > 0) url += "&limit=" + m_ResultLimit;
            if (m_ResultOffset > 0) url += "&offset=" + m_ResultOffset;
            if (m_Rating != Rating.None) url += "&rating=" + m_Rating.ToString().ToUpper();
            CheckAppendRandomId(ref url);

            StartCoroutine(_LoadRoutine(url, (text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
#if USE_BUILD_IN_JSON
                GiphyStickerTrending searchResponse = JsonUtility.FromJson<GiphyStickerTrending>(text);
#else
                    GiphyStickerTrending searchResponse = JsonConvert.DeserializeObject<GiphyStickerTrending>(text);
#endif
                    if (onComplete != null) onComplete(searchResponse);
                }
                else
                {
                    if (onFail != null) onFail();
                }
            }));
        }
        #endregion


        #region -------- Load Routine --------
        private IEnumerator _LoadRoutine(string url, Action<string> onReceive)
        {
#if UNITY_2017_3_OR_NEWER

            using (UnityWebRequest uwr = UnityWebRequest.Get(url))
            {
                uwr.SendWebRequest();

                while (!uwr.isDone)
                {
                    yield return null;
                }

#if UNITY_2020_1_OR_NEWER
                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError || uwr.result == UnityWebRequest.Result.DataProcessingError)
#else
                if (uwr.isNetworkError || uwr.isHttpError)
#endif
                {
                    onReceive("");
                    Debug.LogWarning("Error during UnityWebRequest loading: " + uwr.error);
                }
                else if (uwr.isDone)
                {
                    FullJsonResponseText = uwr.downloadHandler.text;
                    onReceive(uwr.downloadHandler.text);
                }
                else
                {
                    Debug.LogWarning("Error during UnityWebRequest loading.");
                    onReceive("");
                }
            }

#else

        WWW www = new WWW(url);
        yield return www;
        if (www.error == null)
        {
            FullJsonResponseText = www.text;
            onReceive(www.text);
        }
        else
        {
            onReceive("");
            Debug.LogWarning("Error during WWW loading: " + www.error);
        }
        www.Dispose();
        www = null;

#endif
        }
        #endregion
    }
}
