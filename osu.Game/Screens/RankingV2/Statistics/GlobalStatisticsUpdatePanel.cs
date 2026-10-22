// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Online;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Screens.Ranking.Statistics;
using osu.Game.Utils;

namespace osu.Game.Screens.RankingV2.Statistics
{
    public partial class GlobalStatisticsUpdatePanel : CompositeDrawable
    {
        private StatisticValue globalRank = null!;
        private StatisticValue pp = null!;
        private StatisticValue maxCombo = null!;
        private StatisticValue rankedScore = null!;
        private StatisticValue accuracy = null!;
        private StatisticValue totalScore = null!;

        [Resolved]
        private IBindable<ScoreBasedUserStatisticsUpdate?> statisticsUpdate { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

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
                new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding(10),
                    ColumnDimensions =
                    [
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize),
                    ],
                    RowDimensions =
                    [
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize),
                    ],
                    Content = new[]
                    {
                        new[]
                        {
                            new StatisticItemHeader
                            {
                                Text = "Overall Ranking"
                            },
                            Empty(),
                        },
                        new Drawable[]
                        {
                            globalRank = new StatisticValue
                            {
                                Caption = UsersStrings.ShowRankGlobalSimple,
                                Margin = new MarginPadding { Right = 15, Bottom = 5, },
                            },
                            pp = new StatisticValue
                            {
                                Caption = RankingsStrings.StatPerformance,
                                Margin = new MarginPadding { Bottom = 5, },
                            },
                        },
                        new Drawable[]
                        {
                            maxCombo = new StatisticValue
                            {
                                Caption = UsersStrings.ShowStatsMaximumCombo,
                                Margin = new MarginPadding { Right = 15, Bottom = 5, },
                            },
                            rankedScore = new StatisticValue
                            {
                                Caption = UsersStrings.ShowStatsRankedScore,
                                Margin = new MarginPadding { Bottom = 5, },
                            },
                        },
                        new Drawable[]
                        {
                            accuracy = new StatisticValue
                            {
                                Caption = UsersStrings.ShowStatsHitAccuracy,
                                Margin = new MarginPadding { Right = 15, },
                            },
                            totalScore = new StatisticValue
                            {
                                Caption = UsersStrings.ShowStatsTotalScore,
                            },
                        },
                    },
                },
            ];
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            statisticsUpdate.BindValueChanged(_ => updateState(), true);
        }

        private void updateState()
        {
            if (statisticsUpdate.Value == null)
            {
                Alpha = 0;
                return;
            }

            Alpha = 1;

            globalRank.Value = statisticsUpdate.Value.After.GlobalRank != null
                ? LocalisableString.Interpolate($@"#{statisticsUpdate.Value.After.GlobalRank:N0}")
                : "-";
            int? globalRankDelta = statisticsUpdate.Value.After.GlobalRank - statisticsUpdate.Value.Before.GlobalRank;

            if (globalRankDelta != null)
            {
                (globalRank.Difference, globalRank.DifferenceKind) = globalRankDelta switch
                {
                    < 0 => (LocalisableString.Interpolate($"+{Math.Abs(globalRankDelta.Value):N0}"), StatisticValue.Kind.Gain),
                    > 0 => ((-globalRankDelta).ToLocalisableString(@"N0"), StatisticValue.Kind.Loss),
                    _ => ("0", StatisticValue.Kind.NoChange),
                };
            }
            else
            {
                globalRank.Difference = default;
            }

            pp.Value = statisticsUpdate.Value.After.PP != null
                ? LocalisableString.Interpolate($"{statisticsUpdate.Value.After.PP:N0}pp")
                : "-";

            decimal? ppDelta = statisticsUpdate.Value.After.PP - statisticsUpdate.Value.Before.PP;

            if (ppDelta != null)
            {
                (pp.Difference, pp.DifferenceKind) = ppDelta switch
                {
                    < 0 => (LocalisableString.Interpolate($@"{ppDelta:N0}pp"), StatisticValue.Kind.Loss),
                    > 0 => (LocalisableString.Interpolate($@"+{ppDelta:N0}pp"), StatisticValue.Kind.Gain),
                    _ => (@"+0pp", StatisticValue.Kind.NoChange),
                };
            }

            maxCombo.Value = LocalisableString.Interpolate($"{statisticsUpdate.Value.After.MaxCombo:N0}x");

            int maxComboDelta = statisticsUpdate.Value.After.MaxCombo - statisticsUpdate.Value.Before.MaxCombo;
            (maxCombo.Difference, maxCombo.DifferenceKind) = maxComboDelta switch
            {
                < 0 => (LocalisableString.Interpolate($@"{maxComboDelta:N0}x"), StatisticValue.Kind.Loss),
                > 0 => (LocalisableString.Interpolate($@"+{maxComboDelta:N0}x"), StatisticValue.Kind.Gain),
                _ => (@"+0x", StatisticValue.Kind.NoChange),
            };

            rankedScore.Value = statisticsUpdate.Value.After.RankedScore.ToLocalisableString(@"N0");

            long rankedScoreDelta = statisticsUpdate.Value.After.RankedScore - statisticsUpdate.Value.Before.RankedScore;
            (rankedScore.Difference, rankedScore.DifferenceKind) = rankedScoreDelta switch
            {
                < 0 => (rankedScoreDelta.ToLocalisableString(@"N0"), StatisticValue.Kind.Loss),
                > 0 => (LocalisableString.Interpolate($@"+{rankedScoreDelta:N0}"), StatisticValue.Kind.Gain),
                _ => (@"+0", StatisticValue.Kind.NoChange),
            };

            accuracy.Value = statisticsUpdate.Value.After.Accuracy.FormatAccuracy();

            double accuracyDelta = statisticsUpdate.Value.After.Accuracy - statisticsUpdate.Value.Before.Accuracy;
            (accuracy.Difference, accuracy.DifferenceKind) = accuracyDelta switch
            {
                < 0 => (accuracyDelta.FormatAccuracy(), StatisticValue.Kind.Loss),
                > 0 => (LocalisableString.Interpolate($@"+{accuracyDelta.FormatAccuracy()}"), StatisticValue.Kind.Gain),
                _ => (LocalisableString.Interpolate($@"+{0d.FormatAccuracy()}"), StatisticValue.Kind.NoChange),
            };

            totalScore.Value = statisticsUpdate.Value.After.TotalScore.ToLocalisableString(@"N0");

            long totalScoreDelta = statisticsUpdate.Value.After.TotalScore - statisticsUpdate.Value.Before.TotalScore;
            (totalScore.Difference, totalScore.DifferenceKind) = totalScoreDelta switch
            {
                < 0 => (totalScoreDelta.ToLocalisableString(@"N0"), StatisticValue.Kind.Loss),
                > 0 => (LocalisableString.Interpolate($@"+{totalScoreDelta:N0}"), StatisticValue.Kind.Gain),
                _ => (@"+0", StatisticValue.Kind.NoChange),
            };
        }
    }
}
