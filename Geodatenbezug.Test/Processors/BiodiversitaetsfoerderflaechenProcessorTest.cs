﻿using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Geodatenbezug.Processors;

[TestClass]
[DeploymentItem("testdata/lwb_biodiversitaetsfoerderflaechen_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
public class BiodiversitaetsfoerderflaechenProcessorTest
{
    private readonly Topic topic = new()
    {
        TopicTitle = BaseTopic.lwb_biodiversitaetsfoerderflaechen.GetDescription(),
        Canton = Canton.NE,
        BaseTopic = BaseTopic.lwb_biodiversitaetsfoerderflaechen,
        UpdatedAt = DateTime.Now,
    };

    private Mock<ILogger<Processor>> loggerMock;
    private Mock<IGeodiensteApi> geodiensteApiMock;
    private Mock<IAzureStorage> azureStorageMock;
    private BiodiversitaetsfoerderflaechenProcessor processor;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processor>>(MockBehavior.Strict);
        geodiensteApiMock = new Mock<IGeodiensteApi>(MockBehavior.Strict);
        azureStorageMock = new Mock<IAzureStorage>(MockBehavior.Strict);
        processor = new BiodiversitaetsfoerderflaechenProcessor(geodiensteApiMock.Object, azureStorageMock.Object, loggerMock.Object, topic);
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
    }

    [TestMethod]
    public async Task RunGdalProcessingAsync()
    {
        await processor.RunGdalProcessingAsync();
    }
}