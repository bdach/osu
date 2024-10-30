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
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics.UserInterfaceV2;
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
    public partial class BeatmapSubmissionScreen : OsuScreen
    {
        private BeatmapSubmissionOverlay overlay = null!;

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

        private uint? beatmapSetId;

        private SubmissionBeatmapExporter legacyBeatmapExporter = null!;
        private ProgressNotification? exportProgressNotification;
        private MemoryStream beatmapPackageStream = null!;
        private ProgressNotification? updateProgressNotification;

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRangeInternal(new Drawable[]
            {
                overlay = new BeatmapSubmissionOverlay(),
                submissionProgress = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Alpha = 0,
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
                                createSetStep = new SubmissionStageProgress
                                {
                                    StageDescription = BeatmapSubmissionStrings.CreatingBeatmapSet,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                },
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

            overlay.State.BindValueChanged(_ =>
            {
                if (overlay.State.Value == Visibility.Hidden)
                {
                    if (!overlay.Completed)
                        this.Exit();
                    else
                    {
                        submissionProgress.FadeIn(200, Easing.OutQuint);
                        createBeatmapSet();
                    }
                }
            });
            beatmapPackageStream = new MemoryStream();
        }

        private void createBeatmapSet()
        {
            var createRequest = Beatmap.Value.BeatmapSetInfo.OnlineID > 0
                ? PutBeatmapSetRequest.UpdateExisting(
                    (uint)Beatmap.Value.BeatmapSetInfo.OnlineID,
                    Beatmap.Value.BeatmapSetInfo.Beatmaps.Where(b => b.OnlineID > 0).Select(b => (uint)b.OnlineID).ToArray(),
                    (uint)Beatmap.Value.BeatmapSetInfo.Beatmaps.Count(b => b.OnlineID <= 0))
                : PutBeatmapSetRequest.CreateNew((uint)Beatmap.Value.BeatmapSetInfo.Beatmaps.Count);

            createRequest.Success += response =>
            {
                createSetStep.Status.Value = SubmissionStageProgress.StageStatusType.Completed;
                beatmapSetId = response.BeatmapSetId;
                legacyBeatmapExporter = new SubmissionBeatmapExporter(storage, response);
                createBeatmapPackage();
            };
            createRequest.Failure += _ =>
            {
                createSetStep.Status.Value = SubmissionStageProgress.StageStatusType.Failed;
                backButton.Enabled.Value = true;
            }; // TODO: probably show & log error

            createSetStep.Status.Value = SubmissionStageProgress.StageStatusType.InProgress;
            api.Queue(createRequest);
        }

        private void createBeatmapPackage()
        {
            legacyBeatmapExporter.ExportToStreamAsync(Beatmap.Value.BeatmapSetInfo.ToLive(realmAccess), beatmapPackageStream, exportProgressNotification = new ProgressNotification())
                                 .ContinueWith(t =>
                                 {
                                     if (t.IsFaulted)
                                     {
                                         exportStep.Status.Value = SubmissionStageProgress.StageStatusType.Failed; // TODO: probably show & log error
                                         Schedule(() => backButton.Enabled.Value = true);
                                     }
                                     else
                                     {
                                         exportStep.Status.Value = SubmissionStageProgress.StageStatusType.Completed;
                                         uploadBeatmapSet();
                                     }

                                     exportProgressNotification = null;
                                 });
            exportStep.Status.Value = SubmissionStageProgress.StageStatusType.InProgress;
        }

        private void uploadBeatmapSet()
        {
            Debug.Assert(beatmapSetId != null);

            var uploadRequest = new ReplaceBeatmapSetRequest(beatmapSetId.Value, beatmapPackageStream.ToArray());

            uploadRequest.Success += () =>
            {
                uploadStep.Status.Value = SubmissionStageProgress.StageStatusType.Completed;

                if (configManager.Get<bool>(OsuSetting.EditorSubmissionLoadInBrowserAfterSubmission))
                    game?.OpenUrlExternally($"{api.WebsiteRootUrl}/beatmapsets/{beatmapSetId}");

                updateLocalBeatmap();
            };
            uploadRequest.Failure += _ =>
            {
                uploadStep.Status.Value = SubmissionStageProgress.StageStatusType.Failed;
                backButton.Enabled.Value = true;
            }; // TODO: probably show & log error
            uploadRequest.Progressed += (current, total) => uploadStep.Progress.Value = (float)current / total;

            api.Queue(uploadRequest);
            uploadStep.Status.Value = SubmissionStageProgress.StageStatusType.InProgress;
        }

        private void updateLocalBeatmap()
        {
            Debug.Assert(beatmapSetId != null);

            beatmaps.ImportAsUpdate(
                        updateProgressNotification = new ProgressNotification(),
                        new ImportTask(beatmapPackageStream, $"{beatmapSetId}.osz"),
                        Beatmap.Value.BeatmapSetInfo)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            updateStep.Status.Value = SubmissionStageProgress.StageStatusType.Failed;
                            Schedule(() => backButton.Enabled.Value = true);
                            return; // TODO: probably show & log error
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
            Debug.Assert(beatmapSetId != null);

            var getBeatmapSetRequest = new GetBeatmapSetRequest((int)beatmapSetId.Value);
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

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            overlay.Show();
        }
    }
}
