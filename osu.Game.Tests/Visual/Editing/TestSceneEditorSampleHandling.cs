// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Skinning;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    public class TestSceneEditorSampleHandling : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Test]
        public void TestSlidingSampleStopsOnSeek()
        {
            DrawableSlider slider = null;
            PoolableSkinnableSample[] loopingSamples = null;
            PoolableSkinnableSample[] onceOffSamples = null;

            AddStep("get first slider", () =>
            {
                slider = Editor.ChildrenOfType<DrawableSlider>().OrderBy(s => s.HitObject.StartTime).First();
                onceOffSamples = slider.ChildrenOfType<PoolableSkinnableSample>().Where(s => !s.Looping).ToArray();
                loopingSamples = slider.ChildrenOfType<PoolableSkinnableSample>().Where(s => s.Looping).ToArray();
            });

            AddStep("start playback", () => EditorClock.Start());

            AddUntilStep("wait for slider sliding then seek", () =>
            {
                if (!slider.Tracking.Value)
                    return false;

                if (!loopingSamples.Any(s => s.Playing))
                    return false;

                EditorClock.Seek(20000);
                return true;
            });

            AddAssert("non-looping samples are playing", () => onceOffSamples.Length == 4 && loopingSamples.All(s => s.Played || s.Playing));
            AddAssert("looping samples are not playing", () => loopingSamples.Length == 1 && loopingSamples.All(s => s.Played && !s.Playing));
        }

        [Test]
        public void TestEditPreservesNestedSamples()
        {
            DrawableSlider slider = null;
            List<HitSampleInfo> headSamples = null;

            AddStep("get first slider", () =>
            {
                slider = Editor.ChildrenOfType<DrawableSlider>().OrderBy(s => s.HitObject.StartTime).First();
                var headSkinnableSound = slider.HeadCircle.ChildrenOfType<SkinnableSound>().Single();
                headSamples = headSkinnableSound.Samples.OfType<HitSampleInfo>().Select(s => s.With()).ToList();
            });

            AddStep("select slider", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(slider.HitObject);
            });
            AddStep("flip slider", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.J);
                InputManager.ReleaseKey(Key.ControlLeft);
            });

            AddAssert("slider head samples unchanged", () =>
            {
                var headSkinnableSound = slider.HeadCircle.ChildrenOfType<SkinnableSound>().Single();
                var newHeadSamples = headSkinnableSound.Samples.OfType<HitSampleInfo>().Select(s => s.With()).ToList();
                return newHeadSamples.SequenceEqual(headSamples);
            });
        }
    }
}
