using Android.Media;
using Appinventario.Interfaces;

[assembly: Microsoft.Maui.Controls.Dependency(typeof(Appinventario.Platforms.Android.AudioService))]

namespace Appinventario.Platforms.Android;

public class AudioService : IAudio
{
    public void PlayAudioFile(string fileName)
    {
        using var player = new MediaPlayer();
        using var fd = global::Android.App.Application.Context.Assets.OpenFd(fileName);

        player.Prepared += (_, _) => player.Start();
        player.SetDataSource(fd.FileDescriptor, fd.StartOffset, fd.Length);
        player.Prepare();
    }
}
