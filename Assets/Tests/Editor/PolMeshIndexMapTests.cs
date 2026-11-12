// See LICENSE in the project root.

namespace Pal3.Core.Tests
{
    using System;
    using DataReader.Pol;
    using NUnit.Framework;

    public sealed class PolMeshIndexMapTests
    {
        [Test]
        public void CompactionPreservesEverySourceIndexAndTriangleCorner()
        {
            int[] triangles = { 5, 2, 5, 2, 0, 5 };
            var map = new PolMeshIndexMap(7, triangles);
            Assert.That(map.SourceIndices, Is.EqualTo(new[] { 5, 2, 0 }));
            Assert.That(map.Triangles, Is.EqualTo(new[] { 0, 1, 0, 1, 2, 0 }));
            for (int i = 0; i < triangles.Length; i++)
                Assert.That(map.SourceIndices[map.Triangles[i]], Is.EqualTo(triangles[i]));
            Assert.That(triangles, Is.EqualTo(new[] { 5, 2, 5, 2, 0, 5 }));
        }

        [TestCase(-1)]
        [TestCase(4)]
        public void OutOfRangeSourceIndexIsRejected(int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PolMeshIndexMap(4, new[] { 0, index, 2 }));
        }

        [Test]
        public void IncompleteTriangleIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new PolMeshIndexMap(4, new[] { 0, 1 }));
        }

        [Test]
        public void EmptyGeometryHasNoSourceIndices()
        {
            var map = new PolMeshIndexMap(0, Array.Empty<int>());
            Assert.That(map.SourceIndices, Is.Empty);
            Assert.That(map.Triangles, Is.Empty);
        }
    }
}
