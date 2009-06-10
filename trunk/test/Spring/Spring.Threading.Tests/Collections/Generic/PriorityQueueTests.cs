using System;
using System.Collections.Generic;
using NUnit.Framework;
using Spring.Threading;
using Spring.Threading.Collections;

namespace Spring.Collections.Generic
{
    [TestFixture]
    public class PriorityQueueTests : BaseThreadingTestCase
    {
        private class MyReverseComparator : IComparer<int>
        {
            #region IComparer<int> Members

            public int Compare(int x, int y)
            {
                if (x < y) return 1;
                if (x > y) return -1;
                return 0;
            }

            #endregion
        }

        private PriorityQueue<int> populatedQueue(int n)
        {
            PriorityQueue<int> q = new PriorityQueue<int>(n);
            Assert.IsTrue(q.IsEmpty);
            for (int i = n - 1; i >= 0; i -= 2)
                Assert.IsTrue(q.Offer(i));
            for (int i = (n & 1); i < n; i += 2)
                Assert.IsTrue(q.Offer(i));
            Assert.IsFalse(q.IsEmpty);
            Assert.AreEqual(n, q.Count);
            return q;
        }

        [Test]
        public void testAdd()
        {
            PriorityQueue<int> q = new PriorityQueue<int>(DEFAULT_COLLECTION_SIZE);
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
            {
                Assert.AreEqual(i, q.Count);
                Assert.IsTrue(q.Add(i));
            }
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void testAddAll1()
        {
            PriorityQueue<int> q = new PriorityQueue<int>(1);
            q.AddRange(null);
        }


        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void testAddAll2()
        {
            PriorityQueue<object> q = new PriorityQueue<object>(DEFAULT_COLLECTION_SIZE);
            object[] ints = new object[DEFAULT_COLLECTION_SIZE];
            q.AddRange(ints);
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void testAddAll3()
        {
            PriorityQueue<object> q = new PriorityQueue<object>(DEFAULT_COLLECTION_SIZE);
            object[] ints = new object[DEFAULT_COLLECTION_SIZE];
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE - 1; ++i)
                ints[i] = i;
            q.AddRange(ints);
        }

        [Test]
        public void testAddAll5()
        {
            int[] empty = new int[0];
            int[] ints = new int[DEFAULT_COLLECTION_SIZE];
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
                ints[i] = DEFAULT_COLLECTION_SIZE - 1 - i;
            PriorityQueue<int> q = new PriorityQueue<int>(DEFAULT_COLLECTION_SIZE);
            Assert.IsFalse(q.AddRange(empty));
            Assert.IsTrue(q.AddRange(ints));
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
            {
                int output;
                q.Poll(out output);
                Assert.AreEqual(i, output);
            }
        }

        [Test]
        public void testClear()
        {
            PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
            q.Clear();
            Assert.IsTrue(q.IsEmpty);
            Assert.AreEqual(0, q.Count);
            q.Add(1);
            Assert.IsFalse(q.IsEmpty);
            q.Clear();
            Assert.IsTrue(q.IsEmpty);
        }

        [Test]
        public void testConstructor1()
        {
            Assert.AreEqual(0, new PriorityQueue<int>(DEFAULT_COLLECTION_SIZE).Count);
        }

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void testConstructor2()
        {
            PriorityQueue<int> q = new PriorityQueue<int>(0);
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void testConstructor3()
        {
            PriorityQueue<int> q = new PriorityQueue<int>(null);
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void testConstructor4()
        {
            object[] ints = new object[DEFAULT_COLLECTION_SIZE];
            PriorityQueue<object> q = new PriorityQueue<object>(ints);
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void testConstructor5()
        {
            object[] ints = new object[DEFAULT_COLLECTION_SIZE];
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE - 1; ++i)
                ints[i] = i;
            PriorityQueue<object> q = new PriorityQueue<object>(ints);
        }

        [Test]
        public void testConstructor6()
        {
            int[] ints = new int[DEFAULT_COLLECTION_SIZE];
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
                ints[i] = i;
            PriorityQueue<int> q = new PriorityQueue<int>(ints);
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
            {
                int output;
                q.Poll(out output);
                Assert.AreEqual(ints[i], output);
            }
        }

        [Test]
        public void testConstructor7()
        {
            MyReverseComparator cmp = new MyReverseComparator();
            PriorityQueue<int> q = new PriorityQueue<int>(DEFAULT_COLLECTION_SIZE, cmp);
            Assert.AreEqual(cmp, q.Comparator());
            int[] ints = new int[DEFAULT_COLLECTION_SIZE];
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
                ints[i] = i;
            q.AddRange(ints);
            for (int i = DEFAULT_COLLECTION_SIZE - 1; i >= 0; --i)
            {
                int output;
                q.Poll(out output);
                Assert.AreEqual(ints[i], output);
            }
        }

        [Test]
        [Ignore("Fix when we know what to do w/ int generics.")]
        public void testContains()
        {
            PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
            {
                Assert.IsTrue(q.Contains(i));
                int output;
                q.Poll(out output);
                Assert.IsFalse(q.Contains(i));
            }
        }

        [Test]
        [Ignore("Fix when we know what to do w/ int generics.")]
        public void testContainsAll()
        {
//            PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
//            var p = new PriorityQueue<int>(DEFAULT_COLLECTION_SIZE);
//            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
//            {
//                Assert.IsTrue(q.ContainsAll(p));
//                Assert.IsFalse(p.ContainsAll(q));
//                p.Add(i);
//            }
//            Assert.IsTrue(p.ContainsAll(q));
        }


        [Test]
        public void testCount()
        {
            PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
            {
                Assert.AreEqual(DEFAULT_COLLECTION_SIZE - i, q.Count);
                q.Remove();
            }
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
            {
                Assert.AreEqual(i, q.Count);
                q.Add(i);
            }
        }

        [Test]
        [ExpectedException(typeof (NoElementsException))]
        public void testElement()
        {
            PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
            {
                Assert.AreEqual(i, (q.Element()));
                int output;
                q.Poll(out output);
            }
            q.Element();
        }

        [Test]
        public void testEmpty()
        {
            PriorityQueue<int> q = new PriorityQueue<int>(2);
            Assert.IsTrue(q.IsEmpty);
            q.Add(1);
            Assert.IsFalse(q.IsEmpty);
            q.Add(2);
            q.Remove();
            q.Remove();
            Assert.IsTrue(q.IsEmpty);
        }

        [Test]
        public void testIterator()
        {
            PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
            int i = 0;
            IEnumerator<int> it = q.GetEnumerator();
            while (it.MoveNext())
            {
                Assert.IsTrue(q.Contains(it.Current));
                ++i;
            }
            Assert.AreEqual(i, DEFAULT_COLLECTION_SIZE);
        }

        [Test]
        public void testOffer()
        {
            PriorityQueue<int> q = new PriorityQueue<int>(1);
            Assert.IsTrue(q.Offer(zero));
            Assert.IsTrue(q.Offer(one));
        }

        [Test]
        public void testPeek()
        {
            PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
            {
                int peekOut;
                q.Peek(out peekOut);
                Assert.AreEqual(i, peekOut);
                int pollOut;
                q.Poll(out pollOut);
                Assert.IsTrue(!q.Peek(out peekOut) || i != (peekOut));
            }
            int finalOut;
            Assert.IsFalse(q.Peek(out finalOut));
        }

        [Test]
        public void testPoll()
        {
            PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
            {
                int pollOut;
                q.Poll(out pollOut);
                Assert.AreEqual(i, pollOut);
            }
            int finalOut;
            Assert.IsFalse(q.Poll(out finalOut));
        }

        [Test]
        [ExpectedException(typeof (NoElementsException))]
        public void testRemove()
        {
            PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
            {
                Assert.AreEqual(i, (q.Remove()));
            }
            q.Remove();
        }

        [Test]
        [Ignore("Fix when we know what to do w/ int generics.")]
        public void testRemoveAll()
        {
//            for (int i = 1; i < DEFAULT_COLLECTION_SIZE; ++i)
//            {
//                PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
//                PriorityQueue<int> p = populatedQueue(i);
//                Assert.IsTrue(q.RemoveAll(p));
//                Assert.AreEqual(DEFAULT_COLLECTION_SIZE - i, q.Count);
//                for (int j = 0; j < i; ++j)
//                {
//                    int I = (p.Remove());
//                    Assert.IsFalse(q.Contains(I));
//                }
//            }
        }

        [Test]
        public void testRemoveElement()
        {
            PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
            for (int i = 1; i < DEFAULT_COLLECTION_SIZE; i += 2)
            {
                Assert.IsTrue(q.Remove(i));
            }
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; i += 2)
            {
                Assert.IsTrue(q.Remove(i));
                Assert.IsFalse(q.Remove(i + 1));
            }
            Assert.IsTrue(q.IsEmpty);
        }

        [Test]
        [Ignore("Fix when we know what to do w/ int generics.")]
        public void testRetainAll()
        {
//            PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
//            PriorityQueue<int> p = populatedQueue(DEFAULT_COLLECTION_SIZE);
//            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
//            {
//                bool changed = q.RetainAll(p);
//                if (i == 0)
//                    Assert.IsFalse(changed);
//                else
//                    Assert.IsTrue(changed);
//
//                Assert.IsTrue(q.ContainsAll(p));
//                Assert.AreEqual(DEFAULT_COLLECTION_SIZE - i, q.Count);
//                p.Remove();
//            }
        }

        [Test]
        public void testToString()
        {
            PriorityQueue<int> q = populatedQueue(DEFAULT_COLLECTION_SIZE);
            String s = q.ToString();
            for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
            {
                Assert.IsTrue(s.IndexOf(i.ToString()) >= 0);
            }
        }
    }
}