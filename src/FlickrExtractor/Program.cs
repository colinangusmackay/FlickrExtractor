using FlickrNet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;

namespace FlickrExtractor
{
    class Program
    {
        private static string ApiKey => ConfigurationManager.AppSettings["apiKey"];
        private static string SharedSecret => ConfigurationManager.AppSettings["sharedSecret"];

        static void Main(string[] args)
        {
            Flickr flickr = new Flickr(apiKey: ApiKey, sharedSecret: SharedSecret);
            var result = flickr.TestEcho(new Dictionary<string, string>(){{"hello", "world"}});
            Console.WriteLine($"{result.First().Key} : {result.First().Value}");

            GetAuthToken(flickr);


            var pagedCollection = flickr.PeopleGetPhotos("me", 1, 100);
            for (int i = 1; i <= pagedCollection.Pages; i++)
            {
                var collection = pagedCollection;
                if (i != 1)
                    collection = flickr.PeopleGetPhotos("me", i, 100);
                Console.WriteLine($"Photo Collection : {collection.Count}/{collection.Total} ({collection.Page}/{collection.Pages})");
                foreach (var item in collection)
                {
                    Console.WriteLine($"{item.PhotoId} : ");
                    WriteOverviewMetadata(item);
                    var exif = WriteExifData(flickr, item);
                    var info = WriteInfoData(flickr, item);
                    DownloadImage(info);


                    var fullInfo = new
                    {
                        overview = item,
                        exif,
                        info
                    };
                    string path = ConfigurationManager.AppSettings["location"];
                    string metadataPath = Path.Combine(path, $"photos/{item.PhotoId}.json");
                    string json = JsonConvert.SerializeObject(fullInfo, Formatting.Indented);
                    File.WriteAllText(metadataPath, json);
                    Thread.Sleep(2500);
                }
            }

            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        private static void DownloadImage(PhotoInfo info)
        {
            string path = ConfigurationManager.AppSettings["location"];
            try
            {
                string filePath = Path.Combine(path, $"photos/{info.PhotoId}.{info.OriginalFormat}");
                var request = WebRequest.CreateHttp(info.OriginalUrl);
                var response = request.GetResponse();
                using (Stream output = File.OpenWrite(filePath))
                using (Stream input = response.GetResponseStream())
                {
                    input.CopyTo(output);
                }
            }
            catch (Exception ex)
            {
                string exceptionPath = Path.Combine(path, $"photos/{info.PhotoId}-EXCEPTION.txt");
                string message = ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace;
                if (ex.InnerException != null)
                    message += new string('-', 80) + Environment.NewLine + Environment.NewLine + "INNER EXCEPTION : " +
                               Environment.NewLine + ex.InnerException.Message + Environment.NewLine +
                               Environment.NewLine + ex.InnerException.StackTrace;
                File.WriteAllText(exceptionPath, message);
                exceptionPath = Path.Combine(path, $"photos/EXCEPTION-{info.PhotoId}.txt");
                File.WriteAllText(exceptionPath, message);
            }
        }

        private static PhotoInfo WriteInfoData(Flickr flickr, Photo item)
        {
            var info = flickr.PhotosGetInfo(item.PhotoId);
            Console.WriteLine($"  Original URL : {info.OriginalUrl}");
            string path = ConfigurationManager.AppSettings["location"];
            string metadataPath = Path.Combine(path, $"photos/{item.PhotoId}-info.json");
            string json = JsonConvert.SerializeObject(info, Formatting.Indented);
            File.WriteAllText(metadataPath, json);

            return info;
        }


        private static ExifTagCollection WriteExifData(Flickr flickr, Photo item)
        {
            var exif = flickr.PhotosGetExif(item.PhotoId);
            string path = ConfigurationManager.AppSettings["location"];
            string metadataPath = Path.Combine(path, $"photos/{item.PhotoId}-exif.json");
            string json = JsonConvert.SerializeObject(exif, Formatting.Indented);
            File.WriteAllText(metadataPath, json);

            return exif;
        }

        private static void WriteOverviewMetadata(Photo item)
        {
            string path = ConfigurationManager.AppSettings["location"];
            string metadataPath = Path.Combine(path, $"photos/{item.PhotoId}-collection-item.json");
            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            File.WriteAllText(metadataPath, json);
        }

        private static void GetAuthToken(Flickr flickr)
        {
            OAuthAccessToken accessToken = null;
            string path =ConfigurationManager.AppSettings["location"];
            string accessTokenFile = Path.Combine(path, "access-token.json");
            if (File.Exists(accessTokenFile))
            {
                Console.WriteLine("Found access token in file.");
                string json = File.ReadAllText(accessTokenFile);
                accessToken = JsonConvert.DeserializeObject<OAuthAccessToken>(json);
                flickr.OAuthAccessToken = accessToken.Token;
                flickr.OAuthAccessTokenSecret = accessToken.TokenSecret;
                Auth check = flickr.AuthOAuthCheckToken();
                if (check != null)
                    return;
                Console.WriteLine("Token didn't check out.");
            }


            var requestToken = flickr.OAuthGetRequestToken("oob");
            Console.WriteLine($"The request token was {requestToken.Token}");
            string url = $"https://www.flickr.com/services/oauth/authorize?oauth_token={requestToken.Token}";
            Console.WriteLine("About to direct you to " + url);
            Console.WriteLine("Once you have completed authentication with this application you can return here and continue.");
            Process.Start(url);
            Console.WriteLine("Paste the verifier when done:");
            while (accessToken == null)
            {
                Console.Write("Verifier> ");
                string verifier = Console.ReadLine();

                accessToken = flickr.OAuthGetAccessToken(requestToken, verifier);
                if (accessToken == null)
                    Console.WriteLine("Could not get an access token with that verifier. Try again.");
            }

            Console.WriteLine("Saving token to file for future reference.");
            string jsonOut = JsonConvert.SerializeObject(accessToken, Formatting.Indented);
            File.WriteAllText(accessTokenFile, jsonOut);

            Console.WriteLine("Access token details:");
            Console.WriteLine("    Token     : " + accessToken.Token);
            Console.WriteLine("    Secret    : " + accessToken.TokenSecret);
            Console.WriteLine("    Full Name : " + accessToken.FullName);
            Console.WriteLine("    UserId    : " + accessToken.UserId);
            Console.WriteLine("    User Name : " + accessToken.Username);
        }
    }
}
