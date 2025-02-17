// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Screens.Ranking
{
    public partial class UserTagControl : CompositeDrawable
    {
        private FillFlowContainer<DrawableUserTag> tagFlow = null!;

        public BindableList<UserTag> TopTags { get; } = new BindableList<UserTag>();
        public BindableList<UserTag> AllTags { get; } = new BindableList<UserTag>();

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new FillFlowContainer
            {
                Direction = FillDirection.Vertical,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    tagFlow = new ReverseChildIDFillFlowContainer<DrawableUserTag>
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Full,
                        LayoutDuration = 300,
                        LayoutEasing = Easing.OutQuint,
                        Spacing = new Vector2(2),
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            TopTags.BindCollectionChanged(updateTags, true);
        }

        private void updateTags(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var oldItems = tagFlow.ToArray();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    for (int i = Math.Max(0, e.NewStartingIndex); i < oldItems.Length; i++)
                        tagFlow.SetLayoutPosition(oldItems[i], i + e.NewItems!.Count);

                    for (int i = 0; i < e.NewItems!.Count; i++)
                    {
                        var tag = (UserTag)e.NewItems[i]!;
                        var drawableTag = new DrawableUserTag(tag);
                        tagFlow.Insert(e.NewStartingIndex + i, drawableTag);
                    }

                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    for (int i = e.OldStartingIndex; i < tagFlow.Count; i++)
                        tagFlow.SetLayoutPosition(oldItems[i], i - e.NewItems!.Count);

                    for (int i = 0; i < e.OldItems!.Count; i++)
                        tagFlow.Remove(oldItems[e.OldStartingIndex + i], true);

                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                {
                    tagFlow.Clear();
                    break;
                }
            }
        }

        private partial class DrawableUserTag : OsuClickableContainer
        {
            private readonly UserTag userTag;

            private readonly Bindable<int> voteCount = new Bindable<int>();
            private readonly BindableBool voted = new BindableBool();
            private readonly Bindable<bool> confirmed = new BindableBool();

            private Box mainBackground = null!;
            private Box voteBackground = null!;
            private OsuSpriteText tagNameText = null!;
            private OsuSpriteText voteCountText = null!;

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            public DrawableUserTag(UserTag userTag)
            {
                this.userTag = userTag;
                voteCount.BindTo(userTag.VoteCount);
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AutoSizeAxes = Axes.Both;
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                CornerRadius = 8;
                Masking = true;
                Content.RelativeSizeAxes = Axes.None;
                Content.AutoSizeAxes = Axes.Both;
                EdgeEffect = new EdgeEffectParameters
                {
                    Colour = colours.Lime1,
                    Radius = 5,
                    Type = EdgeEffectType.Glow,
                };
                AddRange(new Drawable[]
                {
                    mainBackground = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Padding = new MarginPadding { Horizontal = 6, Vertical = 3, },
                        Spacing = new Vector2(5),
                        Children = new Drawable[]
                        {
                            tagNameText = new OsuSpriteText
                            {
                                Text = userTag.Name,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                            },
                            new Container
                            {
                                AutoSizeAxes = Axes.Both,
                                CornerRadius = 5,
                                Masking = true,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Children = new Drawable[]
                                {
                                    voteBackground = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                    },
                                    voteCountText = new OsuSpriteText
                                    {
                                        Margin = new MarginPadding { Horizontal = 6, Vertical = 3, },
                                    }
                                }
                            }
                        }
                    }
                });
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                const double transition_duration = 300;

                voteCount.BindValueChanged(_ => voteCountText.Text = voteCount.Value.ToLocalisableString(), true);
                voted.BindValueChanged(v =>
                {
                    if (v.NewValue)
                    {
                        voteBackground.FadeColour(colours.Lime3, transition_duration, Easing.OutQuint);
                        voteCountText.FadeColour(Colour4.Black, transition_duration, Easing.OutQuint);
                        this.ScaleTo(1.1f)
                            .Then().ScaleTo(1, 3 * transition_duration, Easing.OutElasticHalf);
                    }
                    else
                    {
                        voteBackground.FadeColour(ColourInfo.GradientVertical(Colour4.FromHex("#333"), Colour4.FromHex("#111")), transition_duration, Easing.OutQuint);
                        voteCountText.FadeColour(Colour4.White, transition_duration, Easing.OutQuint);
                        this.ScaleTo(0.9f)
                            .Then().ScaleTo(1, 3 * transition_duration, Easing.OutElasticHalf);
                    }
                }, true);
                confirmed.BindValueChanged(c =>
                {
                    if (c.NewValue)
                    {
                        mainBackground.FadeColour(colours.Lime1, transition_duration, Easing.OutQuint);
                        tagNameText.FadeColour(Colour4.Black, transition_duration, Easing.OutQuint);
                        FadeEdgeEffectTo(0.5f, transition_duration, Easing.OutQuint);
                    }
                    else
                    {
                        mainBackground.FadeColour(ColourInfo.GradientVertical(Colour4.FromHex("#555"), Colour4.FromHex("#333")), transition_duration, Easing.OutQuint);
                        tagNameText.FadeColour(Colour4.White, transition_duration, Easing.OutQuint);
                        FadeEdgeEffectTo(0f, transition_duration, Easing.OutQuint);
                    }
                }, true);
                FinishTransforms(true);

                Action = () =>
                {
                    if (!voted.Value)
                        voteCount.Value += 1;
                    else
                        voteCount.Value -= 1;

                    voted.Toggle();
                    confirmed.Value = voteCount.Value >= 10;
                };
            }
        }
    }

    public record UserTag(string Name)
    {
        public BindableInt VoteCount { get; } = new BindableInt();
    }
}
