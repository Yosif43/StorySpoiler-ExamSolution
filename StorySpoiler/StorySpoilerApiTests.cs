using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoiler.Models;


namespace StorySpoiler
{
    [TestFixture]
    public class StoryTests
    {
        private RestClient client;
        private static string createdStoryId;

        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("Yoo", "123456");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });
            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);


            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }
        // Tests

        [Test, Order(1)]
        public void CreateStory_ShouldReturnCreated()
        {
            var story = new StoryDTO
            {
                Title = "New Story",
                Description = "This is a new story spoiler description",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            createdStoryId = json.GetProperty("storyId").GetString() ?? string.Empty;

            Assert.That(createdStoryId, Is.Not.Null.And.Not.Empty);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Successfully created!"));
        }

        [Test, Order(2)]
        public void EditStory_ShouldReturnOk()
        {
            var updated = new StoryDTO
            {
                Title = "Updated Story Title",
                Description = "Updated story    spoiler description",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            request.AddJsonBody(updated);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content!);
            Assert.That(json?.Msg, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStory_ShouldReturnOk()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(json, Is.Not.Null);
        }

        [Test, Order(4)]
        public void DeleteStory_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(json.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateStory_MissingRequiredFields_ShouldReturnBadRequest()
        {
            var incompleteFood = new { Name = "", Description = "" };
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(incompleteFood);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            var fakeId = "123";

            var updated = new StoryDTO
            {
                Title = "Does not matter",
                Description = "Does not matter",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{fakeId}", Method.Put);
            request.AddJsonBody(updated);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), response.Content);

            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content!);
            Assert.That(json?.Msg, Is.EqualTo("No spoilers..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingStory_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/api/Story/Delete/this-id-does-not-exist", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content!);
            Assert.That(json?.Msg, Is.EqualTo("Unable to delete this story spoiler!"));
        }



        [OneTimeTearDown]
        public void CleanUp()
        {
            client?.Dispose();
        }
    }
}
