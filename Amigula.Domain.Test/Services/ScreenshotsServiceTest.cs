using Amigula.Domain.Classes;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;
using Amigula.Domain.Services;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amigula.Domain.Test.Services
{
    [TestClass]
    public class ScreenshotsServiceTest
    {
        private IScreenshotsRepository _screenshotsRepository;
        private ScreenshotsService _screenshotsService;

        [TestInitialize]
        public void Initialize()
        {
            _screenshotsRepository = A.Fake<IScreenshotsRepository>();

            _screenshotsService = new ScreenshotsService(_screenshotsRepository);
        }

        [TestMethod]
        public void PrepareTitleScreenshot_GameTitle_ReturnsGameScreenshotsDto()
        {
            const string gameTitle = "Apidya v1.2 (1990) [Publisher name]";
            A.CallTo(() => _screenshotsRepository.GetTitleSubfolder(A<string>.Ignored)).Returns("A\\");

            var result = _screenshotsService.PrepareTitleScreenshot(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ScreenshotsDto));
            Assert.AreEqual("Apidya", result.Title);
            Assert.AreEqual("Apidya.png", result.Screenshot1);
            Assert.AreEqual("Apidya_1.png", result.Screenshot2);
            Assert.AreEqual("Apidya_2.png", result.Screenshot3);
            Assert.AreEqual("A\\", result.GameFolder);
        }

        [TestMethod]
        public void PrepareTitleScreenshot_GameTitleWithSpaces_ReturnsGameScreenshotsDto()
        {
            const string gameTitle = "International Karate Plus v1.3 (1988) [Publisher name]";
            A.CallTo(() => _screenshotsRepository.GetTitleSubfolder(A<string>.Ignored)).Returns("I\\");

            var result = _screenshotsService.PrepareTitleScreenshot(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ScreenshotsDto));
            Assert.AreEqual(result.Title, "International Karate Plus");
            Assert.AreEqual(result.Screenshot1, "International_Karate_Plus.png");
            Assert.AreEqual(result.Screenshot2, "International_Karate_Plus_1.png");
            Assert.AreEqual(result.Screenshot3, "International_Karate_Plus_2.png");
            Assert.AreEqual(result.GameFolder, "I\\");
        }

        [TestMethod]
        public void PrepareTitleScreenshot_NumericGameTitle_ReturnsGameScreenshotsDto()
        {
            const string gameTitle = "1942 v1.3 (1988) [Publisher name]";
            A.CallTo(() => _screenshotsRepository.GetTitleSubfolder(A<string>.Ignored)).Returns("0\\");

            var result = _screenshotsService.PrepareTitleScreenshot(gameTitle);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ScreenshotsDto));
            Assert.AreEqual(result.Title, "1942");
            Assert.AreEqual(result.Screenshot1, "1942.png");
            Assert.AreEqual(result.Screenshot2, "1942_1.png");
            Assert.AreEqual(result.Screenshot3, "1942_2.png");
            Assert.AreEqual(result.GameFolder, "0\\");
        }

        [TestMethod]
        public void PrepareTitleScreenshot_Null_ReturnsGameScreenshotsDto()
        {
            var result = _screenshotsService.PrepareTitleScreenshot(null);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ScreenshotsDto));
            Assert.IsNull(result.Title);
            Assert.IsNull(result.Screenshot1);
            Assert.IsNull(result.Screenshot2);
            Assert.IsNull(result.Screenshot3);
            Assert.IsNull(result.GameFolder);
        }

        [TestMethod]
        public void AddScreenshot_ScreenshotDoesNotExist_ReturnsSuccess()
        {
            const string screenshot = "Screenshot1.png";
            const string gameTitle = "Apidya";
            A.CallTo(() => _screenshotsRepository.Add(A<string>.Ignored, A<string>.Ignored))
                .Returns(new OperationResult {Success = true});

            var result = _screenshotsService.Add(gameTitle, screenshot);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(OperationResult));
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public void AddScreenshot_ScreenshotExists_ReturnsFailure()
        {
            const string screenshot = "screenshot.png";
            const string gameTitle = "Apidya";
            A.CallTo(() => _screenshotsRepository.ScreenshotFileExists(A<string>.Ignored))
                .Returns(true);

            var result = _screenshotsService.Add(gameTitle, screenshot);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(OperationResult));
            Assert.IsFalse(result.Success);
        }
    }
}
