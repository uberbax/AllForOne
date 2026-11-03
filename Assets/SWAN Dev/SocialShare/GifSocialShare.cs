// Created by SwanDEV 2017

using UnityEngine;

/// <summary>
/// [Created by SwanDev] Social share class for sharing GIF/image/media URL and/or text message.
/// *** Developers be aware that not all social network support preview and playback GIF with url.
/// *** Tested results are marked in each Social enum types. Use the ShareTo(...) method to share content(s).
/// </summary>
public class GifSocialShare
{
    #region ----- Social Networks -----
    private string facebookTemplate = "https://www.facebook.com/sharer/sharer.php?u={3}";

    private string twitterMobileTemplate = "https://twitter.com/intent/tweet?url={3}&text={1}&via={2}&hashtags={5}";

    private string tumblrTemplate = "https://www.tumblr.com/widgets/share/tool?canonicalUrl={3}&caption={1}&tags={5}"; //"http://www.tumblr.com/share?v=3&u={3}&t={1}";

    private string vkTemplate = "http://vk.com/share.php?title={0}&comment={1}&url={3}";
    //"http://vk.com/share.php?title={0}&description={1}&image={2}&url={3}"; //old (description & image not work)

    private string pinterestTemplate = "https://pinterest.com/pin/create/button/?url={3}&media={2}&description={1}";

    private string linkedInTemplate = "https://www.linkedin.com/shareArticle?mini=true&url={3}";
    //"https://www.linkedin.com/shareArticle?mini=true&url={3}&title={0}&summary={1}"

    private string odnoklassnikiTemplate = "https://connect.ok.ru/offer?url={3}&title={0}&imageUrl={2}";
    //"http://www.odnoklassniki.ru/dk?st.cmd=addShare&st.s=1&st._surl={3}&st.comments={1}"; //old (Deprecated)

    private string redditTemplate = "https://reddit.com/submit?url={3}&title={0}%20%0D%0A%0D%0A{1}"; //(Set browser to Desktop Mode)

    private string qqTemplate = "http://sns.qzone.qq.com/cgi-bin/qzshare/cgi_qzshare_onekey?url={3}&title={0}";

    private string weiboTemplate = "http://service.weibo.com/share/share.php?appkey=&title={0}%20%0D%0A%0D%0A{1}&url={3}";

    private string lineMeTemplate = "https://lineit.line.me/share/ui?text={0}%20%0D%0A%0D%0A{1}&url={3}";  //bitly gif url

    private string skypeTemplate = "https://web.skype.com/share?url={3}&text={0}%20%0D%0A%0D%0A{1}";    //url with .gif ext

    private string whatsappTemplate = "https://api.whatsapp.com/send?phone={4}&text={0}%20%0D%0A%0D%0A{1}%20%0D%0A0{3}";

    private string telegramTemplate = "https://t.me/share/url?url={3}&text={0}%20%0D%0A%0D%0A{1}";
    //https://telegram.me/share/url?url={3}&text={0}%20%0D%0A{1}&to={4}

    private string flipBoardTemplate = "https://share.flipboard.com/bookmarklet/popout?v=2&url={3}";


    // ------ Deprecated: ----------
    private string mySpaceTemplate = "https://myspace.com/post?u={3}&t={0}&c={1}";  //share to MySpace Stream

    private string baiduTemplate = "http://cang.baidu.com/do/add?it={1}&iu={3}";

    private string googlePlusTemplate = "https://plus.google.com/share?url={3}";

    private string twitterTemplate = "https://giphy.com/gifs/{2}/tweet"; //"https://giphy.com/gifs/{2}/tweet?twit_auth=1&device=desktop"; //use gif ID (Set browser to Desktop Mode)


    public enum Social
    {
        /// <summary>
        /// Valid Parameters:
        /// shareUrl (FULL or Bitly URL for GIF or other media file has a similar preview),
        /// </summary>
        Facebook = 0,
        /// <summary>
        /// Valid Parameters:
        /// description,
        /// imageUrl (The @UserName parameter, that will redirect to the mentioned Twitter user page. Could be your studio/company Twitter account), 
        /// shareUrl (Requires Bitly URL for media file for showing preview),
        /// tags (e.g: "tag1,tag2")
        /// </summary>
        Twitter_Mobile = 2,
        /// <summary>
        /// Valid Parameters:
        /// description,
        /// shareUrl (Requires FULL URL for media file for showing preview),
        /// tags (e.g: "tag1,tag2")
        /// </summary>
        Tumblr = 3,
        /// <summary>
        /// Valid Parameters:
        /// title,
        /// description,
        /// shareUrl (FULL or Bitly URL for GIF or other media file has a similar preview),
        /// </summary>
        VK = 4,
        /// <summary>
        /// Valid Parameters:
        /// description,
        /// imageUrl (Requires FULL URL for GIF or other media file for showing preview), 
        /// shareUrl (An URL that redirect to other web page when the image is clicked),
        /// </summary>
        Pinterest = 5,
        /// <summary>
        /// Valid Parameters:
        /// shareUrl (Requires Bitly URL for GIF or other media file for showing preview),
        /// </summary>
        LinkedIn = 6,
        /// <summary>
        /// Valid Parameters:
        /// title,
        /// imageUrl (Requires FULL URL for GIF or other media file for showing preview), 
        /// shareUrl (An URL that redirect to other web page when the image is clicked),
        /// </summary>
        Odnoklassniki = 7,
        /// <summary>
        /// Valid Parameters:
        /// title,
        /// description,
        /// shareUrl (no preview),
        /// </summary>
        Reddit = 8,
        /// <summary>
        /// Valid Parameters:
        /// title,
        /// shareUrl (no preview),
        /// </summary>
        QQZone = 10,
        /// <summary>
        /// Valid Parameters:
        /// title,
        /// description,
        /// shareUrl (no preview),
        /// </summary>
        Weibo = 11,
        /// <summary>
        /// Valid Parameters:
        /// title (work in case the mobile device browser Desktop Mode enabled),
        /// description (work in case the mobile device browser Desktop Mode enabled),
        /// shareUrl (Requires Bitly URL for GIF or other media file for showing preview),
        /// </summary>
        LineMe = 14,
        /// <summary>
        /// Valid Parameters:
        /// title,
        /// description,
        /// shareUrl (Requires Bitly URL for GIF or other media file for showing preview),
        /// </summary>
        Skype = 15,
        /// <summary>
        /// Valid Parameters:
        /// title,
        /// description,
        /// shareUrl (Requires Bitly URL for GIF or other media file for showing preview),
        /// phoneNo (e.g: Area code and Phone Number together, no '+' sign before them)
        /// </summary>
        Whatsapp = 16,
        /// <summary>
        /// Valid Parameters:
        /// title,
        /// description,
        /// shareUrl (Bitly URL for GIF or other media file has better preview result),
        /// </summary>
        Telegram = 17,
        /// <summary>
        /// Valid Parameters:
        /// shareUrl (Bitly URL for GIF or other media file has better preview result),
        /// </summary>
        FlipBoard = 18,


        // ----- Deprecated -----
        /// <summary>
        /// Deprecated
        /// </summary>
        Twitter = 1,
        /// <summary>
        /// Deprecated
        /// </summary>
        GooglePlus = 9,
        /// <summary>
        /// Deprecated
        /// </summary>
        Baidu = 12,
        /// <summary>
        /// Deprecated
        /// </summary>
        MySpace = 13,
    }

    private string _MakeUrl(Social social, string title = "", string description = "", string imageUrl = "", string shareUrl = "", long phoneNo = 19876543210, string tags = "")
    {
        string phoneStr = "";
        if (phoneNo != 19876543210) phoneStr = phoneNo.ToString();

        string template = string.Empty;
        switch (social)
        {
            case Social.Facebook:
                template = facebookTemplate;
                break;
            case Social.Twitter_Mobile:
                template = twitterMobileTemplate;
                break;
            case Social.Tumblr:
                template = tumblrTemplate;
                break;
            case Social.VK:
                template = vkTemplate;
                break;
            case Social.Pinterest:
                template = pinterestTemplate;
                break;
            case Social.LinkedIn:
                template = linkedInTemplate;
                break;
            case Social.Odnoklassniki:
                template = odnoklassnikiTemplate;
                break;
            case Social.Reddit:
                template = redditTemplate;
                break;
            case Social.QQZone:
                template = qqTemplate;
                break;
            case Social.Weibo:
                template = weiboTemplate;
                break;
            case Social.LineMe:
                template = lineMeTemplate;
                break;
            case Social.Skype:
                template = skypeTemplate;
                break;
            case Social.Whatsapp:
                template = whatsappTemplate;
                break;
            case Social.Telegram:
                template = telegramTemplate;
                break;
            case Social.FlipBoard:
                template = flipBoardTemplate;
                break;

            // Deprecated:
            case Social.Twitter:
                template = twitterTemplate;
                break;
            case Social.GooglePlus:
                template = googlePlusTemplate;
                break;
            case Social.Baidu:
                template = baiduTemplate;
                break;
            case Social.MySpace:
                template = mySpaceTemplate;
                break;
            default:
                break;
        }
        return string.Format(template, _EscapeURL(title), _EscapeURL(description), _EscapeURL(imageUrl), _EscapeURL(shareUrl), phoneStr, tags);
    }

    public void ShareTo(Social socialNetwork, string title = "", string description = "", string imageUrl = "", string shareUrl = "", long phoneNo = 19876543210, string tags = "")
    {
        string url = _MakeUrl(socialNetwork, title, description, imageUrl, shareUrl, phoneNo, tags);
        _Publish(url);
    }

    #endregion


    #region ----- Email -----
    public void SendEmail(string toMailAddress, string subject, string body)
    {
        string url = "mailto:" + toMailAddress +
            "?subject=" + _EscapeURL(subject) +
            "&body=" + _EscapeURL(body);
        _Publish(url);
    }

    #endregion


    #region ----- Common -----
    private string _EscapeURL(string url)
    {
#if UNITY_2017_3_OR_NEWER
        return UnityEngine.Networking.UnityWebRequest.EscapeURL(url).Replace("+", "%20");
#else
        return WWW.EscapeURL(url).Replace("+", "%20");
#endif
    }

    private void _Publish(string url)
    {
        Application.OpenURL(url);
    }

    public string LineBreak_URL
    {
        get
        {
            return "%0D%0A";
        }
    }
    #endregion
}
