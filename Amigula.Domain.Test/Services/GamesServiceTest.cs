using System;
using System.Collections.Generic;
using System.Linq;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;
using Amigula.Domain.Services;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amigula.Domain.Test.Services
{
    /// <summary>
    ///     Tests for the GamesService class
    /// </summary>
    [TestClass]
    public class GamesServiceTest
    {
        private readonly List<GamesDto> _gamesDtos = new List<GamesDto>
        {
            new GamesDto
            {
                Id = 1,
                Title = "A test game Title",
                Disks = 1,
                Favorite = false,
                Genre = "Arcade",
                LastPlayed = null,
                PathToFile = @"D:\Games\Amigula\Test title.adf",
                Publisher = "Psygnosis",
                TimesPlayed = 0,
                UaeConfig = "Default",
                Year = 1985
            },
            new GamesDto
            {
                Id = 2,
                Title = "Another game Title",
                Disks = 2,
                Favorite = false,
                Genre = "Adventure",
                LastPlayed = DateTime.Today.AddDays(-1),
                PathToFile = @"D:\Games\Amigula\Another game title.adf",
                Publisher = "Microprose",
                TimesPlayed = 1,
                UaeConfig = "A500",
                Year = 1989
            },
            new GamesDto
            {
                Id = 3,
                Title = "My favorite game",
                Disks = 3,
                Favorite = true,
                Genre = "RPG",
                LastPlayed = DateTime.Today,
                PathToFile = @"D:\Games\Amigula\My Favorite game.adf",
                Publisher = "Team 17",
                TimesPlayed = 99,
                UaeConfig = "A1200",
                Year = 1994
            }
        };

        private IGamesRepository _gamesRepository;
        private GamesService _gamesService;

        [TestInitialize]
        public void Initialize()
        {
            _gamesRepository = A.Fake<IGamesRepository>();
            _gamesService = new GamesService(_gamesRepository);
        }

        [TestMethod]
        public void GetGamesList_ReturnsGamesDtos()
        {
            A.CallTo(() => _gamesRepository.GetGamesList())
                .Returns(_gamesDtos);

            var result = _gamesService.GetGamesList();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof (IEnumerable<GamesDto>));
            Assert.AreEqual(result.Count(), 3);
        }

        [TestMethod]
        public void GetGameDisks_GameWithOneDisk_ReturnsFullPathString()
        {
            A.CallTo(() => _gamesRepository.IsGameExists(A<string>.Ignored))
                .Returns(true)
                .Once();
            const string gameFilename = "International Karate Plus.adf";

            var result = _gamesService.GetGameDisks(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<string>));
            Assert.AreEqual(result.Count(), 1);
        }

        [TestMethod]
        public void GetGameDisks_GameWithFourDisksMethod1_ReturnsFullPathStringForAllDisks()
        {
            A.CallTo(() => _gamesRepository.IsGameExists(A<string>.Ignored))
                .Returns(true)
                .NumberOfTimes(3);

            const string gameFilename = "Mortal Kombat Disk1.zip";
            const string lastGameDisk = "Mortal Kombat Disk4.zip";

            var result = _gamesService.GetGameDisks(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<string>));
            var enumerable = result as IList<string> ?? result.ToList();
            Assert.AreEqual(enumerable.Count(), 4);
            Assert.IsTrue(enumerable.Last().Equals(lastGameDisk));
        }

        [TestMethod]
        public void GetGameDisks_GameWithFourDisksMethod2_ReturnsFullPathStringForAllDisks()
        {
            A.CallTo(() => _gamesRepository.IsGameExists(A<string>.Ignored))
                .Returns(true)
                .NumberOfTimes(3);

            const string gameFilename = "Mortal Kombat Disk01.zip";
            const string lastGameDisk = "Mortal Kombat Disk04.zip";

            var result = _gamesService.GetGameDisks(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<string>));
            var enumerable = result as IList<string> ?? result.ToList();
            Assert.AreEqual(enumerable.Count(), 4);
            Assert.IsTrue(enumerable.Last().Equals(lastGameDisk));
        }

        [TestMethod]
        public void GetGameDisks_GameWithFourDisksMethod3_ReturnsFullPathStringForAllDisks()
        {
            A.CallTo(() => _gamesRepository.IsGameExists(A<string>.Ignored))
                .Returns(true)
                .NumberOfTimes(3);
            const string gameFilename = "Mortal Kombat (Disk 1 of 4).zip";
            const string lastGameDisk = "Mortal Kombat (Disk 4 of 4).zip";

            var result = _gamesService.GetGameDisks(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<string>));
            var enumerable = result as IList<string> ?? result.ToList();
            Assert.AreEqual(enumerable.Count(), 4);
            Assert.IsTrue(enumerable.Last().Equals(lastGameDisk));
        }

        [TestMethod]
        public void GetGameDisks_GameWithFourDisksMethod4_ReturnsFullPathStringForAllDisks()
        {
            A.CallTo(() => _gamesRepository.IsGameExists(A<string>.Ignored))
                .Returns(true)
                .NumberOfTimes(3);
            const string gameFilename = "Mortal Kombat (Disk 01 of 04).zip";
            const string lastGameDisk = "Mortal Kombat (Disk 04 of 04).zip";

            var result = _gamesService.GetGameDisks(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<string>));
            var enumerable = result as IList<string> ?? result.ToList();
            Assert.AreEqual(enumerable.Count(), 4);
            Assert.IsTrue(enumerable.Last().Equals(lastGameDisk));
        }

        [TestMethod]
        public void GetGameDisks_GameWithFoudDisksMethod5_ReturnsFullPathStringForAllDisks()
        {
            A.CallTo(() => _gamesRepository.IsGameExists(A<string>.Ignored))
                .Returns(true)
                .NumberOfTimes(3);
            const string gameFilename = "Mortal Kombat-1.zip";
            const string lastGameDisk = "Mortal Kombat-4.zip";

            var result = _gamesService.GetGameDisks(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<string>));
            var enumerable = result as IList<string> ?? result.ToList();
            Assert.AreEqual(enumerable.Count(), 4);
            Assert.IsTrue(enumerable.Last().Equals(lastGameDisk));
        }

        [TestMethod]
        public void GetGameDisks_GameWithElevenDisksMethod1_ReturnsFullPathStringForAllDisks()
        {
            A.CallTo(() => _gamesRepository.IsGameExists(A<string>.Ignored))
                .Returns(true)
                .NumberOfTimes(10);
            const string gameFilename = "Mortal Kombat Disk1.zip";
            const string lastGameDisk = "Mortal Kombat Disk11.zip";

            var result = _gamesService.GetGameDisks(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<string>));
            var enumerable = result as IList<string> ?? result.ToList();
            Assert.AreEqual(enumerable.Count(), 11);
            Assert.IsTrue(enumerable.Last().Equals(lastGameDisk));
        }

        [TestMethod]
        public void GetGameDisks_GameWithElevenDisksMethod2_ReturnsFullPathStringForAllDisks()
        {
            A.CallTo(() => _gamesRepository.IsGameExists(A<string>.Ignored))
                .Returns(true)
                .NumberOfTimes(10);
            const string gameFilename = "Mortal Kombat Disk01.zip";
            const string lastGameDisk = "Mortal Kombat Disk11.zip";

            var result = _gamesService.GetGameDisks(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<string>));
            var enumerable = result as IList<string> ?? result.ToList();
            Assert.AreEqual(enumerable.Count(), 11);
            Assert.IsTrue(enumerable.Last().Equals(lastGameDisk));
        }

        [TestMethod]
        public void GetGameDisks_GameWithElevenDisksMethod3_ReturnsFullPathStringForAllDisks()
        {
            A.CallTo(() => _gamesRepository.IsGameExists(A<string>.Ignored))
                .Returns(true)
                .NumberOfTimes(10);
            const string gameFilename = "Mortal Kombat (Disk 1 of 11).zip";
            const string lastGameDisk = "Mortal Kombat (Disk 11 of 11).zip";

            var result = _gamesService.GetGameDisks(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<string>));
            var enumerable = result as IList<string> ?? result.ToList();
            Assert.AreEqual(enumerable.Count(), 11);
            Assert.IsTrue(enumerable.Last().Equals(lastGameDisk));
        }

        [TestMethod]
        public void GetGameDisks_GameWithElevenDisksMethod4_ReturnsFullPathStringForAllDisks()
        {
            A.CallTo(() => _gamesRepository.IsGameExists(A<string>.Ignored))
                .Returns(true)
                .NumberOfTimes(10);
            const string gameFilename = "Mortal Kombat (Disk 01 of 11).zip";
            const string lastGameDisk = "Mortal Kombat (Disk 11 of 11).zip";

            var result = _gamesService.GetGameDisks(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<string>));
            var enumerable = result as IList<string> ?? result.ToList();
            Assert.AreEqual(enumerable.Count(), 11);
            Assert.IsTrue(enumerable.Last().Equals(lastGameDisk));
        }

        [TestMethod]
        public void GetGameDisks_GameWithElevenDisksMethod5_ReturnsFullPathStringforAllDisks()
        {
            A.CallTo(() => _gamesRepository.IsGameExists(A<string>.Ignored))
                .Returns(true)
                .NumberOfTimes(10);
            const string gameFilename = "Mortal Kombat-1.zip";
            const string lastGameDisk = "Mortal Kombat-11.zip";

            var result = _gamesService.GetGameDisks(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<string>));
            var enumerable = result as IList<string> ?? result.ToList();
            Assert.AreEqual(enumerable.Count(), 11);
            Assert.IsTrue(enumerable.Last().Equals(lastGameDisk));
        }
    }
}