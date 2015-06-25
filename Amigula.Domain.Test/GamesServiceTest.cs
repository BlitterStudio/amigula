﻿using System;
using System.Collections.Generic;
using System.Linq;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;
using Amigula.Domain.Services;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amigula.Domain.Test
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
        public void PrepareGameTitleForScreenshot_GameTitle_ReturnsGameScreenshotsDto()
        {
            const string gameTitle = "Apidya v1.2 (1990) [Publisher name]";

            var result = _gamesService.PrepareGameTitleForScreenshot(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof (GameScreenshotsDto));
            Assert.AreEqual(result.Title, "Apidya");
            Assert.AreEqual(result.Screenshot1, "Apidya.png");
            Assert.AreEqual(result.Screenshot2, "Apidya_1.png");
            Assert.AreEqual(result.Screenshot3, "Apidya_2.png");
            Assert.AreEqual(result.GameFolder, "A\\");
        }

        [TestMethod]
        public void PrepareGameTitleForScreenshot_GameTitleWithSpaces_ReturnsGameScreenshotsDto()
        {
            const string gameTitle = "International Karate Plus v1.3 (1988) [Publisher name]";

            var result = _gamesService.PrepareGameTitleForScreenshot(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof (GameScreenshotsDto));
            Assert.AreEqual(result.Title, "International Karate Plus");
            Assert.AreEqual(result.Screenshot1, "International_Karate_Plus.png");
            Assert.AreEqual(result.Screenshot2, "International_Karate_Plus_1.png");
            Assert.AreEqual(result.Screenshot3, "International_Karate_Plus_2.png");
            Assert.AreEqual(result.GameFolder, "I\\");
        }

        [TestMethod]
        public void PrepareGameTitleForScreenshot_NumericGameTitle_ReturnsGameScreenshotsDto()
        {
            const string gameTitle = "1942 v1.3 (1988) [Publisher name]";

            var result = _gamesService.PrepareGameTitleForScreenshot(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof (GameScreenshotsDto));
            Assert.AreEqual(result.Title, "1942");
            Assert.AreEqual(result.Screenshot1, "1942.png");
            Assert.AreEqual(result.Screenshot2, "1942_1.png");
            Assert.AreEqual(result.Screenshot3, "1942_2.png");
            Assert.AreEqual(result.GameFolder, "0\\");
        }

        [TestMethod]
        public void PrepareGameTitleForScreenshot_Null_ReturnsGameScreenshotsDto()
        {
            var result = _gamesService.PrepareGameTitleForScreenshot(null);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof (GameScreenshotsDto));
            Assert.IsNull(result.Title);
            Assert.IsNull(result.Screenshot1);
            Assert.IsNull(result.Screenshot2);
            Assert.IsNull(result.Screenshot3);
            Assert.IsNull(result.GameFolder);
        }
    }
}