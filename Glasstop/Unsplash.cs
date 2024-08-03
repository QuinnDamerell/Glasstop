using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Windows.Media.Streaming.Adaptive;
using Windows.Storage;

namespace Glasstop
{
    internal static class Unsplash
    {
        const string c_accessToken = "ZQmI1OWq3V89Q-xU8KHCoZXViBgX06tGMVvIMBuEscM";
        readonly static HttpClient s_httpClient = new HttpClient();


        // Returns a random photo context from the API.
        public static async Task<UnsplashPhotoContext> GetRandomImage(string searchQuery)
        {
            try
            {
                // Note that we DONT specify the count arg, or the result is an array, even if we say 1.
                // By default the API will return 1 image, which is what HandleImageContextResponse expects.
                var args = new Dictionary<string, string>();
                if(searchQuery != null)
                {
                    args["query"] = searchQuery;
                }

                // Make the request.
                var result = await MakeRequest("photos/random", args);
                return await HandleImageContextResponse(result);
            }
            catch(Exception e)
            {
                Logger.Error("GetRandomImage failed", e);
            }
            return null;
        }


        // Gets an image context from the local cache or the API.
        public static async Task<UnsplashPhotoContext> GetImage(string id)
        {
            try
            {
                // See if we have it cached on disk.
                var c = await TryReadPhotoContextToDisk(id);
                if(c != null)
                {
                    return c;
                }

                // Make the request.
                var result = await MakeRequest($"photos/{id}");
                return await HandleImageContextResponse(result);
            }
            catch(Exception e)
            {
                Logger.Error("GetRandomImage failed", e);
            }
            return null;

        }

        // Get an image file given the context
        public static async Task<StorageFile> GetImageFile(UnsplashPhotoContext c)
        {
            try
            {
                // First see if we have it on disk.
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                string imagePath = GetImagePath(c.Id);
                try
                {
                    return await storageFolder.GetFileAsync(imagePath);
                }
                catch(Exception e)
                {
                    Logger.Info($"Failed to get image {c.Id}, re-downloading.", e);
                }

                // Download.
                var result = await s_httpClient.GetAsync(c.Urls.Raw);
                if(result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Failed to download {c.Id} {result.StatusCode}");
                }

                // Save it.
                var file = await storageFolder.CreateFileAsync(imagePath, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(file, await result.Content.ReadAsByteArrayAsync());
                return file;
            }
            catch(Exception e)
            {
                Logger.Error("GetImageFile failed", e);
            }
            return null;
        }


        // Best effort ties to remove the files.
        public static async Task RemoveImageAndContextFiles(string id)
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            try
            {
                StorageFile file = await storageFolder.GetFileAsync(GetImagePath(id));
                await file.DeleteAsync();
            }
            catch(Exception e)
            { }
            try
            {
                StorageFile file = await storageFolder.GetFileAsync(GetContextPath(id));
                await file.DeleteAsync();
            }
            catch(Exception e)
            { }
        }


        private static string GetImagePath(string id)
        {
            return $"images\\{id}";
        }


        private static string GetContextPath(string id)
        {
            return $"contexts\\{id}";
        }


        // apiPath is the GET path to the API excluding the /
        private static async Task<HttpResponseMessage> MakeRequest(string apiPath, Dictionary<string, string> getParams = null)
        {
            const string c_baseUrl = "https://api.unsplash.com/";
            const string c_accessKeyGetParam = "?client_id="+ c_accessToken;
            string request = c_baseUrl + apiPath + c_accessKeyGetParam;
            if(getParams != null)
            {
                foreach(var kvp in getParams)
                {
                    request += $"&{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}";
                }
            }
            var result = await s_httpClient.GetAsync(request);
            result.EnsureSuccessStatusCode();
            return result;
        }


        private static async Task<UnsplashPhotoContext> HandleImageContextResponse(HttpResponseMessage result)
        {
            // Parse the response.
            if(result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Status is not 200");
            }
            var str = await result.Content.ReadAsStringAsync();
            var c = JsonSerializer.Deserialize<UnsplashPhotoContext>(str);

            // Always write the context to disk, so we have it cached.
            if(!await WritePhotoContextToDisk(c))
            {
                throw new Exception("Failed to write image context to disk");
            }
            return c;
        }


        private static async Task<bool> WritePhotoContextToDisk(UnsplashPhotoContext c)
        {
            try
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = await storageFolder.CreateFileAsync(GetContextPath(c.Id), CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, JsonSerializer.Serialize(c));
                return true;
            }
            catch(Exception e)
            {
                Logger.Error("WritePhotoContextToDisk failed", e);
            }
            return false;
        }


        private static async Task<UnsplashPhotoContext> TryReadPhotoContextToDisk(string id)
        {
            try
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                var file = await storageFolder.GetFileAsync(GetContextPath(id));
                var txt = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<UnsplashPhotoContext>(txt);
            }
            catch(Exception e)
            {
                Logger.Error("TryReadPhotoContextToDisk failed", e);
            }
            return null;
        }
    }


    // These objects are what's returned by the API, but also the data format we use locally.
    class UnsplashPhotoContext
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; } // Ex #6E633A

        [JsonPropertyName("downloads")]
        public int? Downloads { get; set; }

        [JsonPropertyName("likes")]
        public int? Likes { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("location")]
        public UnsplashLocation Location { get; set; }

        [JsonPropertyName("urls")]
        public UnsplashUrls Urls { get; set; }

        [JsonPropertyName("links")]
        public UnsplashLinks Links { get; set; }

        [JsonPropertyName("user")]
        public UnsplashUser User { get; set; }
    }

    class UnsplashUrls
    {
        [JsonPropertyName("raw")]
        public string Raw { get; set; }

        [JsonPropertyName("full")]
        public string Full { get; set; }

        [JsonPropertyName("regular")]
        public string Regular { get; set; }

        [JsonPropertyName("small")]
        public string Small { get; set; }
    }

    class UnsplashUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("portfolio_url")]
        public string PortfolioUrl { get; set; }
    }

    class UnsplashLinks
    {
        [JsonPropertyName("html")]
        public string Html { get; set; }
    }

    class UnsplashLocation
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("position")]
        public UnsplashCoordinates Position { get; set; }
   
    }

    class UnsplashCoordinates
    {
        [JsonPropertyName("latitude")]
        public float? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public float? Longitude { get; set; }
    }
}
