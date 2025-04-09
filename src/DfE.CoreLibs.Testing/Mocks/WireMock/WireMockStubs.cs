using Newtonsoft.Json;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Util;

namespace DfE.CoreLibs.Testing.Mocks.WireMock
{
    public static class WireMockStubs
    {
        public static void AddGetWithJsonResponse<TResponseBody>(this WireMockServer server, string path, TResponseBody responseBody, List<KeyValuePair<string,string>>? parameters)
        {
            var request = Request.Create()
                .WithPath(path)
                .UsingGet();

            foreach (var kvp in parameters ?? [])
            {
                request = request.WithParam(kvp.Key, kvp.Value);
            }

            server
               .Given(request)
               .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody(JsonConvert.SerializeObject(responseBody)));
        }

        public static void AddPatchWithJsonRequest<TRequestBody, TResponseBody>(this WireMockServer server, string path, TRequestBody requestBody, TResponseBody responseBody)
        {
            server
               .Given(Request.Create()
                  .WithPath(path)
                  .WithBody(new JsonMatcher(JsonConvert.SerializeObject(requestBody), true))
                  .UsingPatch())
               .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody(JsonConvert.SerializeObject(responseBody)));
        }

        public static void AddApiCallWithBodyDelegate<TResponseBody>(this WireMockServer server, string path, Func<IBodyData, bool> bodyDelegate, TResponseBody responseBody, HttpMethod? verb = null)
        {
            server
               .Given(Request.Create()
                  .WithPath(path)
                  .WithBody(bodyDelegate!)
                  .UsingMethod(verb == null ? HttpMethod.Post.ToString() : verb.ToString()))
               .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody(JsonConvert.SerializeObject(responseBody)));
        }

        public static void AddPutWithJsonRequest<TRequestBody, TResponseBody>(this WireMockServer server, string path, TRequestBody requestBody, TResponseBody responseBody)
        {
            server
               .Given(Request.Create()
                  .WithPath(path)
                  .WithBody(new JsonMatcher(JsonConvert.SerializeObject(requestBody), true))
                  .UsingPut())
               .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody(JsonConvert.SerializeObject(responseBody)));
        }

        public static void AddPostWithJsonRequest<TRequestBody, TResponseBody>(this WireMockServer server, string path, TRequestBody requestBody, TResponseBody responseBody)
        {
            server
               .Given(Request.Create()
                  .WithPath(path)
                  .WithBody(new JsonMatcher(JsonConvert.SerializeObject(requestBody), true))
                  .UsingPost())
               .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody(JsonConvert.SerializeObject(responseBody)));
        }

        public static void AddAnyPostWithJsonRequest<TResponseBody>(this WireMockServer server, string path, TResponseBody responseBody)
        {
            server
               .Given(Request.Create()
                  .WithPath(path)
                  .UsingPost())
               .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody(JsonConvert.SerializeObject(responseBody)));
        }

        public static void AddErrorResponse(this WireMockServer server, string path, string method, List<KeyValuePair<string, string>>? parameters)
        {
            var request = Request.Create()
                .WithPath(path)
                .UsingGet();

            foreach (var kvp in parameters ?? [])
            {
                request = request.WithParam(kvp.Key, kvp.Value);
            }


            server
               .Given(Request.Create()
                  .WithPath(path)
                  .UsingMethod(method))
               .RespondWith(Response.Create()
                  .WithStatusCode(500));
        }

        public static void Reset(this WireMockServer server)
        {
            server.Reset();
        }
    }
}
