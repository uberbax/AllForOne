// Created by SWAN DEV 2017

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using SDev.GiphyAPI;

public class GiphyDemo : MonoBehaviour
{
	public Dropdown m_Dropdown;
	public InputField m_InputField;
	public InputField m_InputField_JsonText;
	public InputField m_InputFieldKeyWord;

    private string _shareTitle = "This is a Title";
	private string _shareText = "This is a Message";
	private string _shareGifId = "";			//The Giphy unique gif id

	//Reminds: some social platforms support short link preview, while some support preview of full url with .gif extension
	private string _shareGifBitlyUrl = "";		//The bitly gif url
	private string _shareGifFullUrl = "";		//Url with .gif extension
	public void OnButtonShare()
	{
		Debug.Log("OnButtonShare - BitlyUrl: " + _shareGifBitlyUrl + " | Id: " + _shareGifId + " | FullUrl: " + _shareGifFullUrl);
		OnButtonFB();
		OnButtonTwitter_Mobile();
		OnButtonTumblr();
		OnButtonSkype();
	}

	public void OnButtonFB()
	{
		Debug.Log("OnButtonFB: " + _shareGifBitlyUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.Facebook, _shareTitle, _shareText, _shareGifBitlyUrl, _shareGifBitlyUrl);
	}

	public void OnButtonTwitter()
	{
		Debug.Log("OnButtonTwitter: " + _shareGifId);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.Twitter, "hashtag", _shareText, _shareGifId, "https://www.swanob2.com");
	}

	public void OnButtonTwitter_Mobile()
	{
		Debug.Log("OnButtonTwitter_Mobile: " + _shareGifId);
		GifSocialShare gifShare = new GifSocialShare();
		//Short link(BitlyUrl) is better supported by Twitter
		gifShare.ShareTo(GifSocialShare.Social.Twitter_Mobile, "hashtag", _shareText, "https://www.swanob2.com", _shareGifBitlyUrl);
	}

	public void OnButtonTumblr()
	{
		Debug.Log("OnButtonTumblr: " + _shareGifFullUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.Tumblr, _shareTitle, _shareText, _shareGifFullUrl, _shareGifFullUrl);
	}

	public void OnButtonVK()
	{
		Debug.Log("OnButtonVK: " + _shareGifBitlyUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.VK, _shareTitle, _shareText, _shareGifBitlyUrl, _shareGifBitlyUrl);
	}

	public void OnButtonPinterest()
	{
		Debug.Log("OnButtonPinterest: " + _shareGifFullUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.Pinterest, _shareTitle, _shareText, _shareGifFullUrl, _shareGifFullUrl);
	}

	public void OnButtonLinkedIn()
	{
		Debug.Log("OnButtonPLinkedIn: " + _shareGifFullUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.LinkedIn, _shareTitle, _shareText, _shareGifFullUrl, _shareGifFullUrl);
	}

	public void OnButtonOKRU()
	{
		Debug.Log("OnButtonOKRU: " + _shareGifBitlyUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.Odnoklassniki, _shareTitle, _shareText, _shareGifBitlyUrl, _shareGifBitlyUrl);
	}

	public void OnButtonReddit()
	{
		Debug.Log("OnButtonReddit: " + _shareGifBitlyUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.Reddit, _shareTitle, _shareText, _shareGifBitlyUrl, _shareGifBitlyUrl);
	}

	public void OnButtonGooglePlus()
	{
		Debug.Log("OnButtonGooglePlus: " + _shareGifBitlyUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.GooglePlus, _shareTitle, _shareText, _shareGifBitlyUrl, _shareGifBitlyUrl);
	}

	public void OnButtonQQ()
	{
		Debug.Log("OnButtonQQ: " + _shareGifFullUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.QQZone, _shareTitle, _shareText, _shareGifFullUrl, _shareGifFullUrl);
	}

	public void OnButtonWeibo()
	{
		Debug.Log("OnButtonWeibo: " + _shareGifBitlyUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.Weibo, _shareTitle, _shareText, _shareGifBitlyUrl, _shareGifBitlyUrl);
	}

	public void OnButtonMySpace()
	{
		Debug.Log("OnButtonMySpace: " + _shareGifFullUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.MySpace, _shareTitle, _shareText, _shareGifFullUrl, _shareGifFullUrl);
	}

	public void OnButtonLineMe()
	{
		Debug.Log("OnButtonLineMe: " + _shareGifBitlyUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.LineMe, _shareTitle, _shareText, _shareGifBitlyUrl, _shareGifBitlyUrl);
	}

	public void OnButtonSkype()
	{
		Debug.Log("OnButtonSkype: " + _shareGifFullUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.Skype, _shareTitle, _shareText, _shareGifFullUrl, _shareGifFullUrl);
	}

	public void OnButtonBaidu()
	{
		Debug.Log("OnButtonBaidu: " + _shareGifBitlyUrl);
		GifSocialShare gifShare = new GifSocialShare();
		gifShare.ShareTo(GifSocialShare.Social.Baidu, _shareTitle, _shareText, _shareGifBitlyUrl, _shareGifBitlyUrl);
	}

	//-------------------------------------------------
	public void OnButtonSend()
	{
		Debug.Log("OnButtonSend: " + m_Dropdown.options[m_Dropdown.value].text);
		_GiphyApi(m_Dropdown.options[m_Dropdown.value].text);
	}

	private string _SetDisplayString(string text)
	{
		//return "Response received!";
		return (text.Length > 10000)? text.Substring(0, 10000):text;
	}

	private void _GiphyApi(string text)
	{
		string tempStr = "";

		m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);


		//------ Other API -------
		if (text == "Random Id (User)")
        {
			Debug.Log("_GiphyApi > Random Id (User)");
			GiphyManager.Instance.GenerateRandomIdForUser((result) => { // on success
				tempStr = "Random Id (User) - Unique user Id provides by Giphy : " + result.data.random_id
					+ "\n\n * Reminder : You should generate once only for each user at the first time they open your app, and save it down for future usage.";
				Debug.Log(tempStr);
				m_InputField.text = tempStr;
				m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
			},
			() => { // on fail

			});
        }

		if (text == "Gif Categories")
		{
			Debug.Log("_GiphyApi > Gif Categories");
			GiphyManager.Instance.GetGifCategories((result) => { // on success
				tempStr = "Gif Categories - Total Categories : " + (result.data != null && result.data.Count > 0 ? result.data.Count.ToString() : "0") + "\n\n";

				int max = Mathf.Min(100, result.data.Count);
				for (int i = 0; i < max; i++)
				{
					tempStr += result.data[i].name + ", ";
					// Reminder : More parameters available in the result.data....
				}

				Debug.Log(tempStr);
                m_InputField.text = tempStr;
                m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
				Debug.Log(GiphyManager.Instance.FullJsonResponseText);
			},
			() => { // on fail

			});
		}

		if (text == "Autocomplete Terms")
		{
			if (string.IsNullOrEmpty(m_InputFieldKeyWord.text))
			{
				Debug.LogWarning("_GiphyApi > InputField/KeyWord is Empty!");
				return;
			}

			Debug.Log("_GiphyApi > Autocomplete Terms");
			GiphyManager.Instance.GetAutocompleteTerms(m_InputFieldKeyWord.text, 5, 0, (result) => { // on success
				tempStr = "Autocomplete Terms - Total possible terms : " + (result.data != null && result.data.Count > 0 ? result.data.Count.ToString() : "0") + "\n\n";

				int max = Mathf.Min(100, result.data.Count);
				for (int i = 0; i < max; i++)
				{
					tempStr += result.data[i].name + ", ";
				}

				Debug.Log(tempStr);
                m_InputField.text = tempStr;
                m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
				Debug.Log(GiphyManager.Instance.FullJsonResponseText);
			},
			() => { // on fail

			});
		}

		if (text == "Search Channels")
		{
			if (string.IsNullOrEmpty(m_InputFieldKeyWord.text))
			{
				Debug.LogWarning("_GiphyApi > InputField/KeyWord is Empty!");
				return;
			}

			Debug.Log("_GiphyApi > SearchChannels");
			GiphyManager.Instance.SearchChannels(m_InputFieldKeyWord.text, 25, 0, (result) => { // on success
				tempStr = "SearchChannels - Total related Channels : " + (result.data != null && result.data.Count > 0 ? result.data.Count.ToString() : "0") + "\n";

				int max = Mathf.Min(100, result.data.Count);
				for (int i = 0; i < max; i++)
				{
                    SDev.GiphyAPI.GiphySearchChannels.Channel giphyChannel = result.data[i];
					tempStr += "\nChannel Id: " + giphyChannel.id + ", Channel Name: " + giphyChannel.display_name;
					// Reminder : More parameters available in the channel object....
				}

				Debug.Log(tempStr);
                m_InputField.text = tempStr;
                m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
				Debug.Log(GiphyManager.Instance.FullJsonResponseText);
			},
			() => { // on fail

			});
		}

		if (text == "Search Suggestions")
		{
			if (string.IsNullOrEmpty(m_InputFieldKeyWord.text))
			{
				Debug.LogWarning("_GiphyApi > InputField/KeyWord is Empty!");
				return;
			}

			Debug.Log("_GiphyApi > Search Suggestions");
			GiphyManager.Instance.SearchSuggestions(m_InputFieldKeyWord.text, (result) => { // on success
				tempStr = "Search Suggestions - Total related terms : " + (result.data != null && result.data.Count > 0 ? result.data.Count.ToString() : "0") + "\n\n";

				int max = Mathf.Min(100, result.data.Count);
				for (int i = 0; i < max; i++)
				{
					tempStr += result.data[i].name + ", ";
				}

				Debug.Log(tempStr);
                m_InputField.text = tempStr;
                m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
				Debug.Log(GiphyManager.Instance.FullJsonResponseText);
			},
			() => { // on fail

			});
		}

		if (text == "Trending Search Terms")
		{
			Debug.Log("_GiphyApi > Trending Search Terms");
			GiphyManager.Instance.GetTrendingSearchTerms((result) =>
			{ // on success
				tempStr = "Trending Search Terms - Total terms : " + (result.data != null && result.data.Count > 0 ? result.data.Count.ToString() : "0") + "\n\n";

				int max = Mathf.Min(100, result.data.Count);
				for (int i = 0; i < max; i++)
                {
					tempStr += result.data[i] + ", ";
                }

				Debug.Log(tempStr);
				m_InputField.text = tempStr;
				m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
				Debug.Log(GiphyManager.Instance.FullJsonResponseText);
			},
			() =>
			{ // on fail

			});
		}
		//------ Other API -------


		//------ GIF API -------
		if (text == "Search")
		{
			if(string.IsNullOrEmpty(m_InputFieldKeyWord.text))
			{
				Debug.LogWarning("_GiphyApi > Search, KeyWord is Empty!");
			}
			else
			{
				Debug.Log("_GiphyApi > Search");
				GiphyManager.Instance.Search(new List<string>{m_InputFieldKeyWord.text}, (result)=>{
                    if (result == null || result.data.Count <= 0)
                    {
                        tempStr = "1. Search - All GIF URLs (KeyWord = " + m_InputFieldKeyWord.text + "): \nNo matched result!";
                        m_InputField_JsonText.text = tempStr;
                    }
                    else
                    {
                        tempStr = "1. Search - All GIF URLs (KeyWord = " + m_InputFieldKeyWord.text + "): ";
                        for (int i = 0; i < result.data.Count; i++)
                        {
                            tempStr += "\n(" + i + ") " + result.data[i].bitly_gif_url;
                        }
                        _shareGifBitlyUrl = result.data[0].bitly_gif_url;
                        _shareGifFullUrl = result.data[0].images.original.url;
                        _shareGifId = result.data[0].id;

                        tempStr += "\n\nGif Id: " + _shareGifId
                            + "\nbitly_gif_url: " + result.data[0].bitly_gif_url
                            + "\nbitly_url: " + result.data[0].bitly_url
                            + "\ncontent_url: " + result.data[0].content_url
                            + "\nembed_url: " + result.data[0].embed_url
                            + "\nurl: " + result.data[0].url
                            + "\nimages.original.url: " + result.data[0].images.original.url
                            + "\nimages.original.mp4: " + result.data[0].images.original.mp4
                            + "\nimages.original.webp: " + result.data[0].images.original.webp
                            + "\nimages.original_mp4.mp4: " + result.data[0].images.original_mp4.mp4;
                        Debug.Log(tempStr);
                        m_InputField.text = tempStr;
                        m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
                    }
				});
			}
		}

		if(text == "GetByIds")
		{
			Debug.Log("_GiphyApi > GetByIds");
			GiphyManager.Instance.GetByIds(new List<string>{"ZXKZWB13D6gFO", "3oEdva9BUHPIs2SkGk", "u23zXEvNsIbfO"}, (result)=>{
                if (result == null || result.data.Count <= 0)
                {
                    tempStr = "2. GetByIds - (Ids hard-coded for demo = " + "ZXKZWB13D6gFO, 3oEdva9BUHPIs2SkGk, u23zXEvNsIbfO): \nNo matched result!";
                    m_InputField_JsonText.text = tempStr;
                }
                else
                {
                    tempStr = "2. GetByIds - (Ids hard-coded for demo = " + "ZXKZWB13D6gFO, 3oEdva9BUHPIs2SkGk, u23zXEvNsIbfO): ";
                    for (int i = 0; i < result.data.Count; i++)
                    {
                        tempStr += "\n(" + i + ") " + result.data[i].bitly_gif_url;
                    }
                    _shareGifBitlyUrl = result.data[0].bitly_gif_url;
                    _shareGifFullUrl = result.data[0].images.original.url;
                    _shareGifId = result.data[0].id;

                    tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                    Debug.Log(tempStr);
                    m_InputField.text = tempStr;
                    m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
                }
			});
		}

		if(text == "Random")
		{
			Debug.Log("_GiphyApi > Random");
			GiphyManager.Instance.Random((result)=>{
                if (result == null || result.data == null)
                {
                    tempStr = "3. Random result: \nNo matched result!";
                    m_InputField_JsonText.text = tempStr;
                }
                else
                {
                    tempStr = "3. Random result: ";
                    _shareGifBitlyUrl = result.data.image_url;
                    _shareGifFullUrl = result.data.image_original_url;
                    _shareGifId = result.data.id;

                    tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                    Debug.Log(tempStr);
                    m_InputField.text = tempStr;
                    m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
                }
			});
		}

		if(text == "Translate")
		{
			if(string.IsNullOrEmpty(m_InputFieldKeyWord.text))
			{
				Debug.LogWarning("_GiphyApi > Translate, KeyWord is Empty!");
			}
			else
			{
				Debug.Log("_GiphyApi > Translate");
				GiphyManager.Instance.Translate(m_InputFieldKeyWord.text, 3, (result)=>{
                    if (result == null || result.data == null)
                    {
                        tempStr = "4. Translate result (KeyWord = " + m_InputFieldKeyWord.text + "): \nNo matched result!";
                        m_InputField_JsonText.text = tempStr;
                    }
                    else
                    {
                        tempStr = "4. Translate result (KeyWord = " + m_InputFieldKeyWord.text + "): ";
                        _shareGifBitlyUrl = result.data.bitly_gif_url;
                        _shareGifFullUrl = result.data.images.original.url;
                        _shareGifId = result.data.id;

                        tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                        Debug.Log(tempStr);
                        m_InputField.text = tempStr;
                        m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
                    }
				});
			}
		}


		if(text == "Trending")
		{
			Debug.Log("_GiphyApi > Trending");
			GiphyManager.Instance.Trending((result)=>{
                if (result == null || result.data.Count <= 0)
                {
                    tempStr = "5. Trending - All GIF URLs: \nNo matched result!";
                    m_InputField_JsonText.text = tempStr;
                }
                else
                {
                    tempStr = "5. Trending - All GIF URLs: ";
                    for (int i = 0; i < result.data.Count; i++)
                    {
                        tempStr += "\n(" + i + ") " + result.data[i].bitly_gif_url;
                    }
                    _shareGifBitlyUrl = result.data[0].bitly_gif_url;
                    _shareGifFullUrl = result.data[0].images.original.url;
                    _shareGifId = result.data[0].id;

                    tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                    Debug.Log(tempStr);
                    m_InputField.text = tempStr;
                    m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
                }
			});
		}


		//------ Stickers API -------
		if(text == "Search Sticker")
		{
			if(string.IsNullOrEmpty(m_InputFieldKeyWord.text))
			{
				Debug.LogWarning("_GiphyApi > Search Sticker, KeyWord is Empty!");
			}
			else
			{
				Debug.Log("_GiphyApi > Search Sticker");
				GiphyManager.Instance.Search_Sticker(new List<string>{m_InputFieldKeyWord.text}, (result)=>{
                    if (result == null || result.data.Count <= 0)
                    {
                        tempStr = "6. Search_Sticker - All GIF URLs (KeyWord = " + m_InputFieldKeyWord.text + "): \nNo matched result!";
                        m_InputField_JsonText.text = tempStr;
                    }
                    else
                    {
                        tempStr = "6. Search_Sticker - All GIF URLs (KeyWord = " + m_InputFieldKeyWord.text + "): ";
                        for (int i = 0; i < result.data.Count; i++)
                        {
                            tempStr += "\n(" + i + ") " + result.data[i].bitly_gif_url;
                        }
                        _shareGifBitlyUrl = result.data[0].bitly_gif_url;
                        _shareGifFullUrl = result.data[0].images.original.url;
                        _shareGifId = result.data[0].id;

                        tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                        Debug.Log(tempStr);
                        m_InputField.text = tempStr;
                        m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
                    }
				});
			}
		}

		if(text == "Trending Sticker")
		{
			Debug.Log("_GiphyApi > Trending Sticker");
			GiphyManager.Instance.Trending_Sticker((result)=>{
                if (result == null || result.data.Count <= 0)
                {
                    tempStr = "7. Trending_Sticker - All GIF URLs: \nNo matched result!";
                    m_InputField_JsonText.text = tempStr;
                }
                else
                {
                    tempStr = "7. Trending_Sticker - All GIF URLs: ";
                    for (int i = 0; i < result.data.Count; i++)
                    {
                        tempStr += "\n(" + i + ") " + result.data[i].bitly_gif_url;
                    }
                    _shareGifBitlyUrl = result.data[0].bitly_gif_url;
                    _shareGifFullUrl = result.data[0].images.original.url;
                    _shareGifId = result.data[0].id;

                    tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                    Debug.Log(tempStr);
                    m_InputField.text = tempStr;
                    m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
                }
			});
		}

		if(text == "Random Sticker")
		{
			Debug.Log("_GiphyApi > Random Sticker");
			GiphyManager.Instance.Random_Sticker((result)=>{
                if (result == null || result.data == null)
                {
                    tempStr = "8. Random_Sticker result: \nNo matched result!";
                    m_InputField_JsonText.text = tempStr;
                }
                else
                {
                    tempStr = "8. Random_Sticker result: ";
                    _shareGifBitlyUrl = result.data.image_url;
                    _shareGifFullUrl = result.data.image_original_url;
                    _shareGifId = result.data.id;

                    tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                    Debug.Log(tempStr);
                    m_InputField.text = tempStr;
                    m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
                }
			});
		}

		if(text == "Random Sticker with Tag")
		{
			if(string.IsNullOrEmpty(m_InputFieldKeyWord.text))
			{
				Debug.LogWarning("_GiphyApi > Random Sticker with Tag, KeyWord is Empty!");
			}
			else
			{
				Debug.Log("_GiphyApi > Random Sticker with Tag");
				GiphyManager.Instance.Random_Sticker(m_InputFieldKeyWord.text, (result)=>{
                    if (result == null || result.data == null)
                    {
                        tempStr = "9. Random_Sticker with tag result (Tag = " + m_InputFieldKeyWord.text + "): \nNo matched result!";
                        m_InputField_JsonText.text = tempStr;
                    }
                    else
                    {
                        tempStr = "9. Random_Sticker with tag result (Tag = " + m_InputFieldKeyWord.text + "): ";
                        _shareGifBitlyUrl = result.data.image_url;
                        _shareGifFullUrl = result.data.image_original_url;
                        _shareGifId = result.data.id;

                        tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                        Debug.Log(tempStr);
                        m_InputField.text = tempStr;
                        m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
                    }
				});
			}
		}

		if(text == "Translate Sticker")
		{
			if(string.IsNullOrEmpty(m_InputFieldKeyWord.text))
			{
				Debug.LogWarning("_GiphyApi > Translate Sticker, KeyWord is Empty!");
			}
			else
			{
				Debug.Log("Translate Sticker");
				GiphyManager.Instance.Translate_Sticker(m_InputFieldKeyWord.text, 3, (result)=>{
                    if (result == null || result.data == null)
                    {
                        tempStr = "10. Translate_Sticker result (KeyWord = " + m_InputFieldKeyWord.text + "): \nNo matched result!";
                        m_InputField_JsonText.text = tempStr;
                    }
                    else
                    {
                        tempStr = "10. Translate_Sticker result (KeyWord = " + m_InputFieldKeyWord.text + "): ";
                        _shareGifBitlyUrl = result.data.bitly_gif_url;
                        _shareGifFullUrl = result.data.images.original.url;
                        _shareGifId = result.data.id;

                        tempStr += "\n\n_shareGifBitlyUrl: " + _shareGifBitlyUrl + "\n_shareGifFullUrl: " + _shareGifFullUrl + "\n_shareGifId: " + _shareGifId;
                        Debug.Log(tempStr);
                        m_InputField.text = tempStr;
                        m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
                    }
				});
			}
		}

	}

	/// <summary>
	/// Upload API. For upload your local gif to your own channel on Giphy.com
	/// To call this API, you need to create an APP in Giphy Dashboard, and Request a production key with Upload API
	/// Kindly Reminded: read their instructions and terms before your request!
	/// </summary>
	/// <param name="gifFilePath">GIF file path.</param>
	public void UploadApi(string gifFilePath)
	{
		GiphyManager.Instance.Upload(gifFilePath, new List<string>{"GifTagTest", "SwanOB2", "AssetPackage"}, 
			(uploadResult)=>{
				Debug.Log("Upload response, Unique Gif Id: " + uploadResult.data.id);

				//FOR EXAMPLE:
				//Call GetById API with GIF Id to get the GIF links and other data
				GiphyManager.Instance.GetById(uploadResult.data.id, (getByIdResult)=>{
					Debug.Log("GetById response, gif short link: " + getByIdResult.data.bitly_gif_url);

					//Your Code Here: e.g. Share GIF link on social networks like Facebook, Twitter
					//For Facebook put the GIF link as the first link in the post for fetching GIF preview
					//For Twitter put the GIF link as the last link in the post for fetching GIF preview

					m_InputField_JsonText.text = _SetDisplayString(GiphyManager.Instance.FullJsonResponseText);
				});

			}, 
			(progress)=>{
				Debug.Log("Upload Progress: " + progress);
			}
		);
	}

}
