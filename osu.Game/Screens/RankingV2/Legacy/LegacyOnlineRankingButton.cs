// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Scoring;
using osu.Game.Skinning;
using osu.Game.Skinning.Components;
using osuTK;
using osu.Game.Localisation.Ranking;

namespace osu.Game.Screens.RankingV2.Legacy
{
    public partial class LegacyOnlineRankingButton : CompositeDrawable
    {
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

        [BackgroundDependencyLoader]
        private void load(IBindable<IScoreInfo> _)
        {
            // just a dummy BDL that requires `IBindable<IScoreInfo>`
            // this is done so that this component doesn't show up on other skinnable screens

            AutoSizeAxes = Axes.Both;
            InternalChild = new LegacyButton
            {
                AccentColour = Colour4.BlueViolet,
                Text = LegacyOnlineRankingButtonStrings.OnlineRanking,
                Size = new Vector2(200, 30) * LegacySkin.STABLE_MAGIC_SCALE_FACTOR,
                Action = () => { },
            };
        }
    }
}
