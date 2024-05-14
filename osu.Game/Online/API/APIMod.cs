// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;

namespace osu.Game.Online.API
{
    [MessagePackObject]
    public class APIMod : IConfiguredMod
    {
        [JsonProperty("acronym")]
        [Key(0)]
        public string Acronym { get; set; } = string.Empty;

        [JsonProperty("settings")]
        [Key(1)]
        [MessagePackFormatter(typeof(ModSettingsDictionaryFormatter))]
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        IReadOnlyDictionary<string, object> IConfiguredMod.Settings => Settings;

        [JsonConstructor]
        [SerializationConstructor]
        public APIMod()
        {
        }

        public APIMod(Mod mod)
        {
            Acronym = mod.Acronym;

            foreach (var (_, property) in mod.GetSettingsSourceProperties())
            {
                var bindable = (IBindable)property.GetValue(mod)!;

                if (!bindable.IsDefault)
                    Settings.Add(property.Name.ToSnakeCase(), bindable.GetUnderlyingSettingValue());
            }
        }

        public bool ShouldSerializeSettings() => Settings.Count > 0;

        public override string ToString()
        {
            if (Settings.Count > 0)
                return $"{Acronym} ({string.Join(',', Settings.Select(kvp => $"{kvp.Key}:{kvp.Value}"))})";

            return $"{Acronym}";
        }
    }
}
