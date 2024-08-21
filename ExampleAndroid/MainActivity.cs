namespace ExampleAndroid
{
    using Android.Content.PM;
    using AndroidX.Loader.Content;
    using System.Runtime.InteropServices;

    [Activity(Label = "ExampleAndroid", MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@style/AppTheme")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Get the application info
            ApplicationInfo appInfo = Application.Context.ApplicationInfo;

            // Get the native library directory path
            string nativeLibsPath = appInfo.NativeLibraryDir;

            string[] files = Directory.GetFiles(nativeLibsPath);

            for (int i = 0; i < files.Length; i++)
            {
                files[i] = files[i].Substring(nativeLibsPath.Length);
            }

            string libraryPath = $"{nativeLibsPath}/libSDL2-2.0.so";

            nint handle;

            handle = NativeLibrary.Load(libraryPath);

            Program.Main([]);
        }
    }
}