// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;

namespace osu.Game.Tests.Visual.UserInterface
{
    public class TestSceneLabelledSliderBar : OsuTestScene
    {
        private LabelledSliderBar<double> component;

        [TestCase(false)]
        [TestCase(true)]
        public void TestSliderBar(bool hasDescription) => createSliderBar(hasDescription);

        [Test]
        public void TestDisabled()
        {
            createSliderBar();

            AddStep("disable current", () => component.Current.Disabled = true);
            AddStep("enable current", () => component.Current.Disabled = false);
        }

        [Test]
        public void TestTicks()
        {
            createSliderBar();

            AddStep("show ticks", () => component.ShowTicks = true);
            AddStep("add labels", () =>
            {
                component.Labels.Add((0f, "Large"));
                component.Labels.Add((5f, "Normal"));
                component.Labels.Add((10f, "Small"));
            });

            AddStep("change precision", () => ((BindableNumber<double>)component.Current).Precision = 0.1);
            AddStep("restore precision", () => ((BindableNumber<double>)component.Current).Precision = 1);
            AddStep("change min", () => ((BindableNumber<double>)component.Current).MinValue = 3);
            AddStep("change max", () => ((BindableNumber<double>)component.Current).MaxValue = 12);

            AddStep("hide ticks", () => component.ShowTicks = false);
        }

        private void createSliderBar(bool hasDescription = false)
        {
            AddStep("create component", () =>
            {
                Child = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Width = 500,
                    AutoSizeAxes = Axes.Y,
                    Child = component = new LabelledSliderBar<double>
                    {
                        Current = new BindableDouble(5)
                        {
                            MinValue = 0,
                            MaxValue = 10,
                            Precision = 1,
                        }
                    }
                };

                component.Label = "a sample component";
                component.Description = hasDescription ? "this text describes the component" : string.Empty;
            });
        }
    }
}
