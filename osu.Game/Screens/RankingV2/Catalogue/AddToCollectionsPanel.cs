// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Collections;
using osu.Game.Database;
using osu.Game.Extensions;
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
        private SearchContainer searchContainer = null!;
        private SearchTextBox searchTextBox = null!;

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
                        searchContainer = new SearchContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = 30, Bottom = 45, },
                            Children =
                            [
                                new OsuScrollContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    ScrollbarOverlapsContent = false,
                                    Child = collectionFlow = new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(5),
                                    },
                                },
                            ],
                        },
                        searchTextBox = new SearchTextBox
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            RelativeSizeAxes = Axes.X,
                        }
                    ],
                },
            ];

            realm.Run(r =>
            {
                foreach (var collection in r.All<BeatmapCollection>())
                    collectionFlow.Add(new DrawableCollection(collection.ToLive(realm)));
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            searchTextBox.Current.BindValueChanged(_ => searchContainer.SearchTerm = searchTextBox.Current.Value, true);
        }

        private partial class DrawableCollection : CompositeDrawable, IFilterable
        {
            private readonly Live<BeatmapCollection> collection;

            private Box background = null!;
            private OsuSpriteText label = null!;
            private ShearedToggleButton toggleDifficultyButton = null!;
            private ShearedToggleButton toggleSetButton = null!;

            public DrawableCollection(Live<BeatmapCollection> collection)
            {
                this.collection = collection;
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
            }

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            [Resolved]
            private IBindable<IScoreInfo> score { get; set; } = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Masking = true;
                CornerRadius = ShearedButton.CORNER_RADIUS;

                InternalChildren =
                [
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        RowDimensions = [new Dimension(GridSizeMode.AutoSize)],
                        Padding = new MarginPadding { Left = 10, Vertical = 3, },
                        ColumnDimensions =
                        [
                            new Dimension(),
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.AutoSize),
                        ],
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                label = new OsuSpriteText
                                {
                                    Text = collection.PerformRead(c => c.Name),
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                },
                                toggleDifficultyButton = new ShearedToggleButton
                                {
                                    Text = "Difficulty",
                                    Width = 100,
                                    Height = 30,
                                    Action = toggleDifficulty,
                                },
                                toggleSetButton = new ShearedToggleButton
                                {
                                    Text = "Whole set",
                                    Width = 100,
                                    Height = 30,
                                    Action = toggleSet,
                                },
                            }
                        }
                    }
                ];
            }

            private void toggleDifficulty()
            {
                collection.PerformWrite(col =>
                {
                    string? hash = score.Value.Beatmap?.MD5Hash;

                    if (string.IsNullOrEmpty(hash)) return;

                    if (!col.BeatmapMD5Hashes.Remove(hash))
                        col.BeatmapMD5Hashes.Add(hash);
                });
                updateState();
            }

            private void toggleSet()
            {
                collection.PerformWrite(col =>
                {
                    var setHashes = score.Value.Beatmap?.BeatmapSet?.Beatmaps.Select(b => b.MD5Hash).ToHashSet();

                    if (setHashes == null) return;

                    if (setHashes.IsSubsetOf(col.BeatmapMD5Hashes))
                    {
                        foreach (string hash in setHashes)
                            col.BeatmapMD5Hashes.Remove(hash);
                    }
                    else
                    {
                        setHashes.ExceptWith(col.BeatmapMD5Hashes);
                        col.BeatmapMD5Hashes.AddRange(setHashes);
                    }
                });
                updateState();
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                updateState();
            }

            private void updateState()
            {
                collection.PerformRead(col =>
                {
                    if (score.Value.Beatmap == null)
                        return;

                    toggleDifficultyButton.Active.Value = col.BeatmapMD5Hashes.Contains(score.Value.Beatmap.MD5Hash);

                    var setHashes = score.Value.Beatmap?.BeatmapSet?.Beatmaps.Select(b => b.MD5Hash).ToHashSet();

                    if (setHashes == null)
                        return;

                    toggleSetButton.Active.Value = setHashes.IsSubsetOf(col.BeatmapMD5Hashes);
                });
            }

            public IEnumerable<LocalisableString> FilterTerms => [collection.PerformRead(col => col.Name)];

            public bool MatchingFilter
            {
                set => Alpha = value ? 1 : 0;
            }

            public bool FilteringActive { set { } }
        }
    }
}
