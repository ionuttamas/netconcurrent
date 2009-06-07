using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Spring.Threading.Collections.Generic;
using Spring.Threading.Future;

namespace Spring.Threading.Execution
{
    [TestFixture]
    public class AbstractExecutorServiceTests : BaseThreadingTestCase
    {
        internal class DirectExecutorService : AbstractExecutorService
        {
            private volatile bool _shutdown;

            public override bool IsShutdown
            {
                get { return _shutdown; }
            }

            public override bool IsTerminated
            {
                get { return IsShutdown; }
            }

            public override void Execute(IRunnable r)
            {
                r.Run();
            }

            public override void Shutdown()
            {
                _shutdown = true;
            }

            public override IList<IRunnable> ShutdownNow()
            {
                _shutdown = true;
                return (IList<IRunnable>) ArrayList.ReadOnly(new List<IRunnable>());
            }

            public override bool AwaitTermination(TimeSpan duration)
            {
                return IsShutdown;
            }
        }

        [Test]
        public void Execute1()
        {
            ThreadPoolExecutor p = new ThreadPoolExecutor(1, 1, new TimeSpan(0, 1, 0), new ArrayBlockingQueue<IRunnable>(1));
            try
            {
                for (int i = 0; i < 5; ++i)
                {
                    p.Submit(new MediumRunnable());
                }
                Assert.Fail("Should throw an exception.");
            }
            catch (RejectedExecutionException)
            {
            }
            finally
            {
                JoinPool(p);
            }
        }


        [Test]
        public void Execute2()
        {
            ThreadPoolExecutor p = new ThreadPoolExecutor(1, 1, new TimeSpan(0, 1, 0), new ArrayBlockingQueue<IRunnable>(1));
            try
            {
                for (int i = 0; i < 5; ++i)
                {
                    p.Submit(new SmallCallable());
                }
                Assert.Fail("Should throw an exception.");
            }
            catch (RejectedExecutionException)
            {
            }
            JoinPool(p);
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void ExecuteNullRunnable()
        {
            IExecutorService e = new DirectExecutorService();
            TrackedShortRunnable task = null;
            e.Submit(task);
        }

        [Test]
        public void ExecuteRunnable()
        {
            IExecutorService e = new DirectExecutorService();
            TrackedShortRunnable task = new TrackedShortRunnable();
            Assert.IsFalse(task.IsDone);
            IFuture<object> future = e.Submit(task);
            future.GetResult();
            Assert.IsTrue(task.IsDone);
        }


        [Test]
        public void InterruptedSubmit()
        {
			ThreadPoolExecutor p = new ThreadPoolExecutor(1, 1, new TimeSpan(0, 1, 0),  new ArrayBlockingQueue<IRunnable>(10));
            Thread t = new Thread(delegate()
            {
                try
                {
                    p.Submit(new Task(delegate
                         {
                             try
                             {
                                 Thread.Sleep(MEDIUM_DELAY_MS);
                                 Assert.Fail("Should throw an exception");
                             }
                             catch (ThreadInterruptedException)
                             {
                             }

                         })).GetResult();
                }
                catch (ThreadInterruptedException)
                {
                }
            });
            t.Start();
            Thread.Sleep(SHORT_DELAY_MS);
            t.Interrupt();
            JoinPool(p);
        }


        [Test]
        [TestCase(typeof(string))]
        public void InvokeAll1<T>()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                e.InvokeAll((IEnumerable<ICallable<T>>)null);
            }
            catch (ArgumentNullException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void InvokeAll2()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                IList<IFuture<bool>> r = e.InvokeAll(new List<ICallable<bool>>());
                Assert.IsTrue((r.Count == 0));
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void InvokeAll3()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable<string>[] l = new ICallable<string>[] { new StringTask(), null };
                e.InvokeAll(l);
            }
            catch (ArgumentNullException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void InvokeAll4<T>()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable<T>[] l = new ICallable<T>[] { new NPETask<T>() };
                IList<IFuture<T>> result = e.InvokeAll(l);
                Assert.AreEqual(1, result.Count);
                foreach (IFuture<T> future in result)
                {
                    future.GetResult();
                }
            }
            catch (ExecutionException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void InvokeAll5()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable<string>[] l = new ICallable<string>[] {new StringTask(), new StringTask()};
                IList<IFuture<string>> result = e.InvokeAll(l);
                Assert.AreEqual(2, result.Count);
                foreach (IFuture<string> future in result)
                {
                    Assert.AreSame(TEST_STRING, future.GetResult());
                }
            }
            catch (ExecutionException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }

        [Test]
        public void InvokeAny1()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                e.InvokeAny((IEnumerable<Call<bool>>)null);
            }
            catch (ArgumentNullException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void InvokeAny2()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                e.InvokeAny(new ICallable<bool>[0]);
            }
            catch (ArgumentException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void InvokeAny3()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable<string>[] l = new ICallable<string>[] {new StringTask(), null};
                e.InvokeAny(l);
            }
            catch (ArgumentNullException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void InvokeAny4<T>()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable<T>[] l = new ICallable<T>[] { new NPETask<T>() };
                e.InvokeAny(l);
            }
            catch (ExecutionException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void InvokeAny5()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable<string>[] l = new ICallable<string>[] { new StringTask(), new StringTask() };
                string result = e.InvokeAny(l);
                Assert.AreSame(TEST_STRING, result);
            }
            catch (ExecutionException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }

        [Test]
        public void SubmitCallable()
        {
            IExecutorService e = new DirectExecutorService();
            IFuture<string> future = e.Submit(new StringTask());
            string result = future.GetResult();
            Assert.AreSame(TEST_STRING, result);
        }

        [Test]
        public void SubmitEE()
        {
			ThreadPoolExecutor p = new ThreadPoolExecutor(1, 1, new TimeSpan(0,0,0,60), new ArrayBlockingQueue<IRunnable>(10));
		
			try
			{
			    Callable<bool> c = new Call<bool>(
			        delegate
			            {
                            int zero = 0;
                            int i = 5 / zero;
                            return true;
                        });
			
				for (int i = 0; i < 5; i++)
				{
					p.Submit(c).GetResult();
				}
				Assert.Fail("Should throw an exception.");
			}
			catch (ExecutionException)
			{
			}
			JoinPool(p);
        }

        [Test]
        public void SubmitIE()
        {
            ThreadPoolExecutor p = new ThreadPoolExecutor(1, 1, new TimeSpan(0, 60, 0), new ArrayBlockingQueue<IRunnable>(10));

            Callable<bool> c = new Call<bool>(
                delegate
                    {
                        try
                        {
                            p.Submit(new SmallCallable()).GetResult();
                            Assert.Fail("Should throw an exception.");
                        }
                        catch (ThreadInterruptedException)
                        {
                        }
                        catch (RejectedExecutionException)
                        {
                        }
                        catch (ExecutionException)
                        {
                        }
                        return true;
                    });

            Thread t = new Thread(delegate() { try {c.Call();} catch(Exception){} });
            t.Start();
            Thread.Sleep(SHORT_DELAY_MS);
            t.Interrupt();
            t.Join();

            JoinPool(p);
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void SubmitNullCallable()
        {
            IExecutorService e = new DirectExecutorService();
            StringTask t = null;
            e.Submit(t);
        }

        [Test]
        public void SubmitRunnable()
        {
            IExecutorService e = new DirectExecutorService();
            IFuture<object> future = e.Submit(new NullRunnable());
            future.GetResult();
            Assert.IsTrue(future.IsDone);
        }


        [Test]
        public void SubmitRunnable2()
        {
            IExecutorService e = new DirectExecutorService();
            IFuture<string> future = e.Submit(new NullRunnable(), TEST_STRING);
            string result = future.GetResult();
            Assert.AreSame(TEST_STRING, result);
        }


        [Test]
        public void TimedInvokeAll1()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                e.InvokeAll((ICollection<Call<bool>>)null, MEDIUM_DELAY_MS);
            }
            catch (ArgumentNullException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void TimedInvokeAll2()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                IList<IFuture<bool>> r = e.InvokeAll<bool>(new ICallable<Call<bool>>[0], MEDIUM_DELAY_MS);
                Assert.IsTrue((r.Count == 0));
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void TimedInvokeAll3()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable[] l = new ICallable[] { new StringTask(), null };
                e.InvokeAll(l, MEDIUM_DELAY_MS);
            }
            catch (ArgumentNullException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void TimedInvokeAll4()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable[] l = new ICallable[] { new NPETask() };
                IList<IFuture> result = e.InvokeAll(l, MEDIUM_DELAY_MS);
                Assert.AreEqual(1, result.Count);
                foreach (IFuture future in result)
                {
                    future.GetResult();
                }
            }
            catch (ExecutionException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void TimedInvokeAll5()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable[] l = new ICallable[] { new StringTask(), new StringTask() };
                IList<IFuture> result = e.InvokeAll(l, MEDIUM_DELAY_MS);
                Assert.AreEqual(2, result.Count);
                foreach (IFuture future in result)
                {
                    Assert.AreSame(TEST_STRING, future.GetResult());
                }
            }
            catch (ExecutionException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void TimedInvokeAll6()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable[] list = new ICallable[] { new StringTask(), Executors.CreateCallable(new MediumPossiblyInterruptedRunnable(), TEST_STRING), new StringTask() };
                IList<IFuture> result = e.InvokeAll(list, SMALL_DELAY_MS);
                Assert.AreEqual(3, result.Count);
                IEnumerator<IFuture> it = result.GetEnumerator();
                IFuture f1 = null; 
                IFuture f2 = null; 
                IFuture f3 = null;

                if (it.MoveNext()) f1 = (IFuture)it.Current;
                if (it.MoveNext()) f2 = (IFuture)it.Current;
                if (it.MoveNext()) f3 = (IFuture)it.Current;

                if ( f1 == null || f2 == null || f3 == null )
                {
                    Assert.Fail("Missing some futures");
                }

                Assert.IsTrue(f1.IsDone);
                Assert.IsFalse(f1.IsCancelled);
                Assert.IsTrue(f2.IsDone);
                Assert.IsTrue(f3.IsDone);
                Assert.IsTrue(f3.IsCancelled);
            }
            finally
            {
                JoinPool(e);
            }
        }

        [Test]
        public void TimedInvokeAllNullTimeUnit()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                e.InvokeAll(null, MEDIUM_DELAY_MS);
            }
            catch (ArgumentNullException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }

        [Test]
        public void TimedInvokeAny1()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                e.InvokeAny(null, MEDIUM_DELAY_MS);
            }
            catch (ArgumentNullException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }

        [Test]
        public void TimedInvokeAny2()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                e.InvokeAny(new ICallable[0], MEDIUM_DELAY_MS);
            }
            catch (ArgumentException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void TimedInvokeAny3()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable[] l = new ICallable[] { new StringTask(), null };
                e.InvokeAny(l, MEDIUM_DELAY_MS);
            }
            catch (ArgumentNullException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void TimedInvokeAny4()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable[] l = new ICallable[] { new NPETask() };
                e.InvokeAny(l, MEDIUM_DELAY_MS);
            }
            catch (ExecutionException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }


        [Test]
        public void TimedInvokeAny5()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                ICallable[] l = new ICallable[] { new StringTask(), new StringTask() };
                string result = (String) e.InvokeAny(l, MEDIUM_DELAY_MS);
                Assert.AreSame(TEST_STRING, result);
            }
            catch (ExecutionException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }

        [Test]
        public void TimedInvokeAnyNull()
        {
            IExecutorService e = new DirectExecutorService();
            try
            {
                e.InvokeAny(null, new TimeSpan(0));
            }
            catch (ArgumentNullException)
            {
            }
            finally
            {
                JoinPool(e);
            }
        }
    }
}