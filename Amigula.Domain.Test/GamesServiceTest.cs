using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Amigula.Domain.Services;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amigula.Domain.Test
{
    /// <summary>
    /// Summary description for AmigulaServiceTest
    /// </summary>
    [TestClass]
    public class GamesServiceTest
    {
        public GamesServiceTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        private IGamesRepository _gamesRepository;
        private GamesService _gamesService;

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
            Assert.IsInstanceOfType(result, typeof(IEnumerable<GamesDto>));
            Assert.AreEqual(result.Count(), 3);
        }
    }
}
