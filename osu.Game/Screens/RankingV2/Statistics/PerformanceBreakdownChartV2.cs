// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Rulesets.Difficulty;
using osuTK;

namespace osu.Game.Screens.RankingV2.Statistics
{
    public partial class PerformanceBreakdownChartV2 : CompositeDrawable
    {
        private readonly Dictionary<string, PerformanceDisplayAttribute> attributes;
        private readonly Dictionary<string, PerformanceDisplayAttribute> perfectAttributes;

        public PerformanceBreakdownChartV2(IEnumerable<PerformanceDisplayAttribute> attributes, IEnumerable<PerformanceDisplayAttribute> perfectAttributes)
        {
            this.attributes = attributes.ToDictionary(attr => attr.PropertyName);
            this.perfectAttributes = perfectAttributes.ToDictionary(attr => attr.PropertyName);

            Assert.That(this.attributes.Keys.SequenceEqual(this.perfectAttributes.Keys));
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, OsuColour colours)
        {
            const float inner_diameter = 15;
            const float outer_diameter = 200;
            const float text_radius = outer_diameter / 2 + 10;

            AddInternal(new Circle
            {
                Size = new Vector2(outer_diameter),
                Colour = colourProvider.Background4,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });

            string[] keys = attributes.Keys.Order().ToArray();

            Colour4[] seriesColours =
            [
                colours.Pink1,
                colours.Blue1,
                colours.Lime1,
                colours.DarkOrange1,
                colours.Purple1,
                colours.Green1,
                colours.Orange1,
                colours.Red1,
            ];

            for (int i = 0; i < keys.Length; ++i)
            {
                string key = keys[i];
                var seriesColour = seriesColours[i % seriesColours.Length];
                float sliceProportion = 1f / keys.Length;
                const float slice_spacing = 0.02f;
                float textRotationRadians = (2 * MathF.PI * (i + 0.5f) / keys.Length) - (MathF.PI / 2);
                float unitX = MathF.Cos(textRotationRadians);
                float unitY = MathF.Sin(textRotationRadians);
                double relativeValue = attributes[key].Value / perfectAttributes[key].Value;

                AddInternal(new CircularProgress
                {
                    Progress = sliceProportion * (1 - 2 * slice_spacing),
                    Rotation = 360f * ((i + slice_spacing) * sliceProportion),
                    Colour = seriesColour,
                    X = inner_diameter / 2 * unitX,
                    Y = inner_diameter / 2 * unitY,
                    Size = new Vector2((float)((outer_diameter - inner_diameter + 5) * relativeValue)),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                });

                Anchor textAnchor = 0;
                textAnchor |= unitX switch
                {
                    < -0.33f => Anchor.x2,
                    > 0.33f => Anchor.x0,
                    _ => Anchor.x1,
                };
                textAnchor |= unitY switch
                {
                    < -0.33f => Anchor.y2,
                    > 0.33f => Anchor.y0,
                    _ => Anchor.y1,
                };

                AddInternal(new OsuSpriteText
                {
                    Anchor = textAnchor,
                    Origin = textAnchor,
                    X = unitX * text_radius,
                    Y = unitY * text_radius,
                    Colour = seriesColour,
                    Text = attributes[key].DisplayName,
                    Font = OsuFont.Style.Heading2,
                });

                bool valueInsideSlice = relativeValue > 0.6;
                AddInternal(new OsuSpriteText
                {
                    Anchor = valueInsideSlice ? Anchor.Centre : textAnchor,
                    Origin = valueInsideSlice ? Anchor.Centre : textAnchor,
                    Text = relativeValue.ToLocalisableString(@"P0"),
                    Colour = valueInsideSlice ? colourProvider.Background5 : seriesColour,
                    X = unitX * inner_diameter / 2 + unitX * (outer_diameter - inner_diameter) / 2 * (valueInsideSlice ? (float)relativeValue / 2 : (float)relativeValue + 0.04f),
                    Y = unitY * inner_diameter / 2 + unitY * (outer_diameter - inner_diameter) / 2 * (valueInsideSlice ? (float)relativeValue / 2 : (float)relativeValue + 0.04f),
                });
            }

            // TODO: needs total shown somewhere still, but can handle this later
        }
    }
}
