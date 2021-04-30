﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Translate.Core.Translator.Bing.Entities;
using Translate.Core.Translator.Bing.Entities.Api;
using Translate.Core.Translator.Bing.Entities.Post;
using Translate.Core.Translator.Entities;
using Translate.Core.Translator.Enums;
using Translate.Core.Translator.Utils;
using WebException = System.Net.WebException;

namespace Translate.Core.Translator.Bing
{
    public class BingTranslator : ITranslator
    {
        private static string subscriptionKey;
        private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com/";
        private static string location;

        private static AdmAccessToken _admToken;
        private static BingAdmAuth _admAuth;

        private static readonly List<TranslationLanguage> TargetLanguages;
        private static readonly List<TranslationLanguage> SourceLanguages;

        public BingTranslator()
        {
            //InitCookie();
        }


        public BingTranslator(string clientId, string clientSecret)
        {
            //Get Client Id and Client Secret from https://datamarket.azure.com/developer/applications/
            //Refer obtaining AccessToken (http://msdn.microsoft.com/en-us/library/hh454950.aspx) 

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new Exception("app id and client secret is necessary");
            }
            subscriptionKey = clientId;
            location = clientSecret;


            //_admAuth = new BingAdmAuth(clientId, clientSecret);
        }

        static BingTranslator()
        {
            //language list from https://msdn.microsoft.com/en-us/library/hh456380.aspx

            TargetLanguages = new List<TranslationLanguage>()
            {
                new TranslationLanguage("af","Afrikaans / 南非荷兰语"),
                new TranslationLanguage("ar", "Arabic / 阿拉伯语"),
                new TranslationLanguage("bs-Latn", "Bosnian (Latin) / 波斯尼亚 (拉丁语)"),
                new TranslationLanguage("bg", "Bulgarian / 保加利亚语"),
                new TranslationLanguage("ca", "Catalan / 加泰罗尼亚语"),
                new TranslationLanguage("zh-CHS", "Chinese (Simplified) / 简体中文"),
                new TranslationLanguage("zh-CHT", "Chinese (Traditional) / 繁体中文"),
                new TranslationLanguage("yue", "Cantonese (Traditional) / 粤语（繁体）"),
                new TranslationLanguage("hr", "Croatian / 克罗地亚语"),
                new TranslationLanguage("cs", "Czech / 捷克语"),
                new TranslationLanguage("da", "Danish / 丹麦语"),
                new TranslationLanguage("nl", "Dutch / 荷兰语"),
                new TranslationLanguage("en", "English / 英语"),
                new TranslationLanguage("et", "Estonian / 爱沙尼亚语"),
                new TranslationLanguage("fj", "Fijian / 斐济语"),
                new TranslationLanguage("fil", "Filipino / 菲律宾语"),
                new TranslationLanguage("fi", "Finnish / 芬兰语"),
                new TranslationLanguage("fr", "French / 法语"),
                new TranslationLanguage("de", "German / 德语"),
                new TranslationLanguage("el", "Greek / 希腊语"),
                new TranslationLanguage("ht", "Haitian Creole / 海地克里奥尔语"),
                new TranslationLanguage("he", "Hebrew / 希伯来语"),
                new TranslationLanguage("hi", "Hindi / 印地语"),
                new TranslationLanguage("mww", "Hmong Daw /苗语"),
                new TranslationLanguage("hu", "Hungarian / 匈牙利语"),
                new TranslationLanguage("id", "Indonesian / 印度尼西亚语"),
                new TranslationLanguage("it", "Italian / 意大利语"),
                new TranslationLanguage("ja", "Japanese / 日语"),
                new TranslationLanguage("sw", "Kiswahili / 斯瓦希里语"),
                new TranslationLanguage("tlh", "Klingon / 克林贡语"),
                new TranslationLanguage("ko", "Korean / 韩语"),
                new TranslationLanguage("lv", "Latvian / 拉脱维亚语"),
                new TranslationLanguage("lt", "Lithuanian / 立陶宛语"),
                new TranslationLanguage("mg", "Malagasy / 马尔加什语"),
                new TranslationLanguage("ms", "Malay / 马来语"),
                new TranslationLanguage("mt", "Maltese / 马耳他语"),
                new TranslationLanguage("yua", "Yucatec Maya / 玛雅语"),
                new TranslationLanguage("no", "Norwegian Bokmål / 挪威博克马尔语"),
                new TranslationLanguage("fa", "Persian / 波斯语"),
                new TranslationLanguage("pl", "Polish / 波兰语"),
                new TranslationLanguage("pt", "Portuguese / 葡萄牙语"),
                new TranslationLanguage("ro", "Romanian / 罗马尼亚语"),
                new TranslationLanguage("ru", "Russian / 俄语"),
                new TranslationLanguage("sm", "Samoan / 萨摩亚语"),
                new TranslationLanguage("sr-Cyrl", "Serbian (Cyrillic) / 塞尔维亚语"),
                new TranslationLanguage("sr-Latn", "Serbian (Latin) / 塞尔维亚语（拉丁语）"),
                new TranslationLanguage("sk", "Slovak / 斯洛伐克语"),
                new TranslationLanguage("sl", "Slovenian / 斯洛文尼亚语"),
                new TranslationLanguage("es", "Spanish / 西班牙语"),
                new TranslationLanguage("sv", "Swedish / 瑞典语"),
                new TranslationLanguage("ty", "Tahitian / 大溪地语 (塔希提岛)"),
                new TranslationLanguage("th", "Thai / 泰语"),
                new TranslationLanguage("tr", "Turkish / 土耳其语"),
                new TranslationLanguage("uk", "Ukrainian / 乌克兰语"),
                new TranslationLanguage("ur", "Urdu / 乌尔都语"),
                new TranslationLanguage("vi", "Vietnamese / 越南语"),
                new TranslationLanguage("cy", "Welsh / 威尔士语")
            };
            SourceLanguages = new List<TranslationLanguage>() { new TranslationLanguage("", "Auto-detect / 自动检测") };
            SourceLanguages.AddRange(TargetLanguages);
        }

        #region api
        //soap : https://msdn.microsoft.com/en-us/library/ff512435.aspx
        //ajax : https://msdn.microsoft.com/en-us/library/ff512404.aspx
        //http : https://msdn.microsoft.com/en-us/library/ff512419.aspx


        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/ff512419.aspx
        /// </summary>
        /// <param name="text"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private string TranslateByHttp(string text, string from = "en", string to = "zh-CHS")
        {

            try
            {
                //var sw = Stopwatch.StartNew();
                var texts = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
                var transParams = texts.Select(t => new BingPostTransParams() { Text = t }).ToList();
                StartTrans: var uri = $"{endpoint}translate?api-version=3.0&from={from}&to={to}";
                var formData = JsonConvert.SerializeObject(transParams.Where(t => !string.IsNullOrWhiteSpace(t.Text)));


                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12;
                HttpClient client = new HttpClient();
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
                requestMessage.Content = new StringContent(formData, Encoding.UTF8, "application/json");
                requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                requestMessage.Headers.Add("Ocp-Apim-Subscription-Region", location);
                HttpResponseMessage response = client.SendAsync(requestMessage).GetAwaiter().GetResult();

                if (response.StatusCode.ToString() == "OK")
                {
                    string r = response.Content.ReadAsStringAsync().Result.ToString();

                    var result = JsonConvert.DeserializeObject<List<BingPostTransResult>>(r).FirstOrDefault();
                    if (result != null)
                    {
                        return result.Translations.FirstOrDefault().From;
                    }
                    return "";

                }






                //var authToken = GetAuthToken();
                //string uri = "http://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + HttpUtility.UrlEncode(text) + "&from=" + from + "&to=" + to;
                //HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
                //httpWebRequest.Headers.Add("Authorization", authToken);
                //WebResponse response = null;
                //try
                //{
                //    response = httpWebRequest.GetResponse();
                //    using (Stream stream = response.GetResponseStream())
                //    {
                //        if (stream == null)
                //        {
                //            return string.Empty;
                //        }
                //        DataContractSerializer dcs = new DataContractSerializer(typeof(String));
                //        string translation = (string)dcs.ReadObject(stream);

                //        Console.WriteLine("Bing translation for source text '{0}' from {1} to {2} is", text, from, to);
                //        Console.WriteLine(translation);

                //        return translation;
                //    }
                //}
                //catch
                //{
                //    throw;
                //}
                //finally
                //{
                //    response?.Close();
                //}
            }
            catch (WebException e)
            {
                Utils.WebException.ProcessWebException(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return String.Empty;
        }
        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/ff512404.aspx
        /// </summary>
        /// <param name="text"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private BingTransResult TranslateByAjax(string text, string from = "en", string to = "zh-CHS")
        {
            try
            {
                var authToken = GetAuthToken();
                string uri = "http://api.microsofttranslator.com/v2/ajax.svc/GetTranslations?text=" + HttpUtility.UrlEncode(text) + "&from=" + from + "&to=" + to + "&maxTranslations=20";

                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
                httpWebRequest.Headers.Add("Authorization", authToken);
                WebResponse response = null;
                try
                {
                    response = httpWebRequest.GetResponse();
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (stream == null)
                        {
                            return null;
                        }
                        StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
                        string jsonString = streamReader.ReadToEnd();
                        using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
                        {
                            BingTransResult translation = (BingTransResult)new DataContractJsonSerializer(typeof(BingTransResult)).ReadObject(ms);
                            Console.WriteLine("Bing translation for source text '{0}' from {1} to {2} is", text, from, to);
                            Console.WriteLine(translation);
                            return translation;
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (response != null)
                    {
                        response.Close();
                        response = null;
                    }
                }
            }
            catch (WebException e)
            {
                Utils.WebException.ProcessWebException(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        private string GetAuthToken()
        {
            _admToken = _admAuth.GetAccessToken();
            // Create a header with the access_token property of the returned token
            return "Bearer " + _admToken.AccessToken;
        }
        #endregion

        #region post
        private BingPostTransResult TranslateByPost(string text, string from = "-", string to = "zh-CHT")
        {
            var texts = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var transParams = texts.Select(t => new BingPostTransParams() { Text = t }).ToList();
            return TranslateByPost(transParams, from, to);
        }

        private readonly Regex _mtstknRegex = new Regex("mtstkn=([^;]+);");

        private readonly Regex _muidbRegex = new Regex("MUIDB=([^;]+);");

        private string _mtstkn = string.Empty;
        private string _muidb = string.Empty;
        private void InitCookie()
        {
            var cookie = new HttpHelper().GetHtml(new HttpItem()
            {
                Url = "https://www.bing.com/translator/?mkt=zh-CN",
                Timeout = 5000
            }).Cookie;

            _mtstkn = _mtstknRegex.Match(cookie).Groups[1].Value;
            _muidb = _muidbRegex.Match(cookie).Groups[1].Value;
        }

        /// <summary>
        /// Translate from bing website  https://www.bing.com/translator/?mkt=zh-CN
        /// </summary>
        /// <param name="text"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private BingPostTransResult TranslateByPost(List<BingPostTransParams> text, string from = "-", string to = "zh-CHT")
        {
            //https://www.bing.com/translator/api/Translate/TranslateArray?from=-&to=zh-CHS

            if (string.IsNullOrWhiteSpace(from))
            {
                from = "-";//auto
            }
            var maxTryCount = 3;
            var tryCount = 0;
            try
            {
            StartTrans: var uri = $"https://www.bing.com/translator/api/Translate/TranslateArray?from={from}&to={to}";
                var formData = JsonConvert.SerializeObject(text.Where(t => !string.IsNullOrWhiteSpace(t.Text)));

                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Method = "POST";
                var cookie = new CookieContainer();
                cookie.Add(new Cookie("mtstkn", _mtstkn, "/", ".bing.com"));
                cookie.Add(new Cookie("MUIDB", _muidb, "/", ".bing.com"));
                httpWebRequest.CookieContainer = cookie;
                httpWebRequest.UserAgent = "PostmanRuntime/6.4.1";
                httpWebRequest.Accept = "*/*";
                httpWebRequest.Headers.Add("accept-encoding", "gzip, deflate");
                httpWebRequest.Headers.Add("cache-control", "no-cache");
                byte[] bytes = Encoding.UTF8.GetBytes(formData);
                httpWebRequest.ContentLength = bytes.Length;
                using (Stream outputStream = httpWebRequest.GetRequestStream())
                {
                    outputStream.Write(bytes, 0, bytes.Length);
                }
                WebResponse response = null;
                try
                {
                    response = httpWebRequest.GetResponse();
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (stream == null)
                        {
                            return null;
                        }
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(BingPostTransResult));
                        //Get deserialized object from JSON stream
                        BingPostTransResult bingTransResult = (BingPostTransResult)serializer.ReadObject(stream);
                        return bingTransResult;
                    }
                }
                catch (Exception e)
                {
                    if (tryCount++ < maxTryCount)
                    {
                        InitCookie();
                        goto StartTrans;
                    }
                    throw;
                }
                finally
                {
                    response?.Close();
                }
            }
            catch (WebException e)
            {
                Utils.WebException.ProcessWebException(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        #endregion


        //http://api.microsofttranslator.com/v2/ajax.svc/

        //get Support languages : http://api.microsofttranslator.com/v2/ajax.svc/GetLanguagesForTranslate
        //get support languages's names : http://api.microsofttranslator.com/v2/ajax.svc/GetLanguageNames?locale=en&languageCodes=["af","ar","bs-Latn","bg","ca","zh-CHS","zh-CHT","yue","hr","cs","da","nl","en","et","fj","fil","fi","fr","de","el","ht","he","hi","mww","hu","id","it","ja","sw","tlh","tlh-Qaak","ko","lv","lt","mg","ms","mt","yua","no","otq","fa","pl","pt","ro","ru","sm","sr-Cyrl","sr-Latn","sk","sl","es","sv","ty","th","to","tr","uk","ur","vi","cy"]

        public string GetIdentity()
        {
            return "Bing";
        }
        public static string GetName()
        {
            return "Bing Translator / 必应翻译";
        }
        public static string GetChineseLanguage()
        {
            return "zh-CHS";
        }


        public static string GetDescription()
        {
            return "You can on the website translation http://www.bing.com/translator/";
        }

        public static string GetWebsite()
        {
            return "http://www.bing.com/translator/";
        }

        public List<TranslationLanguage> GetAllTargetLanguages()
        {
            return TargetLanguages;
        }

        public List<TranslationLanguage> GetAllSourceLanguages()
        {
            return SourceLanguages;
        }

        public static List<TranslationLanguage> GetTargetLanguages()
        {
            return TargetLanguages;
        }

        public static List<TranslationLanguage> GetSourceLanguages()
        {
            return SourceLanguages;
        }

        public TranslationResult Translate(string text, string @from, string to)
        {
            TranslationResult result = new TranslationResult()
            {
                SourceLanguage = @from,
                TargetLanguage = to,
                SourceText = text,
                TargetText = "",
                FailedReason = ""
            };
            if (SourceLanguages.Count(sl => sl.Code == @from) <= 0)
            {
                result.TranslationResultTypes = TranslationResultTypes.Failed;
                result.FailedReason = "unrecognizable source language";
            }
            else if (TargetLanguages.Count(tl => tl.Code == to) <= 0)
            {
                result.TranslationResultTypes = TranslationResultTypes.Failed;
                result.FailedReason = "unrecognizable target language";
            }
            else
            {
                try
                {
                    result.TranslationResultTypes = TranslationResultTypes.Successed;

                    #region ajax
                    //BingTransResult bingTransResult = TranslateByAjax(text, from, to);
                    //result.SourceLanguage = bingTransResult?.From;
                    //result.TargetText = bingTransResult?.Translations?[0].TranslatedText;
                    #endregion

                    #region http
                    result.TargetText = TranslateByHttp(text, from, to);
                    #endregion

                    #region post

                    //var bingTransResult = TranslateByPost(text, from, to);

                    //result.SourceLanguage = bingTransResult?.From;
                    //foreach (var item in bingTransResult?.Items ?? new List<BingPostTransResultItem>())
                    //{
                    //    result.TargetText += item.Text + "\r\n";
                    //}
                    //result.TargetText = result.TargetText.TrimEnd();

                    #endregion
                }
                catch (Exception exception)
                {
                    result.FailedReason = exception.Message;
                    result.TranslationResultTypes = TranslationResultTypes.Failed;
                }
            }
            return result;
        }
    }
}