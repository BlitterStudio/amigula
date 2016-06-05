using System;
using Amigula.Domain.Interfaces;
using Amigula.Domain.Services;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amigula.Domain.Test.Services
{
    [TestClass]
    public class MetadataServiceTest
    {
        private IMetadataRepository _metadataRepository;
        private MetadataService _metadataService;

        [TestInitialize]
        public void Initialiaze()
        {
            _metadataRepository = A.Fake<IMetadataRepository>();
            _metadataService = new MetadataService(_metadataRepository);
        }

        [TestMethod]
        public void GetGenre_GameTitle_ReturnsString()
        {
            const string gameTitle = "Apidya";
            const string genre = "Shoot em up";
            A.CallTo(() => _metadataRepository.GetGenre(gameTitle))
                .Returns(genre);

            var result = _metadataService.GetGenre(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual(genre, result);
        }

        [TestMethod]
        public void GetPublisher_GameTitle_ReturnsString()
        {
            const string gameTitle = "Apidya";
            const string publisher = "Taito";
            A.CallTo(() => _metadataRepository.GetPublisher(gameTitle))
                .Returns(publisher);

            var result = _metadataService.GetPublisher(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual(publisher, result);
        }

        [TestMethod]
        public void GetYear_GameTitle_ReturnsString()
        {
            const string gameTitle = "Apidya";
            const string year = "1990";
            A.CallTo(() => _metadataRepository.GetYear(gameTitle))
                .Returns(year);

            var result = _metadataService.GetYear(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual(year, result);
        }

        [TestMethod]
        public void GetYearFromFilename_GameFilenameWithDate_ReturnsString()
        {
            const string gameFilename = "gameTitle (1988) (Psygnosis).zip";
            const int expectedYear = 1988;

            var result = MetadataService.GetYearFromFilename(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(int));
            Assert.AreEqual(expectedYear, result);
        }

        [TestMethod]
        public void GetYearFromFilename_GameFilenameWithNoDate_ReturnsString()
        {
            const string gameFilename = "gameTitle (Psygnosis).zip";
            const int expectedYear = 1900;

            var result = MetadataService.GetYearFromFilename(gameFilename);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(int));
            Assert.AreEqual(expectedYear, result);
        }
    }
}
