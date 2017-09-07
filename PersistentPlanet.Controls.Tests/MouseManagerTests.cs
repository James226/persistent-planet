using System;
using MemBus;
using Moq;
using PersistentPlanet.Controls.Controls;
using Xunit;

namespace PersistentPlanet.Controls.Tests
{
    public class MouseManagerTests
    {
        [Fact]
        public void WhenMoveIsCalledThenMouseMoveEventIsRaised()
        {
            var publisher = new Mock<IPublisher>();
            var mouseManager = new MouseManager(publisher.Object);
            mouseManager.Move(3, -2);

            publisher.Verify(p => p.Publish(It.Is<YAxisUpdatedEvent>(e => VerifyEvent(e))));
        }

        private bool VerifyEvent(YAxisUpdatedEvent evt)
        {
            return Math.Abs(evt.Axis.X - 3) < 0.001f && Math.Abs(evt.Axis.Y - -2) < 0.001f;
        }
    }
}
