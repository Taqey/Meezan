using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Meezan.Infrastructure.Tests.Testing;
using Xunit;

namespace Meezan.Infrastructure.Tests.Testing;

public class FoundationBehaviorsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public FoundationBehaviorsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ValidationBehavior_ShouldTrigger_AndReturn400_WhenFileIsEmpty()
    {
        var client = _factory.CreateClient();

        // Prepare multipart form with empty file stream to trigger UploadIndexFileCommandValidator
        using var content = new MultipartFormDataContent();
        var emptyContent = new ByteArrayContent(Array.Empty<byte>());
        emptyContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(emptyContent, "file", "test.xlsx");

        var response = await client.PostAsync("/api/indices/EGX30/upload", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("File must not be empty.", body);
    }

    [Fact]
    public async Task Endpoint_SnapshotTest_SectorsGetAll_ShouldMatchSnapshot()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/sectors");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SnapshotAssert.MatchesStoredSnapshot("sectors_get_all", (int)response.StatusCode, body);
    }
}
