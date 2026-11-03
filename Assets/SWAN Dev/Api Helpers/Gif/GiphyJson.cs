// Created by SwanDEV 2017
using System.Collections.Generic;

namespace SDev.GiphyAPI
{
    #region ------ Shared Class ------
    public class Meta
    {
        /// <summary>
        /// HTTP Response Code.
        /// </summary>
        public int status;
        /// <summary>
        /// HTTP Response Message.
        /// </summary>
        public string msg;
        /// <summary>
        /// A unique ID paired with this response from the API.
        /// </summary>
        public string response_id;
    }
    public class Pagination
    {
        /// <summary>
        /// Total number of items available (not returned on every endpoint).
        /// </summary>
        public int total_count;
        /// <summary>
        /// Total number of items returned.
        /// </summary>
        public int count;
        /// <summary>
        /// Position in pagination.
        /// </summary>
        public int offset;
    }
    public class User
    {
        public string avatar_url;
        public string banner_url;
        public string profile_url;
        public string username;
        public string display_name;

        // something not show in Giphy doc:
        public string twitter;
        public string banner_image;
        public string description;
        public bool is_verified;
        public string website_url;
        public string instagram_url;
    }
    /// <summary>
    /// The Images Object found in the GIF Object contains a series of Rendition Objects.
    /// These Rendition Objects include the URLs and sizes for the many different renditions for each GIF.
    /// (Please note that some GIFs don’t have every property available)
    /// </summary>
    public class Images
    {
        public FixedHeight fixed_height;
        public FixedHeightStill fixed_height_still;
        public FixedHeightDownsampled fixed_height_downsampled;
        public FixedWidth fixed_width;
        public FixedWidthStill fixed_width_still;
        public FixedWidthDownsampled fixed_width_downsampled;
        public FixedHeightSmall fixed_height_small;
        public FixedHeightSmallStill fixed_height_small_still;
        public FixedWidthSmall fixed_width_small;
        public FixedWidthSmallStill fixed_width_small_still;
        public Downsized downsized;
        public DownsizedStill downsized_still;
        public DownsizedLarge downsized_large;
        public DownsizedMedium downsized_medium;
        public Original original;
        public OriginalStill original_still;
        public Looping looping;
        public OriginalMp4 original_mp4;
        public Preview preview;
        public DownsizedSmall downsized_small;
        public PreviewGif preview_gif;
        public PreviewWebp preview_webp;
        public Hd hd;
        //public _4k _4k;
        //public _480wStill _480w_still;
    }
    public class Gif
    {
        public string type;
        public string id;
        public string url;
        public string slug;
        public string bitly_gif_url;
        public string bitly_url;
        public string embed_url;
        public string username;
        public string source;
        public string title;
        public string rating;
        public string content_url;
        public string source_tld;
        public string source_post_url;
        public int is_sticker;
        public string import_datetime;
        public string trending_datetime;
        public string create_datetime;
        public string update_datetime;
        public User user;

        /// <summary>
        /// The Images Object found in the GIF Object contains a series of Rendition Objects.
        /// These Rendition Objects include the URLs and sizes for the many different renditions for each GIF.
        /// (Please note that some GIFs don’t have every property available)
        /// </summary>
        public Images images;

        // something not show in Giphy doc:
        public string image_original_url;
        public string image_url;
        public string image_mp4_url;
        public string image_frames;
        public string image_width;
        public string image_height;
        public string fixed_height_downsampled_url;
        public string fixed_height_downsampled_width;
        public string fixed_height_downsampled_height;
        public string fixed_width_downsampled_url;
        public string fixed_width_downsampled_width;
        public string fixed_width_downsampled_height;
        public string fixed_height_small_url;
        public string fixed_height_small_still_url;
        public string fixed_height_small_width;
        public string fixed_height_small_height;
        public string fixed_width_small_url;
        public string fixed_width_small_still_url;
        public string fixed_width_small_width;
        public string fixed_width_small_height;
        public string caption;
    }

    //public class Image
    //{
    //    public string url;
    //    public string height;
    //    public string width;
    //    public string size;
    //    public string mp4;
    //    public string mp4_size;
    //    public string webp;
    //    public string webp_size;
    //    public string frames;
    //    public string hash;
    //}
    public class FixedHeight
    {
        public string url;
        public string height;
        public string width;
        public string size;
        public string mp4;
        public string mp4_size;
        public string webp;
        public string webp_size;
    }
    public class FixedHeightStill
    {
        public string url;
        public string width;
        public string height;
        public string size;
    }
    public class FixedHeightDownsampled
    {
        public string url;
        public string width;
        public string height;
        public string size;
        public string webp;
        public string webp_size;
    }
    public class FixedWidth
    {
        public string url;
        public string width;
        public string height;
        public string size;
        public string mp4;
        public string mp4_size;
        public string webp;
        public string webp_size;
    }
    public class FixedWidthStill
    {
        public string url;
        public string width;
        public string height;
        public string size;
    }
    public class FixedWidthDownsampled
    {
        public string url;
        public string width;
        public string height;
        public string size;
        public string webp;
        public string webp_size;
    }
    public class FixedHeightSmall
    {
        public string url;
        public string width;
        public string height;
        public string size;
        public string mp4;
        public string mp4_size;
        public string webp;
        public string webp_size;
    }
    public class FixedHeightSmallStill
    {
        public string url;
        public string width;
        public string height;
        public string size;
    }
    public class FixedWidthSmall
    {
        public string url;
        public string width;
        public string height;
        public string size;
        public string mp4;
        public string mp4_size;
        public string webp;
        public string webp_size;
    }
    public class FixedWidthSmallStill
    {
        public string url;
        public string width;
        public string height;
        public string size;
    }
    public class Preview
    {
        public string width;
        public string height;
        public string mp4;
        public string mp4_size;
    }
    public class PreviewGif
    {
        public string url;
        public string width;
        public string height;
        public string size;
    }
    public class PreviewWebp
    {
        public string url;
        public string width;
        public string height;
        public string size;
    }
    public class DownsizedStill
    {
        public string url;
        public string width;
        public string height;
        public string size;
    }
    public class Downsized
    {
        public string url;
        public string width;
        public string height;
        public string size;
    }
    public class DownsizedLarge
    {
        public string url;
        public string width;
        public string height;
        public string size;
    }
    public class DownsizedSmall
    {
        public string width;
        public string height;
        public string mp4;
        public string mp4_size;
    }
    public class DownsizedMedium
    {
        public string url;
        public string width;
        public string height;
        public string size;
    }
    public class Original
    {
        public string url;
        public string width;
        public string height;
        public string size;
        public string mp4;
        public string mp4_size;
        public string webp;
        public string webp_size;
        public string frames;
        public string hash;
    }
    public class OriginalStill
    {
        public string url;
        public string width;
        public string height;
        public string size;
    }
    public class OriginalMp4
    {
        public string width;
        public string height;
        public string mp4;
        public string mp4_size;
    }
    public class Looping
    {
        public string mp4;
        public string mp4_size;
    }
    public class Hd
    {
        public string width;
        public string height;
        public string mp4;
        public string mp4_size;
    }
    //public class _4k
    //{
    //    public string width;
    //    public string height;
    //    public string mp4;
    //    public string mp4_size;
    //}
    //public class _480wStill
    //{
    //    public string url;
    //    public string width;
    //    public string height;
    //    public string size;
    //}
    #endregion

    #region ------ Other Json Object ------
    public class GiphyUserRandomId
    {
        public Data data;
        public Meta meta;

        public class Data
        {
            public string random_id;
        }
    }

    public class GiphySearchSuggestion
    {
        public List<Data> data;
        public Meta meta;

        public class Data
        {
            public string name;
            public string analytics_response_payload;
        }
    }

    public class GiphyAutocomplete
    {
        public List<Data> data;
        public Pagination pagination;
        public Meta meta;

        public class Data
        {
            public string name;
            public string analytics_response_payload;
        }
    }

    public class GiphySearchChannels
    {
        public List<Channel> data;
        public Pagination pagination;
        public Meta meta;

        public class ChannelTag
        {
            /// <summary> Tag unique ID. e.g. 328 </summary>
            public long id;

            /// <summary> The ID of the channel associated with the tag. e.g. 52 </summary>
            public long channel;

            /// <summary> The rank of the tag. e.g. 1 </summary>
            public string tag;

            /// <summary> The tag. e.g. happy </summary>
            public long rank;
        }

        public class Channel
        {
            /// <summary>
            /// Channel unique ID.
            /// </summary>
            public long id;
            /// <summary>
            /// Channel relative URL. e.g. "/giphystudios"
            /// </summary>
            public string url;
            /// <summary>
            /// The display name of the channel.
            /// </summary>
            public string display_name;
            /// <summary>
            /// Parent Channel ID. (this should be an object but not a number as Giphy doc shows, maybe a Channel object)
            /// </summary>
            public object parent;
            /// <summary>
            /// The unique channel slug. This slug is used in the channel URL.
            /// </summary>
            public string slug;
            /// <summary>
            /// e.g. "community"
            /// </summary>
            public string type;
            /// <summary>
            /// e.g. "gif"
            /// </summary>
            public string content_type;
            /// <summary>
            /// An object containing data about the user associated with this Channel.
            /// </summary>
            public User user;
            /// <summary>
            /// Channel banner image URL.
            /// </summary>
            public string banner_image;
            /// <summary>
            /// The short display name of the channel.
            /// </summary>
            public string short_display_name;
            /// <summary>
            /// Channel description.
            /// </summary>
            public string description;
            /// <summary>
            /// Channel metadata description.
            /// </summary>
            public string metadata_description;
            /// <summary>
            /// Indicates that the channel has sub-channels.
            /// </summary>
            public bool has_children;
            /// <summary>
            /// Indicates that the channel is visible.
            /// </summary>
            public bool is_visible;
            /// <summary>
            /// Indicates that the channel is private.
            /// </summary>
            public bool is_private;
            /// <summary>
            /// Indicates that the channel is live.
            /// </summary>
            public bool is_live;
            /// <summary>
            /// An object containing data about the channel's featured gif.
            /// </summary>
            public Gif featured_gif;
            /// <summary>
            /// Channel screensaver GIF's ID.
            /// </summary>
            public string screensaver_gif;
            /// <summary>
            /// List of channel tags
            /// </summary>
            public List<ChannelTag> tags;
            /// <summary>
            /// e.g. "2020-07-20T03:27:19+0000"
            /// </summary>
            public string live_since_datetime;
            /// <summary>
            /// e.g. "2020-07-21T03:27:19+0000"
            /// </summary>
            public string live_until_datetime;
            /// <summary>
            /// List of parent channels.
            /// </summary>
            public List<Channel> ancestors;
            /// <summary>
            /// List of channel syncable tags.
            /// </summary>
            public List<ChannelTag> syncable_tags;
            /// <summary>
            /// e.g. "e=ZXZlbnRfdHlwZT1DSEFOTkVMX1NFQVJDSCZjaWQ9YTMxNzNj..."
            /// </summary>
            public string analytics_response_payload;
        }
    }

    public class GiphyTrendingSearchTerms
    {
        public List<string> data;
        public Meta meta;
    }

    public class GiphyGifCategories
    {
        public List<Category> data;
        public Pagination pagination;
        public Meta meta;

        public class Subcategory
        {
            public string name;
            public string name_encoded;
        }

        public class Category
        {
            public string name;
            public string name_encoded;
            public List<Subcategory> subcategories;
            public Gif gif;
        }
    }
    #endregion

    #region ------ Upload Json Object ------
    public class GiphyUpload
    {
        public Data data;
        public Meta meta;

        public class Data
        {
            public string id;
        }
    }
    #endregion

    #region ------ GIF Json Object ------
    public class GiphySearch
    {
        public List<Gif> data;
        public Pagination pagination;
        public Meta meta;
    }

    public class GiphyGetById
    {
        public Gif data;
        public Meta meta;
    }

    public class GiphyGetByIds
    {
        public List<Gif> data;
        public Pagination pagination;
        public Meta meta;
    }

    public class GiphyRandom
    {
        public Gif data;
        public Meta meta;
    }

    public class GiphyTranslate
    {
        public Gif data;
        public Meta meta;
    }

    public class GiphyTrending
    {
        public List<Gif> data;
        public Pagination pagination;
        public Meta meta;
    }
    #endregion

    #region ------ Sticker Json Object ------
    public class GiphyStickerSearch
    {
        public List<Gif> data;
        public Pagination pagination;
        public Meta meta;
    }

    public class GiphyStickerRandom
    {
        public Gif data;
        public Meta meta;
    }

    public class GiphyStickerTranslate
    {
        public Gif data;
        public Meta meta;
    }

    public class GiphyStickerTrending
    {
        public List<Gif> data;
        public Pagination pagination;
        public Meta meta;
    }
    #endregion
}
