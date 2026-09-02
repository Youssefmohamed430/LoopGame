using Hangfire;
using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.Events;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Application.Services.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LoopGame.Tests.Services;

public class EventPublishingTests
{
    [Fact]
    public void InProcessEventPublisher_DispatchesToAllRegisteredHandlers()
    {
        // Arrange
        var handler1 = new Mock<IEventHandler>();
        var handler2 = new Mock<IEventHandler>();
        var handlers = new List<IEventHandler> { handler1.Object, handler2.Object };

        var publisher = new InProcessEventPublisher(handlers, NullLogger<InProcessEventPublisher>.Instance);
        var gameEvent = new GameEventDto(1, "practice_attempt", "loops", "Ideal", null);

        // Act
        publisher.Publish(gameEvent);

        // Assert
        handler1.Verify(h => h.Handle(gameEvent), Times.Once);
        handler2.Verify(h => h.Handle(gameEvent), Times.Once);
    }

    [Fact]
    public void InProcessEventPublisher_IsolatesHandlerExceptions()
    {
        // Arrange
        var failingHandler = new Mock<IEventHandler>();
        failingHandler.Setup(h => h.Handle(It.IsAny<GameEventDto>()))
            .Throws(new InvalidOperationException("Handler error"));

        var succeedingHandler = new Mock<IEventHandler>();

        var handlers = new List<IEventHandler> { failingHandler.Object, succeedingHandler.Object };
        var publisher = new InProcessEventPublisher(handlers, NullLogger<InProcessEventPublisher>.Instance);
        var gameEvent = new GameEventDto(1, "choice_submission", "beat1", "Ideal", null);

        // Act & Assert (Should not throw)
        publisher.Publish(gameEvent);

        succeedingHandler.Verify(h => h.Handle(gameEvent), Times.Once);
    }

    [Fact]
    public void AssessmentEventHandler_EnqueuesHangfireJob()
    {
        // Arrange
        var backgroundJobs = new Mock<IBackgroundJobClient>();
        var handler = new AssessmentEventHandler(backgroundJobs.Object, NullLogger<AssessmentEventHandler>.Instance);
        var gameEvent = new GameEventDto(1, "hint_request", "loops", "Free", null);

        // Act
        handler.Handle(gameEvent);

        // Assert - verify Enqueue was invoked on IBackgroundJobClient
        backgroundJobs.Verify(
            b => b.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()),
            Times.Once);
    }

    [Fact]
    public void AssessmentEventHandler_SwallowsExceptions_WhenHangfireFails()
    {
        // Arrange
        var backgroundJobs = new Mock<IBackgroundJobClient>();
        backgroundJobs.Setup(b => b.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()))
            .Throws(new Exception("Hangfire down"));

        var handler = new AssessmentEventHandler(backgroundJobs.Object, NullLogger<AssessmentEventHandler>.Instance);
        var gameEvent = new GameEventDto(1, "hint_request", "loops", "Free", null);

        // Act & Assert (Should not throw exception)
        handler.Handle(gameEvent);
    }
}
