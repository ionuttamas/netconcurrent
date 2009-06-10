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

        private ReentrantLock _testee;

        [SetUp] public void SetUp()
        {
            _threadException = null;  
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
            _testee = new ReentrantLock(isFair);
            Thread t1 = new Thread(InterruptedLock);
            Thread t2 = new Thread(InterruptibleLock);

            Assert.IsFalse(_testee.HasQueuedThreads);
            _testee.Lock();
            t1.Start();
            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_testee.HasQueuedThreads);
            t2.Start();
            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_testee.HasQueuedThreads);
            t1.Interrupt();
            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_testee.HasQueuedThreads);
            _testee.Unlock();
            Thread.Sleep(SHORT_DELAY);
            Assert.IsFalse(_testee.HasQueuedThreads);

            JoinAndVerifyThreads(t1, t2);
        }

        [TestCase(true)]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void GetQueueLengthReportsNumberOfWaitingThreads(bool isFair)
        {
            _testee = new ReentrantLock(isFair);
            Thread t1 = new Thread(InterruptedLock);
            Thread t2 = new Thread(InterruptibleLock);

            Assert.AreEqual(0, _testee.QueueLength);
            _testee.Lock();
            t1.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.AreEqual(1, _testee.QueueLength);
            t2.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.AreEqual(2, _testee.QueueLength);
            t1.Interrupt();

            Thread.Sleep(SHORT_DELAY);
            Assert.AreEqual(1, _testee.QueueLength);
            _testee.Unlock();

            Thread.Sleep(SHORT_DELAY);
            Assert.AreEqual(0, _testee.QueueLength);

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
            _testee = new ReentrantLock(isFair);
            Thread t1 = new Thread(InterruptedLock);
            Thread t2 = new Thread(InterruptibleLock);
            Assert.IsFalse(_testee.IsQueuedThread(t1));
            Assert.IsFalse(_testee.IsQueuedThread(t2));
            _testee.Lock();
            t1.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_testee.IsQueuedThread(t1));
            t2.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_testee.IsQueuedThread(t1));
            Assert.IsTrue(_testee.IsQueuedThread(t2));
            t1.Interrupt();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsFalse(_testee.IsQueuedThread(t1));
            Assert.IsTrue(_testee.IsQueuedThread(t2));
            _testee.Unlock();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsFalse(_testee.IsQueuedThread(t1));

            Thread.Sleep(SHORT_DELAY);
            Assert.IsFalse(_testee.IsQueuedThread(t2));

            JoinAndVerifyThreads(t1, t2);
        }

        [TestCase(true)]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void GetQueuedThreadsIncludeWaitingThreads(bool isFair)
        {
            _testee = new PublicReentrantLock(isFair);
            Thread t1 = new Thread(InterruptedLock);
            Thread t2 = new Thread(InterruptibleLock);

            Assert.That(_testee.QueuedThreads.Count, Is.EqualTo(0));
            _testee.Lock();
            Assert.IsTrue((_testee.QueuedThreads.Count == 0));
            t1.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_testee.QueuedThreads.Contains(t1));
            t2.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(_testee.QueuedThreads.Contains(t1));
            Assert.IsTrue(_testee.QueuedThreads.Contains(t2));
            t1.Interrupt();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsFalse(_testee.QueuedThreads.Contains(t1));
            Assert.IsTrue(_testee.QueuedThreads.Contains(t2));
            _testee.Unlock();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue((_testee.QueuedThreads.Count == 0));
            
            JoinAndVerifyThreads(t1, t2);
        }

        [Test] public void TimedTryLockIsInterruptible([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            myLock.Lock();
            Thread t = new Thread(ExceptionRecordingAction(
                delegate
                {
                    Assert.Throws<ThreadInterruptedException>(
                        delegate
                        {
                            myLock.TryLock(MEDIUM_DELAY);
                        });
                }));
            t.Start();
            t.Interrupt();

            JoinAndVerifyThreads(t);
        }

        [Test] public void TryLockChokesWhenIsLocked([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            myLock.Lock();
            Thread t = new Thread(ExceptionRecordingAction(
                delegate { Assert.IsFalse(myLock.TryLock());}));
            t.Start();
            JoinAndVerifyThreads(SHORT_DELAY, t);
            myLock.Unlock();
        }

        [Test] public void TimedTryLockTimesOutOnLockedLock([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            myLock.Lock();
            Thread t = ExceptionRecordingThread(delegate
            {
                Assert.IsFalse(myLock.TryLock(new TimeSpan(0, 0, 1 / 1000)));
            });
            t.Start();
            JoinAndVerifyThreads(SHORT_DELAY, t);
            myLock.Unlock();
        }

        [Test] public void GetHoldCountReturnsNumberOfRecursiveHolds([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            for (int i = 1; i <= DEFAULT_COLLECTION_SIZE; i++)
            {
                myLock.Lock();
                Assert.AreEqual(i, myLock.HoldCount);
            }
            for (int i = 1; i <= DEFAULT_COLLECTION_SIZE; i++)
            {
                Assert.IsTrue(myLock.TryLock());
                Assert.AreEqual(i, myLock.HoldCount);
            }
            for (int i = 1; i <= DEFAULT_COLLECTION_SIZE; i++)
            {
                Assert.IsTrue(myLock.TryLock(SHORT_DELAY));
                Assert.AreEqual(i, myLock.HoldCount);
            }
            for (int i = 1; i <= DEFAULT_COLLECTION_SIZE; i++)
            {
                myLock.LockInterruptibly();
                Assert.AreEqual(i, myLock.HoldCount);
            }
            for (int i = DEFAULT_COLLECTION_SIZE * 4; i > 0; i--)
            {
                myLock.Unlock();
                Assert.AreEqual(i - 1, myLock.HoldCount);
            }
        }

        [Test] public void IsIsLockedReportsIfLockOwnedByCurrnetThread([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            myLock.Lock();
            Assert.IsTrue(myLock.IsLocked);
            myLock.Unlock();
            Assert.IsFalse(myLock.IsLocked);
            Thread t = ExceptionRecordingThread(
                delegate
                {
                    myLock.Lock();
                    Thread.Sleep(new TimeSpan(10000 * SMALL_DELAY.Milliseconds));
                    myLock.Unlock();
                });
            t.Start();

            Thread.Sleep(SHORT_DELAY);
            Assert.IsTrue(myLock.IsLocked);
            JoinAndVerifyThreads(t);
            Assert.IsFalse(myLock.IsLocked);
        }

        [Test] public void LockInterruptiblySucceedsWhenUnlockedElseIsInterruptible([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            myLock.LockInterruptibly();
            Thread t = new Thread(InterruptedLock);
            t.Start();
            t.Interrupt();
            Assert.IsTrue(myLock.IsLocked);
            Assert.IsTrue(myLock.IsHeldByCurrentThread);
            JoinAndVerifyThreads(t);
        }

        [Test, ExpectedException(typeof(SynchronizationLockException))]
        public void AwaitChokesWhenLockIsNotOwned([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            ICondition c = myLock.NewCondition();
            c.Await();
        }

        [Test, ExpectedException(typeof(SynchronizationLockException))]
        public void SignalChokesWhenLockIsNotOwned([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            ICondition c = myLock.NewCondition();
            c.Signal();
        }

        [Test] public void AwaitTimeoutInNanosWithoutSignal([Values(true, false)] bool isFair)
        {
            TimeSpan timeToWait = new TimeSpan(1);
            ReentrantLock myLock = new ReentrantLock(isFair);
            ICondition c = myLock.NewCondition();
            myLock.Lock();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool result = c.Await(timeToWait);
            sw.Stop();
            Assert.IsFalse(result);
            Assert.That(sw.Elapsed, Is.Not.LessThan(timeToWait));
            myLock.Unlock();
        }

        [Test] public void AwaitTimeoutWithoutSignal([Values(true, false)] bool isFair)
        {
            TimeSpan timeToWait = SHORT_DELAY;
            ReentrantLock myLock = new ReentrantLock(isFair);
            ICondition c = myLock.NewCondition();
            myLock.Lock();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool result = c.Await(timeToWait);
            sw.Stop();
            Assert.IsFalse(result);
            Assert.That(sw.Elapsed, Is.Not.LessThan(timeToWait));
            myLock.Unlock();
        }

        [Test] public void AwaitUntilTimeoutWithoutSignal([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            ICondition c = myLock.NewCondition();
            myLock.Lock();
            DateTime until = DateTime.Now.AddMilliseconds(10);
            bool result = c.AwaitUntil(until);
            Assert.That(DateTime.Now, Is.GreaterThanOrEqualTo(until));
            Assert.IsFalse(result);
            myLock.Unlock();
        }

        [Test] public void AwaitReturnsWhenSignalled([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            ICondition c = myLock.NewCondition();
            Thread t = ExceptionRecordingThread(delegate
            {
                myLock.Lock();
                c.Await();
                myLock.Unlock();
            });

            t.Start();
            Thread.Sleep(SHORT_DELAY);
            myLock.Lock();
            c.Signal();
            myLock.Unlock();
            JoinAndVerifyThreads(SHORT_DELAY, t);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void HasWaitersChokesOnNullParameter([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            myLock.HasWaiters(null);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void GetWaitQueueLengthChokesOnNullParameter([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            myLock.GetWaitQueueLength(null);
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
            ReentrantLock myLock = new ReentrantLock(isFair);
            ICondition c = myLock.NewCondition();
            ReentrantLock lock2 = new ReentrantLock(isFair);
            lock2.HasWaiters(c);
        }

        [TestCase(true, ExpectedException = typeof(SynchronizationLockException))]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void HasWaitersChokesWhenNotLocked(bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            ICondition c = myLock.NewCondition();
            myLock.HasWaiters(c);
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void GetWaitQueueLengthChokesWhenNotOwned([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            ICondition c = (myLock.NewCondition());
            ReentrantLock lock2 = new ReentrantLock(isFair);
            lock2.GetWaitQueueLength(c);
        }

        [TestCase(true, ExpectedException = typeof(SynchronizationLockException))]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void GetWaitQueueLengthChokesWhenNotLocked(bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            ICondition c = (myLock.NewCondition());
            myLock.GetWaitQueueLength(c);
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
            ReentrantLock myLock = new ReentrantLock(true);
            ICondition c = myLock.NewCondition();
            Thread t = ExceptionRecordingThread(delegate
            {
                using(myLock.Lock())
                {
                    Assert.IsFalse(myLock.HasWaiters(c));
                    Assert.That(myLock.GetWaitQueueLength(c), Is.EqualTo(0));
                    c.Await();
                }
            });

            t.Start();

            Thread.Sleep(SHORT_DELAY);
            myLock.Lock();
            Assert.IsTrue(myLock.HasWaiters(c));
            Assert.AreEqual(1, myLock.GetWaitQueueLength(c));
            c.Signal();
            myLock.Unlock();

            Thread.Sleep(SHORT_DELAY);
            myLock.Lock();
            Assert.IsFalse(myLock.HasWaiters(c));
            Assert.AreEqual(0, myLock.GetWaitQueueLength(c));
            myLock.Unlock();
            JoinAndVerifyThreads(SHORT_DELAY, t);
        }

        [TestCase(true)]
        [TestCase(false, ExpectedException = typeof(NotSupportedException))]
        public void GetWaitQueueLengthReturnsNumberOfThreads(bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);

            ICondition c = myLock.NewCondition();
            Thread t1 = ExceptionRecordingThread(delegate
            {
                using (myLock.Lock())
                {
                    Assert.IsFalse(myLock.HasWaiters(c));
                    Assert.AreEqual(0, myLock.GetWaitQueueLength(c));
                    c.Await();
                }
            });

            Thread t2 = ExceptionRecordingThread(delegate
            {
                using (myLock.Lock())
                {
                    Assert.IsTrue(myLock.HasWaiters(c));
                    Assert.AreEqual(1, myLock.GetWaitQueueLength(c));
                    c.Await();
                }
            });

            t1.Start();

            Thread.Sleep(SHORT_DELAY);
            t2.Start();

            Thread.Sleep(SHORT_DELAY);
            myLock.Lock();
            Assert.IsTrue(myLock.HasWaiters(c));
            Assert.AreEqual(2, myLock.GetWaitQueueLength(c));
            c.SignalAll();
            myLock.Unlock();

            Thread.Sleep(SHORT_DELAY);
            myLock.Lock();
            Assert.IsFalse(myLock.HasWaiters(c));
            Assert.AreEqual(0, myLock.GetWaitQueueLength(c));
            myLock.Unlock();

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
            ReentrantLock myLock = new ReentrantLock(isFair);
            ICondition c = myLock.NewCondition();
            UninterruptableThread thread = new UninterruptableThread(myLock, c);

            thread.InternalThread.Start();

            while (!thread.LockStarted)
            {
                Thread.Sleep(100);
            }

            myLock.Lock();
            try
            {
                thread.InternalThread.Interrupt();
                thread.CanAwake = true;
                c.Signal();
            }
            finally
            {
                myLock.Unlock();
            }

            JoinAndVerifyThreads(thread.InternalThread);
            Assert.IsTrue(thread.Interrupted);
        }

        [Test] public void AwaitIsInterruptible([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);

            ICondition c = myLock.NewCondition();
            Thread t = ExceptionRecordingThread(delegate
            {
                Assert.Throws<ThreadInterruptedException>(delegate
                {
                    using(myLock.Lock()) c.Await();
                });
            });

            t.Start();

            Thread.Sleep(SHORT_DELAY);
            t.Interrupt();
            
            JoinAndVerifyThreads(SHORT_DELAY, t);
        }

        [Test] public void AwaitNanosIsInterruptible([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);

            ICondition c = myLock.NewCondition();
            Thread t = ExceptionRecordingThread(delegate
            {
                Assert.Throws<ThreadInterruptedException>(delegate
                {
                    using(myLock.Lock()) c.Await(new TimeSpan(0, 0, 0, 1));
                });
            });

            t.Start();

            Thread.Sleep(SHORT_DELAY);
            t.Interrupt();

            JoinAndVerifyThreads(SHORT_DELAY, t);
        }

        [Test] public void AwaitUntilIsInterruptible([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);

            ICondition c = myLock.NewCondition();
            Thread t = ExceptionRecordingThread(delegate
            {
                Assert.Throws<ThreadInterruptedException>(delegate
                {
                    using(myLock.Lock())
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
            ReentrantLock myLock = new ReentrantLock(isFair);

            ICondition c = myLock.NewCondition();
            Thread t1 = ExceptionRecordingThread(delegate
            {
                using(myLock.Lock()) c.Await();
            });

            Thread t2 = ExceptionRecordingThread(delegate
            {
                using(myLock.Lock()) c.Await();
            });

            t1.Start();
            t2.Start();

            Thread.Sleep(SHORT_DELAY);
            myLock.Lock();
            c.SignalAll();
            myLock.Unlock();

            JoinAndVerifyThreads(SHORT_DELAY, t1, t2);
        }

        [Test] public void AwaitAfterMultipleReentrantLockingPreservesLockCount([Values(true, false)] bool isFair)
        {
            ReentrantLock myLock = new ReentrantLock(isFair);
            ICondition c = myLock.NewCondition();
            Thread t1 = ExceptionRecordingThread(delegate
            {
                using (myLock.Lock())
                {
                    Assert.That(myLock.HoldCount, Is.EqualTo(1));
                    c.Await();
                    Assert.That(myLock.HoldCount, Is.EqualTo(1));
                }
            });
            Thread t2 = ExceptionRecordingThread(delegate
            {
                using (myLock.Lock())
                    using (myLock.Lock())
                    {
                        Assert.That(myLock.HoldCount, Is.EqualTo(2));
                        c.Await();
                        Assert.That(myLock.HoldCount, Is.EqualTo(2));
                    }
            });

            t1.Start();
            t2.Start();
            Thread.Sleep(SHORT_DELAY);
            Assert.IsFalse(myLock.IsLocked);
            using(myLock.Lock()) c.SignalAll();

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
            ReentrantLock myLock = new ReentrantLock(isFair);
            StringAssert.Contains("Unlocked", myLock.ToString());
            using (myLock.Lock())
            {
                StringAssert.Contains("Locked by thread", myLock.ToString());
            }
            StringAssert.Contains("Unlocked", myLock.ToString());
        }

        #region Private Methods

        private ThreadStart ExceptionRecordingAction(ThreadStart action)
        {
            return delegate
            {
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
            return new Thread(ExceptionRecordingAction(action));
        }

        private void InterruptedLock()
        {
            try
            {
                Assert.Throws<ThreadInterruptedException>(delegate
                {
                    _testee.LockInterruptibly();
                });
            }
            catch (Exception e)
            {
                if (_threadException != null) _threadException = e;
            }
        }

        private void InterruptibleLock()
        {
            try
            {
                _testee.LockInterruptibly();
            }
            catch (ThreadInterruptedException)
            {
            }
            catch (Exception e)
            {
                if (_threadException != null) _threadException = e;
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
            }
            foreach (Thread thread in threads)
            {
                Assert.IsFalse(thread.IsAlive, "Thread {0} is expected to be terminated but still alive.", thread);
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