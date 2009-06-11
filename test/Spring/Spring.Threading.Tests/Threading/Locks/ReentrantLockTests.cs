using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using NUnit.Framework;

namespace Spring.Threading.Locks
{
    [TestFixture]
    public class ReentrantLockTests : BaseThreadingTestCase
    {
        private Exception _threadException;

        private ReentrantLock _lock;

        private ThreadCounter _threadCounter;



        [SetUp] public void SetUp()
        {
            _threadException = null;
            _threadCounter = new ThreadCounter();
        }

        private class ThreadCounter
        {
            private volatile int _count;

            public void Increment()
            {
                lock(this)
                {
                    _count++;
                    Monitor.PulseAll(this);
                }
            }

            public void AssertThreadCount(int expected, TimeSpan timeToWait)
            {
                lock (this  )
                {
                    DateTime deadline = DateTime.UtcNow + timeToWait;
                    while (timeToWait.Ticks > 0 && expected != _count)
                    {
                        Monitor.Wait(this, timeToWait);
                        timeToWait = deadline - DateTime.UtcNow;
                    }
                    Assert.That(_count, Is.EqualTo(expected), "Thread count is not as expected.");
                }
            }

        }

        private class UninterruptableThread
        {
            public readonly Thread InternalThread;
            private readonly ICondition _condition;
            private readonly ReentrantLock _myLock;

            public volatile bool CanAwake;
            public volatile bool Interrupted;
            public volatile bool LockStarted;

            public UninterruptableThread(ReentrantLock myLock, ICondition c)
            {
                _myLock = myLock;
                _condition = c;
                InternalThread = new Thread(Run);
            }

            private void Run()
            {
                using(_myLock.Lock())
                {
                    LockStarted = true;

                    while (!CanAwake)
                    {
                        _condition.AwaitUninterruptibly();
                    }
                    Interrupted = IsCurrentThreadInterrupted();
                }
            }
        }

        [Serializable]
        private class PublicReentrantLock : ReentrantLock
        {
            internal PublicReentrantLock(bool fair) : base(fair)
            {
            }

            public ICollection<Thread> GetWaitingThreadsPublic(ICondition c)
            {
                return base.GetWaitingThreads(c);
            }
        }

        [Test] public void DefaultConstructorIsNotFair()
        {
            Assert.IsFalse(new ReentrantLock().IsFair);
        }

        [Test] public void ConstructorSetsGivenFairness()
        {
            Assert.IsTrue(new ReentrantLock(true).IsFair);
            Assert.IsFalse(new ReentrantLock(false).IsFair);
        }

        [Test] public void LockIsNotInterruptibe([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            _lock.Lock();
            Thread t1 = ExceptionRecordingThread(
                delegate
                    {
                        _lock.Lock();
                        Assert.IsTrue(IsCurrentThreadInterrupted());
                        Assert.IsTrue(_lock.IsLocked);
                        Assert.IsTrue(_lock.IsHeldByCurrentThread);
                        _lock.Unlock();
                    }, "T1");
            StartAndVerifyThread(t1);
            t1.Interrupt();
            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(t1.IsAlive);
            Assert.IsTrue(_lock.IsLocked);
            Assert.IsTrue(_lock.IsHeldByCurrentThread);
            _lock.Unlock();
            JoinAndVerifyThreads(t1);
        }

        [Test] public void LockSucceedsOnUnlockedLock([Values(true, false)] bool isFair)
        {
            ReentrantLock rl = new ReentrantLock(isFair);
            rl.Lock();
            Assert.IsTrue(rl.IsLocked);
            rl.Unlock();
        }

        [Test] public void UnlockChokesWhenAttemptOnUnownedLock([Values(true, false)] bool isFair)
        {
            ReentrantLock rl = new ReentrantLock(isFair);
            Assert.Throws<SynchronizationLockException>(rl.Unlock);
        }

        [Test] public void TryLockSucceedsOnUnlockedLock([Values(true, false)] bool isFair)
        {
            ReentrantLock rl = new ReentrantLock(isFair);
            Assert.IsTrue(rl.TryLock());
            Assert.IsTrue(rl.IsLocked);
            rl.Unlock();
        }

        [TestCase(true)]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void HasQueuedThreadsReportsExistenceOfWaitingThreads(bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            Thread t1 = ExceptionRecordingThread(InterruptedLock, "T1");
            Thread t2 = ExceptionRecordingThread(InterruptibleLock, "T2");

            Assert.IsFalse(_lock.HasQueuedThreads);
            _lock.Lock();
            t1.Start();
            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_lock.HasQueuedThreads);
            t2.Start();
            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_lock.HasQueuedThreads);
            t1.Interrupt();
            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_lock.HasQueuedThreads);
            _lock.Unlock();
            Thread.Sleep(SHORT_DELAY);
            Assert.IsFalse(_lock.HasQueuedThreads);

            JoinAndVerifyThreads(t1, t2);
        }

        [TestCase(true)]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void GetQueueLengthReportsNumberOfWaitingThreads(bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            Thread t1 = ExceptionRecordingThread(InterruptedLock, "T1");
            Thread t2 = ExceptionRecordingThread(InterruptibleLock, "T2");

            Assert.AreEqual(0, _lock.QueueLength);
            _lock.Lock();
            t1.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.AreEqual(1, _lock.QueueLength);
            t2.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.AreEqual(2, _lock.QueueLength);
            t1.Interrupt();

            Thread.Sleep(SHORT_DELAY);
            Assert.AreEqual(1, _lock.QueueLength);
            _lock.Unlock();

            Thread.Sleep(SHORT_DELAY);
            Assert.AreEqual(0, _lock.QueueLength);

            JoinAndVerifyThreads(t1, t2);
        }

        [TestCase(true, ExpectedException = typeof(ArgumentNullException))]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void IsQueuedThreadChokesOnNullParameter(bool isFair)
        {
            ReentrantLock sync = new ReentrantLock(isFair);
            sync.IsQueuedThread(null);
        }

        [TestCase(true)]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void IsQueuedThreadReportsWhetherThreadIsQueued(bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            Thread t1 = ExceptionRecordingThread(InterruptedLock, "T1");
            Thread t2 = ExceptionRecordingThread(InterruptibleLock, "T2");
            Assert.IsFalse(_lock.IsQueuedThread(t1));
            Assert.IsFalse(_lock.IsQueuedThread(t2));
            _lock.Lock();
            t1.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_lock.IsQueuedThread(t1));
            t2.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_lock.IsQueuedThread(t1));
            Assert.IsTrue(_lock.IsQueuedThread(t2));
            t1.Interrupt();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsFalse(_lock.IsQueuedThread(t1));
            Assert.IsTrue(_lock.IsQueuedThread(t2));
            _lock.Unlock();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsFalse(_lock.IsQueuedThread(t1));

            Thread.Sleep(SHORT_DELAY);
            Assert.IsFalse(_lock.IsQueuedThread(t2));

            JoinAndVerifyThreads(t1, t2);
        }

        [TestCase(true)]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void GetQueuedThreadsIncludeWaitingThreads(bool isFair)
        {
            _lock = new PublicReentrantLock(isFair);
            Thread t1 = ExceptionRecordingThread(InterruptedLock, "T1");
            Thread t2 = ExceptionRecordingThread(InterruptibleLock, "T2");

            Assert.That(_lock.QueuedThreads.Count, Is.EqualTo(0));
            _lock.Lock();
            Assert.IsTrue((_lock.QueuedThreads.Count == 0));
            t1.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_lock.QueuedThreads.Contains(t1));
            t2.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_lock.QueuedThreads.Contains(t1));
            Assert.IsTrue(_lock.QueuedThreads.Contains(t2));
            t1.Interrupt();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsFalse(_lock.QueuedThreads.Contains(t1));
            Assert.IsTrue(_lock.QueuedThreads.Contains(t2));
            _lock.Unlock();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue((_lock.QueuedThreads.Count == 0));
            
            JoinAndVerifyThreads(t1, t2);
        }

        [Test] public void TimedTryLockIsInterruptible([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            _lock.Lock();
            Thread t = new Thread(ExceptionRecordingAction(
                delegate
                {
                    Assert.Throws<ThreadInterruptedException>(
                        delegate
                        {
                            _lock.TryLock(MEDIUM_DELAY);
                        });
                }));
            t.Start();
            t.Interrupt();

            JoinAndVerifyThreads(t);
        }

        [Test] public void TimedTryLockAllSucceedsFromMultipleThreads([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            _lock.LockInterruptibly();
            ThreadStart action =
                delegate
                    {
                        Assert.IsTrue(_lock.TryLock(LONG_DELAY));
                        try
                        {
                            Thread.Sleep(SHORT_DELAY);
                        }
                        finally
                        {
                            _lock.Unlock();
                        }

                    };
            Thread t1 = ExceptionRecordingThread(action, "T1");
            Thread t2 = ExceptionRecordingThread(action, "T2");
            StartAndVerifyThread(t1, t2);
            Assert.IsTrue(_lock.IsLocked);
            Assert.IsTrue(_lock.IsHeldByCurrentThread);
            _lock.Unlock();
            JoinAndVerifyThreads(t1, t2);
        }

        [Test] public void TryLockChokesWhenIsLocked([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            _lock.Lock();
            Thread t = new Thread(ExceptionRecordingAction(
                delegate { Assert.IsFalse(_lock.TryLock());}));
            t.Start();
            JoinAndVerifyThreads(SHORT_DELAY, t);
            _lock.Unlock();
        }

        [Test] public void TimedTryLockTimesOutOnLockedLock([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            _lock.Lock();
            Thread t = ExceptionRecordingThread(delegate
            {
                Assert.IsFalse(_lock.TryLock(new TimeSpan(0, 0, 1 / 1000)));
            });
            t.Start();
            JoinAndVerifyThreads(SHORT_DELAY, t);
            _lock.Unlock();
        }

        [Test] public void GetHoldCountReturnsNumberOfRecursiveHolds([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);

            RecursiveHolds(delegate { _lock.Lock(); });
            RecursiveHolds((delegate
            {
                Assert.IsTrue(_lock.TryLock());
            }));
            RecursiveHolds(delegate
            {
                Assert.IsTrue(_lock.TryLock(SHORT_DELAY));
            });
            RecursiveHolds(delegate { _lock.LockInterruptibly(); });
        }

        private void RecursiveHolds(Task lockTask)
        {
            for (int i = 1; i <= DEFAULT_COLLECTION_SIZE; i++)
            {
                lockTask();
                Assert.AreEqual(i, _lock.HoldCount);
            }
            for (int i = DEFAULT_COLLECTION_SIZE; i > 0; i--)
            {
                _lock.Unlock();
                Assert.AreEqual(i - 1, _lock.HoldCount);
            }
        }

        [Test] public void IsIsLockedReportsIfLockOwnedByCurrnetThread([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            _lock.Lock();
            Assert.IsTrue(_lock.IsLocked);
            _lock.Unlock();
            Assert.IsFalse(_lock.IsLocked);
            Thread t = ExceptionRecordingThread(
                delegate
                {
                    _lock.Lock();
                    Thread.Sleep(new TimeSpan(10000 * SMALL_DELAY.Milliseconds));
                    _lock.Unlock();
                });
            t.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_lock.IsLocked);
            JoinAndVerifyThreads(t);
            Assert.IsFalse(_lock.IsLocked);
        }

        [Test] public void LockInterruptiblySucceedsWhenUnlockedElseIsInterruptible([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            _lock.LockInterruptibly();
            Thread t1 = ExceptionRecordingThread(InterruptedLock, "T1");
            Thread t2 = ExceptionRecordingThread(delegate { using (_lock.LockInterruptibly()) ;}, "T2");
            Thread t3 = ExceptionRecordingThread(delegate { using (_lock.LockInterruptibly()) ;}, "T3");
            StartAndVerifyThread(t1, t2, t3);
            t1.Interrupt();
            JoinAndVerifyThreads(t1);
            Assert.IsTrue(_lock.IsLocked);
            Assert.IsTrue(_lock.IsHeldByCurrentThread);
            _lock.Unlock();
            JoinAndVerifyThreads(t2, t3);
        }

        [Test, ExpectedException(typeof(SynchronizationLockException))]
        public void AwaitChokesWhenLockIsNotOwned([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            ICondition c = _lock.NewCondition();
            c.Await();
        }

        [Test, ExpectedException(typeof(SynchronizationLockException))]
        public void SignalChokesWhenLockIsNotOwned([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            ICondition c = _lock.NewCondition();
            c.Signal();
        }

        [Test] public void AwaitTimeoutInNanosWithoutSignal([Values(true, false)] bool isFair)
        {
            TimeSpan timeToWait = new TimeSpan(1);
            _lock = new ReentrantLock(isFair);
            ICondition c = _lock.NewCondition();
            _lock.Lock();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool result = c.Await(timeToWait);
            sw.Stop();
            Assert.IsFalse(result);
            Assert.That(sw.Elapsed, Is.Not.LessThan(timeToWait));
            _lock.Unlock();
        }

        [Test] public void AwaitTimeoutWithoutSignal([Values(true, false)] bool isFair)
        {
            TimeSpan timeToWait = SHORT_DELAY;
            _lock = new ReentrantLock(isFair);
            ICondition c = _lock.NewCondition();
            _lock.Lock();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool result = c.Await(timeToWait);
            sw.Stop();
            Assert.IsFalse(result);
            Assert.That(sw.Elapsed, Is.Not.LessThan(timeToWait));
            _lock.Unlock();
        }

        [Test] public void AwaitUntilTimeoutWithoutSignal([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            ICondition c = _lock.NewCondition();
            _lock.Lock();
            DateTime until = DateTime.Now.AddMilliseconds(10);
            bool result = c.AwaitUntil(until);
            Assert.That(DateTime.Now, Is.GreaterThanOrEqualTo(until));
            Assert.IsFalse(result);
            _lock.Unlock();
        }

        [Test] public void AwaitReturnsWhenSignalled([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            ICondition c = _lock.NewCondition();
            Thread t = ExceptionRecordingThread(delegate
            {
                _lock.Lock();
                c.Await();
                _lock.Unlock();
            });

            t.Start();
            Thread.Sleep(SHORT_DELAY);
            _lock.Lock();
            c.Signal();
            _lock.Unlock();
            JoinAndVerifyThreads(SHORT_DELAY, t);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void HasWaitersChokesOnNullParameter([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            _lock.HasWaiters(null);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void GetWaitQueueLengthChokesOnNullParameter([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            _lock.GetWaitQueueLength(null);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void GetWaitingThreadsChokesOnNullParameter([Values(true, false)] bool isFair)
        {
            PublicReentrantLock myLock = new PublicReentrantLock(isFair);
            myLock.GetWaitingThreadsPublic(null);
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void HasWaitersChokesWhenNotOwned([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            ICondition c = _lock.NewCondition();
            ReentrantLock lock2 = new ReentrantLock(isFair);
            lock2.HasWaiters(c);
        }

        [TestCase(true, ExpectedException = typeof(SynchronizationLockException))]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void HasWaitersChokesWhenNotLocked(bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            ICondition c = _lock.NewCondition();
            _lock.HasWaiters(c);
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void GetWaitQueueLengthChokesWhenNotOwned([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            ICondition c = (_lock.NewCondition());
            ReentrantLock lock2 = new ReentrantLock(isFair);
            lock2.GetWaitQueueLength(c);
        }

        [TestCase(true, ExpectedException = typeof(SynchronizationLockException))]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void GetWaitQueueLengthChokesWhenNotLocked(bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            ICondition c = (_lock.NewCondition());
            _lock.GetWaitQueueLength(c);
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void GetWaitingThreadsChokesWhenNotOwned([Values(true, false)] bool isFair)
        {
            PublicReentrantLock myLock = new PublicReentrantLock(isFair);
            ICondition c = (myLock.NewCondition());
            PublicReentrantLock lock2 = new PublicReentrantLock(isFair);
            lock2.GetWaitingThreadsPublic(c);
        }

        [TestCase(true, ExpectedException = typeof(SynchronizationLockException))]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void GetWaitingThreadsChokesWhenNotLocked(bool isFair)
        {
            PublicReentrantLock myLock = new PublicReentrantLock(isFair);
            ICondition c = (myLock.NewCondition());
            myLock.GetWaitingThreadsPublic(c);
        }

        [Test] public void HasWaitersReturnTrueWhenThreadIsWaitingElseFalse([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(true);
            ICondition c = _lock.NewCondition();
            Thread t = ExceptionRecordingThread(delegate
            {
                using(_lock.Lock())
                {
                    Assert.IsFalse(_lock.HasWaiters(c));
                    Assert.That(_lock.GetWaitQueueLength(c), Is.EqualTo(0));
                    c.Await();
                }
            });

            t.Start();

            Thread.Sleep(SHORT_DELAY);
            _lock.Lock();
            Assert.IsTrue(_lock.HasWaiters(c));
            Assert.AreEqual(1, _lock.GetWaitQueueLength(c));
            c.Signal();
            _lock.Unlock();

            Thread.Sleep(SHORT_DELAY);
            _lock.Lock();
            Assert.IsFalse(_lock.HasWaiters(c));
            Assert.AreEqual(0, _lock.GetWaitQueueLength(c));
            _lock.Unlock();
            JoinAndVerifyThreads(SHORT_DELAY, t);
        }

        [TestCase(true)]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void GetWaitQueueLengthReturnsNumberOfThreads(bool isFair)
        {
            _lock = new ReentrantLock(isFair);

            ICondition c = _lock.NewCondition();
            Thread t1 = ExceptionRecordingThread(delegate
            {
                using (_lock.Lock())
                {
                    Assert.IsFalse(_lock.HasWaiters(c));
                    Assert.AreEqual(0, _lock.GetWaitQueueLength(c));
                    c.Await();
                }
            }, "T1");

            Thread t2 = ExceptionRecordingThread(delegate
            {
                using (_lock.Lock())
                {
                    Assert.IsTrue(_lock.HasWaiters(c));
                    Assert.AreEqual(1, _lock.GetWaitQueueLength(c));
                    c.Await();
                }
            }, "T2");

            t1.Start();

            Thread.Sleep(SHORT_DELAY);
            t2.Start();

            Thread.Sleep(SHORT_DELAY);
            _lock.Lock();
            Assert.IsTrue(_lock.HasWaiters(c));
            Assert.AreEqual(2, _lock.GetWaitQueueLength(c));
            c.SignalAll();
            _lock.Unlock();

            Thread.Sleep(SHORT_DELAY);
            _lock.Lock();
            Assert.IsFalse(_lock.HasWaiters(c));
            Assert.AreEqual(0, _lock.GetWaitQueueLength(c));
            _lock.Unlock();

            JoinAndVerifyThreads(SHORT_DELAY, t1, t2);
        }

        [TestCase(true)]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void GetWaitingThreadsIncludesWaitingThreads(bool isFair)
        {
            PublicReentrantLock myLock = new PublicReentrantLock(isFair);

            ICondition c = myLock.NewCondition();
            Thread t1 = new Thread(ExceptionRecordingAction(
                delegate
                {
                    myLock.Lock();
                    Assert.That(myLock.GetWaitingThreadsPublic(c).Count, Is.EqualTo(0));
                    c.Await();
                    myLock.Unlock();
                }));

            Thread t2 = new Thread(ExceptionRecordingAction(
                delegate
                {
                    myLock.Lock();
                    Assert.That(myLock.GetWaitingThreadsPublic(c).Count, Is.Not.EqualTo(0));
                    c.Await();
                    myLock.Unlock();
                }));

            myLock.Lock();
            Assert.That(myLock.GetWaitingThreadsPublic(c).Count, Is.EqualTo(0));
            myLock.Unlock();
            t1.Start();

            Thread.Sleep(SHORT_DELAY);
            t2.Start();

            Thread.Sleep(SHORT_DELAY);
            myLock.Lock();
            Assert.IsTrue(myLock.HasWaiters(c));
            Assert.IsTrue(myLock.GetWaitingThreadsPublic(c).Contains(t1));
            Assert.IsTrue(myLock.GetWaitingThreadsPublic(c).Contains(t2));
            c.SignalAll();
            myLock.Unlock();

            Thread.Sleep(SHORT_DELAY);
            myLock.Lock();
            Assert.IsFalse(myLock.HasWaiters(c));
            Assert.IsTrue((myLock.GetWaitingThreadsPublic(c).Count == 0));
            myLock.Unlock();

            JoinAndVerifyThreads(SHORT_DELAY, t1, t2);
        }

        [Test] public void AwaitUninterruptiblyCannotBeInterrupted([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            ICondition c = _lock.NewCondition();
            UninterruptableThread thread = new UninterruptableThread(_lock, c);

            thread.InternalThread.Start();

            while (!thread.LockStarted)
            {
                Thread.Sleep(100);
            }

            _lock.Lock();
            try
            {
                thread.InternalThread.Interrupt();
                thread.CanAwake = true;
                c.Signal();
            }
            finally
            {
                _lock.Unlock();
            }

            JoinAndVerifyThreads(thread.InternalThread);
            Assert.IsTrue(thread.Interrupted);
        }

        [Test] public void AwaitIsInterruptible([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);

            ICondition c = _lock.NewCondition();
            Thread t = ExceptionRecordingThread(delegate
            {
                Assert.Throws<ThreadInterruptedException>(delegate
                {
                    using(_lock.Lock()) c.Await();
                });
            });

            t.Start();

            Thread.Sleep(SHORT_DELAY);
            t.Interrupt();
            
            JoinAndVerifyThreads(SHORT_DELAY, t);
        }

        [Test] public void AwaitNanosIsInterruptible([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);

            ICondition c = _lock.NewCondition();
            Thread t = ExceptionRecordingThread(delegate
            {
                Assert.Throws<ThreadInterruptedException>(delegate
                {
                    using(_lock.Lock()) c.Await(new TimeSpan(0, 0, 0, 1));
                });
            });

            t.Start();

            Thread.Sleep(SHORT_DELAY);
            t.Interrupt();

            JoinAndVerifyThreads(SHORT_DELAY, t);
        }

        [Test] public void AwaitUntilIsInterruptible([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);

            ICondition c = _lock.NewCondition();
            Thread t = ExceptionRecordingThread(delegate
            {
                Assert.Throws<ThreadInterruptedException>(delegate
                {
                    using(_lock.Lock())
                        c.AwaitUntil(DateTime.Now.AddMilliseconds(10000));
                });
            });

            t.Start();

            Thread.Sleep(SHORT_DELAY);
            t.Interrupt();

            JoinAndVerifyThreads(SHORT_DELAY, t);
        }

        [Test] public void SignalAllWakesUpAllThreads([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);

            ICondition c = _lock.NewCondition();
            Thread t1 = ExceptionRecordingThread(delegate
            {
                using(_lock.Lock()) c.Await();
            }, "T1");

            Thread t2 = ExceptionRecordingThread(delegate
            {
                using(_lock.Lock()) c.Await();
            }, "T2");

            t1.Start();
            t2.Start();

            Thread.Sleep(SHORT_DELAY);
            _lock.Lock();
            c.SignalAll();
            _lock.Unlock();

            JoinAndVerifyThreads(SHORT_DELAY, t1, t2);
        }

        [Test] public void AwaitAfterMultipleReentrantLockingPreservesLockCount([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            ICondition c = _lock.NewCondition();
            Thread t1 = ExceptionRecordingThread(delegate
            {
                using (_lock.Lock())
                {
                    Assert.That(_lock.HoldCount, Is.EqualTo(1));
                    c.Await();
                    Assert.That(_lock.HoldCount, Is.EqualTo(1));
                }
            }, "T1");
            Thread t2 = ExceptionRecordingThread(delegate
            {
                using (_lock.Lock())
                    using (_lock.Lock())
                    {
                        Assert.That(_lock.HoldCount, Is.EqualTo(2));
                        c.Await();
                        Assert.That(_lock.HoldCount, Is.EqualTo(2));
                    }
            }, "T2");

            t1.Start();
            t2.Start();
            Thread.Sleep(SHORT_DELAY);
            Assert.IsFalse(_lock.IsLocked);
            using(_lock.Lock()) c.SignalAll();

            JoinAndVerifyThreads(SHORT_DELAY, t1, t2);
        }

        [Test] public void SerializationDeserializesAsUunlocked([Values(true, false)] bool isFair)
        {
            ReentrantLock l = new ReentrantLock(isFair);
            l.Lock();
            l.Unlock();
            MemoryStream bout = new MemoryStream(10000);

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(bout, l);

            MemoryStream bin = new MemoryStream(bout.ToArray());
            BinaryFormatter formatter2 = new BinaryFormatter();
            ReentrantLock r = (ReentrantLock)formatter2.Deserialize(bin);
            r.Lock();
            r.Unlock();
        }

        [Test] public void ToStringVerification([Values(true, false)] bool isFair)
        {
            _lock = new ReentrantLock(isFair);
            StringAssert.Contains("Unlocked", _lock.ToString());
            using (_lock.Lock())
            {
                StringAssert.Contains("Locked by thread", _lock.ToString());
            }
            StringAssert.Contains("Unlocked", _lock.ToString());
        }

        #region Private Methods

        private void StartAndVerifyThread(params Thread[] threads)
        {
            StartAndVerifyThread(threads.Length, SMALL_DELAY, threads);
        }

        private void StartAndVerifyThread(int count, params Thread[] threads)
        {
            StartAndVerifyThread(count, SMALL_DELAY, threads);
        }

        private void StartAndVerifyThread(TimeSpan timeToWait, params Thread[] threads)
        {
            StartAndVerifyThread(threads.Length, timeToWait, threads);
        }

        private void StartAndVerifyThread(int count, TimeSpan timeToWait, params Thread[] threads)
        {
            foreach (Thread thread in threads)
            {
                thread.Start();
            }
            _threadCounter.AssertThreadCount(count, timeToWait);
        }

        private ThreadStart ExceptionRecordingAction(ThreadStart action)
        {
            return delegate
            {
                _threadCounter.Increment();
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    if (_threadException == null)
                    {
                        while(e is TargetInvocationException) e = e.InnerException;
                        _threadException = e;
                    }
                }
            };
        }

        private Thread ExceptionRecordingThread(ThreadStart action)
        {
            return ExceptionRecordingThread(action, "Test Thread");
        }

        private Thread ExceptionRecordingThread(ThreadStart action, string name)
        {
            Thread t = new Thread(ExceptionRecordingAction(action));
            t.Name = name;
            return t;
        }

        private void InterruptedLock()
        {
            Assert.Throws<ThreadInterruptedException>(delegate
            {
                _lock.LockInterruptibly();
            });
        }

        private void InterruptibleLock()
        {
            try
            {
                _lock.LockInterruptibly();
            }
            catch (ThreadInterruptedException)
            {
            }
        }

        private static bool IsCurrentThreadInterrupted()
        {
            try
            {
                Thread.Sleep(0); // get exception if interrupted.
            }
            catch (ThreadInterruptedException)
            {
                return true;
            }
            return false;
        }
        
        private void JoinAndVerifyThreads(params Thread[] threads)
        {
            JoinAndVerifyThreads(MEDIUM_DELAY, threads);
        }

        private void JoinAndVerifyThreads(TimeSpan timeToWait, params Thread[] threads)
        {
            foreach (Thread thread in threads)
            {
                thread.Join(timeToWait);
                Assert.IsFalse(thread.IsAlive, "Thread {0} is expected to be terminated but still alive.", thread.Name);
            }
            VerifyThreads();
        }

        private void VerifyThreads()
        {
            if (_threadException != null)
            {
                Exception e = _threadException;
                _threadException = null;
                throw e;
            }
        }
        #endregion
    }
}