using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OldPhonePad.Tests;

public class DecodeEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private HttpClient Client => factory.CreateClient();

    [Theory]
    [InlineData("33#", "E")]
    [InlineData("227*#", "B")]
    [InlineData("4433555 555666#", "HELLO")]
    [InlineData("8 88777444666*664#", "TURING")]
    public async Task Decode_ReturnsTheDecodedTextForValidInput(string input, string expected)
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync("/v1/decode", new { input });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.Equal(expected, body.GetProperty("output").GetString());
        Assert.Equal(input, body.GetProperty("input").GetString());
    }

    [Fact]
    public async Task Decode_ReportsThePhysicalKeyPressCount()
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/v1/decode", new { input = "222 2 22#" });

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.Equal(7, body.GetProperty("keyPressCount").GetInt32());
    }

    [Theory]
    [InlineData("22a#")]
    [InlineData("22")]
    [InlineData("")]
    public async Task Decode_Returns400ProblemDetailsForMalformedInput(string input)
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync("/v1/decode", new { input });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.True(body.TryGetProperty("errors", out JsonElement errors));
        Assert.True(errors.TryGetProperty("input", out _));
    }

    [Fact]
    public async Task Decode_Returns400_WhenTheInputExceedsTheConfiguredLimit()
    {
        string oversized = new string('2', 10_001) + "#";

        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/v1/decode", new { input = oversized });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Decode_AcceptsACustomLayout()
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync("/v1/decode", new
        {
            input = "2 22 3#",
            layout = new Dictionary<string, string> { ["2"] = "XY", ["3"] = "Z" },
        });

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.Equal("XYZ", body.GetProperty("output").GetString());
    }

    [Theory]
    [InlineData("22", "AB")]
    [InlineData("#", "AB")]
    public async Task Decode_Returns400ForAnInvalidCustomLayout(string key, string characters)
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync("/v1/decode", new
        {
            input = "2#",
            layout = new Dictionary<string, string> { [key] = characters },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(body.GetProperty("errors").TryGetProperty("layout", out _));
    }

    [Fact]
    public async Task Batch_Returns200WithPerItemOutcomes_EvenWhenSomeItemsFail()
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync("/v1/decode/batch", new
        {
            inputs = new[] { "33#", "not-valid#", "4433555 555666#" },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.Equal(2, body.GetProperty("succeededCount").GetInt32());
        Assert.Equal(1, body.GetProperty("failedCount").GetInt32());

        JsonElement results = body.GetProperty("results");
        Assert.Equal(3, results.GetArrayLength());
        Assert.Equal("E", results[0].GetProperty("output").GetString());
        Assert.False(results[1].GetProperty("succeeded").GetBoolean());
        Assert.Equal("HELLO", results[2].GetProperty("output").GetString());
    }

    [Fact]
    public async Task Batch_PreservesTheOrderOfSubmittedInputs()
    {
        string[] inputs = ["2#", "22#", "222#"];

        HttpResponseMessage response = await Client.PostAsJsonAsync("/v1/decode/batch", new { inputs });
        JsonElement results = (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("results");

        Assert.Equal("A", results[0].GetProperty("output").GetString());
        Assert.Equal("B", results[1].GetProperty("output").GetString());
        Assert.Equal("C", results[2].GetProperty("output").GetString());
    }

    [Fact]
    public async Task Batch_Returns400ForAnEmptyOrOversizedBatch()
    {
        HttpResponseMessage empty = await Client.PostAsJsonAsync(
            "/v1/decode/batch", new { inputs = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        HttpResponseMessage tooMany = await Client.PostAsJsonAsync(
            "/v1/decode/batch", new { inputs = Enumerable.Repeat("2#", 101).ToArray() });

        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
    }

    [Fact]
    public async Task Keypad_DescribesEveryButtonAndTheControlKeys()
    {
        JsonElement body = await Client.GetFromJsonAsync<JsonElement>("/v1/keypad", Json);

        Assert.Equal(10, body.GetProperty("buttons").GetArrayLength());
        Assert.Equal("#", body.GetProperty("sendKey").GetString());
        Assert.Equal("*", body.GetProperty("backspaceKey").GetString());

        JsonElement seven = body.GetProperty("buttons")
            .EnumerateArray()
            .Single(button => button.GetProperty("button").GetString() == "7");

        Assert.Equal("PQRS", seven.GetProperty("characters").GetString());
    }

    [Fact]
    public async Task Health_ReportsHealthy()
    {
        HttpResponseMessage response = await Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerDocumentIsPublished()
    {
        HttpResponseMessage response = await Client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement document = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.Equal("Old Phone Pad API", document.GetProperty("info").GetProperty("title").GetString());
        Assert.True(document.GetProperty("paths").TryGetProperty("/v1/decode", out _));
    }

    [Fact]
    public async Task DemoPageIsServedAtTheRoot()
    {
        HttpResponseMessage response = await Client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}
