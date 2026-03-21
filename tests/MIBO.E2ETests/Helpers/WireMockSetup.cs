using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MIBO.E2ETests.Helpers;

public static class WireMockSetup
{
    public static void StubAgentChatSse(WireMockServer server, string responseText = "Hello from agent")
    {
        var sseBody =
            $"data: {{\"type\":\"token\",\"content\":\"{responseText}\"}}\n\n" +
            $"data: {{\"type\":\"done\",\"content\":\"{responseText}\"}}\n\n";

        server
            .Given(Request.Create().WithPath("/chat").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(sseBody));
    }

    public static void StubAgentChatError(WireMockServer server, int statusCode = 500)
    {
        server
            .Given(Request.Create().WithPath("/chat").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithBody("{\"error\":\"Internal server error\"}"));
    }

    public static void StubDummyJsonProducts(WireMockServer server)
    {
        server
            .Given(Request.Create().WithPath("/products").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "products": [
                        {"id": 1, "title": "Test Product", "price": 9.99, "category": "test"},
                        {"id": 2, "title": "Another Product", "price": 19.99, "category": "test"}
                    ],
                    "total": 2,
                    "skip": 0,
                    "limit": 30
                }
                """));
    }

    public static void StubDummyJsonProductById(WireMockServer server, int id = 1)
    {
        server
            .Given(Request.Create().WithPath($"/products/{id}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""
                {
                    "id": {{id}},
                    "title": "Test Product",
                    "description": "A test product",
                    "price": 9.99,
                    "category": "test",
                    "brand": "TestBrand"
                }
                """));
    }

    public static void StubDummyJsonCategories(WireMockServer server)
    {
        server
            .Given(Request.Create().WithPath("/products/categories").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                [
                    {"slug": "smartphones", "name": "Smartphones", "url": "https://example.com/smartphones"},
                    {"slug": "laptops", "name": "Laptops", "url": "https://example.com/laptops"}
                ]
                """));
    }

    public static void StubDummyJsonProductSearch(WireMockServer server)
    {
        server
            .Given(Request.Create().WithPath("/products/search").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "products": [
                        {"id": 1, "title": "Search Result", "price": 9.99, "category": "test"}
                    ],
                    "total": 1,
                    "skip": 0,
                    "limit": 30
                }
                """));
    }
}
