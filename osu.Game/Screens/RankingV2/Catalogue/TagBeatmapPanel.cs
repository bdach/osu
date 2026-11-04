// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking;
using osu.Game.Screens.RankingV2.Statistics;

namespace osu.Game.Screens.RankingV2.Catalogue
{
    public partial class TagBeatmapPanel : StatisticItemPanel
    {
        [Resolved]
        private IBindable<IScoreInfo> score { get; set; } = null!;

        public TagBeatmapPanel()
            : base("Tag the beatmap")
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.None;
            RelativeSizeAxes = Axes.Both;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // TODO: when old results are gone (or when reviewers complain), change this so that this uses the common API fetch from the parent screen
            score.BindValueChanged(val => Child = new UserTagControl(val.NewValue.Beatmap!)
            {
                RelativeSizeAxes = Axes.X,
                Writable = true,
            }, true);
        }
    }
}
