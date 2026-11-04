// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.BeatmapSet.Buttons;
using osu.Game.Screens.RankingV2.Statistics;
using osuTK;

namespace osu.Game.Screens.RankingV2.Catalogue
{
    public partial class RateBeatmapPanel : StatisticItemPanel
    {
        private readonly Bindable<int?> instantaneousRating = new Bindable<int?>();
        private readonly Bindable<int?> committedRating = new Bindable<int?>();

        private OsuSpriteText ratingText = null!;
        private FavouriteButton favouriteButton = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private Bindable<APIBeatmap?> apiBeatmap { get; set; } = null!;

        public RateBeatmapPanel()
            : base("Rate the beatmap")
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(5),
                Children =
                [
                    new RatingControl
                    {
                        InstantaneousRating = { BindTarget = instantaneousRating, },
                        CommittedRating = { BindTarget = committedRating, },
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                    new Container
                    {
                        AutoSizeAxes = Axes.Y,
                        Width = 56,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Child = ratingText = new OsuSpriteText
                        {
                            Font = OsuFont.Style.Heading1.With(fixedWidth: true),
                            Spacing = new Vector2(-3),
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                        }
                    },
                    favouriteButton = new FavouriteButton
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Margin = new MarginPadding { Left = 10, },
                    },
                ]
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            apiBeatmap.BindValueChanged(_ => favouriteButton.BeatmapSet.Value = apiBeatmap.Value?.BeatmapSet);

            instantaneousRating.BindValueChanged(_ => updateState());
            committedRating.BindValueChanged(_ => updateState(), true);
        }

        private void updateState()
        {
            if (instantaneousRating.Value != null)
                ratingText.Colour = colourProvider.Highlight1;
            else if (committedRating.Value != null)
                ratingText.Colour = colours.Orange1;
            else
                ratingText.Colour = colourProvider.Background2;

            ratingText.Text = $@"{instantaneousRating.Value ?? committedRating.Value ?? 0}/10";
        }

        private partial class RatingControl : FillFlowContainer<OsuClickableContainer>
        {
            public Bindable<int?> InstantaneousRating { get; } = new Bindable<int?>();
            public Bindable<int?> CommittedRating { get; } = new Bindable<int?>();

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                AutoSizeAxes = Axes.Both;
                Direction = FillDirection.Horizontal;

                for (int i = 1; i <= 10; ++i)
                {
                    Add(new OsuClickableContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Child = new SpriteIcon
                        {
                            Icon = FontAwesome.Solid.Star,
                            Size = new Vector2(30),
                        },
                        Action = commitRating,
                    });
                }
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                InstantaneousRating.BindValueChanged(_ => updateState());
                CommittedRating.BindValueChanged(_ => updateState(), true);
            }

            protected override bool OnMouseMove(MouseMoveEvent e)
            {
                int count = this.Count(star => star.ScreenSpaceDrawQuad.TopLeft.X < e.ScreenSpaceLastMousePosition.X);
                if (count > 0)
                    InstantaneousRating.Value = count;

                return true;
            }

            protected override bool OnHover(HoverEvent e) => true;

            protected override void OnHoverLost(HoverLostEvent e)
            {
                base.OnHoverLost(e);

                InstantaneousRating.Value = null;
            }

            private void commitRating()
            {
                if (InstantaneousRating.Value > 0)
                {
                    CommittedRating.Value = InstantaneousRating.Value;
                    InstantaneousRating.Value = null;
                }
            }

            private void updateState()
            {
                for (int i = 0; i < Count; ++i)
                {
                    var star = this[i];
                    int rating = i + 1;

                    if (rating <= InstantaneousRating.Value)
                        star.Colour = colourProvider.Highlight1;
                    else if (rating <= CommittedRating.Value)
                        star.Colour = colours.Orange1;
                    else
                        star.Colour = colourProvider.Background2;
                }
            }
        }
    }
}
