using Amigula.Domain.DTO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amigula.AmigaForeverRepository.Test
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
        public void GetEmulatorPaths_ReturnsEmulatorDto()
        {
            var result = _amigaForever.GetEmulatorPaths();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(EmulatorDto));
            Assert.IsNotNull(result.ConfigurationFilesPath);
            Assert.IsNotNull(result.EmulatorPath);
        }
    }
}
