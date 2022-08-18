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

        public void ChangeRefresh(int timer)
        {
            XDocument xDocument = XDocument.Load("Configuration.xml");
            var newTime = xDocument.Descendants("refresh").First();
            newTime.ReplaceWith(new XElement("refresh", timer));
            myConfig.refresh = timer.ToString();
            xDocument.Save("Configuration.xml");
        }

        public void AddRssLink(string link)
        {
            int countLink = XDocument.Load("Configuration.xml").Root.Element("rss").Element("links").Elements().Count();
            
                XDocument xDocument = XDocument.Load("Configuration.xml");
                var newLink = xDocument.Descendants("links").First();
                newLink.Add(new XElement($"link{countLink}", link));
                xDocument.Save("Configuration.xml");
      

        }

        public async Task<IActionResult> Index(int timer, string? link)
        {
            if (link != null)
            {
                AddRssLink(link);
            }

            if (timer == 0)
            {
                ViewBag.Refresh = myConfig.refresh;
            }
            else
            {
                ChangeRefresh(timer);
                myConfig.refresh = timer.ToString();
                ViewBag.Refresh = myConfig.refresh;
            }


            var blogs = new List<FeedNews>();
             
            for (int i = 0; i < XDocument.Load("Configuration.xml").Root.Element("rss").Element("links").Elements().Count(); i++)
            {
                using (var client = new HttpClient())
                {

                    Regex regex = new Regex(@"(<[\a-zA-Z]+>)+(&[a-z]+;)*(Читать дальше? &rarr;?)?(Читать далее?)?", RegexOptions.Compiled);
                    Regex regexTag = new Regex(@"(<+(\w+\W+)+>)+", RegexOptions.Compiled);
                    Regex regexSpace = new Regex(@"&+(nbsp;)", RegexOptions.Compiled);
                    var loadLink = XDocument.Load("Configuration.xml").Root.Element("rss").Element("links").Element($"link{i}").Value;
                    client.BaseAddress = new Uri(loadLink);
                    var responseMessage = await client.GetAsync(loadLink);
                    var responseString = await responseMessage.Content.ReadAsStringAsync();

                    XDocument document = XDocument.Parse(responseString);
                    var titleName = document.Descendants("title").First().Value;

                    var feedItems = from item in document.Root.Descendants().First(q => q.Name.LocalName == "channel").Elements().Where(q => q.Name.LocalName == "item")
                                    select new FeedNews
                                    {
                                        Link = item.Elements().First(q => q.Name.LocalName == "link").Value,
                                        Title = item.Elements().First(q => q.Name.LocalName == "title").Value,
                                        Description = regexSpace.Replace( regexTag.Replace(regex.Replace(item.Elements().First(q => q.Name.LocalName == "description").Value, ""), ""), " "),
                                        PublicationDate = ParseDate(item.Elements().First(q => q.Name.LocalName == "pubDate").Value),
                                        ChanelName = titleName.ToString()
                                    };
                    blogs.AddRange(feedItems.ToList());


                }
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