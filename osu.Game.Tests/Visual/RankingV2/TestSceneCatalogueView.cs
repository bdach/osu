// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Collections;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
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
            AddStep("create some collections", () =>
            {
                Realm.Write(r =>
                {
                    for (int i = 0; i < 30; ++i)
                        r.Add(new BeatmapCollection { Name = $"Collection #{i + 1}" });
                });
            });
            AddStep("set up request handling", () =>
            {
                ((DummyAPIAccess)API).HandleRequest = req =>
                {
                    switch (req)
                    {
                        case GetBeatmapSetRequest getBeatmapSetRequest:
                            var beatmapSet = CreateAPIBeatmapSet(Beatmap.Value.BeatmapInfo);
                            beatmapSet.Beatmaps.Single().TopTags =
                            [
                                new APIBeatmapTag { TagId = 3, VoteCount = 4 },
                                new APIBeatmapTag { TagId = 2, VoteCount = 3 },
                                new APIBeatmapTag { TagId = 0, VoteCount = 2 },
                            ];
                            Scheduler.AddDelayed(() => getBeatmapSetRequest.TriggerSuccess(beatmapSet), 500);
                            return true;

                        case ListTagsRequest listTagsRequest:
                        {
                            Scheduler.AddDelayed(() => listTagsRequest.TriggerSuccess(new APITagCollection
                            {
                                Tags =
                                [
                                    new APITag { Id = 0, Name = "uncategorised tag", Description = "This probably isn't real but could be and should be handled.", },
                                    new APITag { Id = 1, Name = "song representation/simple", Description = "Accessible and straightforward map design.", },
                                    new APITag
                                    {
                                        Id = 2, Name = "style/clean",
                                        Description = "Visually uncluttered and organised patterns, often involving few overlaps and equal visual spacing between objects.",
                                    },
                                    new APITag
                                    {
                                        Id = 3, Name = "aim/aim control", Description = "Patterns with velocity or direction changes which strongly go against a player's natural movement pattern.",
                                    },
                                    new APITag { Id = 4, Name = "tap/bursts", Description = "Patterns requiring continuous movement and alternating, typically 9 notes or less.", },
                                    new APITag { Id = 5, Name = "style/mono-heavy", Description = "Features monos used in large amounts.", RulesetId = 1, },
                                ]
                            }), 500);
                            return true;
                        }

                        case PostBeatmapFavouriteRequest:
                        case AddBeatmapTagRequest:
                        case RemoveBeatmapTagRequest:
                        {
                            Scheduler.AddDelayed(req.TriggerSuccess, 500);
                            return true;
                        }
                    }

                    return false;
                };
            });
            AddStep("create content", () => Child = new PopoverContainer
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
