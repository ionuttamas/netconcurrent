using System.Collections.Generic;

namespace Spring.Threading.InterlockedAtomics
{
    public interface IAtomicArray<T> : IList<T>
    {
        /// <summary> 
        /// Eventually sets to the given value at the given <paramref name="index"/>
        /// </summary>
        /// <param name="newValue">
        /// the new value
        /// </param>
        /// <param name="index">
        /// the index to set
        /// </param>
        void LazySet(int index, T newValue);

        /// <summary> 
        /// Atomically sets the element at position <paramref name="index"/> to <paramref name="newValue"/> 
        /// and returns the old value.
        /// </summary>
        /// <param name="index">
        /// Ihe index
        /// </param>
        /// <param name="newValue">
        /// The new value
        /// </param>
        T Exchange(int index, T newValue);

        /// <summary> 
        /// Atomically sets the element at <paramref name="index"/> to <paramref name="newValue"/>
        /// if the current value equals the <paramref name="expectedValue"/>.
        /// </summary>
        /// <param name="index">
        /// The index
        /// </param>
        /// <param name="expectedValue">
        /// The expected value
        /// </param>
        /// <param name="newValue">
        /// The new value
        /// </param>
        /// <returns> 
        /// true if successful. False return indicates that
        /// the actual value was not equal to the expected value.
        /// </returns>
        bool CompareAndSet(int index, T expectedValue, T newValue);

        /// <summary> 
        /// Atomically sets the element at <paramref name="index"/> to <paramref name="newValue"/>
        /// if the current value equals the <paramref name="expectedValue"/>.
        /// May fail spuriously.
        /// </summary>
        /// <param name="index">
        /// The index
        /// </param>
        /// <param name="expectedValue">
        /// The expected value
        /// </param>
        /// <param name="newValue">
        /// The new value
        /// </param>
        /// <returns> 
        /// True if successful, false otherwise.
        /// </returns>
        bool WeakCompareAndSet(int index, T expectedValue, T newValue);

    }
}
