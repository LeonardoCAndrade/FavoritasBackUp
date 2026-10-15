using Android.Content.PM;
using Android.OS;
using AndroidX.Core.Content;

namespace FavoritasBackUp.Platforms.Android.Services;

internal enum PhotoLibraryAccess { Denied, Partial, Full }

internal sealed class PhotoLibraryPermission : Permissions.BasePlatformPermission
{
    private const string ReadImages = "android.permission.READ_MEDIA_IMAGES";
    private const string ReadSelected = "android.permission.READ_MEDIA_VISUAL_USER_SELECTED";
    private const string ReadStorage = "android.permission.READ_EXTERNAL_STORAGE";

    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake
            ? [(ReadImages, true), (ReadSelected, true)]
            : Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu
                ? [(ReadImages, true)]
                : [(ReadStorage, true)];

    internal static PhotoLibraryAccess GetAccess()
    {
        var context = global::Android.App.Application.Context;
        var fullPermission = Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu ? ReadImages : ReadStorage;
        if (ContextCompat.CheckSelfPermission(context, fullPermission) == Permission.Granted)
            return PhotoLibraryAccess.Full;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake &&
            ContextCompat.CheckSelfPermission(context, ReadSelected) == Permission.Granted)
            return PhotoLibraryAccess.Partial;
        return PhotoLibraryAccess.Denied;
    }
}
