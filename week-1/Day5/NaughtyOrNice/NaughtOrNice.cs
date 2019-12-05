using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace NaughtyOrNice
{
    public static class NaughtOrNice
    {
        [FunctionName("naughtyornice")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            HttpClient client = new HttpClient();

            var wishData = await client.GetStringAsync("https://christmaswishes.azurewebsites.net/api/Wishes");

            List<Wish> data = JsonConvert.DeserializeObject<List<Wish>>(wishData);

            List<Document> documents = new List<Document>();

            int id = 1;
            foreach(var w in data.Where(x => !string.IsNullOrEmpty(x.Message)).ToList())
            {
                try
                {
                    HttpClient httpClient = new HttpClient();

                    //Detect Language of message
                    HttpResponseMessage response = await httpClient.SendAsync(BuildLangDetectionRequest(w.Message));
                    string result = await response.Content.ReadAsStringAsync();
                    LanguageResult[] detectedLang = JsonConvert.DeserializeObject<LanguageResult[]>(result);

                    if (detectedLang[0].Language == "en")
                    {
                        documents.Add(new Document { id = id.ToString(), text = w.Message });
                        w.Id = id.ToString();
                        id++;
                    }
                    else
                    {
                        response = await httpClient.SendAsync(BuildTranslateRequest(w.Message));
                        result = await response.Content.ReadAsStringAsync();
                        var TranslatedLang = JsonConvert.DeserializeObject<TranslationResult[]>(result);
                        w.Message = TranslatedLang[0].Translations[0].Text;

                        documents.Add(new Document { id = id.ToString(), text = w.Message });
                        w.Id = id.ToString();
                        id++;
                    }
                }
                catch (Exception ex)
                {

                }
            };

            HttpClient sentHttpClient = new HttpClient();
            var sentResponse = await sentHttpClient.SendAsync(BuildSentimentRequest(documents));
            var sentResult = await sentResponse.Content.ReadAsStringAsync();
            var sentimentResults = JsonConvert.DeserializeObject<SentimentResult>(sentResult);

            StringBuilder sb = new StringBuilder();

            sentimentResults.documents.ToList().ForEach(r =>
            {
                string msg = data.Where(x => x.Id == r.id).FirstOrDefault().Who + " was " + (r.score > 0.5 ? "a nice kid." : "a naughty kid.");
                sb.Append(msg).AppendLine().AppendLine();
            });

            return (ActionResult)new OkObjectResult(sb.ToString());
        }

        private static HttpRequestMessage BuildLangDetectionRequest(string message)
        {
            object[] body = new object[] { new { Text = message } };
            var requestBody = JsonConvert.SerializeObject(body);

            HttpRequestMessage rm = new HttpRequestMessage();
            rm.Method = HttpMethod.Post;
            rm.RequestUri = new Uri("https://api.cognitive.microsofttranslator.com/detect?api-version=3.0");
            rm.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            rm.Headers.Add("Ocp-Apim-Subscription-Key", "95998ec30920498cabff293ad572139e");

            return rm;
        }

        private static HttpRequestMessage BuildTranslateRequest(string message)
        {
            object[] body = new object[] { new { Text = message } };
            var requestBody = JsonConvert.SerializeObject(body);

            HttpRequestMessage rm = new HttpRequestMessage();
            rm.Method = HttpMethod.Post;
            rm.RequestUri = new Uri("https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=en");
            rm.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            rm.Headers.Add("Ocp-Apim-Subscription-Key", "95998ec30920498cabff293ad572139e");

            return rm;
        }

        private static HttpRequestMessage BuildSentimentRequest(List<Document> messages)
        {
            var requestBody = JsonConvert.SerializeObject(new SentimentRequest { documents = messages });

            HttpRequestMessage rm = new HttpRequestMessage();
            rm.Method = HttpMethod.Post;
            rm.RequestUri = new Uri("https://naughtynice.cognitiveservices.azure.com/text/analytics/v2.1/sentiment");
            rm.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            rm.Headers.Add("Ocp-Apim-Subscription-Key", "");

            return rm;
        }
    }

    public class Wish
    {
        public string Who { get; set; }
        public string Message { get; set; }
        public string Id { get; set; }
    }

    public class LanguageResult
    {
        public string Language { get; set; }
        public float Score { get; set; }
        public bool IsTranslationSupported { get; set; }
        public bool IsTransliterationSupported { get; set; }
        public AltTranslations[] Alternatives { get; set; }
    }

    public class AltTranslations
    {
        public string Language { get; set; }
        public float Score { get; set; }
        public bool IsTranslationSupported { get; set; }
        public bool IsTransliterationSupported { get; set; }
    }

    public class TranslationResult
    {
        public DetectedLanguage DetectedLanguage { get; set; }
        public TextResult SourceText { get; set; }
        public Translation[] Translations { get; set; }
    }

    public class DetectedLanguage
    {
        public string Language { get; set; }
        public float Score { get; set; }
    }

    public class TextResult
    {
        public string Text { get; set; }
        public string Script { get; set; }
    }

    public class Translation
    {
        public string Text { get; set; }
        public TextResult Transliteration { get; set; }
        public string To { get; set; }
        public Alignment Alignment { get; set; }
        public SentenceLength SentLen { get; set; }
    }

    public class Alignment
    {
        public string Proj { get; set; }
    }

    public class SentenceLength
    {
        public int[] SrcSentLen { get; set; }
        public int[] TransSentLen { get; set; }
    }

    public class Document
    {
        public string language = "en";
        public string id { get; set; }
        public string text { get; set; }
        public double score { get; set; }
    }

    public class SentimentRequest
    {
        public List<Document> documents { get; set; }
    }

    public class SentimentResult
    {
        public Document[] documents { get; set; }
        public object[] errors { get; set; }
    }
}
