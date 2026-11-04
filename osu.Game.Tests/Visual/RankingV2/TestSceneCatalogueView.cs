// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Scoring;
using osu.Game.Screens.RankingV2.Catalogue;
using osu.Game.Tests.Resources;
using Realms;

namespace osu.Game.Tests.Visual.RankingV2
{
    public partial class TestSceneCatalogueView : OsuTestScene
    {
        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Cached(typeof(IBindable<IScoreInfo>))]
        private Bindable<IScoreInfo> score { get; set; } = new Bindable<IScoreInfo>();

        [Cached]
        private OverlayColourProvider colours { get; set; } = new OverlayColourProvider(OverlayColourScheme.Blue);

        [BackgroundDependencyLoader]
        private void load()
        {
            Dependencies.Cache(Realm);
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("set score", () => setScore(new OsuRuleset().RulesetInfo));
            AddStep("set up request handling", () =>
            {
                ((DummyAPIAccess)API).HandleRequest = req =>
                {
                    switch (req)
                    {
                        case GetBeatmapSetRequest getBeatmapSetRequest:
                            getBeatmapSetRequest.TriggerSuccess(CreateAPIBeatmapSet(Beatmap.Value.BeatmapInfo));
                            return true;

                        case PostBeatmapFavouriteRequest favouriteRequest:
                            favouriteRequest.TriggerSuccess();
                            return true;
                    }

                    return false;
                };
            });
            AddStep("create content", () => Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding
                {
                    Horizontal = 30,
                    Vertical = 10,
                },
                Child = new CatalogueView(),
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            realm.Run(r =>
            {
                var beatmapInfo = r.All<BeatmapInfo>()
                                   .Filter($"{nameof(BeatmapInfo.Ruleset)}.{nameof(RulesetInfo.OnlineID)} = $0 AND {nameof(BeatmapInfo.StatusInt)} = 1", 0)
                                   .FirstOrDefault();

                if (beatmapInfo != null)
                    Beatmap.Value = beatmaps.GetWorkingBeatmap(beatmapInfo);
            });
        }

        private void setScore(RulesetInfo rulesetInfo)
        {
            var s = TestResources.CreateTestScoreInfo(Beatmap.Value.BeatmapInfo);
            s.Ruleset = rulesetInfo;
            score.Value = s;
        }
    }
}
