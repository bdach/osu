// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserInterface
{
    public partial class HotkeyTooltip : VisibilityContainer, ITooltip<HotkeyTooltipContent>
    {
        private readonly Box background;
        private bool instantMovement = true;
        private HotkeyTooltipContent? currentContent;
        private readonly FillFlowContainer verticalFlow;

        public void SetContent(HotkeyTooltipContent content)
        {
            if (currentContent != null && EqualityComparer<HotkeyTooltipContent>.Default.Equals(content, currentContent.Value)) return;

            currentContent = content;
            updateContent();

            if (IsPresent)
            {
                AutoSizeDuration = 250;
                background.FlashColour(OsuColour.Gray(0.4f), 1000, Easing.OutQuint);
            }
            else
                AutoSizeDuration = 0;
        }

        public HotkeyTooltip()
        {
            AutoSizeAxes = Axes.Both;
            AutoSizeEasing = Easing.OutQuint;

            CornerRadius = 5;
            Masking = true;
            EdgeEffect = new EdgeEffectParameters
            {
                Type = EdgeEffectType.Shadow,
                Colour = Color4.Black.Opacity(40),
                Radius = 5,
            };
            Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.9f,
                },
                verticalFlow = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Spacing = new Vector2(5),
                    Padding = new MarginPadding(5),
                    Direction = FillDirection.Vertical,
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colour)
        {
            background.Colour = colour.Gray3;
        }

        protected override void PopIn()
        {
            instantMovement |= !IsPresent;
            this.FadeIn(500, Easing.OutQuint);
        }

        protected override void PopOut() => this.Delay(150).FadeOut(500, Easing.OutQuint);

        public void Move(Vector2 pos)
        {
            if (instantMovement)
            {
                Position = pos;
                instantMovement = false;
            }
            else
            {
                this.MoveTo(pos, 200, Easing.OutQuint);
            }
        }

        private void updateContent()
        {
            verticalFlow.Clear();

            if (currentContent == null)
                return;

            verticalFlow.Add(new OsuSpriteText { Text = currentContent.Value.Description });

            foreach (var (hotkey, description) in currentContent.Value.Hotkeys)
            {
                verticalFlow.Add(new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(5),
                    Children = new Drawable[]
                    {
                        new HotkeyDisplay
                        {
                            Hotkey = hotkey,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Margin = new MarginPadding { Top = 1, }
                        },
                        new OsuSpriteText
                        {
                            Text = description,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                        },
                    }
                });
            }
        }
    }

    public struct HotkeyTooltipContent : IEquatable<HotkeyTooltipContent>
    {
        public LocalisableString Description { get; set; }
        public (Hotkey, LocalisableString)[] Hotkeys { get; set; }

        public bool Equals(HotkeyTooltipContent other) => Description.Equals(other.Description) && Hotkeys.SequenceEqual(other.Hotkeys);
        public override bool Equals(object? obj) => obj is HotkeyTooltipContent other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Description, StructuralComparisons.StructuralEqualityComparer.GetHashCode(Hotkeys));
    }
}