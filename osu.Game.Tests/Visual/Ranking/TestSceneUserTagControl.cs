// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Screens.Ranking;

namespace osu.Game.Tests.Visual.Ranking
{
    public partial class TestSceneUserTagControl : OsuTestScene
    {
        private readonly BindableList<UserTag> topTags = new BindableList<UserTag>();

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create control", () =>
            {
                Child = new UserTagControl
                {
                    Width = 500,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    TopTags = { BindTarget = topTags }
                };
            });
            AddRepeatStep("add some tags", () =>
            {
                topTags.Add(new UserTag($"tag #{topTags.Count}")
                {
                    VoteCount = { Value = topTags.Count }
                });
            }, 11);
        }
    }
}
