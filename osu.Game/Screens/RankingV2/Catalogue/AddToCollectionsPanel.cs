// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Collections;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking.Statistics;
using osuTK;

namespace osu.Game.Screens.RankingV2.Catalogue
{
    public partial class AddToCollectionsPanel : CompositeDrawable
    {
        private FillFlowContainer collectionFlow = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            Masking = true;
            CornerRadius = 5;

            InternalChildren =
            [
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.Black,
                    Alpha = 0.6f,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(10),
                    Children =
                    [
                        new StatisticItemHeader
                        {
                            Text = "Add to collections",
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = 30 },
                            Child = new OsuScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = collectionFlow = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(5),
                                },
                            },
                        },
                    ],
                },
            ];

            realm.Run(r =>
            {
                foreach (var collection in r.All<BeatmapCollection>())
                    collectionFlow.Add(new DrawableCollection(collection.ToLive(realm)));
            });
        }

        private partial class DrawableCollection : OsuAnimatedButton
        {
            private readonly Live<BeatmapCollection> collection;

            private Box background = null!;
            private OsuSpriteText label = null!;

            public DrawableCollection(Live<BeatmapCollection> collection)
            {
                this.collection = collection;
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                ScaleOnMouseDown = 0.95f;
            }

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            [Resolved]
            private IBindable<IScoreInfo> score { get; set; } = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                Children =
                [
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    label = new OsuSpriteText
                    {
                        Margin = new MarginPadding(8),
                        Text = collection.PerformRead(c => c.Name),
                    },
                ];

                Action = () =>
                {
                    collection.PerformWrite(col =>
                    {
                        string? hash = score.Value.Beatmap?.MD5Hash;

                        if (!string.IsNullOrEmpty(hash))
                        {
                            if (!col.BeatmapMD5Hashes.Remove(hash))
                                col.BeatmapMD5Hashes.Add(hash);
                        }
                    });
                    updateState();
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                updateState();
            }

            private void updateState()
            {
                string? hash = score.Value.Beatmap?.MD5Hash;
                background.Colour = hash != null && collection.PerformRead(col => col.BeatmapMD5Hashes.Contains(hash))
                    ? colourProvider.Highlight1
                    : colourProvider.Background3;
                label.Colour = OsuColour.ForegroundTextColourFor(background.Colour);
            }
        }
    }
}
