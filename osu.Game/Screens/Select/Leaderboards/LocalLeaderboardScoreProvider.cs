// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;
using Realms;

namespace osu.Game.Screens.Select.Leaderboards
{
    public partial class LocalLeaderboardScoreProvider : Component, ILeaderboardScoreProvider
    {
        public IBindableList<ScoreInfo> Scores => scores;
        private readonly BindableList<ScoreInfo> scores = new BindableList<ScoreInfo>();

        public IBindable<bool> Loading => loading;
        private readonly BindableBool loading = new BindableBool();

        public Bindable<BeatmapInfo> Beatmap { get; } = new Bindable<BeatmapInfo>();
        public Bindable<RulesetInfo> Ruleset { get; } = new Bindable<RulesetInfo>();
        public Bindable<bool> ModFilterActive { get; } = new BindableBool();
        public Bindable<IReadOnlyList<Mod>> Mods { get; } = new Bindable<IReadOnlyList<Mod>>([]);

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        private IDisposable? scoreSubscription;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Beatmap.BindValueChanged(_ => Scheduler.AddOnce(subscribeToLocalScores));
            Ruleset.BindValueChanged(_ => Scheduler.AddOnce(subscribeToLocalScores));
            ModFilterActive.BindValueChanged(_ => Scheduler.AddOnce(subscribeToLocalScores));
            Mods.BindValueChanged(_ => Scheduler.AddOnce(subscribeToLocalScores));
            subscribeToLocalScores();
        }

        private void subscribeToLocalScores()
        {
            Debug.Assert(Beatmap.Value != null);
            Debug.Assert(Ruleset.Value != null);
            loading.Value = true;

            scoreSubscription?.Dispose();
            scoreSubscription = null;

            scoreSubscription = realm.RegisterForNotifications(r =>
                r.All<ScoreInfo>().Filter($"{nameof(ScoreInfo.BeatmapInfo)}.{nameof(BeatmapInfo.ID)} == $0"
                                          + $" AND {nameof(ScoreInfo.BeatmapInfo)}.{nameof(BeatmapInfo.Hash)} == {nameof(ScoreInfo.BeatmapHash)}"
                                          + $" AND {nameof(ScoreInfo.Ruleset)}.{nameof(RulesetInfo.ShortName)} == $1"
                                          + $" AND {nameof(ScoreInfo.DeletePending)} == false"
                    , Beatmap.Value.ID, Ruleset.Value.ShortName), localScoresChanged);

            void localScoresChanged(IRealmCollection<ScoreInfo> sender, ChangeSet? changes)
            {
                // This subscription may fire from changes to linked beatmaps, which we don't care about.
                // It's currently not possible for a score to be modified after insertion, so we can safely ignore callbacks with only modifications.
                if (changes?.HasCollectionChanges() == false)
                    return;

                var newScores = sender.AsEnumerable();

                if (ModFilterActive.Value && !Mods.Value.Any())
                {
                    // we need to filter out all scores that have any mods to get all local nomod scores
                    newScores = newScores.Where(s => !s.Mods.Any());
                }
                else if (ModFilterActive.Value)
                {
                    // otherwise find all the scores that have all of the currently selected mods (similar to how web applies mod filters)
                    // we're creating and using a string HashSet representation of selected mods so that it can be translated into the DB query itself
                    var selectedMods = Mods.Value.Select(m => m.Acronym).ToHashSet();

                    newScores = newScores.Where(s => selectedMods.SetEquals(s.Mods.Select(m => m.Acronym)));
                }

                newScores = newScores.Detach().OrderByTotalScore();

                scores.Clear();
                scores.AddRange(newScores);
                loading.Value = false;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            scoreSubscription?.Dispose();
            scoreSubscription = null;
        }
    }
}
