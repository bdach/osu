// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.IO.Archives;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Screens.Menu;
using osuTK;

namespace osu.Game.Screens.Edit.Submission
{
    // TODO: it should be mentioned somewhere that this process is partially destructive due to backwards compatibility
    public partial class GuestBeatmapSubmissionScreen : OsuScreen
    {
        public override bool AllowBackButton => false;

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        [Resolved]
        private RealmAccess realmAccess { get; set; } = null!;

        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuConfigManager configManager { get; set; } = null!;

        [Resolved]
        private OsuGame? game { get; set; }

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        private Container submissionProgress = null!;
        private SubmissionStageProgress exportStep = null!;
        private SubmissionStageProgress createSetStep = null!;
        private SubmissionStageProgress uploadStep = null!;
        private SubmissionStageProgress updateStep = null!;
        private Container successContainer = null!;
        private Container flashLayer = null!;
        private RoundedButton backButton = null!;

        private SubmissionBeatmapExporter legacyBeatmapExporter = null!;
        private ProgressNotification? exportProgressNotification;
        private MemoryStream beatmapPackageStream = null!;
        private ProgressNotification? updateProgressNotification;

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRangeInternal(new Drawable[]
            {
                submissionProgress = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Width = 0.6f,
                    Masking = true,
                    CornerRadius = 10,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background5,
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Padding = new MarginPadding(20),
                            Spacing = new Vector2(5),
                            Children = new Drawable[]
                            {
                                exportStep = new SubmissionStageProgress
                                {
                                    StageDescription = BeatmapSubmissionStrings.ExportingBeatmapSet,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                },
                                uploadStep = new SubmissionStageProgress
                                {
                                    StageDescription = BeatmapSubmissionStrings.UploadingBeatmapSetContents,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                },
                                updateStep = new SubmissionStageProgress
                                {
                                    StageDescription = BeatmapSubmissionStrings.UpdatingLocalBeatmap,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                },
                                successContainer = new Container
                                {
                                    Padding = new MarginPadding(20),
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    AutoSizeAxes = Axes.Both,
                                    AutoSizeDuration = 500,
                                    AutoSizeEasing = Easing.OutQuint,
                                    Masking = true,
                                    CornerRadius = BeatmapCard.CORNER_RADIUS,
                                    Child = flashLayer = new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Masking = true,
                                        CornerRadius = BeatmapCard.CORNER_RADIUS,
                                        Depth = float.MinValue,
                                        Alpha = 0,
                                        Child = new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                        }
                                    }
                                },
                                backButton = new RoundedButton
                                {
                                    Text = CommonStrings.Back,
                                    Width = 150,
                                    Action = this.Exit,
                                    Enabled = { Value = false },
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                }
                            }
                        }
                    }
                }
            });

            beatmapPackageStream = new MemoryStream();
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            createBeatmapPackage();
        }

        private void createBeatmapPackage()
        {
            legacyBeatmapExporter = new SubmissionBeatmapExporter(storage);
            legacyBeatmapExporter.ExportToStreamAsync(Beatmap.Value.BeatmapSetInfo.ToLive(realmAccess), beatmapPackageStream, exportProgressNotification = new ProgressNotification())
                                 .ContinueWith(t =>
                                 {
                                     if (t.IsFaulted)
                                     {
                                         exportStep.Status.Value = SubmissionStageProgress.StageStatusType.Failed; // TODO: probably show error
                                         Logger.Log($"Beatmap set submission failed on export: {t.Exception}");
                                         Schedule(() => backButton.Enabled.Value = true);
                                     }
                                     else
                                     {
                                         exportStep.Status.Value = SubmissionStageProgress.StageStatusType.Completed;

                                         patchBeatmap();
                                     }

                                     exportProgressNotification = null;
                                 });
            exportStep.Status.Value = SubmissionStageProgress.StageStatusType.InProgress;
        }

        private void patchBeatmap()
        {
            Debug.Assert(Beatmap.Value.BeatmapSetInfo.OnlineID > 0 && Beatmap.Value.BeatmapInfo.OnlineID > 0);

            // TODO: this can't be disposed until done with `beatmapPackageStream`. probably dispose of in `Dispose()` or something
            var archiveReader = new ZipArchiveReader(beatmapPackageStream);
            var patchRequest = new PatchBeatmapRequest(
                (uint)Beatmap.Value.BeatmapSetInfo.OnlineID,
                (uint)Beatmap.Value.BeatmapInfo.OnlineID,
                Beatmap.Value.BeatmapInfo.File!.Filename,
                archiveReader.GetStream(Beatmap.Value.BeatmapInfo.File!.Filename).ReadAllBytesToArray());
            patchRequest.Success += () =>
            {
                uploadStep.Status.Value = SubmissionStageProgress.StageStatusType.Completed;

                if (configManager.Get<bool>(OsuSetting.EditorSubmissionLoadInBrowserAfterSubmission))
                    game?.OpenUrlExternally($"{api.WebsiteRootUrl}/beatmaps/{patchRequest.BeatmapID}");

                updateLocalBeatmap();
            };
            patchRequest.Failure += ex =>
            {
                uploadStep.Status.Value = SubmissionStageProgress.StageStatusType.Failed;
                Logger.Log($"Beatmap submission failed on upload: {ex}");
                backButton.Enabled.Value = true;
            }; // TODO: probably show error
            patchRequest.Progressed += (current, total) => uploadStep.Progress.Value = (float)current / total;

            api.Queue(patchRequest);
            uploadStep.Status.Value = SubmissionStageProgress.StageStatusType.InProgress;
        }

        private void updateLocalBeatmap()
        {
            Debug.Assert((uint)Beatmap.Value.BeatmapSetInfo.OnlineID > 0 && Beatmap.Value.BeatmapInfo.OnlineID > 0);

            // TODO: this is broken by differential update. there is no archive available in that case.
            beatmaps.ImportAsUpdate(
                        updateProgressNotification = new ProgressNotification(),
                        new ImportTask(beatmapPackageStream, $"{Beatmap.Value.BeatmapSetInfo.OnlineID}.osz"),
                        Beatmap.Value.BeatmapSetInfo)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            updateStep.Status.Value = SubmissionStageProgress.StageStatusType.Failed;
                            Logger.Log($"Beatmap submission failed on local update: {t.Exception}");
                            Schedule(() => backButton.Enabled.Value = true);
                            return; // TODO: probably show error
                        }

                        updateStep.Status.Value = SubmissionStageProgress.StageStatusType.Completed;
                        Schedule(() =>
                        {
                            backButton.Enabled.Value = true;
                            backButton.Action = () =>
                            {
                                game?.PerformFromScreen(s =>
                                {
                                    if (s is OsuScreen osuScreen)
                                    {
                                        BeatmapSetInfo importedSet = t.GetResultSafely()!.Value;
                                        var targetBeatmap = importedSet.Beatmaps.FirstOrDefault(b => b.DifficultyName == Beatmap.Value.BeatmapInfo.DifficultyName) ?? importedSet.Beatmaps.First();
                                        osuScreen.Beatmap.Value = beatmaps.GetWorkingBeatmap(targetBeatmap);
                                    }

                                    s.Push(new EditorLoader());
                                }, [typeof(MainMenu)]);
                            };
                        });

                        showBeatmapCard();
                    });
            updateStep.Status.Value = SubmissionStageProgress.StageStatusType.InProgress;
        }

        private void showBeatmapCard()
        {
            Debug.Assert((uint)Beatmap.Value.BeatmapSetInfo.OnlineID > 0 && Beatmap.Value.BeatmapInfo.OnlineID > 0);

            var getBeatmapSetRequest = new GetBeatmapSetRequest(Beatmap.Value.BeatmapSetInfo.OnlineID);
            getBeatmapSetRequest.Success += beatmapSet =>
            {
                LoadComponentAsync(new BeatmapCardExtra(beatmapSet, false), loaded =>
                {
                    successContainer.Add(loaded);
                    flashLayer.FadeOutFromOne(2000, Easing.OutQuint);
                });
            };

            api.Queue(getBeatmapSetRequest);
        }

        protected override void Update()
        {
            base.Update();

            if (exportProgressNotification != null && exportProgressNotification.Ongoing)
                exportStep.Progress.Value = exportProgressNotification.Progress;

            if (updateProgressNotification != null && updateProgressNotification.Ongoing)
                updateStep.Progress.Value = updateProgressNotification.Progress;
        }
    }
}
