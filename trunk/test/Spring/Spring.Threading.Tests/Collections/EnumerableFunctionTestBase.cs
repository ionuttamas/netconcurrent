using System.Collections;

namespace Spring.Collections
{
    /* Example usage of EnumeratorFunctionTestBase

        [TestFixture(typeof(string))]
        [TestFixture(typeof(int))]
        public class ListEnumerableTest<T> : EnumerableFunctionTestBase
        {
            protected override IEnumerable Enumerable
            {
                get { return new ArrayList(TestData<T>.MakeTestArray(55)); }
            }
        }

        [TestFixture(typeof(string))]
        [TestFixture(typeof(int))]
        public class ArrayEnumerableTest<T> : EnumerableFunctionTestBase
        {
            [SetUp] public void SetUp()
            {
                AntiHangingLimit = 600;
            }
            protected override IEnumerable Enumerable
            {
                get { return TestData<T>.MakeTestArray(555); }
            }
        }

     */

    /// <summary>
    /// Basic functionality test cases for implementation of <see cref="IEnumerable"/>.
    /// </summary>
    /// <author>Kenneth Xu</author>
    public abstract class EnumerableFunctionTestBase : EnumeratorFunctionTestBase 
    {
        protected abstract IEnumerable Enumerable { get;  }

        protected sealed override IEnumerator Enumerator
        {
            get { return Enumerable.GetEnumerator(); }
        }
    }
}
