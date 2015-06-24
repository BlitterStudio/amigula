using System;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;
using Amigula.Domain.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FakeItEasy;

namespace Amigula.Domain.Test
{
    [TestClass]
    public class EmulatorServiceTest
    {
        private EmulatorService _emulatorService;
        private IEmulatorRepository _emulatorRepository;

        [TestInitialize]
        public void Initialize()
        {
            _emulatorRepository = A.Fake<IEmulatorRepository>();

            _emulatorService = new EmulatorService(_emulatorRepository);
        }

        [TestMethod]
        public void GetEmulatorPaths_ReturnsEmulatorDto()
        {
            A.CallTo(() => _emulatorRepository.GetEmulatorPaths())
                .Returns(new EmulatorDto());

            var result = _emulatorService.GetEmulatorPaths();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(EmulatorDto));
        }
    }
}
