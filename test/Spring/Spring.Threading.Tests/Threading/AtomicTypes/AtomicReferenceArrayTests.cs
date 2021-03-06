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
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Spring.Threading.AtomicTypes
{
	[TestFixture]
	public class AtomicReferenceArrayTests : BaseThreadingTestCase
	{
		private class AnonymousClassRunnable
		{
			private AtomicReferenceArray<int> a;

			public AnonymousClassRunnable(AtomicReferenceArray<int> a)
			{
				InitBlock(a);
			}

            private void InitBlock(AtomicReferenceArray<int> a)
			{
				this.a = a;
			}

			[Test]
			public void Run()
			{
				while (!a.CompareAndSet(0, two, three))
					Thread.Sleep(SHORT_DELAY);
			}
		}

		[Test]
		public void DefaultConstructor()
		{
            AtomicReferenceArray<object> ai = new AtomicReferenceArray<object>(DEFAULT_COLLECTION_SIZE);
			for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
			{
				Assert.IsNull(ai[i]);
			}
		}

		[ExpectedException(typeof (ArgumentNullException))]
		[Test]
		public void NullReferenceExceptionForConstructor()
		{
			object[] a = null;
			new AtomicReferenceArray<object>(a);
		}

		[Test]
		public void NewAtomicReferenceArrayFromExistingArrayConstructor()
		{
			int[] a = new int[] {two, one, three, four, seven};
            AtomicReferenceArray<int> ai = new AtomicReferenceArray<int>(a);
			Assert.AreEqual(a.Length, ai.Length());
			for (int i = 0; i < a.Length; ++i)
				Assert.AreEqual(a[i], ai[i]);
		}


		[Test]
		public void OutOfBoundsIndexingException()
		{
            AtomicReferenceArray<int> ai = new AtomicReferenceArray<int>(DEFAULT_COLLECTION_SIZE);
			try
			{
				int a = ai[DEFAULT_COLLECTION_SIZE];
			}
			catch (IndexOutOfRangeException success)
			{
                string s = success.Message;
			}
			try
			{
				int a = ai[- 1];
			}
			catch (IndexOutOfRangeException success)
			{
                string s = success.Message;
			}
			try
			{
				ai.SetNewAtomicValue(DEFAULT_COLLECTION_SIZE, 0);
			}
			catch (IndexOutOfRangeException success)
			{
                string s = success.Message;
			}
			try
			{
				ai.SetNewAtomicValue(- 1, 0);
			}
			catch (IndexOutOfRangeException success)
			{
                string s = success.Message;
			}
		}

		[Test]
		public void GetReturnsLastValueSetAtIndex()
		{
            AtomicReferenceArray<int> ai = new AtomicReferenceArray<int>(DEFAULT_COLLECTION_SIZE);
			for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
			{
				ai.SetNewAtomicValue(i, one);
				Assert.AreEqual(one, ai[i]);
				ai.SetNewAtomicValue(i, two);
				Assert.AreEqual(two, ai[i]);
				ai.SetNewAtomicValue(i, m3);
				Assert.AreEqual(m3, ai[i]);
			}
		}

		[Test]
		public void GetReturnsLastValueLazySetAtIndex()
		{
            AtomicReferenceArray<int> ai = new AtomicReferenceArray<int>(DEFAULT_COLLECTION_SIZE);
			for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
			{
				ai.LazySet(i, one);
				Assert.AreEqual(one, ai[i]);
				ai.LazySet(i, two);
				Assert.AreEqual(two, ai[i]);
				ai.LazySet(i, m3);
				Assert.AreEqual(m3, ai[i]);
			}
		}

		[Test]
		public void CompareExistingValueAndSetNewValue()
		{
            AtomicReferenceArray<int> ai = new AtomicReferenceArray<int>(DEFAULT_COLLECTION_SIZE);
			for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
			{
				ai.SetNewAtomicValue(i, one);
				Assert.IsTrue(ai.CompareAndSet(i, one, two));
				Assert.IsTrue(ai.CompareAndSet(i, two, m4));
				Assert.AreEqual(m4, ai[i]);
				Assert.IsFalse(ai.CompareAndSet(i, m5, seven));
				Assert.IsFalse((seven.Equals(ai[i])));
				Assert.IsTrue(ai.CompareAndSet(i, m4, seven));
				Assert.AreEqual(seven, ai[i]);
			}
		}

		[Test]
		public void CompareAndSetInMultipleThreads()
		{
            AtomicReferenceArray<int> a = new AtomicReferenceArray<int>(1);
			a.SetNewAtomicValue(0, one);
			Thread t = new Thread(new ThreadStart(new AnonymousClassRunnable(a).Run));

			t.Start();
			Assert.IsTrue(a.CompareAndSet(0, one, two));
			t.Join(LONG_DELAY);
			Assert.IsFalse(t.IsAlive);
			Assert.AreEqual(a[0], three);
		}

		[Test]
		public void WeakCompareAndSet()
		{
            AtomicReferenceArray<int> ai = new AtomicReferenceArray<int>(DEFAULT_COLLECTION_SIZE);
			for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
			{
				ai.SetNewAtomicValue(i, one);
				while (!ai.WeakCompareAndSet(i, one, two))
					;
				while (!ai.WeakCompareAndSet(i, two, m4))
					;
				Assert.AreEqual(m4, ai[i]);
				while (!ai.WeakCompareAndSet(i, m4, seven))
					;
				Assert.AreEqual(seven, ai[i]);
			}
		}

		[Test]
		public void GetExistingValueAndSetNewValue()
		{
            AtomicReferenceArray<int> ai = new AtomicReferenceArray<int>(DEFAULT_COLLECTION_SIZE);
			for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
			{
				ai.SetNewAtomicValue(i, one);
				Assert.AreEqual(one, ai.SetNewAtomicValue(i, zero));
				Assert.AreEqual(0, ((int) ai.SetNewAtomicValue(i, m10)));
				Assert.AreEqual(m10, ai.SetNewAtomicValue(i, one));
			}
		}

		[Test]
		public void SerializeAndDeserialize()
		{
            AtomicReferenceArray<int> atomicReferenceArray = new AtomicReferenceArray<int>(DEFAULT_COLLECTION_SIZE);
			for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
			{
				atomicReferenceArray.SetNewAtomicValue(i, -i);
			}
			MemoryStream bout = new MemoryStream(10000);

			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Serialize(bout, atomicReferenceArray);

			MemoryStream bin = new MemoryStream(bout.ToArray());
			BinaryFormatter formatter2 = new BinaryFormatter();
            AtomicReferenceArray<int> r = (AtomicReferenceArray<int>)formatter2.Deserialize(bin);

			Assert.AreEqual(atomicReferenceArray.Length(), r.Length());
			for (int i = 0; i < DEFAULT_COLLECTION_SIZE; ++i)
			{
				Assert.AreEqual(r[i], atomicReferenceArray[i]);
			}

		}

		[Test]
		public void ReferenceArrayToString()
		{
			int[] a = new int[] {two, one, three, four, seven};
            AtomicReferenceArray<int> ai = new AtomicReferenceArray<int>(a);
			Assert.AreEqual(convertArrayToString(a), ai.ToString());
		}

		private static string convertArrayToString(int[] array)
		{
			if (array.Length == 0)
				return "[]";

			StringBuilder buf = new StringBuilder();
			buf.Append('[');
			buf.Append(array[0]);

			for (int i = 1; i < array.Length; i++)
			{
				buf.Append(", ");
				buf.Append(array[i]);
			}

			buf.Append("]");
			return buf.ToString();
		}
	}
}