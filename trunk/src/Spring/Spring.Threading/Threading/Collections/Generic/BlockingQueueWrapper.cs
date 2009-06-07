#region License

/*
 * Copyright (C) 2002-2008 the original author or authors.
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

#region Imports

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Spring.Collections;
using Spring.Collections.Generic;
using Spring.Threading.Locks;

#endregion

namespace Spring.Threading.Collections.Generic
{
    /// <summary> 
    /// An implementation of <see cref="IBlockingQueue{T}"/> by wrapping a
    /// regular queue.
    /// </summary>
    /// <remarks>
    /// This class supports an optional fairness policy for ordering waiting 
    /// producer and consumer threads.  By default, this ordering is not 
    /// guaranteed. However, a queue constructed with fairness set to 
    /// <see langword="true"/> grants threads access in FIFO order. Fairness
    /// generally decreases throughput but reduces variability and avoids
    /// starvation.
    /// </remarks>
    /// <author>Doug Lea</author>
    /// <author>Griffin Caprio (.NET)</author>
    /// <author>Kenneth Xu</author>
    [Serializable]
    public class BlockingQueueWrapper<T> : AbstractBlockingQueue<T>
    {
        /// <summary>Main lock guarding all access </summary>
        private readonly ReentrantLock _lock;

        /// <summary>Condition for waiting takes </summary>
        private readonly ICondition _notEmptyCondition;

        /// <summary>Condition for waiting puts </summary>
        private readonly ICondition _notFullCondition;

        private readonly IQueue<T> _wrapped;

        private readonly int _capacity;


        #region Constructors

        /// <summary>
        /// Construct a blocking queue that based on the given regular 
        /// <paramref name="queue"/>.
        /// </summary>
        /// <param name="queue">
        /// A regular queue to be wrapped as blocking queue.
        /// </param>
        /// <param name="capacity">
        /// The capacity of the queue. zero (<c>0</c>) to indicate an
        /// unbounded queue.
        /// </param>
        /// <param name="isFair">
        /// <c>true</c> to grant access to longest waiting threads, otherwise 
        /// it does not guarantee any particular access order.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="queue"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="capacity"/> is negative.
        /// </exception>
        public BlockingQueueWrapper(IQueue<T> queue, int capacity, bool isFair)
        {
            if (queue==null) throw new ArgumentNullException("queue");
            if (capacity<0) throw new ArgumentOutOfRangeException(
                "capacity", capacity, "must not be negative.");
            _lock = new ReentrantLock(isFair);
            _wrapped = queue;
            _capacity = capacity;
            _notEmptyCondition = _lock.NewCondition();
            _notFullCondition = _lock.NewCondition();
        }

        /// <summary>
        /// Construct a blocking queue that based on the given regular 
        /// <paramref name="queue"/>. There is no guarantee to the order of
        /// the blocked threads being given access.
        /// </summary>
        /// <param name="queue">
        /// A regular queue to be wrapped as blocking queue.
        /// </param>
        /// <param name="capacity">
        /// The capacity of the queue. zero (<c>0</c>) to indicate an
        /// unbounded queue.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="queue"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="capacity"/> is negative.
        /// </exception>
        public BlockingQueueWrapper(IQueue<T> queue, int capacity)
            : 
            this(queue, capacity, false)
        {
        }

        /// <summary>
        /// Construct a blocking queue that based on the given regular 
        /// unbounded <paramref name="queue"/>.
        /// </summary>
        /// <param name="queue">
        /// A regular queue to be wrapped as blocking queue.
        /// </param>
        /// <param name="isFair">
        /// <c>true</c> to grant access to longest waiting threads, otherwise 
        /// it does not guarantee any particular access order.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="queue"/> is <c>null</c>.
        /// </exception>
        public BlockingQueueWrapper(IQueue<T> queue, bool isFair)
            : this(queue, 0, isFair)
        {
        }

        /// <summary>
        /// Construct a blocking queue that based on the given regular 
        /// unbounded <paramref name="queue"/>. There is no guarantee to the 
        /// order of the blocked threads being given access.
        /// </summary>
        /// <param name="queue">
        /// A regular queue to be wrapped as blocking queue.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="queue"/> is <c>null</c>.
        /// </exception>
        public BlockingQueueWrapper(IQueue<T> queue)
            : this(queue, 0, false)
        {
        }
        #endregion

        /// <summary> 
        /// Atomically removes all of the elements from this queue.
        /// The queue will be empty after this call returns.
        /// </summary>
        public sealed override void Clear()
        {
            using (_lock.LockAndUse())
            {
                _wrapped.Clear();
                _notFullCondition.SignalAll();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public override int Capacity
        {
            get { return _capacity; }
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current 
        /// <see cref="ICollection{T}"/>.
        /// </summary>
        /// <remarks>
        /// This implmentation list out all the elements separated by comma.
        /// </remarks>
        /// <returns>
        /// A <see cref="string"/> that represents the current collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            ReentrantLock currentLock = _lock;
            currentLock.Lock();
            try
            {
                return base.ToString();
            }
            finally
            {
                currentLock.Unlock();
            }
        }

        /// <summary> 
        /// Returns <see langword="true"/> if this queue contains the specified 
        /// element.
        /// </summary>
        /// <remarks>
        /// More formally, returns <see langword="true"/> if and only if this 
        /// queue contains at least one element <i>element</i> such that 
        /// <i>elementToSearchFor.equals(element)</i>.
        /// </remarks>
        /// <param name="elementToSearchFor">
        /// Object to be checked for containment in this queue.
        /// </param>
        /// <returns>
        /// <see langword="true"/>
        /// If this queue contains the specified element.
        /// </returns>
        public override bool Contains(T elementToSearchFor)
        {
            ReentrantLock currentLock = _lock;
            currentLock.Lock();
            try
            {
                return _wrapped.Contains(elementToSearchFor);
            }
            finally
            {
                currentLock.Unlock();
            }
        }

        /// <summary> 
        /// Removes a single instance of the specified element from this queue,
        /// if it is present.  More formally, removes an <i>element</i> such
        /// that <i>elementToRemove.Equals(element)</i>, if this queue contains 
        /// one or more such elements.
        /// </summary>
        /// <param name="elementToRemove">
        /// element to be removed from this queue, if present.
        /// </param>
        /// <returns> <see langword="true"/> if this queue contained the 
        /// specified element or if this queue changed as a result of the call, 
        /// <see langword="false"/> otherwise.
        /// </returns>
        public override bool Remove(T elementToRemove)
        {
            ReentrantLock currentLock = _lock;
            currentLock.Lock();
            try
            {
                bool isSuccess = _wrapped.Remove(elementToRemove);
                if(isSuccess) _notFullCondition.Signal();
                return isSuccess;
            }
            finally
            {
                currentLock.Unlock();
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="ICollection"/> to an 
        /// <see cref="Array"/>, starting at a particular <see cref="Array"/> 
        /// index.
        /// </summary>
        ///
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination 
        /// of the elements copied from <see cref="ICollection"/>. The 
        /// <see cref="Array"/> must have zero-based indexing. 
        /// </param>
        /// <param name="index">
        /// The zero-based index in array at which copying begins. 
        /// </param>
        /// <exception cref="ArgumentNullException">array is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is less than zero. 
        /// </exception>
        /// <exception cref="ArgumentException">
        /// array is multidimensional.-or- index is equal to or greater than 
        /// the length of array.
        /// -or- 
        /// The number of elements in the source <see cref="ICollection"/> 
        /// is greater than the available space from index to the end of the 
        /// destination array. 
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// The type of the source <see cref="ICollection"/> cannot be cast 
        /// automatically to the type of the destination array. 
        /// </exception>
        /// <filterpriority>2</filterpriority>
        protected override void CopyTo(Array array, int index)
        {
            using(_lock.LockAndUse())
            {
                base.CopyTo(array, index);
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="ICollection{T}"/> to an 
        /// <see cref="Array"/>, starting at a particular <see cref="Array"/> 
        /// index.
        /// </summary>
        /// 
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the 
        /// destination of the elements copied from <see cref="ICollection{T}"/>. 
        /// The <see cref="Array"/> must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in array at which copying begins.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// arrayIndex is less than 0.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// array is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// array is multidimensional.<br/>-or-<br/>
        /// arrayIndex is equal to or greater than the length of array. <br/>-or-<br/>
        /// The number of elements in the source <see cref="ICollection{T}"/> 
        /// is greater than the available space from arrayIndex to the end of 
        /// the destination array. <br/>-or-<br/>
        /// Type T cannot be cast automatically to the type of the destination array.
        /// </exception>
        public override void CopyTo(T[] array, int arrayIndex)
        {
            using(_lock.LockAndUse())
            {
                base.CopyTo(array, arrayIndex);
            }
        }

        /// <summary> 
        /// Returns the number of additional elements that this queue can ideally
        /// (in the absence of memory or resource constraints) accept without
        /// blocking, or <see cref="System.Int32.MaxValue"/> if there is no intrinsic
        /// limit.
        /// </summary>
        /// <remarks>
        /// <b>Important</b>: You <b>cannot</b> always tell if an attempt to insert
        /// an element will succeed by inspecting <see cref="RemainingCapacity"/>
        /// because it may be the case that another thread is about to
        /// insert or remove an element.
        /// </remarks>
        /// <returns>The remaining capacity.</returns>
        public override int RemainingCapacity
        {
            get { return _capacity == 0 ? int.MaxValue : _capacity - Count; }
        }

        /// <summary> 
        /// Returns the number of elements in this queue.
        /// </summary>
        /// <returns>The number of elements in this queue.</returns>
        public override int Count
        {
            get
            {
                ReentrantLock lock_Renamed = _lock;
                lock_Renamed.Lock();
                try
                {
                    return _wrapped.Count;
                }
                finally
                {
                    lock_Renamed.Unlock();
                }
            }

        }

        /// <summary>
        /// Inserts the specified element into this queue if it is possible to 
        /// do so immediately without violating capacity restrictions. 
        /// </summary>
        /// <remarks>
        /// When using a capacity-restricted queue, this method is generally 
        /// preferable to <see cref="AbstractQueue.Add"/>, which can fail to 
        /// insert an element only by throwing an exception. 
        /// </remarks>
        /// <param name="element">The element to add.</param>
        /// <returns>
        /// <c>true</c> if the element was added to this queue. Otherwise 
        /// <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="element"/> is <c>null</c> and the queue 
        /// implementation doesn't allow <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If some property of the supplied <paramref name="element"/> 
        /// prevents it from being added to this queue. 
        /// </exception>
        public override bool Offer(T element)
        {
            ReentrantLock currentLock = _lock;
            currentLock.Lock();
            try
            {
                bool isSuccess = _wrapped.Offer(element);
                if (isSuccess) _notEmptyCondition.Signal();
                return isSuccess;
            }
            finally
            {
                currentLock.Unlock();
            }
        }

        /// <summary>
        /// Retrieves and removes the head of this queue into out parameter
        /// <paramref name="element"/>. 
        /// </summary>
        /// <param name="element">
        /// Set to the head of this queue. <c>default(T)</c> if queue is empty.
        /// </param>
        /// <returns>
        /// <c>false</c> if the queue is empty. Otherwise <c>true</c>.
        /// </returns>
        public override bool Poll(out T element)
        {
            ReentrantLock currentLock = _lock;
            currentLock.Lock();
            try
            {
                bool isSuccess = _wrapped.Poll(out element);
                if (isSuccess) _notFullCondition.Signal();
                return isSuccess;
            }
            finally
            {
                currentLock.Unlock();
            }
        }

        /// <summary> 
        /// Inserts the specified element into this queue, waiting if necessary
        /// for space to become available.
        /// </summary>
        /// <param name="element">the element to add</param>
        /// <exception cref="System.ArgumentNullException">
        /// If the specified element is <see langword="null"/> and this queue 
        /// does not permit <see langword="null"/> elements.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If some property of the supplied <paramref name="element"/> prevents
        /// it from being added to this queue.
        /// </exception>
        public override void Put(T element)
        {
            ReentrantLock currentLock = _lock;
            currentLock.LockInterruptibly();
            try
            {
                try
                {
                    while (!_wrapped.Offer(element))
                        _notFullCondition.Await();
                    _notEmptyCondition.Signal();
                }
                catch (ThreadInterruptedException)
                {
                    _notFullCondition.Signal();
                    throw;
                }
            }
            finally
            {
                currentLock.Unlock();
            }
        }

        /// <summary> 
        /// Inserts the specified element into this queue, waiting up to the
        /// specified wait time if necessary for space to become available.
        /// </summary>
        /// <param name="element">The element to add.</param>
        /// <param name="duration">How long to wait before giving up.</param>
        /// <returns>
        /// <see langword="true"/> if successful, or <see langword="false"/> if
        /// the specified waiting time elapses before space is available.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If the specified element is <see langword="null"/> and this queue 
        /// does not permit <see langword="null"/> elements.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If some property of the supplied <paramref name="element"/> prevents
        /// it from being added to this queue.
        /// </exception>
        public override bool Offer(T element, TimeSpan duration)
        {
            ReentrantLock currentLock = _lock;
            currentLock.LockInterruptibly();
            try
            {
                TimeSpan durationToWait = duration;
                DateTime deadline = DateTime.Now.Add(durationToWait);
                while(!_wrapped.Offer(element))
                {
                    if (durationToWait.Ticks <= 0)
                        return false;
                    try
                    {
                        _notFullCondition.Await(durationToWait);
                        durationToWait = deadline.Subtract(DateTime.Now);
                    }
                    catch (ThreadInterruptedException)
                    {
                        _notFullCondition.Signal();
                        throw;
                    }
                }
                _notEmptyCondition.Signal();
                return true;
            }
            finally
            {
                currentLock.Unlock();
            }
        }

        /// <summary> 
        /// Retrieves and removes the head of this queue, waiting if necessary
        /// until an element becomes available.
        /// </summary>
        /// <returns> the head of this queue</returns>
        public override T Take()
        {
            ReentrantLock currentLock = _lock;
            currentLock.LockInterruptibly();
            try
            {
                try
                {
                    T element;
                    while (!_wrapped.Poll(out element))
                        _notEmptyCondition.Await();
                    _notFullCondition.Signal();
                    return element;
                }
                catch (ThreadInterruptedException)
                {
                    _notEmptyCondition.Signal();
                    throw;
                }
            }
            finally
            {
                currentLock.Unlock();
            }
        }

        /// <summary> 
        /// Retrieves and removes the head of this queue, waiting up to the
        /// specified wait time if necessary for an element to become available.
        /// </summary>
        /// <param name="element">
        /// Set to the head of this queue. <c>default(T)</c> if queue is empty.
        /// </param>
        /// <param name="duration">How long to wait before giving up.</param>
        /// <returns> 
        /// <c>false</c> if the queue is still empty after waited for the time 
        /// specified by the <paramref name="duration"/>. Otherwise <c>true</c>.
        /// </returns>
        public override bool Poll(TimeSpan duration,out T element)
        {
            ReentrantLock currentLock = _lock;
            currentLock.LockInterruptibly();
            try
            {
                TimeSpan durationToWait = duration;
                DateTime deadline = DateTime.Now.Add(durationToWait);
                while (!_wrapped.Poll(out element))
                {
                    if (durationToWait.Ticks <= 0)
                    {
                        element = default(T);
                        return false;
                    }
                    try
                    {
                        _notEmptyCondition.Await(durationToWait);
                        durationToWait = deadline.Subtract(DateTime.Now);
                    }
                    catch (ThreadInterruptedException)
                    {
                        _notEmptyCondition.Signal();
                        throw;
                    }
                }
                _notFullCondition.Signal();
                return true;
            }
            finally
            {
                currentLock.Unlock();
            }
        }


        /// <summary> 
        /// Does the real work for all <c>Drain</c> methods. Caller must
        /// guarantee the <paramref name="action"/> is not <c>null</c> and
        /// <paramref name="maxElements"/> is greater then zero (0).
        /// </summary>
        /// <seealso cref="IBlockingQueue{T}.DrainTo(ICollection{T})"/>
        /// <seealso cref="IBlockingQueue{T}.DrainTo(ICollection{T}, int)"/>
        /// <seealso cref="IBlockingQueue{T}.Drain(System.Action{T})"/>
        /// <seealso cref="IBlockingQueue{T}.DrainTo(ICollection{T},int)"/>
        protected override int DoDrainTo(Action<T> action, int maxElements)
        {
            ReentrantLock currentLock = _lock;
            currentLock.Lock();
            try
            {
                T element;
                int n;
                for (n = 0; n < maxElements && _wrapped.Poll(out element); n++)
                {
                    action(element);
                }
                if (n == 1)
                {
                    _notFullCondition.Signal();
                }
                else if (n != 0)
                {
                    _notFullCondition.SignalAll();
                }
                return n;
            }
            finally
            {
                currentLock.Unlock();
            }
        }


        /// <summary>
        /// Retrieves, but does not remove, the head of this queue into out
        /// parameter <paramref name="element"/>.
        /// </summary>
        /// <param name="element">
        /// The head of this queue. <c>default(T)</c> if queue is empty.
        /// </param>
        /// <returns>
        /// <c>false</c> is the queue is empty. Otherwise <c>true</c>.
        /// </returns>
        public override bool Peek(out T element)
        {
            ReentrantLock currentLock = _lock;
            currentLock.Lock();
            try
            {
                return _wrapped.Peek(out element);
            }
            finally
            {
                currentLock.Unlock();
            }
        }

        /// <summary>
        /// Always return true as blocking queue is always synchronized 
        /// (thread-safe).
        /// </summary>
        protected override bool IsSynchronized
        {
            get { return true; }
        }


        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate 
        /// through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public override IEnumerator<T> GetEnumerator()
        {
            ReentrantLock lock_Renamed = _lock;
            lock_Renamed.Lock();
            try
            {
                return new BlockingQueueEnumeratorWrapper(_wrapped.GetEnumerator(), _lock);
            }
            finally
            {
                lock_Renamed.Unlock();
            }
        }

        private class BlockingQueueEnumeratorWrapper : IEnumerator<T>
        {

            private readonly ReentrantLock _lock;

            private readonly IEnumerator<T> _wrapped;

            public T Current
            {
                get
                {
                    using (_lock.LockAndUse())
                    {
                        return _wrapped.Current;
                    }
                }

            }

            internal BlockingQueueEnumeratorWrapper(
                IEnumerator<T> enumerator, ReentrantLock reentrantLock)
            {
                _wrapped = enumerator;
                _lock = reentrantLock;
            }

            public bool MoveNext()
            {
                using(_lock.LockAndUse())
                {
                    return _wrapped.MoveNext();
                }
            }

            public void Reset()
            {
                using(_lock.LockAndUse())
                {
                    _wrapped.Reset();
                }
            }

            #region IDisposable Members

            public void Dispose()
            {
                using (_lock.LockAndUse())
                {
                    _wrapped.Dispose();
                }
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current
            {
                get { return Current; }
            }

            #endregion
        }
    }
}