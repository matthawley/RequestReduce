﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using Moq;
using RequestReduce.Module;
using RequestReduce.Reducer;
using RequestReduce.Utilities;
using Xunit;

namespace RequestReduce.Facts.Module
{
    public class ReducingQueueFacts
    {
        class FakeReducingQueue : ReducingQueue, IDisposable
        {
            public FakeReducingQueue(IReducer reducer, IReductionRepository reductionRepository) : base(reducer, reductionRepository)
            {
                isRunning = false;
            }

            public ConcurrentQueue<string> BaseQueue
            {
                get { return queue; }
            }

            public new void ProcessQueuedItem()
            {
                base.ProcessQueuedItem();
            }

            public FakeReductionRepository ReductionRepository { get { return reductionRepository as FakeReductionRepository;  } }

            public void Dispose()
            {
                backgroundThread.Abort();
            }
        }

        class FakeReductionRepository : IReductionRepository
        {
            private Hashtable dict = new Hashtable();

            public string FindReduction(string urls)
            {
                var key = Hasher.Hash(urls);
                return dict[key] as string;
            }

            public void AddReduction(Guid key, string reducedUrl)
            {
                dict[key] = reducedUrl;
            }
        }

        class TestableReducingQueue : Testable<FakeReducingQueue>, IDisposable
        {
            public TestableReducingQueue()
            {
                Inject<IReductionRepository>(new FakeReductionRepository());
                Mock<IReducer>().Setup(x => x.Process(Hasher.Hash("url"), "url")).Returns("reducedUrl");
            }

            public void Dispose()
            {
                ClassUnderTest.Dispose();
            }
        }

        public class Enqueue
        {
            [Fact]
            public void WillPutUrlsInTheQueue()
            {
                var testable = new TestableReducingQueue();
                string result;

                testable.ClassUnderTest.Enqueue("urls");

                testable.ClassUnderTest.BaseQueue.TryDequeue(out result);
                Assert.Equal("urls", result);
            }
        }

        public class ProcessQueuedItem
        {
            [Fact]
            public void WillReduceQueuedCSS()
            {
                var testable = new TestableReducingQueue();
                testable.ClassUnderTest.Enqueue("url");

                testable.ClassUnderTest.ProcessQueuedItem();

                testable.Mock<IReducer>().Verify(x => x.Process(It.IsAny<Guid>(), "url"), Times.Once());
            }

            [Fact]
            public void WillNotReduceItemIfAlreadyReduced()
            {
                var testable = new TestableReducingQueue();
                testable.ClassUnderTest.ReductionRepository.AddReduction(Hasher.Hash("url"), "url");
                testable.ClassUnderTest.Enqueue("url");

                testable.ClassUnderTest.ProcessQueuedItem();

                testable.Mock<IReducer>().Verify(x => x.Process(It.IsAny<Guid>(), "url"), Times.Never());
            }
        }

        public class Count
        {
            [Fact]
            public void WillReturnTheCountOfTheBaseQueue()
            {
                var testable = new TestableReducingQueue();
                testable.ClassUnderTest.Enqueue("url");
                testable.ClassUnderTest.Enqueue("url");

                var result = testable.ClassUnderTest.Count;

                Assert.Equal(2, result);
            }
        }

        public class CaptureErrors
        {
            [Fact]
            public void WillCaptureErrorsIfAnErrorActionIsRegistered()
            {
                var testable = new TestableReducingQueue();
                Exception error = null;
                testable.ClassUnderTest.CaptureError(x => error= x);
                testable.Mock<IReducer>().Setup(x => x.Process(It.IsAny<Guid>(), "url")).Throws(new ApplicationException());
                testable.ClassUnderTest.Enqueue("url");

                testable.ClassUnderTest.ProcessQueuedItem();

                Assert.True(error is ApplicationException);
            }
        }
    }
}