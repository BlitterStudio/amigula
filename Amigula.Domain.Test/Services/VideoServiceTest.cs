using System.Collections.Generic;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;
using Amigula.Domain.Services;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amigula.Domain.Test.Services
{
    [TestClass]
    public class VideoServiceTest
    {
        private VideoService _videoService;
        private IVideoRepository _videoRepository;

        [TestInitialize]
        public void Initialize()
        {
            _videoRepository = A.Fake<IVideoRepository>();
            _videoService = new VideoService(_videoRepository);
        }

        [TestMethod]
        public void UrlExists_ValidHttpUrl_ReturnsTrue()
        {
            const string urlToTest = "http://www.youtube.com";

            var result = _videoService.UrlExists(urlToTest);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UrlExists_ValidHttpsUrl_ReturnsTrue()
        {
            const string urlToTest = "https://www.youtube.com";

            var result = _videoService.UrlExists(urlToTest);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UrlExists_ValidUrlWithoutHttp_ReturnsTrue()
        {
            const string urlToTest = "www.youtube.com";

            var result = _videoService.UrlExists(urlToTest);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UrlExists_Null_ReturnsFalse()
        {
            var result = _videoService.UrlExists(null);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void UrlExists_InvalidUrl_ReturnsFalse()
        {
            const string urlToTest = "not a url";

            var result = _videoService.UrlExists(urlToTest);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetVideos_GameTitle_ReturnsVideosDto()
        {
            const string gameTitle = "Apidya";
            A.CallTo(() => _videoRepository.GetVideos(A<string>.Ignored))
                .Returns(new List<VideoDto>());

            var result = _videoService.GetVideos(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IEnumerable<VideoDto>));
        }

        [TestMethod]
        public void GetEmbeddedVideo_EmptyListVideosDto_ReturnsNull()
        {
            var videos = new List<VideoDto>();

            var result = _videoService.GetEmbeddedVideo(videos);
            
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetEmbeddedVideo_PopulatedListVideosDto_ReturnsFirstVideo()
        {
            var videos = new List<VideoDto>
            {
                new VideoDto
                {
                    LinkUrl = "http://www.url.com",
                    EmbedUrl = "http://www.url.com/embed"
                },
                new VideoDto
                {
                    LinkUrl = "http://www.google.com",
                    EmbedUrl = "http://www.google.com/embed"
                },
                new VideoDto
                {
                    LinkUrl = "http://www.twitter.com",
                    EmbedUrl = "http://www.twitter.com/embed"
                }
            };

            var result = _videoService.GetEmbeddedVideo(videos);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual("http://www.url.com/embed", result);
        }
    }
}
