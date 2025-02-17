// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Testing;
using osu.Game.Screens.Ranking;

namespace osu.Game.Tests.Visual.Ranking
{
    public partial class TestSceneUserTagControl : OsuTestScene
    {
        private readonly BindableList<UserTag> topTags = new BindableList<UserTag>();
        private readonly BindableList<UserTag> extraTags = new BindableList<UserTag>();

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create control", () =>
            {
                Child = new PopoverContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new UserTagControl
                    {
                        Width = 500,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        TopTags = { BindTarget = topTags },
                        ExtraTags = { BindTarget = extraTags },
                    }
                };
            });
            AddRepeatStep("add some tags", () =>
            {
                topTags.Add(new UserTag($"tag #{topTags.Count}")
                {
                    VoteCount = { Value = topTags.Count }
                });
            }, 11);
            AddRepeatStep("add some extra tags", () => extraTags.Add(new UserTag($"extra tag #{extraTags.Count}")), 5);
            AddStep("remove a top tag", () => topTags.RemoveAt(0));
            AddStep("remove an extra tag", () => extraTags.RemoveAt(0));
        }
    }
}
