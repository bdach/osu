// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Objects;

namespace osu.Game.Screens.Edit
{
    public class AddHitObjectCommand : ICommand
    {
        public HitObject HitObject { get; set; }

        public AddHitObjectCommand(HitObject hitObject)
        {
            HitObject = hitObject;
        }

        public void Apply(EditorBeatmap editorBeatmap)
        {
            editorBeatmap.Add(HitObject);
        }

        public void Rollback(EditorBeatmap editorBeatmap)
        {
            editorBeatmap.Remove(HitObject);
        }
    }
}
