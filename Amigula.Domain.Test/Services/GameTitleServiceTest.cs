using Amigula.Domain.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amigula.Domain.Test.Services
{
    [TestClass]
    public class GameTitleServiceTest
    {
        private GameTitleService _gameTitleService;

        [TestInitialize]
        public void Initialize()
        {
            _gameTitleService = new GameTitleService();
        }

        [TestMethod]
        public void PrepareTitleUrl_Title_ReturnsString()
        {
            const string gameTitle = "Apidya v1.2 (1990) [Publisher name]";

            var result = _gameTitleService.PrepareTitleUrl(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual(result, "Apidya");
        }

        [TestMethod]
        public void PrepareTitleUrl_TitleWithSpaces_ReturnsStringWithoutSpaces()
        {
            const string gameTitle = "International Karate Plus v1.3 (1988) [Publisher name]";

            var result = _gameTitleService.PrepareTitleUrl(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual(result, "International%20Karate%20Plus");
        }

        [TestMethod]
        public void PrepareTitleUrl_Null_ReturnsEmptyString()
        {
            var result = _gameTitleService.PrepareTitleUrl(null);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual(result, "");
        }
    }
}
