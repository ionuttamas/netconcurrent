using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Spring.Collections
{
    /// <summary>
    /// Basic functionality test cases for implementation of <see cref="ICollection"/>.
    /// </summary>
    /// <author>Kenneth Xu</author>
    public abstract class CollectionFunctionTestBase : EnumerableFunctionTestBase
    {
        protected sealed override IEnumerable Enumerable
        {
            get { return Collection; }
        }

        protected abstract ICollection Collection { get; }

        [Test] public void CopyToChokesWithNullArray()
        {
            Assert.Throws<ArgumentNullException>(delegate
            {
                Collection.CopyTo(null, 0);
            });
        }


        [Test] public void CopyToChokesWithNegativeIndex()
        {
            var c = Collection;
            Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                c.CopyTo(new object[0], -1);
            });

            Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                c.CopyTo(NewArray<object>(1, c.Count), 0);
            });
        }

        [Test] public void CopyToChokesWhenArrayIsTooSmallToHold()
        {
            var c = Collection;
            Assert.Throws<ArgumentException>(delegate
            {
                c.CopyTo(new object[c.Count -1], 0);
            });
            Assert.Throws<ArgumentException>(delegate
            {
                c.CopyTo(NewArray<object>(1, c.Count-1), 1);
            });
            Assert.Throws<ArgumentException>(delegate
            {
                c.CopyTo(NewArray<object>(1, c.Count), 2);
            });
        }

        [Test] public void CopyToZeroLowerBoundArray()
        {
            var c = Collection;
            var target = new object[c.Count];
            c.CopyTo(target, 0);
            CollectionAssert.AreEqual(target, c);
        }

        [Test] public void CopyToArbitraryLowerBoundArray()
        {
            var c = Collection;
            var target = NewArray<object>(1, c.Count);
            c.CopyTo(target, 1);
            CollectionAssert.AreEqual(target, c);
        }

        public void CopyTo(Array array, int index)
        {
            var collection = Collection;
            object[] objects = new object[10];
            collection.CopyTo(objects, 0);
        }

        public void SyncRoot()
        {
            
        }

        public void IsSynchronized()
        {
            
        }

        private static Array NewArray<T>(int from, int to)
        {
            return Array.CreateInstance(
                typeof(T), // Array type
                new int[] { to - from + 1 }, // Size
                new int[] { from }); // lower bound
        }

    }

    [TestFixture(typeof(string))]
    [TestFixture(typeof(int))]
    public class ListAsCollectionTest<T> : CollectionFunctionTestBase
    {
        protected override ICollection Collection
        {
            get { return new ArrayList(TestData<T>.MakeTestArray(55)); }
        }
    }

    [TestFixture(typeof(string))]
    [TestFixture(typeof(int))]
    public class ArrayAsCollectionTest<T> : CollectionFunctionTestBase
    {
        [SetUp]
        public void SetUp()
        {
            AntiHangingLimit = 600;
        }
        protected override ICollection Collection
        {
            get { return TestData<T>.MakeTestArray(555); }
        }
    }

}
