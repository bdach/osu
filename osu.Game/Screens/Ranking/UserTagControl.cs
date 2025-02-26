// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osuTK;

namespace osu.Game.Screens.Ranking
{
    public partial class UserTagControl : CompositeDrawable
    {
        public override bool HandlePositionalInput => true;

        private readonly Cached layout = new Cached();

        private FillFlowContainer<DrawableUserTag> tagFlow = null!;

        private BindableList<UserTag> displayedTags { get; } = new BindableList<UserTag>();
        private BindableList<UserTag> extraTags { get; } = new BindableList<UserTag>();

        private Bindable<APITag[]?> allTags = null!;
        private readonly Bindable<APIBeatmapTag[]?> topTags = new Bindable<APIBeatmapTag[]?>();

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private Bindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(SessionStatics sessionStatics)
        {
            AutoSizeAxes = Axes.Y;
            InternalChild = new FillFlowContainer
            {
                Direction = FillDirection.Vertical,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(8),
                Children = new Drawable[]
                {
                    tagFlow = new FillFlowContainer<DrawableUserTag>
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Full,
                        LayoutDuration = 300,
                        LayoutEasing = Easing.OutQuint,
                        Spacing = new Vector2(4),
                    },
                    new ExtraTagsButton
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        TopTags = { BindTarget = displayedTags },
                        ExtraTags = { BindTarget = extraTags },
                    }
                }
            };

            allTags = sessionStatics.GetBindable<APITag[]?>(Static.AllBeatmapTags);

            if (allTags.Value == null)
            {
                var listTagsRequest = new ListTagsRequest();
                listTagsRequest.Success += tags => allTags.Value = tags.ToArray();
                api.Queue(listTagsRequest);
            }

            var getBeatmapSetRequest = new GetBeatmapSetRequest(beatmap.Value.BeatmapInfo.BeatmapSet!.OnlineID);
            getBeatmapSetRequest.Success += set => topTags.Value = set.Beatmaps.SingleOrDefault(b => b.MatchesOnlineID(beatmap.Value.BeatmapInfo))?.TopTags;
            api.Queue(getBeatmapSetRequest);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            allTags.BindValueChanged(_ => updateTags());
            topTags.BindValueChanged(_ => updateTags());
            updateTags();

            displayedTags.BindCollectionChanged(displayTags, true);
        }

        private void updateTags()
        {
            if (allTags.Value == null || topTags.Value == null)
                return;

            var allTagsById = allTags.Value.ToDictionary(t => t.Id);

            foreach (var topTag in topTags.Value)
            {
                if (allTagsById.Remove(topTag.TagId, out var tag))
                    displayedTags.Add(new UserTag(tag) { VoteCount = { Value = topTag.VoteCount } });
            }

            extraTags.AddRange(allTagsById.Select(t => new UserTag(t.Value)));
        }

        private void displayTags(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var oldItems = tagFlow.ToArray();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    for (int i = 0; i < e.NewItems!.Count; i++)
                    {
                        var tag = (UserTag)e.NewItems[i]!;
                        var drawableTag = new DrawableUserTag(tag);
                        tagFlow.Insert(tagFlow.Count, drawableTag);
                        tag.VoteCount.BindValueChanged(sortTags, true);
                        layout.Invalidate();
                    }

                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    for (int i = 0; i < e.OldItems!.Count; i++)
                    {
                        var tag = (UserTag)e.OldItems[i]!;
                        tag.VoteCount.ValueChanged -= sortTags;
                        tagFlow.Remove(oldItems[e.OldStartingIndex + i], true);
                    }

                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                {
                    tagFlow.Clear();
                    break;
                }
            }
        }

        private void sortTags(ValueChangedEvent<int> _) => layout.Invalidate();

        protected override void Update()
        {
            base.Update();

            if (!layout.IsValid && !IsHovered)
            {
                var sortedTags = new Dictionary<UserTag, int>(
                    displayedTags.OrderByDescending(t => t.VoteCount.Value)
                                 .ThenByDescending(t => t.Voted.Value)
                                 .Select((tag, index) => new KeyValuePair<UserTag, int>(tag, index)));

                foreach (var drawableTag in tagFlow)
                    tagFlow.SetLayoutPosition(drawableTag, sortedTags[drawableTag.UserTag]);

                layout.Validate();
            }
        }

        private partial class DrawableUserTag : OsuClickableContainer
        {
            public readonly UserTag UserTag;

            private readonly Bindable<int> voteCount = new Bindable<int>();
            private readonly BindableBool voted = new BindableBool();
            private readonly Bindable<bool> confirmed = new BindableBool();

            private Box mainBackground = null!;
            private Box voteBackground = null!;
            private OsuSpriteText tagNameText = null!;
            private OsuSpriteText voteCountText = null!;
            private LoadingSpinner spinner = null!;

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            [Resolved]
            private Bindable<WorkingBeatmap> beatmap { get; set; } = null!;

            [Resolved]
            private IAPIProvider api { get; set; } = null!;

            private APIRequest? requestInFlight;

            public DrawableUserTag(UserTag userTag)
            {
                UserTag = userTag;
                voteCount.BindTo(userTag.VoteCount);
                voted.BindTo(userTag.Voted);
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
                                Text = UserTag.Name,
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
                                    },
                                    spinner = new LoadingSpinner(withBox: true)
                                    {
                                        Alpha = 0,
                                        Size = new Vector2(18),
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

                voteCount.BindValueChanged(_ =>
                {
                    voteCountText.Text = voteCount.Value.ToLocalisableString();
                    confirmed.Value = voteCount.Value >= 10;
                }, true);
                voted.BindValueChanged(v =>
                {
                    if (v.NewValue)
                    {
                        voteBackground.FadeColour(colours.Lime3, transition_duration, Easing.OutQuint);
                        voteCountText.FadeColour(Colour4.Black, transition_duration, Easing.OutQuint);
                    }
                    else
                    {
                        voteBackground.FadeColour(ColourInfo.GradientVertical(Colour4.FromHex("#333"), Colour4.FromHex("#111")), transition_duration, Easing.OutQuint);
                        voteCountText.FadeColour(Colour4.White, transition_duration, Easing.OutQuint);
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
                    if (requestInFlight != null)
                        return;

                    spinner.Show();

                    APIRequest request;

                    switch (voted.Value)
                    {
                        case true:
                            var removeReq = new RemoveBeatmapTagRequest(beatmap.Value.BeatmapInfo.OnlineID, UserTag.Id);
                            removeReq.Success += () =>
                            {
                                voteCount.Value -= 1;
                                voted.Value = false;
                            };
                            request = removeReq;
                            break;

                        case false:
                            var addReq = new AddBeatmapTagRequest(beatmap.Value.BeatmapInfo.OnlineID, UserTag.Id);
                            addReq.Success += () =>
                            {
                                voteCount.Value += 1;
                                voted.Value = true;
                            };
                            request = addReq;
                            break;
                    }

                    request.Success += () =>
                    {
                        spinner.Hide();
                        requestInFlight = null;
                    };
                    request.Failure += _ =>
                    {
                        spinner.Hide();
                        requestInFlight = null;
                    };
                    api.Queue(requestInFlight = request);
                };
            }
        }

        private partial class ExtraTagsButton : GrayButton, IHasPopover
        {
            public BindableList<UserTag> TopTags { get; } = new BindableList<UserTag>();
            public BindableList<UserTag> ExtraTags { get; } = new BindableList<UserTag>();

            public ExtraTagsButton()
                : base(FontAwesome.Solid.Plus)
            {
                Size = new Vector2(30);

                Action = this.ShowPopover;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                ExtraTags.BindCollectionChanged((_, _) => Enabled.Value = ExtraTags.Count > 0, true);
            }

            public Popover GetPopover() => new ExtraTagsPopover
            {
                TopTags = { BindTarget = TopTags },
                ExtraTags = { BindTarget = ExtraTags },
            };
        }

        private partial class ExtraTagsPopover : OsuPopover
        {
            public BindableList<UserTag> TopTags { get; } = new BindableList<UserTag>();
            public BindableList<UserTag> ExtraTags { get; } = new BindableList<UserTag>();

            public ExtraTagsPopover()
                : base(false)
            {
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Children = new[]
                {
                    new OsuMenu(Direction.Vertical, true)
                    {
                        Items = items,
                        MaxHeight = 375,
                    },
                };
            }

            private OsuMenuItem[] items => ExtraTags.Select(tag => new OsuMenuItem(tag.Name, MenuItemType.Standard, () =>
            {
                TopTags.Add(tag);
                ExtraTags.Remove(tag);
                this.HidePopover();
            })).ToArray();
        }
    }

    public record UserTag
    {
        public long Id { get; }
        public string Name { get; }
        public BindableInt VoteCount { get; } = new BindableInt();
        public BindableBool Voted { get; } = new BindableBool();

        public UserTag(APITag tag)
        {
            Id = tag.Id;
            Name = tag.Name;
        }
    }
}
