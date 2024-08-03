using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.UserProfile;

namespace Glasstop
{
    internal static class ImageSetter
    {
        public static bool HasSupport()
        {
            return UserProfilePersonalizationSettings.IsSupported();
        }

        public static async Task<bool> SetImage(StorageFile file, bool desktop = true, bool lockscreen = true)
        {
            try
            {
                if(!UserProfilePersonalizationSettings.IsSupported())
                {
                    return false;
                }

                bool status = false;
                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                if(desktop)
                { 
                    status = await profileSettings.TrySetWallpaperImageAsync(file);
                }
                if(lockscreen)
                {
                    status = await profileSettings.TrySetLockScreenImageAsync(file);
                    await LockScreen.SetImageFileAsync(file);
                }
                return status;
            }
            catch(Exception ex)
            {
                Logger.Error($"Failed to set image to wallpaper or lock screen.", ex);
            }
            return false;
        }
    }
}
