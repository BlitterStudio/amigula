using Amigula.Domain.Classes;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;
using Amigula.Domain.Services;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amigula.Domain.Test.Services
{
    [TestClass]
    public class MusicPlayerServiceTest
    {
        private IMusicPlayerRepository _musicPlayerRepository;
        private MusicPlayerService _musicPlayerService;

        [TestInitialize]
        public void Initialize()
        {
            _musicPlayerRepository = A.Fake<IMusicPlayerRepository>();
            _musicPlayerService = new MusicPlayerService(_musicPlayerRepository);
        }

        [TestMethod]
        public void GetPlayerPath_ReturnsMusicPlayerDto()
        {
            var musicPlayer = new MusicPlayerDto {PlayerPath = @"C:\Deliplayer\deli.exe"};
            A.CallTo(() => _musicPlayerRepository.GetPlayerPath())
                .Returns(musicPlayer);

            var result = _musicPlayerService.GetPlayerPath();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof (MusicPlayerDto));
            Assert.AreEqual(musicPlayer.PlayerPath, result.PlayerPath);
        }

        [TestMethod]
        public void PlayGameMusic_GameTitle_ReturnsOperationResult()
        {
            const string gameTitle = "Apidya";
            A.CallTo(() => _musicPlayerRepository.PlayGameMusic(gameTitle))
                .Returns(new OperationResult {Success = true});

            var result = _musicPlayerService.PlayGameMusic(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof (OperationResult));
            Assert.IsTrue(result.Success);
        }
    }
}