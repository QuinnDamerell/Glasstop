using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Media.Streaming.Adaptive;

namespace Glasstop
{
    internal static class Settings
    {
        // Returns the current image or null if there is none
        public static string CurrentImageId
        {
            get
            {
                var v = Windows.Storage.ApplicationData.Current.LocalSettings.Values[c_CurrentImageIdKey];
                if(v == null)
                {
                    return null;
                }
                return v.ToString();
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values[c_CurrentImageIdKey] = value;
            }
        }
        const string c_CurrentImageIdKey = "CurrentImageId";

        // Returns the search queries string or the default
        public static string SearchQueries
        {
            get
            {
                var v = Windows.Storage.ApplicationData.Current.LocalSettings.Values[c_SearchQueiresKey];
                if(v == null)
                {
                    return "Mountians Purple, Nature Water";
                }
                return v.ToString();
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values[c_SearchQueiresKey] = value;
            }
        }
        const string c_SearchQueiresKey = "SearchQueries";




        // From the current image ID, this returns the next image.
        // If this is the most recent image, it returns null.
        public static string GetNextImageId()
        {
            var currentId = CurrentImageId;
            var list = GetImageIdHistory();
            if(list == null || currentId == null)
            {
                return null;
            }
            bool returnNext = false;
            foreach(var s in list)
            {
                if(returnNext)
                {
                    return s;
                }
                if(s == currentId)
                {
                    returnNext = true;
                }
            }
            return null;
        }


        // From the current image ID, this returns the last image.
        // If this is the last image, it returns null.
        public static string GetLastImageId()
        {
            var currentId = CurrentImageId;
            var list = GetImageIdHistory();
            if(list == null || currentId == null)
            {
                return null;
            }
            string lastImageId = null;
            foreach(var s in list)
            {
                if(s == currentId)
                {
                    break;
                }
                lastImageId = s;
            }
            return lastImageId;
        }

        const string c_ImageIdHistoryKey = "ImageIdHistory";



        // Adds this image to the image id history, if it's not already there.
        public static void AddToImageIdHistory(string id, bool newest = true)
        {
            // If there is no list, make it now.
            var list = GetImageIdHistory();
            if(list == null || list.Count == 0)
            {
                SetImageIdHistory(new List<string>() { id });
                return;
            }
            foreach(var s in list)
            {
                if(s == id)
                {
                    return;
                }
            }
            if(newest)
            {
                list.Add(id);
            }
            else
            {
                list.Insert(0, id);
            }
            SetImageIdHistory(list);
        }


        public static void TrimHistoryOnAppStart()
        {
            // Get the list.
            var list = GetImageIdHistory();
            if(list == null)
            {
                return;
            }

            // Remove the oldest, aka the ones at the start
            // We remove them and then write the history back before we async delete them.
            List<string> toRemove = new List<string>();
            while(list.Count > 40)
            {
                toRemove.Add(list[0]);
                list.RemoveAt(0);
            }
            SetImageIdHistory(list);

            // Now, async, try to delete the files.
            Task.Run(async () =>
            {
                foreach(var id in toRemove)
                {
                    await Unsplash.RemoveImageAndContextFiles(id);
                }
            });
        }


        // Returns null if there is no list or it fails.
        private static List<string> GetImageIdHistory()
        {
            try
            {
                var v = Windows.Storage.ApplicationData.Current.LocalSettings.Values[c_ImageIdHistoryKey];
                if(v == null)
                {
                    return null;
                }
                var json = v.ToString();
                return JsonSerializer.Deserialize<List<string>>(json);
            }
            catch(Exception e)
            {
                Logger.Error("Failed to parse history list.", e);
            }
            return null;
        }

        private static void SetImageIdHistory(List<string> list)
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values[c_ImageIdHistoryKey] = JsonSerializer.Serialize(list);
        }
    }
}
