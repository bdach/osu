// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace osu.Game.Online.Spectator
{
    [Serializable]
    [MessagePackObject]
    public class EndPlaySessionV2Response
    {
        [Key(0)]
        public long? ScoreToken { get; set; }

        [Key(1)]
        public List<SequenceNumberRange> MissingFrameBundles { get; set; } = [];

        [IgnoreMember]
        public long MissingFrameBundleCount => MissingFrameBundles.Sum(range => range.End - range.Start + 1);

        [Serializable]
        [MessagePackObject]
        public class SequenceNumberRange : IEquatable<SequenceNumberRange>
        {
            [Key(0)]
            public long Start { get; set; }

            [Key(1)]
            public long End { get; set; }

            public SequenceNumberRange(long start, long end)
            {
                Start = start;
                End = end;
            }

            public override string ToString() => $@"[{Start},{End}]";

            public bool Equals(SequenceNumberRange? other)
                => other != null && Start == other.Start && End == other.End;
        }

        [SerializationConstructor]
        public EndPlaySessionV2Response(long? scoreToken, List<SequenceNumberRange> missingFrameBundles)
        {
            ScoreToken = scoreToken;
            MissingFrameBundles = missingFrameBundles;
        }

        public EndPlaySessionV2Response(long? scoreToken, ICollection<long> frameBundlesReceived, SequenceNumberRange sequenceNumberRange)
        {
            ScoreToken = scoreToken;

            long[] orderedSequenceNumbers = frameBundlesReceived
                                            .OrderBy(b => b)
                                            .ToArray();

            if (orderedSequenceNumbers.Length == 0)
            {
                MissingFrameBundles.Add(sequenceNumberRange);
            }
            else
            {
                long lastSeenSequenceNumber = orderedSequenceNumbers[0];

                if (lastSeenSequenceNumber > sequenceNumberRange.Start)
                    MissingFrameBundles.Add(new SequenceNumberRange(sequenceNumberRange.Start, lastSeenSequenceNumber - 1));

                for (int i = 1; i < orderedSequenceNumbers.Length; ++i)
                {
                    long sequenceNumber = orderedSequenceNumbers[i];

                    if (sequenceNumber != lastSeenSequenceNumber + 1)
                        MissingFrameBundles.Add(new SequenceNumberRange(lastSeenSequenceNumber + 1, sequenceNumber - 1));

                    lastSeenSequenceNumber = sequenceNumber;
                }

                if (lastSeenSequenceNumber < sequenceNumberRange.End)
                    MissingFrameBundles.Add(new SequenceNumberRange(lastSeenSequenceNumber, sequenceNumberRange.End));
            }
        }

        public IEnumerable<FrameDataBundle> GetMissingFrameBundles(ICollection<FrameDataBundle> frameBundlesSent)
        {
            if (MissingFrameBundles.Count == 0)
            {
                foreach (var bundle in frameBundlesSent)
                    yield return bundle;
            }

            int lastRangeIndex = 0;
            var lastRange = MissingFrameBundles[lastRangeIndex];

            foreach (var frameBundle in frameBundlesSent.OrderBy(b => b.SequenceNumber))
            {
                long? sequenceNumber = frameBundle.SequenceNumber;

                if (sequenceNumber == null)
                    continue;

                if (sequenceNumber < lastRange.Start)
                    continue;

                if (sequenceNumber > lastRange.End)
                {
                    if (lastRangeIndex < MissingFrameBundles.Count - 1)
                    {
                        lastRangeIndex += 1;
                        lastRange = MissingFrameBundles[lastRangeIndex];
                    }

                    continue;
                }

                yield return frameBundle;
            }
        }
    }
}
