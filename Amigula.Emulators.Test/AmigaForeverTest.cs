using Amigula.Domain.DTO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amigula.Emulators.Test
{
    [TestClass]
    public class AmigaForeverTest
    {
        private AmigaForever _amigaForever;

        [TestInitialize]
        public void Initialize()
        {
            _amigaForever = new AmigaForever();
        }

        [TestMethod]
        public void AmigaForeverGetEmulatorPaths_ReturnsEmulatorDto()
        {
            // This test will only pass if you have Amiga Forever installed!
            var result = _amigaForever.GetEmulatorPaths();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(EmulatorDto));
            // only enable the below if you have Amiga Forever installed
            //Assert.IsNotNull(result.ConfigurationFilesPath);
            //Assert.IsNotNull(result.EmulatorPath);
        }
    }
}
