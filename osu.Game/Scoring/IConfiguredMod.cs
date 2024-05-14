// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Configuration;

namespace osu.Game.Scoring
{
    public interface IConfiguredMod : IEquatable<IConfiguredMod>
    {
        string Acronym { get; }

        IReadOnlyDictionary<string, object> Settings { get; }

        bool IEquatable<IConfiguredMod>.Equals(IConfiguredMod? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Acronym == other.Acronym && Settings.SequenceEqual(other.Settings, ModSettingsEqualityComparer.Default);
        }

        private class ModSettingsEqualityComparer : IEqualityComparer<KeyValuePair<string, object>>
        {
            public static ModSettingsEqualityComparer Default { get; } = new ModSettingsEqualityComparer();

            public bool Equals(KeyValuePair<string, object> x, KeyValuePair<string, object> y)
            {
                object xValue = x.Value.GetUnderlyingSettingValue();
                object yValue = y.Value.GetUnderlyingSettingValue();

                return x.Key == y.Key && EqualityComparer<object>.Default.Equals(xValue, yValue);
            }

            public int GetHashCode(KeyValuePair<string, object> obj) => HashCode.Combine(obj.Key, obj.Value.GetUnderlyingSettingValue());
        }
    }
}
