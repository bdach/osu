// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Screens.Select;

namespace osu.Game.Beatmaps
{
    public static class BeatmapMetadataInfoExtensions
    {
        public static bool Match(IBeatmapMetadataInfo metadataInfo, FilterCriteria.OptionalTextFilter filter)
        {
            if (filter.Matches(metadataInfo.Artist)) return true;
            if (filter.Matches(metadataInfo.ArtistUnicode)) return true;
            if (filter.Matches(metadataInfo.Title)) return true;
            if (filter.Matches(metadataInfo.TitleUnicode)) return true;
            if (filter.Matches(metadataInfo.Source)) return true;
            if (filter.Matches(metadataInfo.Tags)) return true;

            return false;
        }
    }
}
