// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Graphics;
using osu.Game.Overlays;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Skinning;

namespace osu.Game.Screens.RankingV2
{
    [Cached(typeof(ResultsScreenV2))]
    public partial class ResultsScreenV2 : ScreenWithBeatmapBackground
    {
        public Bindable<Visibility> DetailsVisible = new Bindable<Visibility>();

        [Cached(typeof(IBindable<IScoreInfo>))]
        private Bindable<IScoreInfo> score = new Bindable<IScoreInfo>();

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        private SkinnableContainer mainScreen = null!;
        private Drawable detailsView = null!;

        // TODO: multiplayer screens accept null score when showing a playlist item's scores - decide how to handle that
        public ResultsScreenV2(IScoreInfo initialScore)
        {
            score.Value = initialScore;
        }

        [BackgroundDependencyLoader]
        private void load(LargeTextureStore textures)
        {
            InternalChildren =
            [
                mainScreen = new SkinnableContainer(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Results))
                {
                    RelativeSizeAxes = Axes.Both,
                },
                detailsView = new InputBlockingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    Children =
                    [
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colour4.Black,
                            Alpha = 0.7f,
                        },
                        new Sprite
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Texture = textures.Get(@"Ranking/extended-placeholder.png"),
                            FillMode = FillMode.Fit,
                        },
                    ]
                }
            ];
        }

        public delegate void ScreenTransition(Drawable mainScreen, Drawable subScreen);

        public void PopInDetails(ScreenTransition transition)
        {
            detailsView.FinishTransforms();
            DetailsVisible.Value = Visibility.Visible;
            transition.Invoke(mainScreen, detailsView);
        }

        public void PopOutDetails(ScreenTransition transition)
        {
            detailsView.FinishTransforms();
            DetailsVisible.Value = Visibility.Hidden;
            transition.Invoke(mainScreen, detailsView);
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
