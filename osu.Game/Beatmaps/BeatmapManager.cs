// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Game.Beatmaps.Formats;
using osu.Game.Beatmaps.Timing;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.IO;
using osu.Game.IO.Archives;
using osu.Game.Models;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Skinning;
using osu.Game.Utils;
using osuTK;
using Realms;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Handles general operations related to global beatmap management.
    /// </summary>
    public class BeatmapManager : ModelManager<BeatmapSetInfo>, IModelImporter<BeatmapSetInfo>, IWorkingBeatmapCache
    {
        public const string OSL_HASH_INDICATOR_PREFIX = @"// ORIGINAL_OSL_FILE_HASH=";

        public ITrackStore BeatmapTrackStore { get; }

        private readonly BeatmapImporter beatmapImporter;

        private readonly WorkingBeatmapCache workingBeatmapCache;

        private readonly BeatmapExporter beatmapExporter;

        public ProcessBeatmapDelegate? ProcessBeatmap { private get; set; }

        public override bool PauseImports
        {
            get => base.PauseImports;
            set
            {
                base.PauseImports = value;
                beatmapImporter.PauseImports = value;
            }
        }

        public BeatmapManager(Storage storage, RealmAccess realm, IAPIProvider? api, AudioManager audioManager, IResourceStore<byte[]> gameResources, GameHost? host = null,
                              WorkingBeatmap? defaultBeatmap = null, BeatmapDifficultyCache? difficultyCache = null, bool performOnlineLookups = false)
            : base(storage, realm)
        {
            if (performOnlineLookups)
            {
                if (api == null)
                    throw new ArgumentNullException(nameof(api), "API must be provided if online lookups are required.");

                if (difficultyCache == null)
                    throw new ArgumentNullException(nameof(difficultyCache), "Difficulty cache must be provided if online lookups are required.");
            }

            var userResources = new RealmFileStore(realm, storage).Store;

            BeatmapTrackStore = audioManager.GetTrackStore(userResources);

            beatmapImporter = CreateBeatmapImporter(storage, realm);
            beatmapImporter.ProcessBeatmap = (beatmapSet, scope) => ProcessBeatmap?.Invoke(beatmapSet, scope);
            beatmapImporter.PostNotification = obj => PostNotification?.Invoke(obj);

            workingBeatmapCache = CreateWorkingBeatmapCache(audioManager, gameResources, userResources, defaultBeatmap, host);

            beatmapExporter = new BeatmapExporter(storage)
            {
                PostNotification = obj => PostNotification?.Invoke(obj)
            };
        }

        protected virtual WorkingBeatmapCache CreateWorkingBeatmapCache(AudioManager audioManager, IResourceStore<byte[]> resources, IResourceStore<byte[]> storage, WorkingBeatmap? defaultBeatmap,
                                                                        GameHost? host)
        {
            return new WorkingBeatmapCache(BeatmapTrackStore, audioManager, resources, storage, defaultBeatmap, host);
        }

        protected virtual BeatmapImporter CreateBeatmapImporter(Storage storage, RealmAccess realm) => new BeatmapImporter(storage, realm);

        /// <summary>
        /// Create a new beatmap set, backed by a <see cref="BeatmapSetInfo"/> model,
        /// with a single difficulty which is backed by a <see cref="BeatmapInfo"/> model
        /// and represented by the returned usable <see cref="WorkingBeatmap"/>.
        /// </summary>
        public WorkingBeatmap CreateNew(RulesetInfo ruleset, APIUser user)
        {
            var metadata = new BeatmapMetadata
            {
                Author = new RealmUser
                {
                    OnlineID = user.OnlineID,
                    Username = user.Username,
                }
            };

            var beatmapSet = new BeatmapSetInfo
            {
                DateAdded = DateTimeOffset.UtcNow,
                Beatmaps =
                {
                    new BeatmapInfo(ruleset, new BeatmapDifficulty(), metadata)
                }
            };

            foreach (BeatmapInfo b in beatmapSet.Beatmaps)
                b.BeatmapSet = beatmapSet;

            var imported = beatmapImporter.ImportModel(beatmapSet);

            if (imported == null)
                throw new InvalidOperationException("Failed to import new beatmap");

            return imported.PerformRead(s => GetWorkingBeatmap(s.Beatmaps.First()));
        }

        /// <summary>
        /// Add a new difficulty to the provided <paramref name="targetBeatmapSet"/> based on the provided <paramref name="referenceWorkingBeatmap"/>.
        /// The new difficulty will be backed by a <see cref="BeatmapInfo"/> model
        /// and represented by the returned <see cref="WorkingBeatmap"/>.
        /// </summary>
        /// <remarks>
        /// Contrary to <see cref="CopyExistingDifficulty"/>, this method does not preserve hitobjects and beatmap-level settings from <paramref name="referenceWorkingBeatmap"/>.
        /// The created beatmap will have zero hitobjects and will have default settings (including difficulty settings), but will preserve metadata and existing timing points.
        /// </remarks>
        /// <param name="targetBeatmapSet">The <see cref="BeatmapSetInfo"/> to add the new difficulty to.</param>
        /// <param name="referenceWorkingBeatmap">The <see cref="WorkingBeatmap"/> to use as a baseline reference when creating the new difficulty.</param>
        /// <param name="rulesetInfo">The ruleset with which the new difficulty should be created.</param>
        public virtual WorkingBeatmap CreateNewDifficulty(BeatmapSetInfo targetBeatmapSet, WorkingBeatmap referenceWorkingBeatmap, RulesetInfo rulesetInfo)
        {
            var newBeatmapInfo = new BeatmapInfo(rulesetInfo, new BeatmapDifficulty(), referenceWorkingBeatmap.Metadata.DeepClone())
            {
                DifficultyName = NamingUtils.GetNextBestName(targetBeatmapSet.Beatmaps.Select(b => b.DifficultyName), "New Difficulty")
            };
            var newBeatmap = new Beatmap { BeatmapInfo = newBeatmapInfo };
            foreach (var timingPoint in referenceWorkingBeatmap.Beatmap.ControlPointInfo.TimingPoints)
                newBeatmap.ControlPointInfo.Add(timingPoint.Time, timingPoint.DeepClone());

            return addDifficultyToSet(targetBeatmapSet, newBeatmap, referenceWorkingBeatmap.Skin);
        }

        /// <summary>
        /// Add a copy of the provided <paramref name="referenceWorkingBeatmap"/> to the provided <paramref name="targetBeatmapSet"/>.
        /// The new difficulty will be backed by a <see cref="BeatmapInfo"/> model
        /// and represented by the returned <see cref="WorkingBeatmap"/>.
        /// </summary>
        /// <remarks>
        /// Contrary to <see cref="CreateNewDifficulty"/>, this method creates a nearly-exact copy of <paramref name="referenceWorkingBeatmap"/>
        /// (with the exception of a few key properties that cannot be copied under any circumstance, like difficulty name, beatmap hash, or online status).
        /// </remarks>
        /// <param name="targetBeatmapSet">The <see cref="BeatmapSetInfo"/> to add the copy to.</param>
        /// <param name="referenceWorkingBeatmap">The <see cref="WorkingBeatmap"/> to be copied.</param>
        public virtual WorkingBeatmap CopyExistingDifficulty(BeatmapSetInfo targetBeatmapSet, WorkingBeatmap referenceWorkingBeatmap)
        {
            var newBeatmap = referenceWorkingBeatmap.GetPlayableBeatmap(referenceWorkingBeatmap.BeatmapInfo.Ruleset).Clone();
            BeatmapInfo newBeatmapInfo;

            newBeatmap.BeatmapInfo = newBeatmapInfo = referenceWorkingBeatmap.BeatmapInfo.Clone();
            // assign a new ID to the clone.
            newBeatmapInfo.ID = Guid.NewGuid();
            // add "(copy)" suffix to difficulty name, and additionally ensure that it doesn't conflict with any other potentially pre-existing copies.
            newBeatmapInfo.DifficultyName = NamingUtils.GetNextBestName(
                targetBeatmapSet.Beatmaps.Select(b => b.DifficultyName),
                $"{newBeatmapInfo.DifficultyName} (copy)");
            // clear the hash, as that's what is used to match .osu files with their corresponding realm beatmaps.
            newBeatmapInfo.Hash = string.Empty;
            // clear online properties.
            newBeatmapInfo.ResetOnlineInfo();

            return addDifficultyToSet(targetBeatmapSet, newBeatmap, referenceWorkingBeatmap.Skin);
        }

        private WorkingBeatmap addDifficultyToSet(BeatmapSetInfo targetBeatmapSet, IBeatmap newBeatmap, ISkin beatmapSkin)
        {
            // populate circular beatmap set info <-> beatmap info references manually.
            // several places like `Save()` or `GetWorkingBeatmap()`
            // rely on them being freely traversable in both directions for correct operation.
            targetBeatmapSet.Beatmaps.Add(newBeatmap.BeatmapInfo);
            newBeatmap.BeatmapInfo.BeatmapSet = targetBeatmapSet;

            save(newBeatmap.BeatmapInfo, newBeatmap, beatmapSkin, transferCollections: false);

            workingBeatmapCache.Invalidate(targetBeatmapSet);
            return GetWorkingBeatmap(newBeatmap.BeatmapInfo);
        }

        /// <summary>
        /// Delete a beatmap difficulty.
        /// </summary>
        /// <param name="beatmapInfo">The beatmap difficulty to hide.</param>
        public void Hide(BeatmapInfo beatmapInfo)
        {
            Realm.Run(r =>
            {
                using (var transaction = r.BeginWrite())
                {
                    if (!beatmapInfo.IsManaged)
                        beatmapInfo = r.Find<BeatmapInfo>(beatmapInfo.ID)!;

                    beatmapInfo.Hidden = true;
                    transaction.Commit();
                }
            });
        }

        /// <summary>
        /// Restore a beatmap difficulty.
        /// </summary>
        /// <param name="beatmapInfo">The beatmap difficulty to restore.</param>
        public void Restore(BeatmapInfo beatmapInfo)
        {
            Realm.Run(r =>
            {
                using (var transaction = r.BeginWrite())
                {
                    if (!beatmapInfo.IsManaged)
                        beatmapInfo = r.Find<BeatmapInfo>(beatmapInfo.ID)!;

                    beatmapInfo.Hidden = false;
                    transaction.Commit();
                }
            });
        }

        public void RestoreAll()
        {
            Realm.Run(r =>
            {
                using (var transaction = r.BeginWrite())
                {
                    foreach (var beatmap in r.All<BeatmapInfo>().Where(b => b.Hidden))
                        beatmap.Hidden = false;

                    transaction.Commit();
                }
            });
        }

        /// <summary>
        /// Returns a list of all usable <see cref="BeatmapSetInfo"/>s.
        /// </summary>
        /// <returns>A list of available <see cref="BeatmapSetInfo"/>.</returns>
        public List<BeatmapSetInfo> GetAllUsableBeatmapSets()
        {
            return Realm.Run(r =>
            {
                r.Refresh();
                return r.All<BeatmapSetInfo>().Where(b => !b.DeletePending).Detach();
            });
        }

        /// <summary>
        /// Perform a lookup query on available <see cref="BeatmapSetInfo"/>s.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The first result for the provided query, or null if no results were found.</returns>
        public Live<BeatmapSetInfo>? QueryBeatmapSet(Expression<Func<BeatmapSetInfo, bool>> query)
        {
            return Realm.Run(r => r.All<BeatmapSetInfo>().FirstOrDefault(query)?.ToLive(Realm));
        }

        /// <summary>
        /// Perform a lookup query on available <see cref="BeatmapInfo"/>s.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The first result for the provided query, or null if no results were found.</returns>
        public BeatmapInfo? QueryBeatmap(Expression<Func<BeatmapInfo, bool>> query) => Realm.Run(r =>
            r.All<BeatmapInfo>().Filter($"{nameof(BeatmapInfo.BeatmapSet)}.{nameof(BeatmapSetInfo.DeletePending)} == false").FirstOrDefault(query)?.Detach());

        /// <summary>
        /// A default representation of a WorkingBeatmap to use when no beatmap is available.
        /// </summary>
        public IWorkingBeatmap DefaultBeatmap => workingBeatmapCache.DefaultBeatmap;

        /// <summary>
        /// Saves an existing <see cref="IBeatmap"/> file against a given <see cref="BeatmapInfo"/>.
        /// </summary>
        /// <remarks>
        /// This method will also update any user beatmap collection hash references to the new post-saved hash.
        /// </remarks>
        /// <param name="beatmapInfo">The <see cref="BeatmapInfo"/> to save the content against. The file referenced by <see cref="BeatmapInfo.Path"/> will be replaced.</param>
        /// <param name="beatmapContent">The <see cref="IBeatmap"/> content to write.</param>
        /// <param name="beatmapSkin">The beatmap <see cref="ISkin"/> content to write, null if to be omitted.</param>
        public virtual void Save(BeatmapInfo beatmapInfo, IBeatmap beatmapContent, ISkin? beatmapSkin = null) =>
            save(beatmapInfo, beatmapContent, beatmapSkin, transferCollections: true);

        public void DeleteAllVideos()
        {
            Realm.Write(r =>
            {
                var items = r.All<BeatmapSetInfo>().Where(s => !s.DeletePending && !s.Protected);
                DeleteVideos(items.ToList());
            });
        }

        public void ResetAllOffsets()
        {
            const string reset_complete_message = "All offsets have been reset!";
            Realm.Write(r =>
            {
                var items = r.All<BeatmapInfo>();

                foreach (var beatmap in items)
                {
                    if (beatmap.UserSettings.Offset != 0)
                        beatmap.UserSettings.Offset = 0;
                }

                PostNotification?.Invoke(new ProgressCompletionNotification { Text = reset_complete_message });
            });
        }

        public void Delete(Expression<Func<BeatmapSetInfo, bool>>? filter = null, bool silent = false)
        {
            Realm.Run(r =>
            {
                var items = r.All<BeatmapSetInfo>().Where(s => !s.DeletePending && !s.Protected);

                if (filter != null)
                    items = items.Where(filter);

                Delete(items.ToList(), silent);
            });
        }

        /// <summary>
        /// Delete a beatmap difficulty immediately.
        /// </summary>
        /// <remarks>
        /// There's no undoing this operation, as we don't have a soft-deletion flag on <see cref="BeatmapInfo"/>.
        /// This may be a future consideration if there's a user requirement for undeleting support.
        /// </remarks>
        public void DeleteDifficultyImmediately(BeatmapInfo beatmapInfo)
        {
            Realm.Write(r =>
            {
                if (!beatmapInfo.IsManaged)
                    beatmapInfo = r.Find<BeatmapInfo>(beatmapInfo.ID)!;

                Debug.Assert(beatmapInfo.BeatmapSet != null);
                Debug.Assert(beatmapInfo.File != null);

                var setInfo = beatmapInfo.BeatmapSet;

                DeleteFile(setInfo, beatmapInfo.File);
                setInfo.Beatmaps.Remove(beatmapInfo);
                r.Remove(beatmapInfo.Metadata);
                r.Remove(beatmapInfo);

                updateHashAndMarkDirty(setInfo);
                workingBeatmapCache.Invalidate(setInfo);
            });
        }

        /// <summary>
        /// Delete videos from a list of beatmaps.
        /// This will post notifications tracking progress.
        /// </summary>
        public void DeleteVideos(List<BeatmapSetInfo> items, bool silent = false)
        {
            const string no_videos_message = "No videos found to delete!";

            if (items.Count == 0)
            {
                if (!silent)
                    PostNotification?.Invoke(new ProgressCompletionNotification { Text = no_videos_message });
                return;
            }

            var notification = new ProgressNotification
            {
                Progress = 0,
                Text = $"Preparing to delete all {HumanisedModelName} videos...",
                CompletionText = no_videos_message,
                State = ProgressNotificationState.Active,
            };

            if (!silent)
                PostNotification?.Invoke(notification);

            int i = 0;
            int deleted = 0;

            foreach (var b in items)
            {
                if (notification.State == ProgressNotificationState.Cancelled)
                    // user requested abort
                    return;

                var video = b.Files.FirstOrDefault(f => OsuGameBase.VIDEO_EXTENSIONS.Any(ex => f.Filename.EndsWith(ex, StringComparison.OrdinalIgnoreCase)));

                if (video != null)
                {
                    DeleteFile(b, video);
                    deleted++;
                    notification.CompletionText = $"Deleted {deleted} {HumanisedModelName} video(s)!";
                }

                notification.Text = $"Deleting videos from {HumanisedModelName}s ({deleted} deleted)";

                notification.Progress = (float)++i / items.Count;
            }

            notification.State = ProgressNotificationState.Completed;
        }

        public void UndeleteAll()
        {
            Realm.Run(r => Undelete(r.All<BeatmapSetInfo>().Where(s => s.DeletePending).ToList()));
        }

        public Task<Live<BeatmapSetInfo>?> ImportAsUpdate(ProgressNotification notification, ImportTask importTask, BeatmapSetInfo original) =>
            beatmapImporter.ImportAsUpdate(notification, importTask, original);

        public Task<ExternalEditOperation<BeatmapSetInfo>> BeginExternalEditing(BeatmapSetInfo model) =>
            beatmapImporter.BeginExternalEditing(model);

        public Task Export(BeatmapSetInfo beatmap) => beatmapExporter.ExportAsync(beatmap.ToLive(Realm));

        private void updateHashAndMarkDirty(BeatmapSetInfo setInfo)
        {
            setInfo.Hash = beatmapImporter.ComputeHash(setInfo);
            setInfo.Status = BeatmapOnlineStatus.LocallyModified;
        }

        private void save(BeatmapInfo beatmapInfo, IBeatmap beatmapContent, ISkin? beatmapSkin, bool transferCollections)
        {
            var setInfo = beatmapInfo.BeatmapSet;
            Debug.Assert(setInfo != null);

            // Difficulty settings must be copied first due to the clone in `Beatmap<>.BeatmapInfo_Set`.
            // This should hopefully be temporary, assuming said clone is eventually removed.

            // Warning: The directionality here is important. Changes have to be copied *from* beatmapContent (which comes from editor and is being saved)
            // *to* the beatmapInfo (which is a database model and needs to receive values without the taiko slider velocity multiplier for correct operation).
            // CopyTo() will undo such adjustments, while CopyFrom() will not.
            beatmapContent.Difficulty.CopyTo(beatmapInfo.Difficulty);

            // All changes to metadata are made in the provided beatmapInfo, so this should be copied to the `IBeatmap` before encoding.
            beatmapContent.BeatmapInfo = beatmapInfo;

            // Since now this is a locally-modified beatmap, we also set all relevant flags to indicate this.
            // Importantly, the `ResetOnlineInfo()` call must happen before encoding, as online ID is encoded into the `.osu` file,
            // which influences the beatmap checksums.
            beatmapInfo.LastLocalUpdate = DateTimeOffset.Now;
            beatmapInfo.Status = BeatmapOnlineStatus.LocallyModified;
            beatmapInfo.ResetOnlineInfo();

            Realm.Write(r =>
            {
                // AddFile generally handles updating/replacing files, but this is a case where the filename may have also changed so let's delete for simplicity.
                var existingFiles = new HashSet<RealmNamedFileUsage>();

                if (beatmapInfo.File?.Filename != null && setInfo.GetFile(beatmapInfo.File.Filename) is RealmNamedFileUsage existingPlayFile)
                    existingFiles.Add(existingPlayFile);
                if (beatmapInfo.EditFile?.Filename != null && setInfo.GetFile(beatmapInfo.EditFile.Filename) is RealmNamedFileUsage existingEditFile)
                    existingFiles.Add(existingEditFile);

                // ensure that two difficulties from the set don't point at the same beatmap file.
                if (setInfo.Beatmaps.Any(b => b.ID != beatmapInfo.ID
                                              && string.Equals(b.Path, createBeatmapFilenameFromMetadata(beatmapInfo, legacyFormat: true), StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"{setInfo.GetDisplayString()} already has a difficulty with the name of '{beatmapInfo.DifficultyName}'.");
                }

                foreach (var file in existingFiles)
                    DeleteFile(setInfo, file);

                string oldMd5Hash = beatmapInfo.MD5Hash;
                saveSingleBeatmap(beatmapInfo, beatmapContent, beatmapSkin);

                updateHashAndMarkDirty(setInfo);

                var liveBeatmapSet = r.Find<BeatmapSetInfo>(setInfo.ID)!;

                setInfo.CopyChangesToRealm(liveBeatmapSet);

                if (transferCollections)
                    beatmapInfo.TransferCollectionReferences(r, oldMd5Hash);

                liveBeatmapSet.Beatmaps.Single(b => b.ID == beatmapInfo.ID)
                              .UpdateLocalScores(r);

                // do not look up metadata.
                // this is a locally-modified set now, so looking up metadata is busy work at best and harmful at worst.
                ProcessBeatmap?.Invoke(liveBeatmapSet, MetadataLookupScope.None);
            });

            Debug.Assert(beatmapInfo.BeatmapSet != null);
        }

        private void saveSingleBeatmap(BeatmapInfo beatmapInfo, IBeatmap editBeatmapContent, ISkin? beatmapSkin)
        {
            Debug.Assert(beatmapInfo.BeatmapSet != null);

            // pass 1: direct save, no concern for backwards compatibility
            using var editBeatmapStream = new MemoryStream();
            using (var sw = new StreamWriter(editBeatmapStream, Encoding.UTF8, 1024, true))
                new LegacyBeatmapEncoder(editBeatmapContent, beatmapSkin).Encode(sw);

            editBeatmapStream.Seek(0, SeekOrigin.Begin);
            beatmapInfo.EditHash = editBeatmapStream.ComputeSHA2Hash();

            AddFile(beatmapInfo.BeatmapSet, editBeatmapStream, createBeatmapFilenameFromMetadata(beatmapInfo, legacyFormat: false));

            // pass 2: backwards-compatible save
            // this is decoding the just-encoded beatmap because we don't have a proper beatmap deep clone primitive :/
            Beatmap legacyBeatmapContent;
            editBeatmapStream.Seek(0, SeekOrigin.Begin);
            using (var sr = new LineBufferedReader(editBeatmapStream, leaveOpen: true))
                legacyBeatmapContent = new LegacyBeatmapDecoder().Decode(sr);

            var playableBeatmap = new FlatWorkingBeatmap(legacyBeatmapContent).GetPlayableBeatmap(beatmapInfo.Ruleset);

            // Convert beatmap elements to be compatible with legacy format
            // So we truncate time and position values to integers, and convert paths with multiple segments to bezier curves
            foreach (var controlPoint in playableBeatmap.ControlPointInfo.AllControlPoints)
                controlPoint.Time = Math.Floor(controlPoint.Time);

            for (int i = 0; i < playableBeatmap.Breaks.Count; i++)
                playableBeatmap.Breaks[i] = new BreakPeriod(Math.Floor(playableBeatmap.Breaks[i].StartTime), Math.Floor(playableBeatmap.Breaks[i].EndTime));

            foreach (var hitObject in playableBeatmap.HitObjects)
            {
                // Truncate end time before truncating start time because end time is dependent on start time
                if (hitObject is IHasDuration hasDuration && hitObject is not IHasPath)
                    hasDuration.Duration = Math.Floor(hasDuration.EndTime) - Math.Floor(hitObject.StartTime);

                hitObject.StartTime = Math.Floor(hitObject.StartTime);

                if (hitObject is not IHasPath hasPath) continue;

                // stable's hit object parsing expects the entire slider to use only one type of curve,
                // and happens to use the last non-empty curve type read for the entire slider.
                // this clear of the last control point type handles an edge case
                // wherein the last control point of an otherwise-single-segment slider path has a different type than previous,
                // which would lead to sliders being mangled when exported back to stable.
                // normally, that would be handled by the `BezierConverter.ConvertToModernBezier()` call below,
                // which outputs a slider path containing only BEZIER control points,
                // but a non-inherited last control point is (rightly) not considered to be starting a new segment,
                // therefore it would fail to clear the `CountSegments() <= 1` check.
                // by clearing explicitly we both fix the issue and avoid unnecessary conversions to BEZIER.
                if (hasPath.Path.ControlPoints.Count > 1)
                    hasPath.Path.ControlPoints[^1].Type = null;

                if (BezierConverter.CountSegments(hasPath.Path.ControlPoints) <= 1
                    && hasPath.Path.ControlPoints[0].Type!.Value.Degree == null) continue;

                var newControlPoints = BezierConverter.ConvertToModernBezier(hasPath.Path.ControlPoints);

                // Truncate control points to integer positions
                foreach (var pathControlPoint in newControlPoints)
                {
                    pathControlPoint.Position = new Vector2(
                        (float)Math.Floor(pathControlPoint.Position.X),
                        (float)Math.Floor(pathControlPoint.Position.Y));
                }

                hasPath.Path.ControlPoints.Clear();
                hasPath.Path.ControlPoints.AddRange(newControlPoints);
            }

            // Encode to legacy format
            var playBeatmapStream = new MemoryStream();

            string preamble = $"""
                              // This file was automatically generated using osu!(lazer) from {createBeatmapFilenameFromMetadata(beatmapInfo, legacyFormat: false)}.
                              // DO NOT MAKE ANY MANUAL CHANGES TO THIS FILE. USE ONLY FACILITIES PROVIDED IN osu!(lazer) FOR EDITING THIS FILE.
                              {OSL_HASH_INDICATOR_PREFIX}{beatmapInfo.EditHash}

                              """;

            playBeatmapStream.Write(Encoding.UTF8.GetBytes(preamble));

            using (var sw = new StreamWriter(playBeatmapStream, Encoding.UTF8, 1024, true))
                new LegacyBeatmapEncoder(playableBeatmap, beatmapSkin).Encode(sw);

            playBeatmapStream.Seek(0, SeekOrigin.Begin);

            beatmapInfo.MD5Hash = playBeatmapStream.ComputeMD5Hash();
            beatmapInfo.Hash = playBeatmapStream.ComputeSHA2Hash();

            AddFile(beatmapInfo.BeatmapSet, playBeatmapStream, createBeatmapFilenameFromMetadata(beatmapInfo, legacyFormat: true));
        }

        private static string createBeatmapFilenameFromMetadata(BeatmapInfo beatmapInfo, bool legacyFormat)
        {
            var metadata = beatmapInfo.Metadata;
            return $"{metadata.Artist} - {metadata.Title} ({metadata.Author.Username}) [{beatmapInfo.DifficultyName}].{(legacyFormat ? @"osu" : @"osl")}".GetValidFilename();
        }

        #region Implementation of ICanAcceptFiles

        public Task Import(params string[] paths) => beatmapImporter.Import(paths);

        public Task Import(ImportTask[] tasks, ImportParameters parameters = default) => beatmapImporter.Import(tasks, parameters);

        public Task<IEnumerable<Live<BeatmapSetInfo>>> Import(ProgressNotification notification, ImportTask[] tasks, ImportParameters parameters = default) =>
            beatmapImporter.Import(notification, tasks, parameters);

        public Task<Live<BeatmapSetInfo>?> Import(ImportTask task, ImportParameters parameters = default, CancellationToken cancellationToken = default) =>
            beatmapImporter.Import(task, parameters, cancellationToken);

        public Live<BeatmapSetInfo>? Import(BeatmapSetInfo item, ArchiveReader? archive = null, CancellationToken cancellationToken = default) =>
            beatmapImporter.ImportModel(item, archive, default, cancellationToken);

        public IEnumerable<string> HandledExtensions => beatmapImporter.HandledExtensions;

        #endregion

        #region Implementation of IWorkingBeatmapCache

        /// <summary>
        /// Retrieve a <see cref="WorkingBeatmap"/> instance for the provided <see cref="BeatmapInfo"/>
        /// </summary>
        /// <param name="beatmapInfo">The beatmap to lookup.</param>
        /// <param name="refetch">Whether to force a refetch from the database to ensure <see cref="BeatmapInfo"/> is up-to-date.</param>
        /// <returns>A <see cref="WorkingBeatmap"/> instance correlating to the provided <see cref="BeatmapInfo"/>.</returns>
        public WorkingBeatmap GetWorkingBeatmap(BeatmapInfo? beatmapInfo, bool refetch = false)
        {
            if (beatmapInfo != null)
            {
                if (refetch)
                    workingBeatmapCache.Invalidate(beatmapInfo);

                // Detached beatmapsets don't come with files as an optimisation (see `RealmObjectExtensions.beatmap_set_mapper`).
                // If we seem to be missing files, now is a good time to re-fetch.
                bool missingFiles = beatmapInfo.BeatmapSet?.Files.Count == 0;

                if (refetch || beatmapInfo.IsManaged || missingFiles)
                {
                    Guid id = beatmapInfo.ID;
                    beatmapInfo = Realm.Run(r => r.Find<BeatmapInfo>(id)?.Detach()) ?? beatmapInfo;
                }

                Debug.Assert(beatmapInfo.IsManaged != true);
            }

            return workingBeatmapCache.GetWorkingBeatmap(beatmapInfo);
        }

        WorkingBeatmap IWorkingBeatmapCache.GetWorkingBeatmap(BeatmapInfo beatmapInfo) => GetWorkingBeatmap(beatmapInfo);
        void IWorkingBeatmapCache.Invalidate(BeatmapSetInfo beatmapSetInfo) => workingBeatmapCache.Invalidate(beatmapSetInfo);
        void IWorkingBeatmapCache.Invalidate(BeatmapInfo beatmapInfo) => workingBeatmapCache.Invalidate(beatmapInfo);

        public event Action<WorkingBeatmap>? OnInvalidated
        {
            add => workingBeatmapCache.OnInvalidated += value;
            remove => workingBeatmapCache.OnInvalidated -= value;
        }

        public override bool IsAvailableLocally(BeatmapSetInfo model) => Realm.Run(realm => realm.All<BeatmapSetInfo>().Any(s => s.OnlineID == model.OnlineID && !s.DeletePending));

        #endregion

        #region Implementation of IPostImports<out BeatmapSetInfo>

        public Action<IEnumerable<Live<BeatmapSetInfo>>>? PresentImport
        {
            set => beatmapImporter.PresentImport = value;
        }

        #endregion

        public override string HumanisedModelName => "beatmap";
    }

    /// <summary>
    /// Delegate type for beatmap processing callbacks.
    /// </summary>
    /// <param name="beatmapSet">The beatmap set to be processed.</param>
    /// <param name="lookupScope">The scope to use when looking up metadata.</param>
    public delegate void ProcessBeatmapDelegate(BeatmapSetInfo beatmapSet, MetadataLookupScope lookupScope);
}
