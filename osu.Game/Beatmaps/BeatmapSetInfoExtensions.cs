// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Localisation;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Models;
using osu.Game.Online.API;
using osu.Game.Rulesets;

namespace osu.Game.Beatmaps
{
    public static class BeatmapSetInfoExtensions
    {
        /// <summary>
        /// Returns the storage path for the file in this beatmapset with the given filename, if any exists, otherwise null.
        /// The path returned is relative to the user file storage.
        /// The lookup is case insensitive.
        /// </summary>
        /// <param name="model">The model to operate on.</param>
        /// <param name="filename">The name of the file to get the storage path of.</param>
        public static string? GetPathForFile(this IHasRealmFiles model, string filename) => model.GetFile(filename)?.File.GetStoragePath();

        /// <summary>
        /// A user-presentable display title representing this metadata.
        /// </summary>
        public static string GetDisplayTitle(this IBeatmapSetInfo setInfo)
        {
            string author = string.IsNullOrEmpty(setInfo.Host.Username) ? string.Empty : $" ({setInfo.Host.Username})";

            string artist = string.IsNullOrEmpty(setInfo.Metadata.Artist) ? "unknown artist" : setInfo.Metadata.Artist;
            string title = string.IsNullOrEmpty(setInfo.Metadata.Title) ? "unknown title" : setInfo.Metadata.Title;

            return $"{artist} - {title}{author}".Trim();
        }

        /// <summary>
        /// A user-presentable display title representing this beatmap, with localisation handling for potentially romanisable fields.
        /// </summary>
        public static RomanisableString GetDisplayTitleRomanisable(this IBeatmapSetInfo setInfo, bool includeCreator = true)
        {
            string author = !includeCreator || string.IsNullOrEmpty(setInfo.Host.Username) ? string.Empty : $"({setInfo.Host.Username})";
            string artistUnicode = string.IsNullOrEmpty(setInfo.Metadata.ArtistUnicode) ? setInfo.Metadata.Artist : setInfo.Metadata.ArtistUnicode;
            string titleUnicode = string.IsNullOrEmpty(setInfo.Metadata.TitleUnicode) ? setInfo.Metadata.Title : setInfo.Metadata.TitleUnicode;

            return new RomanisableString($"{artistUnicode} - {titleUnicode} {author}".Trim(), $"{setInfo.Metadata.Artist} - {setInfo.Metadata.Title} {author}".Trim());
        }

        /// <summary>
        /// Returns the file usage for the file in this beatmapset with the given filename, if any exists, otherwise null.
        /// The path returned is relative to the user file storage.
        /// The lookup is case insensitive.
        /// </summary>
        /// <param name="model">The model to operate on.</param>
        /// <param name="filename">The name of the file to get the storage path of.</param>
        public static RealmNamedFileUsage? GetFile(this IHasRealmFiles model, string filename) =>
            model.Files.SingleOrDefault(f => string.Equals(f.Filename, filename, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Get the beatmapset info page URL, or <c>null</c> if unavailable.
        /// </summary>
        public static string? GetOnlineURL(this IBeatmapSetInfo beatmapSetInfo, IAPIProvider api, IRulesetInfo? ruleset = null)
        {
            if (beatmapSetInfo.OnlineID <= 0)
                return null;

            if (ruleset != null)
                return $@"{api.Endpoints.WebsiteUrl}/beatmapsets/{beatmapSetInfo.OnlineID}#{ruleset.ShortName}";

            return $@"{api.Endpoints.WebsiteUrl}/beatmapsets/{beatmapSetInfo.OnlineID}";
        }
    }
}
