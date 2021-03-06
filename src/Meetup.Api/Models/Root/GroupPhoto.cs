using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Meetup.Api
{
    public class GroupPhoto
    {
        [JsonProperty("photo_link")]
        public string PhotoLink { get; set; }

        [JsonProperty("highres_link")]
        public string HighResLink { get; set; }

        [JsonProperty("thumb_link")]
        public string ThumbLink { get; set; }

        [JsonProperty("photo_id")]
        public int PhotoId { get; set; }
    }
}