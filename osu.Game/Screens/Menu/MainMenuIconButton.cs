// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Beatmaps.ControlPoints;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Screens.Menu
{
    public partial class MainMenuIconButton : MainMenuButton
    {
        private readonly SpriteIcon icon;

        public MainMenuIconButton(LocalisableString text, string sampleName, IconUsage symbol, Color4 colour, Action? clickAction = null, float extraWidth = 0, params Key[] triggerKeys)
            : base(text, sampleName, colour, clickAction, extraWidth, triggerKeys)
        {
            Add(icon = new SpriteIcon
            {
                Shadow = true,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(32),
                Position = new Vector2(0, 0),
                Margin = new MarginPadding { Top = -4 },
                Icon = symbol
            });
        }

        private bool rightward;

        protected override void OnNewBeat(int beatIndex, TimingControlPoint timingPoint, EffectControlPoint effectPoint, ChannelAmplitudes amplitudes)
        {
            base.OnNewBeat(beatIndex, timingPoint, effectPoint, amplitudes);

            if (!IsHovered) return;

            double duration = timingPoint.BeatLength / 2;

            icon.RotateTo(rightward ? BOUNCE_ROTATION : -BOUNCE_ROTATION, duration * 2, Easing.InOutSine);

            icon.Animate(
                i => i.MoveToY(-10, duration, Easing.Out),
                i => i.ScaleTo(HOVER_SCALE, duration, Easing.Out)
            ).Then(
                i => i.MoveToY(0, duration, Easing.In),
                i => i.ScaleTo(new Vector2(HOVER_SCALE, HOVER_SCALE * BOUNCE_COMPRESSION), duration, Easing.In)
            );

            rightward = !rightward;
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (State == ButtonState.Expanded)
            {
                double duration = TimeUntilNextBeat;

                icon.ClearTransforms();
                icon.RotateTo(rightward ? -BOUNCE_ROTATION : BOUNCE_ROTATION, duration, Easing.InOutSine);
                icon.ScaleTo(new Vector2(HOVER_SCALE, HOVER_SCALE * BOUNCE_COMPRESSION), duration, Easing.Out);
            }

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);

            icon.ClearTransforms();
            icon.RotateTo(0, 500, Easing.Out);
            icon.MoveTo(Vector2.Zero, 500, Easing.Out);
            icon.ScaleTo(Vector2.One, 200, Easing.Out);
        }
    }
}
