// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
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

        private Container submissionProgress = null!;
        private SubmissionStageProgress exportStep = null!;
        private SubmissionStageProgress createSetStep = null!;
        private SubmissionStageProgress uploadStep = null!;
        private Container successContainer = null!;
        private Container flashLayer = null!;
        private RoundedButton backButton = null!;

        private uint? beatmapSetId;

        private SubmissionBeatmapExporter legacyBeatmapExporter = null!;
        private ProgressNotification? progressNotification;
        private MemoryStream beatmapPackageStream = null!;

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
            var createRequest = new CreateBeatmapSetRequest((uint)Beatmap.Value.BeatmapSetInfo.Beatmaps.Count);

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
            legacyBeatmapExporter.ExportToStreamAsync(Beatmap.Value.BeatmapSetInfo.ToLive(realmAccess), beatmapPackageStream, progressNotification = new ProgressNotification())
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

                                     progressNotification = null;
                                 });
            exportStep.Status.Value = SubmissionStageProgress.StageStatusType.InProgress;
        }

        private void uploadBeatmapSet()
        {
            Debug.Assert(beatmapSetId != null);

            var uploadRequest = new PutBeatmapSetRequest(beatmapSetId.Value, beatmapPackageStream.ToArray());

            uploadRequest.Success += () =>
            {
                uploadStep.Status.Value = SubmissionStageProgress.StageStatusType.Completed;

                if (configManager.Get<bool>(OsuSetting.EditorSubmissionLoadInBrowserAfterSubmission))
                    game?.OpenUrlExternally($"{api.WebsiteRootUrl}/beatmapsets/{beatmapSetId}");

                backButton.Enabled.Value = true;
                showBeatmapCard();
                // TODO: probably redownload at this point
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

            if (progressNotification != null && progressNotification.Ongoing)
                exportStep.Progress.Value = progressNotification.Progress;
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            overlay.Show();
        }
    }
}
