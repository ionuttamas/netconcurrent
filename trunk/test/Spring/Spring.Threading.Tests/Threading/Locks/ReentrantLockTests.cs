using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using NUnit.Framework;
using Spring.Util;

namespace Spring.Threading.Locks
{
    [TestFixture]
    public class ReentrantLockTests : BaseThreadingTestCase
    {
        private class AnonymousClassRunnable : IRunnable
        {
            private readonly ReentrantLock _myLock;

            public AnonymousClassRunnable(ReentrantLock myLock)
            {
                _myLock = myLock;
            }

            #region IRunnable Members

            public void Run()
            {
                try
                {
                    _myLock.TryLock(MEDIUM_DELAY_MS);
                    Debug.Fail("Should throw an exception.");
                }
                catch (ThreadInterruptedException)
                {
                }
            }

            #endregion
        }

        private class AnonymousClassRunnable1 : IRunnable
        {
            private readonly ReentrantLock _myLock;

            public AnonymousClassRunnable1(ReentrantLock myLock)
            {
                _myLock = myLock;
            }

            #region IRunnable Members

            public void Run()
            {
                Debug.Assert(!_myLock.TryLock());
            }

            #endregion
        }

        private class AnonymousClassRunnable2 : IRunnable
        {
            private readonly ReentrantLock _myLock;

            public AnonymousClassRunnable2(ReentrantLock myLock)
            {
                _myLock = myLock;
            }

            #region IRunnable Members

            public void Run()
            {
                Debug.Assert(!_myLock.TryLock(new TimeSpan(0, 0, 1/1000)));
            }

            #endregion
        }

        private class AnonymousClassRunnable3 : IRunnable
        {
            private readonly ReentrantLock _myLock;

            public AnonymousClassRunnable3(ReentrantLock myLock)
            {
                _myLock = myLock;
            }

            #region IRunnable Members

            public void Run()
            {
                _myLock.Lock();
                Thread.Sleep(new TimeSpan(10000*SMALL_DELAY_MS.Milliseconds));
                _myLock.Unlock();
            }

            #endregion
        }

        private class AnonymousClassRunnable4 : IRunnable
        {
            private readonly ReentrantLock _myLock;
            private readonly ICondition condition;

            public AnonymousClassRunnable4(ReentrantLock myLock, ICondition c)
            {
                _myLock = myLock;
                condition = c;
            }

            #region IRunnable Members

            public void Run()
            {
                _myLock.Lock();
                condition.Await();
                _myLock.Unlock();
            }

            #endregion
        }

        private class AnonymousClassRunnable5 : IRunnable
        {
            private readonly ReentrantLock _myLock;
            private readonly ICondition condition;

            public AnonymousClassRunnable5(ReentrantLock myLock, ICondition c)
            {
                _myLock = myLock;
                condition = c;
            }

            #region IRunnable Members

            public void Run()
            {
                _myLock.Lock();
                Debug.Assert(!_myLock.HasWaiters(condition));
                Debug.Assert(0 == _myLock.GetWaitQueueLength(condition));
                condition.Await();
                _myLock.Unlock();
            }

            #endregion
        }

        private class AnonymousClassRunnable6 : IRunnable
        {
            private readonly ReentrantLock _myLock;
            private readonly ICondition condition;

            public AnonymousClassRunnable6(ReentrantLock myLock, ICondition c)
            {
                _myLock = myLock;
                condition = c;
            }

            #region IRunnable Members

            public void Run()
            {
                _myLock.Lock();
                Debug.Assert(!_myLock.HasWaiters(condition));
                Debug.Assert(0 == _myLock.GetWaitQueueLength(condition));
                condition.Await();
                _myLock.Unlock();
            }

            #endregion
        }

        private class AnonymousClassRunnable7 : IRunnable
        {
            private readonly ICondition condition;
            private readonly ReentrantLock myLock;

            public AnonymousClassRunnable7(ReentrantLock myLock, ICondition c)
            {
                this.myLock = myLock;
                condition = c;
            }

            #region IRunnable Members

            public void Run()
            {
                myLock.Lock();
                Debug.Assert(myLock.HasWaiters(condition));
                Debug.Assert(1 == myLock.GetWaitQueueLength(condition));
                condition.Await();
                myLock.Unlock();
            }

            #endregion
        }

        private class AnonymousClassRunnable8 : IRunnable
        {
            private readonly ICondition condition;
            private readonly PublicReentrantLock myLock;

            public AnonymousClassRunnable8(PublicReentrantLock myLock, ICondition c)
            {
                this.myLock = myLock;
                condition = c;
            }

            #region IRunnable Members

            public void Run()
            {
                myLock.Lock();
                Debug.Assert(myLock.GetWaitingThreads(condition).Count == 0);
                condition.Await();
                myLock.Unlock();
            }

            #endregion
        }

        private class AnonymousClassRunnable9 : IRunnable
        {
            private readonly ICondition condition;
            private readonly PublicReentrantLock myLock;

            public AnonymousClassRunnable9(PublicReentrantLock myLock, ICondition c)
            {
                this.myLock = myLock;
                condition = c;
            }

            #region IRunnable Members

            public void Run()
            {
                myLock.Lock();
                Debug.Assert(myLock.GetWaitingThreads(condition).Count != 0);
                condition.Await();
                myLock.Unlock();
            }

            #endregion
        }

        private class AnonymousClassRunnable10 : IRunnable
        {
            private readonly ICondition condition;
            private readonly ReentrantLock myLock;

            public AnonymousClassRunnable10(ReentrantLock myLock, ICondition c)
            {
                this.myLock = myLock;
                condition = c;
            }

            #region IRunnable Members

            public void Run()
            {
                try
                {
                    myLock.Lock();
                    condition.Await();
                    myLock.Unlock();
                    Debug.Fail("Should throw an exception.");
                }
                catch (ThreadInterruptedException)
                {
                }
            }

            #endregion
        }

        private class AnonymousClassRunnable11 : IRunnable
        {
            private readonly ICondition condition;
            private readonly ReentrantLock myLock;

            public AnonymousClassRunnable11(ReentrantLock myLock, ICondition c)
            {
                this.myLock = myLock;
                condition = c;
            }

            #region IRunnable Members

            public void Run()
            {
                try
                {
                    myLock.Lock();
                    condition.Await(new TimeSpan(0, 0, 0, 1));
                    myLock.Unlock();
                    Debug.Fail("Should throw an exception.");
                }
                catch (ThreadInterruptedException)
                {
                }
            }

            #endregion
        }

        private class AnonymousClassRunnable12 : IRunnable
        {
            private readonly ICondition _condition;
            private readonly ReentrantLock _myLock;

            public AnonymousClassRunnable12(ReentrantLock myLock, ICondition c)
            {
                _myLock = myLock;
                _condition = c;
            }

            #region IRunnable Members

            public void Run()
            {
                try
                {
                    _myLock.Lock();
                    _condition.AwaitUntil(DateTime.Now.AddMilliseconds(10000));
                    _myLock.Unlock();
                    Debug.Fail("Should throw exception.");
                }
                catch (ThreadInterruptedException)
                {
                }
            }

            #endregion
        }

        private class AnonymousClassRunnable13 : IRunnable
        {
            private readonly ICondition condition;
            private readonly ReentrantLock myLock;

            public AnonymousClassRunnable13(ReentrantLock myLock, ICondition c)
            {
                this.myLock = myLock;
                condition = c;
            }

            #region IRunnable Members

            public void Run()
            {
                myLock.Lock();
                condition.Await();
                myLock.Unlock();
            }

            #endregion
        }

        private class AnonymousClassRunnable14 : IRunnable
        {
            private readonly ICondition condition;
            private readonly ReentrantLock myLock;

            public AnonymousClassRunnable14(ReentrantLock myLock, ICondition c)
            {
                this.myLock = myLock;
                condition = c;
            }

            #region IRunnable Members

            public void Run()
            {
                myLock.Lock();
                condition.Await();
                myLock.Unlock();
            }

            #endregion
        }

        internal class InterruptibleLockRunnable : IRunnable
        {
            internal ReentrantLock myLock;

            internal InterruptibleLockRunnable(ReentrantLock l)
            {
                myLock = l;
            }

            #region IRunnable Members

            public void Run()
            {
                try
                {
                    myLock.LockInterruptibly();
                }
                catch (ThreadInterruptedException)
                {
                }
            }

            #endregion
        }

        internal class InterruptedLockRunnable : IRunnable
        {
            internal ReentrantLock myLock;

            internal InterruptedLockRunnable(ReentrantLock l)
            {
                myLock = l;
            }

            #region IRunnable Members

            public void Run()
            {
                try
                {
                    myLock.LockInterruptibly();
                    Debug.Fail("Should throw an exception.");
                }
                catch (ThreadInterruptedException)
                {
                }
            }

            #endregion
        }

        internal class UninterruptableThread : IRunnable
        {
            public readonly Thread InternalThread;
            private readonly ICondition _condition;
            private readonly ReentrantLock _myLock;

            public volatile bool canAwake;
            public volatile bool interrupted;
            public volatile bool lockStarted;

            public UninterruptableThread(ReentrantLock myLock, ICondition c)
            {
                _myLock = myLock;
                _condition = c;
                InternalThread = new Thread(Run);
            }

            #region IRunnable Members

            public void Run()
            {
                _myLock.Lock();
                lockStarted = true;

                while (!canAwake)
                {
                    _condition.AwaitUninterruptibly();
                }

                interrupted = InternalThread.IsAlive;

                _myLock.Unlock();
            }

            #endregion
        }

        [Serializable]
        internal class PublicReentrantLock : ReentrantLock
        {
            internal PublicReentrantLock()
            {
            }

            internal PublicReentrantLock(bool fair) : base(fair)
            {
            }
        }

        [Test]
        public void Await()
        {
            ReentrantLock myLock = new ReentrantLock();
            ICondition c = myLock.NewCondition();
            Thread t = new Thread(new AnonymousClassRunnable4(myLock, c).Run);

            t.Start();
            Thread.Sleep(SHORT_DELAY_MS);
            myLock.Lock();
            c.Signal();
            myLock.Unlock();
            t.Join(SHORT_DELAY_MS);
            Assert.IsFalse(t.IsAlive);
        }

        [Test]
        [ExpectedException(typeof (SynchronizationLockException))]
        public void Await_IllegalMonitor()
        {
            ReentrantLock myLock = new ReentrantLock();
            ICondition c = myLock.NewCondition();
            c.Await();
        }

        [Test]
        public void Await_Interrupt()
        {
            ReentrantLock myLock = new ReentrantLock();

            ICondition c = myLock.NewCondition();
            Thread t = new Thread(new AnonymousClassRunnable10(myLock, c).Run);

            t.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            t.Interrupt();
            t.Join(SHORT_DELAY_MS);
            Assert.IsFalse(t.IsAlive);
        }

        [Test]
        public void Await_Timeout()
        {
            ReentrantLock myLock = new ReentrantLock();
            ICondition c = myLock.NewCondition();
            myLock.Lock();
            c.Await(SHORT_DELAY_MS);
            myLock.Unlock();
        }

        [Test]
        public void AwaitNanos_Interrupt()
        {
            ReentrantLock myLock = new ReentrantLock();

            ICondition c = myLock.NewCondition();
            Thread t = new Thread(new AnonymousClassRunnable11(myLock, c).Run);

            t.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            t.Interrupt();
            t.Join(SHORT_DELAY_MS);
            Assert.IsFalse(t.IsAlive);
        }

        [Test]
        public void AwaitNanos_Timeout()
        {
            ReentrantLock myLock = new ReentrantLock();
            ICondition c = myLock.NewCondition();
            myLock.Lock();
            Assert.IsTrue(c.Await(new TimeSpan(1)));
            myLock.Unlock();
        }

        [Test]
        public void AwaitUninterruptibly()
        {
            ReentrantLock myLock = new ReentrantLock();
            ICondition c = myLock.NewCondition();
            UninterruptableThread thread = new UninterruptableThread(myLock, c);

            thread.InternalThread.Start();

            while (!thread.lockStarted)
            {
                Thread.Sleep(new TimeSpan(0, 0, 0, 0, 100));
            }

            myLock.Lock();
            try
            {
                thread.InternalThread.Interrupt();
                thread.canAwake = true;
                c.Signal();
            }
            finally
            {
                myLock.Unlock();
            }

            thread.InternalThread.Join();
            Assert.IsTrue(thread.interrupted);
            Assert.IsFalse(thread.InternalThread.IsAlive);
        }

        [Test]
        public void AwaitUntil_Interrupt()
        {
            ReentrantLock myLock = new ReentrantLock();

            ICondition c = myLock.NewCondition();
            Thread t = new Thread(new AnonymousClassRunnable12(myLock, c).Run);

            t.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            t.Interrupt();
            t.Join(SHORT_DELAY_MS);
            Assert.IsFalse(t.IsAlive);
        }

        [Test]
        public void AwaitUntil_Timeout()
        {
            ReentrantLock myLock = new ReentrantLock();
            ICondition c = myLock.NewCondition();
            myLock.Lock();
            c.AwaitUntil(DateTime.Now.AddMilliseconds(10));
            myLock.Unlock();
        }

        [Test]
        public void Constructor()
        {
            ReentrantLock rl = new ReentrantLock();
            Assert.IsFalse(rl.IsFair);
            ReentrantLock r2 = new ReentrantLock(true);
            Assert.IsTrue(r2.IsFair);
        }

        [Test]
        public void GetHoldCount()
        {
            ReentrantLock myLock = new ReentrantLock();
            for (int i = 1; i <= DEFAULT_COLLECTION_SIZE; i++)
            {
                myLock.Lock();
                Assert.AreEqual(i, myLock.HoldCount);
            }
            for (int i = DEFAULT_COLLECTION_SIZE; i > 0; i--)
            {
                myLock.Unlock();
                Assert.AreEqual(i - 1, myLock.HoldCount);
            }
        }

        [Test]
        public void GetQueuedThreads()
        {
            PublicReentrantLock myLock = new PublicReentrantLock(true);
            Thread t1 = new Thread(new InterruptedLockRunnable(myLock).Run);
            Thread t2 = new Thread(new InterruptibleLockRunnable(myLock).Run);

            Assert.IsTrue((myLock.QueuedThreads.Count == 0));
            myLock.Lock();
            Assert.IsTrue((myLock.QueuedThreads.Count == 0));
            t1.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsTrue(CollectionUtils.Contains(myLock.QueuedThreads, t1));
            t2.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsTrue(CollectionUtils.Contains(myLock.QueuedThreads, t1));
            Assert.IsTrue(CollectionUtils.Contains(myLock.QueuedThreads, t2));
            t1.Interrupt();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsFalse(CollectionUtils.Contains(myLock.QueuedThreads, t1));
            Assert.IsTrue(CollectionUtils.Contains(myLock.QueuedThreads, t2));
            myLock.Unlock();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsTrue((myLock.QueuedThreads.Count == 0));
            t1.Join();
            t2.Join();
        }


        [Test]
        public void GetQueueLength()
        {
            ReentrantLock myLock = new ReentrantLock(true);
            Thread t1 = new Thread(new InterruptedLockRunnable(myLock).Run);
            Thread t2 = new Thread(new InterruptibleLockRunnable(myLock).Run);

            Assert.AreEqual(0, myLock.QueueLength);
            myLock.Lock();
            t1.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.AreEqual(1, myLock.QueueLength);
            t2.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.AreEqual(2, myLock.QueueLength);
            t1.Interrupt();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.AreEqual(1, myLock.QueueLength);
            myLock.Unlock();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.AreEqual(0, myLock.QueueLength);
            t1.Join();
            t2.Join();
        }


        [Test]
        public void GetQueueLength_fair()
        {
            ReentrantLock myLock = new ReentrantLock(true);
            Thread t1 = new Thread(new InterruptedLockRunnable(myLock).Run);
            Thread t2 = new Thread(new InterruptibleLockRunnable(myLock).Run);
            Assert.AreEqual(0, myLock.QueueLength);
            myLock.Lock();
            t1.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.AreEqual(1, myLock.QueueLength);
            t2.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.AreEqual(2, myLock.QueueLength);
            t1.Interrupt();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.AreEqual(1, myLock.QueueLength);
            myLock.Unlock();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.AreEqual(0, myLock.QueueLength);
            t1.Join();
            t2.Join();
        }

        [Test]
        public void GetWaitingThreads()
        {
            PublicReentrantLock myLock = new PublicReentrantLock(true);

            ICondition c = myLock.NewCondition();
            Thread t1 = new Thread(new AnonymousClassRunnable8(myLock, c).Run);

            Thread t2 = new Thread(new AnonymousClassRunnable9(myLock, c).Run);

            myLock.Lock();
            Assert.IsTrue((myLock.GetWaitingThreads(c).Count == 0));
            myLock.Unlock();
            t1.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            t2.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            myLock.Lock();
            Assert.IsTrue(myLock.HasWaiters(c));
            Assert.IsTrue(CollectionUtils.Contains(myLock.GetWaitingThreads(c), t1));
            Assert.IsTrue(CollectionUtils.Contains(myLock.GetWaitingThreads(c), t2));
            c.SignalAll();
            myLock.Unlock();

            Thread.Sleep(SHORT_DELAY_MS);
            myLock.Lock();
            Assert.IsFalse(myLock.HasWaiters(c));
            Assert.IsTrue((myLock.GetWaitingThreads(c).Count == 0));
            myLock.Unlock();
            t1.Join(SHORT_DELAY_MS);
            t2.Join(SHORT_DELAY_MS);
            Assert.IsFalse(t1.IsAlive);
            Assert.IsFalse(t2.IsAlive);
        }

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void GetWaitingThreadsIAE()
        {
            PublicReentrantLock myLock = new PublicReentrantLock(true);
            ICondition c = (myLock.NewCondition());
            PublicReentrantLock lock2 = new PublicReentrantLock(true);
            lock2.GetWaitingThreads(c);
        }


        [Test]
        [ExpectedException(typeof (SynchronizationLockException))]
        public void GetWaitingThreadsIMSE()
        {
            PublicReentrantLock myLock = new PublicReentrantLock(true);
            ICondition c = (myLock.NewCondition());
            myLock.GetWaitingThreads(c);
        }

        [Test]
        [ExpectedException(typeof (NullReferenceException))]
        public void GetWaitingThreadsNRE()
        {
            PublicReentrantLock myLock = new PublicReentrantLock(true);
            myLock.GetWaitingThreads(null);
        }

        [Test]
        public void GetWaitQueueLength()
        {
            ReentrantLock myLock = new ReentrantLock(true);

            ICondition c = myLock.NewCondition();
            Thread t1 = new Thread(new AnonymousClassRunnable6(myLock, c).Run);

            Thread t2 = new Thread(new AnonymousClassRunnable7(myLock, c).Run);

            t1.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            t2.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            myLock.Lock();
            Assert.IsTrue(myLock.HasWaiters(c));
            Assert.AreEqual(2, myLock.GetWaitQueueLength(c));
            c.SignalAll();
            myLock.Unlock();

            Thread.Sleep(SHORT_DELAY_MS);
            myLock.Lock();
            Assert.IsFalse(myLock.HasWaiters(c));
            Assert.AreEqual(0, myLock.GetWaitQueueLength(c));
            myLock.Unlock();
            t1.Join(SHORT_DELAY_MS);
            t2.Join(SHORT_DELAY_MS);
            Assert.IsFalse(t1.IsAlive);
            Assert.IsFalse(t2.IsAlive);
        }

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void GetWaitQueueLengthArgumentException()
        {
            ReentrantLock myLock = new ReentrantLock(true);
            ICondition c = (myLock.NewCondition());
            ReentrantLock lock2 = new ReentrantLock(true);
            lock2.GetWaitQueueLength(c);
        }

        [Test]
        [ExpectedException(typeof (NullReferenceException))]
        public void GetWaitQueueLengthNRE()
        {
            ReentrantLock myLock = new ReentrantLock(true);
            myLock.GetWaitQueueLength(null);
        }

        [Test]
        [ExpectedException(typeof (SynchronizationLockException))]
        public void GetWaitQueueLengthSLE()
        {
            ReentrantLock myLock = new ReentrantLock(true);
            ICondition c = (myLock.NewCondition());
            myLock.GetWaitQueueLength(c);
        }


        [Test]
        public void HasQueuedThread()
        {
            ReentrantLock sync = new ReentrantLock(true);
            Thread t1 = new Thread(new InterruptedLockRunnable(sync).Run);
            Thread t2 = new Thread(new InterruptibleLockRunnable(sync).Run);
            Assert.IsFalse(sync.IsQueuedThread(t1));
            Assert.IsFalse(sync.IsQueuedThread(t2));
            sync.Lock();
            t1.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsTrue(sync.IsQueuedThread(t1));
            t2.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsTrue(sync.IsQueuedThread(t1));
            Assert.IsTrue(sync.IsQueuedThread(t2));
            t1.Interrupt();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsFalse(sync.IsQueuedThread(t1));
            Assert.IsTrue(sync.IsQueuedThread(t2));
            sync.Unlock();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsFalse(sync.IsQueuedThread(t1));

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsFalse(sync.IsQueuedThread(t2));
            t1.Join();
            t2.Join();
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void HasQueuedThreadNPE()
        {
            ReentrantLock sync = new ReentrantLock(true);
            sync.IsQueuedThread(null);
        }

        [Test]
        public void HasQueuedThreads()
        {
            ReentrantLock myLock = new ReentrantLock(true);
            Thread t1 = new Thread(new InterruptedLockRunnable(myLock).Run);
            Thread t2 = new Thread(new InterruptibleLockRunnable(myLock).Run);

            Assert.IsFalse(myLock.HasQueuedThreads);
            myLock.Lock();
            t1.Start();
            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsTrue(myLock.HasQueuedThreads);
            t2.Start();
            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsTrue(myLock.HasQueuedThreads);
            t1.Interrupt();
            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsTrue(myLock.HasQueuedThreads);
            myLock.Unlock();
            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsFalse(myLock.HasQueuedThreads);
            t1.Join();
            t2.Join();
        }

        [Test]
        public void HasWaiters()
        {
            ReentrantLock myLock = new ReentrantLock(true);
            ICondition c = myLock.NewCondition();
            Thread t = new Thread(new AnonymousClassRunnable5(myLock, c).Run);

            t.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            myLock.Lock();
            Assert.IsTrue(myLock.HasWaiters(c));
            Assert.AreEqual(1, myLock.GetWaitQueueLength(c));
            c.Signal();
            myLock.Unlock();

            Thread.Sleep(SHORT_DELAY_MS);
            myLock.Lock();
            Assert.IsFalse(myLock.HasWaiters(c));
            Assert.AreEqual(0, myLock.GetWaitQueueLength(c));
            myLock.Unlock();
            t.Join(SHORT_DELAY_MS);
            Assert.IsFalse(t.IsAlive);
        }

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void HasWaitersIAE()
        {
            ReentrantLock myLock = new ReentrantLock(true);
            ICondition c = myLock.NewCondition();
            ReentrantLock lock2 = new ReentrantLock(true);
            lock2.HasWaiters(c);
        }


        [Test]
        [ExpectedException(typeof (SynchronizationLockException))]
        public void HasWaitersIMSE()
        {
            ReentrantLock myLock = new ReentrantLock(true);
            ICondition c = myLock.NewCondition();
            myLock.HasWaiters(c);
        }

        [Test]
        [ExpectedException(typeof (NullReferenceException))]
        public void HasWaitersNRE()
        {
            ReentrantLock myLock = new ReentrantLock(true);
            myLock.HasWaiters(null);
        }


        [Test]
        public void InterruptedException2()
        {
            ReentrantLock myLock = new ReentrantLock();
            myLock.Lock();
            Thread t = new Thread(new AnonymousClassRunnable(myLock).Run);
            t.Start();
            t.Interrupt();
        }

        [Test]
        public void IsFairLock()
        {
            ReentrantLock rl = new ReentrantLock(true);
            rl.Lock();
            Assert.IsTrue(rl.IsLocked);
            rl.Unlock();
        }


        [Test]
        public void IsIsLocked()
        {
            ReentrantLock myLock = new ReentrantLock();
            myLock.Lock();
            Assert.IsTrue(myLock.IsLocked);
            myLock.Unlock();
            Assert.IsFalse(myLock.IsLocked);
            Thread t = new Thread(new AnonymousClassRunnable3(myLock).Run);
            t.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            Assert.IsTrue(myLock.IsLocked);
            t.Join();
            Assert.IsFalse(myLock.IsLocked);
        }

        [Test]
        public void Lock()
        {
            ReentrantLock rl = new ReentrantLock();
            rl.Lock();
            Assert.IsTrue(rl.IsLocked);
            rl.Unlock();
        }


        [Test]
        public void LockInterruptibly1()
        {
            ReentrantLock myLock = new ReentrantLock();
            myLock.Lock();
            Thread t = new Thread(new InterruptedLockRunnable(myLock).Run);
            t.Start();
            Thread.Sleep(SHORT_DELAY_MS);
            t.Interrupt();
            Thread.Sleep(SHORT_DELAY_MS);
            myLock.Unlock();
            t.Join();
        }


        [Test]
        public void LockInterruptibly2()
        {
            ReentrantLock myLock = new ReentrantLock();
            myLock.LockInterruptibly();
            Thread t = new Thread(new InterruptedLockRunnable(myLock).Run);
            t.Start();
            t.Interrupt();
            Assert.IsTrue(myLock.IsLocked);
            Assert.IsTrue(myLock.HeldByCurrentThread);
            t.Join();
        }

        [Test]
        public void OutputToString()
        {
            ReentrantLock myLock = new ReentrantLock();
            String us = myLock.ToString();
            Assert.IsTrue(us.IndexOf("Unlocked") >= 0);
            myLock.Lock();
            String ls = myLock.ToString();
            Assert.IsTrue(ls.IndexOf("Locked by thread") >= 0);
        }

        [Test]
        public void Serialization()
        {
            ReentrantLock l = new ReentrantLock();
            l.Lock();
            l.Unlock();
            MemoryStream bout = new MemoryStream(10000);

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(bout, l);

            MemoryStream bin = new MemoryStream(bout.ToArray());
            BinaryFormatter formatter2 = new BinaryFormatter();
            ReentrantLock r = (ReentrantLock) formatter2.Deserialize(bin);
            r.Lock();
            r.Unlock();
        }


        [Test]
        [ExpectedException(typeof (SynchronizationLockException))]
        public void Signal_IllegalMonitor()
        {
            ReentrantLock myLock = new ReentrantLock();
            ICondition c = myLock.NewCondition();
            c.Signal();
        }


        [Test]
        public void SignalAll()
        {
            ReentrantLock myLock = new ReentrantLock();

            ICondition c = myLock.NewCondition();
            Thread t1 = new Thread(new AnonymousClassRunnable13(myLock, c).Run);

            Thread t2 = new Thread(new AnonymousClassRunnable14(myLock, c).Run);

            t1.Start();
            t2.Start();

            Thread.Sleep(SHORT_DELAY_MS);
            myLock.Lock();
            c.SignalAll();
            myLock.Unlock();
            t1.Join(SHORT_DELAY_MS);
            t2.Join(SHORT_DELAY_MS);
            Assert.IsFalse(t1.IsAlive);
            Assert.IsFalse(t2.IsAlive);
        }

        [Test]
        public void TryLock()
        {
            ReentrantLock rl = new ReentrantLock();
            Assert.IsTrue(rl.TryLock());
            Assert.IsTrue(rl.IsLocked);
            rl.Unlock();
        }

        [Test]
        public void TryLock_Timeout()
        {
            ReentrantLock myLock = new ReentrantLock();
            myLock.Lock();
            Thread t = new Thread(new AnonymousClassRunnable2(myLock).Run);
            t.Start();
            t.Join();
            myLock.Unlock();
        }

        [Test]
        public void TryLockWhenIsLocked()
        {
            ReentrantLock myLock = new ReentrantLock();
            myLock.Lock();
            Thread t = new Thread(new AnonymousClassRunnable1(myLock).Run);
            t.Start();
            t.Join();
            myLock.Unlock();
        }

        [Test]
        [ExpectedException(typeof (SynchronizationLockException))]
        public void Unlock_InvalidOperationException()
        {
            ReentrantLock rl = new ReentrantLock();
            rl.Unlock();
            Assert.Fail("Should throw an exception");
        }
    }
}