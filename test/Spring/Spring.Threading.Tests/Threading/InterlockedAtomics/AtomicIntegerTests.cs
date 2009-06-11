#region License

/*
 * Copyright 2002-2008 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using NUnit.Framework;

namespace Spring.Threading.AtomicTypes
{
    /// <summary>
    /// Unit tests for the AtomicLong class
    /// </summary>
    /// <author>Griffin Caprio (.NET)</author>
    /// <author>Andreas Doehring (.NET)</author>
    [TestFixture]
    public class AtomicIntegerTests : BaseThreadingTestCase
    {
        [Test]
        public void Constructor()
        {
            AtomicInteger ai = new AtomicInteger(1);
            Assert.AreEqual(1, ai.Value);
        }

        [Test]
        public void DefaultConstructor()
        {
            AtomicInteger ai = new AtomicInteger();
            Assert.AreEqual(0, ai.Value);
        }

        [Test]
        public void GetLastSetValue()
        {
            AtomicInteger ai = new AtomicInteger(1);
            Assert.AreEqual(1, ai.Value);
            ai.Value = 2;
            Assert.AreEqual(2, ai.Value);
            ai.Value = -3;
            Assert.AreEqual(-3, ai.Value);
        }

        [Test]
        public void GetLastLazySetValue()
        {
            AtomicInteger ai = new AtomicInteger(1);
            Assert.AreEqual(1, ai.Value);
            ai.LazySet(2);
            Assert.AreEqual(2, ai.Value);
            ai.LazySet(-3);
            Assert.AreEqual(-3, ai.Value);
        }

        [Test]
        public void CompareExpectedValueAndSetNewValue()
        {
            AtomicInteger ai = new AtomicInteger(1);
            Assert.IsTrue(ai.CompareAndSet(1, 2));
            Assert.IsTrue(ai.CompareAndSet(2, -4));
            Assert.AreEqual(-4, ai.Value);
            Assert.IsFalse(ai.CompareAndSet(-5, 7));
            Assert.IsFalse((7 == ai.Value));
            Assert.IsTrue(ai.CompareAndSet(-4, 7));
            Assert.AreEqual(7, ai.Value);
        }

        [Test]
        public void CompareExpectedValueAndSetNewValueInMultipleThreads()
        {
            AtomicInteger ai = new AtomicInteger(1);
            Thread t = new Thread(delegate()
            {
                while (!ai.CompareAndSet(2, 3))
                    Thread.Sleep(100);
            });
            t.Start();
            Assert.IsTrue(ai.CompareAndSet(1, 2));
            t.Join(LONG_DELAY);
            Assert.IsFalse(t.IsAlive);
            Assert.AreEqual(ai.Value, 3);
        }

        [Test]
        public void WeakCompareExpectedValueAndSetNewValue()
        {
            AtomicInteger ai = new AtomicInteger(1);
            while (!ai.WeakCompareAndSet(1, 2)) { }
            while (!ai.WeakCompareAndSet(2, -4)) { }
            Assert.AreEqual(-4, ai.Value);
            while (!ai.WeakCompareAndSet(-4, 7)) { }
            Assert.AreEqual(7, ai.Value);
            Assert.IsFalse(ai.WeakCompareAndSet(-4, 7));
        }

        [Test]
        public void Exchange()
        {
            AtomicInteger ai = new AtomicInteger(1);
            Assert.AreEqual(1, ai.Exchange(0));
            Assert.AreEqual(0, ai.Exchange(-10));
            Assert.AreEqual(-10, ai.Exchange(1));
        }

        [Test]
        public void AddDeltaAndReturnPreviousValue()
        {
            AtomicInteger ai = new AtomicInteger(1);
            Assert.AreEqual(1, ai.AddDeltaAndReturnPreviousValue(2));
            Assert.AreEqual(3, ai.Value);
            Assert.AreEqual(3, ai.AddDeltaAndReturnPreviousValue(-4));
            Assert.AreEqual(-1, ai.Value);
        }

        [Test]
        public void ReturnValueAndDecrement()
        {
            AtomicInteger ai = new AtomicInteger(1);
            Assert.AreEqual(1, ai.ReturnValueAndDecrement());
            Assert.AreEqual(0, ai.ReturnValueAndDecrement());
            Assert.AreEqual(-1, ai.ReturnValueAndDecrement());
        }

        [Test]
        public void ReturnValueAndIncrement()
        {
            AtomicInteger ai = new AtomicInteger(1);
            Assert.AreEqual(1, ai.ReturnValueAndIncrement());
            Assert.AreEqual(2, ai.Value);
            ai.Value = -2;
            Assert.AreEqual(-2, ai.ReturnValueAndIncrement());
            Assert.AreEqual(-1, ai.ReturnValueAndIncrement());
            Assert.AreEqual(0, ai.ReturnValueAndIncrement());
            Assert.AreEqual(1, ai.Value);
        }

        [Test]
        public void AddDeltaAndReturnNewValue()
        {
            AtomicInteger ai = new AtomicInteger(1);
            Assert.AreEqual(3, ai.AddDeltaAndReturnNewValue(2));
            Assert.AreEqual(3, ai.Value);
            Assert.AreEqual(-1, ai.AddDeltaAndReturnNewValue(-4));
            Assert.AreEqual(-1, ai.Value);
        }

        [Test]
        public void DecrementValueAndReturn()
        {
            AtomicInteger ai = new AtomicInteger(1);
            Assert.AreEqual(0, ai.DecrementValueAndReturn());
            Assert.AreEqual(-1, ai.DecrementValueAndReturn());
            Assert.AreEqual(-2, ai.DecrementValueAndReturn());
            Assert.AreEqual(-2, ai.Value);
        }

        [Test]
        public void IncrementValueAndReturn()
        {
            AtomicInteger ai = new AtomicInteger(1);
            Assert.AreEqual(2, ai.IncrementValueAndReturn());
            Assert.AreEqual(2, ai.Value);
            ai.Value = -2;
            Assert.AreEqual(-1, ai.IncrementValueAndReturn());
            Assert.AreEqual(0, ai.IncrementValueAndReturn());
            Assert.AreEqual(1, ai.IncrementValueAndReturn());
            Assert.AreEqual(1, ai.Value);
        }

        [Test]
        public void SerializationAndDeserialization()
        {
            AtomicInteger l = new AtomicInteger();

            l.Value = 22;
            MemoryStream bout = new MemoryStream(10000);

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(bout, l);

            MemoryStream bin = new MemoryStream(bout.ToArray());
            BinaryFormatter formatter2 = new BinaryFormatter();
            AtomicInteger r = (AtomicInteger)formatter2.Deserialize(bin);
            Assert.AreEqual(l.Value, r.Value);
        }

        [Test]
        public void Value()
        {
            AtomicInteger ai = new AtomicInteger(42);
            Assert.AreEqual(ai.Value, 42);
        }

        [Test]
        public void ToStringTest()
        {
            AtomicInteger ai = new AtomicInteger();
            for (int i = -12; i < 6; ++i)
            {
                ai.Value = i;
                Assert.AreEqual(ai.ToString(), Convert.ToString(i));
            }
        }

        [Test]
        public void ImplicitConverter()
        {
            AtomicInteger ai = new AtomicInteger(1);
            int result = ai;
            Assert.AreEqual(1, result);
            ai.Value = 3;
            result = ai;
            Assert.AreEqual(3, result);
        }
    }
}