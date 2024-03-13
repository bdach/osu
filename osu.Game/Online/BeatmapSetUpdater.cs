// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Screens.Select.Carousel;

namespace osu.Game.Online
{
    /// <summary>
    /// Shared component without visual content which encompasses the UX of updating beatmap sets.
    /// </summary>
    public partial class BeatmapSetUpdater : Component
    {
        [Resolved]
        private BeatmapModelDownloader beatmapDownloader { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private LoginOverlay? loginOverlay { get; set; }

        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }

        private Bindable<bool> preferNoVideo = null!;

        private bool updateConfirmed;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            preferNoVideo = config.GetBindable<bool>(OsuSetting.PreferNoVideo);
        }

        public Task<ArchiveDownloadRequest<IBeatmapSetInfo>?> UpdateBeatmapSet(BeatmapSetInfo beatmapSetInfo)
        {
            if (!api.IsLoggedIn)
            {
                loginOverlay?.Show();
                return Task.FromResult<ArchiveDownloadRequest<IBeatmapSetInfo>?>(null);
            }

            if (dialogOverlay != null && beatmapSetInfo.Status == BeatmapOnlineStatus.LocallyModified && !updateConfirmed)
            {
                var taskCompletionSource = new TaskCompletionSource<ArchiveDownloadRequest<IBeatmapSetInfo>?>();

                dialogOverlay.Push(new UpdateLocalConfirmationDialog(() =>
                {
                    updateConfirmed = true;
                    UpdateBeatmapSet(beatmapSetInfo).ContinueWith(t =>
                    {
                        switch (t.Status)
                        {
                            case TaskStatus.RanToCompletion:
                                taskCompletionSource.SetResult(t.GetResultSafely());
                                break;

                            case TaskStatus.Canceled:
                                taskCompletionSource.SetCanceled();
                                break;

                            case TaskStatus.Faulted:
                                taskCompletionSource.SetException(t.Exception!);
                                break;
                        }
                    });
                }, () => taskCompletionSource.SetCanceled()));

                return taskCompletionSource.Task;
            }

            updateConfirmed = false;

            beatmapDownloader.DownloadAsUpdate(beatmapSetInfo, preferNoVideo.Value);
            return Task.FromResult(beatmapDownloader.GetExistingDownload(beatmapSetInfo));
        }
    }
}
