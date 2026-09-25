// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Overlays;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Skinning;

namespace osu.Game.Screens.RankingV2
{
    public partial class ResultsScreenV2 : ScreenWithBeatmapBackground
    {
        [Cached(typeof(IBindable<IScoreInfo>))]
        private Bindable<IScoreInfo> score = new Bindable<IScoreInfo>();

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        // TODO: multiplayer screens accept null score when showing a playlist item's scores - decide how to handle that
        public ResultsScreenV2(IScoreInfo initialScore)
        {
            score.Value = initialScore;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new SkinnableContainer(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Results))
            {
                RelativeSizeAxes = Axes.Both,
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            score.BindValueChanged(_ => updateState(), true);
        }

        private void updateState()
        {
            // TODO: this is a double hack
            // - `SkinnableModDisplay` binds to the global mods bindable to read mods to display
            //   hacking stuff here seems *marginally* less evil than adjusting that component to receive an `IScoreInfo` and magically decide what to show
            // - also `IScoreInfo` hasn't got mods exposed and needs an interface for mods
            Mods.Value = (score.Value as ScoreInfo)?.Mods ?? [];
        }
    }
}
