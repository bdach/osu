// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Online.Spectator;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Tests.Online
{
    [TestFixture]
    public class TestEndPlaySessionV2Response
    {
        [Test]
        public void TestCorrectCompression()
        {
            long[] sequenceNumbers =
            [
                1,
                3, 4, 5, 6,
                11, 12, 13,
                15,
                17, 18, 19, 20
            ];
            var response = new EndPlaySessionV2Response(1234, sequenceNumbers, new EndPlaySessionV2Response.SequenceNumberRange(0, 25));

            Assert.That(response.MissingFrameBundles, Has.Count.EqualTo(6));
            Assert.That(response.MissingFrameBundles[0], Is.EqualTo(new EndPlaySessionV2Response.SequenceNumberRange(0, 0)));
            Assert.That(response.MissingFrameBundles[1], Is.EqualTo(new EndPlaySessionV2Response.SequenceNumberRange(2, 2)));
            Assert.That(response.MissingFrameBundles[2], Is.EqualTo(new EndPlaySessionV2Response.SequenceNumberRange(7, 10)));
            Assert.That(response.MissingFrameBundles[3], Is.EqualTo(new EndPlaySessionV2Response.SequenceNumberRange(14, 14)));
            Assert.That(response.MissingFrameBundles[4], Is.EqualTo(new EndPlaySessionV2Response.SequenceNumberRange(16, 16)));
            Assert.That(response.MissingFrameBundles[5], Is.EqualTo(new EndPlaySessionV2Response.SequenceNumberRange(20, 25)));
            Assert.That(response.MissingFrameBundleCount, Is.EqualTo(14));
        }

        [Test]
        public void TestCorrectCompression_NothingMissing()
        {
            var response = new EndPlaySessionV2Response(1234, Enumerable.Range(0, 26).Select(i => (long)i).ToArray(), new EndPlaySessionV2Response.SequenceNumberRange(0, 25));

            Assert.That(response.MissingFrameBundles, Has.Count.EqualTo(0));
            Assert.That(response.MissingFrameBundleCount, Is.EqualTo(0));
        }

        [Test]
        public void TestCorrectCompression_AllMissing()
        {
            var response = new EndPlaySessionV2Response(1234, [], new EndPlaySessionV2Response.SequenceNumberRange(0, 25));

            Assert.That(response.MissingFrameBundles, Has.Count.EqualTo(1));
            Assert.That(response.MissingFrameBundles[0], Is.EqualTo(new EndPlaySessionV2Response.SequenceNumberRange(0, 25)));
            Assert.That(response.MissingFrameBundleCount, Is.EqualTo(26));
        }

        [Test]
        public void TestCorrectDecompression()
        {
            var frameBundles = Enumerable.Range(0, 26).Select(seq => new FrameDataBundle(new FrameHeader(new ScoreInfo(), new ScoreProcessorStatistics()), [], seq))
                                         .ToArray();
            var response = new EndPlaySessionV2Response(1234,
            [
                new EndPlaySessionV2Response.SequenceNumberRange(1, 1),
                new EndPlaySessionV2Response.SequenceNumberRange(3, 6),
                new EndPlaySessionV2Response.SequenceNumberRange(11, 13),
                new EndPlaySessionV2Response.SequenceNumberRange(15, 15),
                new EndPlaySessionV2Response.SequenceNumberRange(17, 20),
            ]);

            long?[] missingSequenceNumbers = response.GetMissingFrameBundles(frameBundles).Select(b => b.SequenceNumber).ToArray();
            Assert.That(missingSequenceNumbers, Is.EqualTo(
                [
                    1,
                    3, 4, 5, 6,
                    11, 12, 13,
                    15,
                    17, 18, 19, 20
                ]
            ));
        }
    }
}
