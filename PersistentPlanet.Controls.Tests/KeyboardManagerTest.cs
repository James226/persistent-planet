using Moq;
using PersistentPlanet.Primitives.Platform;
using Xunit;

namespace PersistentPlanet.Controls.Tests
{
    public class KeyboardManagerTest
    {
        public class WhenKeyUpIsCalled
        {
            private readonly Mock<IKeyboardAxisManager> _xAxisManager;
            private readonly Mock<IKeyboardAxisManager> _zAxisManager;

            public WhenKeyUpIsCalled()
            {
                _xAxisManager = new Mock<IKeyboardAxisManager>();
                _zAxisManager = new Mock<IKeyboardAxisManager>();
                var keyboardManager = new KeyboardManager(_xAxisManager.Object, _zAxisManager.Object);

                keyboardManager.KeyUp(Key.W);
            }

            [Fact]
            public void ThenTheXAxisManagerIsInvoked()
            {
                _xAxisManager.Verify(m => m.KeyUp(Key.W));
            }

            [Fact]
            public void ThenTheZAxisManagerIsInvoked()
            {
                _zAxisManager.Verify(m => m.KeyUp(Key.W));
            }
        }

        public class WhenKeyDownIsCalled
        {
            private readonly Mock<IKeyboardAxisManager> _xAxisManager;
            private readonly Mock<IKeyboardAxisManager> _zAxisManager;

            public WhenKeyDownIsCalled()
            {
                _xAxisManager = new Mock<IKeyboardAxisManager>();
                _zAxisManager = new Mock<IKeyboardAxisManager>();
                var keyboardManager = new KeyboardManager(_xAxisManager.Object, _zAxisManager.Object);

                keyboardManager.KeyDown(Key.W);
            }

            [Fact]
            public void ThenTheXAxisManagerIsInvoked()
            {
                _xAxisManager.Verify(m => m.KeyDown(Key.W));
            }

            [Fact]
            public void ThenTheZAxisManagerIsInvoked()
            {
                _zAxisManager.Verify(m => m.KeyDown(Key.W));
            }
        }
    }
}
