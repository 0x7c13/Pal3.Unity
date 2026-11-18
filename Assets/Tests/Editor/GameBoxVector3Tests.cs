// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using Core.Primitives;
    using NUnit.Framework;

    public sealed class GameBoxVector3Tests
    {
        [TestCase(1, 2, 3)]
        [TestCase(-70.413155f, .125f, 140.5f)]
        public void EqualityIsReflexiveForDistinctAxisValues(float x, float y, float z)
        {
            var value = new GameBoxVector3(x, y, z);
            var copy = new GameBoxVector3(x, y, z);
            Assert.That(value.Equals(value), Is.True);
            Assert.That(value.Equals((object)copy), Is.True);
            Assert.That(value == copy, Is.True);
            Assert.That(value != copy, Is.False);
        }

        [Test]
        public void DifferentZCoordinatesAreUnequalInBothDirections()
        {
            var first = new GameBoxVector3(1, 2, 1);
            var second = new GameBoxVector3(1, 2, 3);
            Assert.That(first.Equals(second), Is.False);
            Assert.That(second.Equals(first), Is.False);
            Assert.That(first == second, Is.False);
            Assert.That(second == first, Is.False);
            Assert.That(first != second, Is.True);
        }
    }
}
