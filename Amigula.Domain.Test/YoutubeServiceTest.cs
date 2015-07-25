using Amigula.Domain.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amigula.Domain.Test
{
    [TestClass]
    public class YoutubeServiceTest
    {
        private VideoService _youtubeService;

        [TestInitialize]
        public void Initialize()
        {
            _youtubeService = new VideoService();
        }

        [TestMethod]
        public void UrlExists_ValidHttpUrl_ReturnsTrue()
        {
            const string urlToTest = "http://www.youtube.com";

            var result = _youtubeService.UrlExists(urlToTest);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UrlExists_ValidHttpsUrl_ReturnsTrue()
        {
            const string urlToTest = "https://www.youtube.com";

            var result = _youtubeService.UrlExists(urlToTest);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UrlExists_ValidUrlWithoutHttp_ReturnsTrue()
        {
            const string urlToTest = "www.youtube.com";

            var result = _youtubeService.UrlExists(urlToTest);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UrlExists_Null_ReturnsFalse()
        {
            var result = _youtubeService.UrlExists(null);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void UrlExists_InvalidUrl_ReturnsFalse()
        {
            const string urlToTest = "not a url";

            var result = _youtubeService.UrlExists(urlToTest);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsFalse(result);
        }
    }
}
