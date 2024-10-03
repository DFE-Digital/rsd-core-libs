using DfE.CoreLibs.AsyncProcessing.Interfaces;
using DfE.CoreLibs.AsyncProcessing.Services;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using MediatR;
using NSubstitute;

namespace DfE.CoreLibs.AsyncProcessing.Tests.Services
{
    public class BackgroundServiceFactoryTests
    {
        private readonly IMediator _mediator;
        private readonly BackgroundServiceFactory _backgroundServiceFactory;

        public BackgroundServiceFactoryTests()
        {
            _mediator = Substitute.For<IMediator>();
            _backgroundServiceFactory = new BackgroundServiceFactory(_mediator);
        }

        [Theory]
        [CustomAutoData]
        public async Task EnqueueTask_ShouldProcessTaskInQueue(
            Func<Task<int>> taskFunc,
            IBackgroundServiceEvent eventMock)
        {
            // Arrange
            Func<int, IBackgroundServiceEvent> eventFactory = _ => eventMock;

            var semaphore = new SemaphoreSlim(0, 1);
            bool taskExecuted = false;

            Func<Task<int>> wrappedTaskFunc = async () =>
            {
                taskExecuted = true;
                semaphore.Release();
                return await taskFunc();
            };

            // Act
            _backgroundServiceFactory.EnqueueTask(wrappedTaskFunc, eventFactory);

            // Ensure the task gets processed
            await semaphore.WaitAsync(1000);

            // Assert
            Assert.True(taskExecuted, "Task in the queue should have been processed.");
        }

        [Theory]
        [CustomAutoData]
        public async Task EnqueueTask_ShouldPublishEvent_WhenEventFactoryIsProvided(
            int taskResult,
            IBackgroundServiceEvent eventMock)
        {
            // Arrange
            Func<int, IBackgroundServiceEvent> eventFactory = _ => eventMock;

            // Act
            _backgroundServiceFactory.EnqueueTask(() => Task.FromResult(taskResult), eventFactory);

            // Trigger processing
            await Task.Delay(100);

            // Assert
            await _mediator.Received(1).Publish(eventMock, Arg.Any<CancellationToken>());
        }

        [Theory]
        [CustomAutoData]
        public async Task StartProcessingQueue_ShouldProcessTasksSequentially(
            Func<Task<int>> taskFunc,
            IBackgroundServiceEvent eventMock)
        {
            // Arrange
            Func<int, IBackgroundServiceEvent> eventFactory = _ => eventMock;

            int taskCount = 0;
            Func<Task<int>> wrappedTaskFunc = async () =>
            {
                Interlocked.Increment(ref taskCount);
                return await taskFunc();
            };

            _backgroundServiceFactory.EnqueueTask(wrappedTaskFunc, eventFactory);
            _backgroundServiceFactory.EnqueueTask(wrappedTaskFunc, eventFactory);

            // Act
            await Task.Delay(100); // Allow time for processing

            // Assert
            Assert.Equal(2, taskCount);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldStopProcessing_WhenCancellationRequested()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var task = _backgroundServiceFactory.StartAsync(cts.Token);

            // Act
            await cts.CancelAsync();
            await task;

            // Assert
            Assert.True(task.IsCompletedSuccessfully);
        }
    }
}
