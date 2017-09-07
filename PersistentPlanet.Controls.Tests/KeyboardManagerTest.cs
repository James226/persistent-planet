using System;
using System.Numerics;
using FluentAssertions;
using MemBus;
using Moq;
using PersistentPlanet.Controls.Controls;
using PersistentPlanet.Primitives.Platform;
using Xunit;

namespace PersistentPlanet.Controls.Tests
{
    public class KeyboardManagerTest
    {
        [InlineData(new[] { Key.W }, 0, 1)]
        [InlineData(new[] { Key.S }, 0, -1)]
        [InlineData(new[] { Key.A }, -1, 0)]
        [InlineData(new[] { Key.D }, 1, 0)]
        [InlineData(new[] { Key.W, Key.D }, 1, 1)]
        [InlineData(new[] { Key.W, Key.S }, 0, 0)]
        [Theory]
        public void WhenKeyDownThenXAxisIsCorrect(Key[] keys, float x, float y)
        {
            var publisher = new Mock<IPublisher>();
            var keyboardManager = new KeyboardManager(publisher.Object);
            foreach (var key in keys)
            {
                keyboardManager.KeyDown(key);
            }

            publisher.Verify(p => p.Publish(It.Is<XAxisUpdatedEvent>(e => VerifyEvent(e, x, y))));
        }

        [Fact]
        public void WhenKeyUpThenXAxisIsCorrect()
        {
            var publisher = new Mock<IPublisher>();
            var keyboardManager = new KeyboardManager(publisher.Object);
            keyboardManager.KeyDown(Key.W);
            keyboardManager.KeyUp(Key.W);

            publisher.Verify(p => p.Publish(It.Is<XAxisUpdatedEvent>(e => VerifyEvent(e, 0, 0))));
        }

        [Fact]
        public void WhenKeyDownWithUnknownKeyXAxisIsNotBroadcast()
        {
            var publisher = new Mock<IPublisher>();
            var keyboardManager = new KeyboardManager(publisher.Object);
            keyboardManager.KeyDown(Key.K);

            publisher.Verify(p => p.Publish(It.IsAny<XAxisUpdatedEvent>()), Times.Never);
        }

        [Fact]
        public void WhenKeyUpWithUnknownKeyXAxisIsNotBroadcast()
        {
            var publisher = new Mock<IPublisher>();
            var keyboardManager = new KeyboardManager(publisher.Object);
            keyboardManager.KeyUp(Key.K);

            publisher.Verify(p => p.Publish(It.IsAny<XAxisUpdatedEvent>()), Times.Never);
        }

        public static bool VerifyEvent(XAxisUpdatedEvent evt, float x, float y)
        {
            return Math.Abs(evt.XAxis.X - x) < 0.001f && Math.Abs(evt.XAxis.Y - y) < 0.001f;
        }
    }
}
