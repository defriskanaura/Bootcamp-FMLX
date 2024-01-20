using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace WebMVC;

// public class ExampleActionForAPI : Controller
// {
// 	 private readonly IHttpClientFactory httpClientFactory;

// 		public ExampleActionForAPI(IHttpClientFactory httpClientFactory)
// 		{
// 			this.httpClientFactory = httpClientFactory;
// 		}
		
// 	[HttpPost]
// 	public async Task<IActionResult> Add(AddRegionViewModel model)
// 	{
//         HttpClient client = httpClientFactory.CreateClient();

// 		var httpRequestMessage = new HttpRequestMessage()
// 		{
// 			Method = HttpMethod.Post,
// 			RequestUri = new Uri("https://localhost:7081/api/regions"),
// 			Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json")
// 		};

// 		var httpResponseMessage = await client.SendAsync(httpRequestMessage);
// 		httpResponseMessage.EnsureSuccessStatusCode();

// 		var respose = await httpResponseMessage.Content.ReadFromJsonAsync<RegionDto>();

// 		if (respose is not null)
// 		{
// 			return RedirectToAction("Index", "Regions");
// 		}

// 		return View();
// 	}

// }

// public class AddRegionViewModel
// {
// }