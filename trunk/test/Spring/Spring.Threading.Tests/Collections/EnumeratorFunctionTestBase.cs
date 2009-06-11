using System;
using System.Collections;
using NUnit.Framework;

namespace Spring.Collections
{
    /* Example usage of EnumeratorFunctionTestBase
 
        [TestFixture(typeof(string))]
        [TestFixture(typeof(int))]
        public class ListEnumeratorTest<T> : EnumeratorFunctionTestBase
        {
            protected override IEnumerator Enumerator
            {
                get { return new ArrayList(TestData<T>.MakeTestArray(55)).GetEnumerator(); }
            }
        }

        [TestFixture(typeof(string))]
        [TestFixture(typeof(int))]
        public class ArrayEnumeratorTest<T> : EnumeratorFunctionTestBase
        {
            [SetUp] public void SetUp()
            {
                AntiHangingLimit = 600;
            }
            protected override IEnumerator Enumerator
            {
                get { return TestData<T>.MakeTestArray(555).GetEnumerator(); }
            }
        }

     */

    /// <summary>
    /// Basic functionality test cases for implementation of <see cref="IEnumerator"/>.
    /// </summary>
    /// <author>Kenneth Xu</author>
    public abstract class EnumeratorFunctionTestBase
    {
        private int _antiHangingLimit = 512;
        protected int AntiHangingLimit
        {
            get { return _antiHangingLimit; }
            set { _antiHangingLimit = value; }
        }

        protected abstract IEnumerator Enumerator { get; }

        [Test] public void IteratingThroughEnumeratorOnce()
        {
            Iterate(Enumerator);
        }

        [Test] public void IterateEnumeratorResetAndIterateAgain()
        {
            IEnumerator e = Enumerator;
            int count = Iterate(e);
            e.Reset();
            Assert.That(Iterate(e), Is.EqualTo(count));

        }

        private int Iterate(IEnumerator enumerator)
        {
            int count = 0;
            object value;
            Assert.Throws<InvalidOperationException>(delegate { value = enumerator.Current; });
            while(enumerator.MoveNext())
            {
                value = enumerator.Current;
                if (++count >= _antiHangingLimit)
                {
                    Assert.Fail("Endless enumerator? reached the {0} iteration limit set by AntiHangingLimit property.", _antiHangingLimit);
                }
            }
            Assert.Throws<InvalidOperationException>(delegate { value = enumerator.Current; });
            return count;
        }
    }

}
