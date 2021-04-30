using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Translate.Core.Translator.Bing.Entities.Post
{
    [DataContract]
    public class BingPostTransResult
    {
        //[DataMember(Name = "text")]
        //public string From { get; set; }

        //[DataMember(Name = "to")]
        //public string To { get; set; }

        //[DataMember(Name = "items")]
        [JsonProperty("translations"),DataMember(Name = "translations")]
        public List<Translations> Translations { get; set; }
    }
    [DataContract]
    public class Translations
    {
        [JsonProperty("text"), DataMember(Name = "text")]
        public string From { get; set; }

        [JsonProperty("to"), DataMember(Name = "to")]
        public string To { get; set; }
    }
    //[DataContract]
    //public class BingPostTransResultItem : BingPostTransParams
    //{
    //    [DataMember(Name = "wordAlignment")]
    //    public string WordAlignment { get; set; }
    //}
}