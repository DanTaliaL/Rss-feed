using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WorkTask2.Models;
using WorkTask2.Models.ViewModel;

namespace WorkTask2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private MyConfig myConfig;

        public HomeController(ILogger<HomeController> logger, IOptions<MyConfig> myConfig)
        {
            this.logger = logger;
            this.myConfig = myConfig.Value;
        }
        public async Task<IActionResult> Index()
        {
            ViewBag.Refresh = myConfig.refresh;

            var blogs = new List<FeedNews>();
            using (var client = new HttpClient())
            {
                Regex regex = new Regex(@"(<[^,]+>)*(&[a-z]+;)*", RegexOptions.Compiled);
                client.BaseAddress = new Uri(myConfig.link);
                var responseMessage = await client.GetAsync(myConfig.link);
                var responseString = await responseMessage.Content.ReadAsStringAsync();

                XDocument document = XDocument.Parse(responseString);
                var feedItems = from item in document.Root.Descendants().First(q => q.Name.LocalName == "channel").Elements().Where(q => q.Name.LocalName == "item")
                                select new FeedNews
                                {
                                    Link = item.Elements().First(q => q.Name.LocalName == "link").Value,
                                    Title = item.Elements().First(q => q.Name.LocalName == "title").Value,
                                    Description = regex.Replace(item.Elements().First(q => q.Name.LocalName == "description").Value,""),
                                    PublicationDate = ParseDate(item.Elements().First(q => q.Name.LocalName == "pubDate").Value)
                                };
                blogs = feedItems.ToList();

                
            }
            return View(blogs);
        }

        private DateTime ParseDate(string date)
        {
            DateTime result;
            if (DateTime.TryParse(date, out result))
            {
                return result;
            }
            else
            {
                return DateTime.MinValue;
            }
        }
    }
}