// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Specialized;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Pooling;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.Chat;
using osu.Game.Online.Spectator;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Screens.Play.HUD
{
    // TODO: stuff still shows in autoplay
    public partial class SpectatorList : CompositeDrawable, ISerialisableDrawable
    {
        private const int max_spectators_displayed = 10;

        public BindableList<SpectatorUser> Spectators { get; } = new BindableList<SpectatorUser>();
        public Bindable<LocalUserPlayingState> UserPlayingState { get; } = new Bindable<LocalUserPlayingState>();

        private OsuSpriteText header = null!;
        private FillFlowContainer mainFlow = null!;
        private FillFlowContainer<SpectatorListEntry> spectatorsFlow = null!;
        private DrawablePool<SpectatorListEntry> pool = null!;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, SpectatorClient? client, Player? player)
        {
            AutoSizeAxes = Axes.Both;

            InternalChildren = new[]
            {
                Empty().With(t => t.Size = new Vector2(100, 50)),
                mainFlow = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    AutoSizeDuration = 250,
                    AutoSizeEasing = Easing.OutQuint,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        header = new OsuSpriteText
                        {
                            Colour = colours.Blue1,
                            Font = OsuFont.GetFont(size: 12),
                        },
                        spectatorsFlow = new FillFlowContainer<SpectatorListEntry>
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                        }
                    }
                },
                pool = new DrawablePool<SpectatorListEntry>(max_spectators_displayed),
            };

            if (client != null)
                ((IBindableList<SpectatorUser>)Spectators).BindTo(client.WatchingUsers);

            if (player != null)
                ((IBindable<LocalUserPlayingState>)UserPlayingState).BindTo(player.PlayingState);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Spectators.BindCollectionChanged(onSpectatorsChanged, true);
        }

        private void onSpectatorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    for (int i = 0; i < e.NewItems!.Count; i++)
                    {
                        var spectator = (SpectatorUser)e.NewItems![i]!;
                        int index = e.NewStartingIndex + i;

                        if (index >= max_spectators_displayed)
                            break;

                        spectatorsFlow.Insert(e.NewStartingIndex + i, pool.Get(entry =>
                        {
                            entry.Current.Value = spectator;
                            entry.UserPlayingState = UserPlayingState;
                        }));
                    }

                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    spectatorsFlow.RemoveAll(entry => e.OldItems!.Contains(entry.Current.Value), false);

                    for (int i = 0; i < spectatorsFlow.Count; i++)
                        spectatorsFlow.SetLayoutPosition(spectatorsFlow[i], i);

                    if (Spectators.Count >= max_spectators_displayed && spectatorsFlow.Count < max_spectators_displayed)
                    {
                        for (int i = spectatorsFlow.Count; i < max_spectators_displayed; i++)
                        {
                            var spectator = Spectators[i];
                            spectatorsFlow.Insert(i, pool.Get(entry =>
                            {
                                entry.Current.Value = spectator;
                                entry.UserPlayingState = UserPlayingState;
                            }));
                        }
                    }

                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                {
                    spectatorsFlow.Clear(false);
                    break;
                }

                default:
                    throw new NotSupportedException();
            }

            header.Text = $@"Spectators ({Spectators.Count})";
            mainFlow.FadeTo(Spectators.Count > 0 ? 1 : 0, 250, Easing.OutQuint);
        }

        private partial class SpectatorListEntry : PoolableDrawable
        {
            public Bindable<SpectatorUser> Current { get; } = new Bindable<SpectatorUser>();

            private readonly BindableWithCurrent<LocalUserPlayingState> current = new BindableWithCurrent<LocalUserPlayingState>();

            public Bindable<LocalUserPlayingState> UserPlayingState
            {
                get => current.Current;
                set => current.Current = value;
            }

            private OsuSpriteText username = null!;
            private DrawableLinkCompiler? linkCompiler;

            [Resolved]
            private OsuGame? game { get; set; }

            [BackgroundDependencyLoader]
            private void load()
            {
                AutoSizeAxes = Axes.Both;

                InternalChildren = new Drawable[]
                {
                    username = new OsuSpriteText(),
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                UserPlayingState.BindValueChanged(_ => updateEnabledState());
                Current.BindValueChanged(_ => updateState(), true);
            }

            private void updateState()
            {
                username.Text = Current.Value.Username;
                linkCompiler?.Expire();
                AddInternal(linkCompiler = new DrawableLinkCompiler([username])
                {
                    IdleColour = Colour4.White,
                    Action = () => game?.HandleLink(new LinkDetails(LinkAction.OpenUserProfile, Current.Value)),
                });
                updateEnabledState();
            }

            private void updateEnabledState()
            {
                if (linkCompiler != null)
                    linkCompiler.Enabled.Value = UserPlayingState.Value != LocalUserPlayingState.Playing;
            }
        }

        public bool UsesFixedAnchor { get; set; }
    }
}
