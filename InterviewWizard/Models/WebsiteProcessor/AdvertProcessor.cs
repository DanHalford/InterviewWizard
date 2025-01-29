using AngleSharp.Html.Parser;
using System.Text;
using System.Text.RegularExpressions;

namespace InterviewWizard.Models.WebsiteProcessor
{
    public class AdvertProcessor
    {
        private Dictionary<string, Func<string, Task<AdvertProcessResult>>> _domainFunctionMap;

        public AdvertProcessor()
        {
            _domainFunctionMap = new Dictionary<string, Func<string, Task<AdvertProcessResult>>>()
            {
                { @"www\.seek\.(com\.au|co\.nz)", ProcessSeekAdvert }
                //{ @"(www|au|ca|hk|ie|nz|sg|za|uk)\.indeed\.com", ProcessIndeedAdvert }
            };
        }

        public async Task<AdvertProcessResult> ProcessUrl(string url)
        {
            var uri = new Uri(url);
            string domain = uri.Host;
            foreach (var entry in _domainFunctionMap)
            {
                if (Regex.IsMatch(domain, entry.Key, RegexOptions.IgnoreCase))
                {
                    return await entry.Value(url);
                }
            }
            return new AdvertProcessResult() { Success = false, Content = null };
        }

        private async Task<string> LoadContent(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                //Removing as Azure Services does not support Playwright and I can't get docker working.
                //
                //var pw = await Playwright.CreateAsync();
                //var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
                //var context = await browser.NewContextAsync(new BrowserNewContextOptions { UserAgent = "InterviewWizard" });
                //var page = await context.NewPageAsync();
                //await page.GotoAsync(url);
                //await page.WaitForURLAsync(url, new PageWaitForURLOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 10000 });
                //var content = await page.ContentAsync();
                //await page.CloseAsync();
                //await browser.CloseAsync();
                //return content;
                return await client.GetStringAsync(url);
            }
        }

        private async Task<AdvertProcessResult> ProcessSeekAdvert(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                var content = await LoadContent(url);
                var parser = new HtmlParser();
                var document = await parser.ParseDocumentAsync(content);
                var body = document.Body;
                var sb = new StringBuilder();
                var detailsBlock = body.QuerySelectorAll("*[data-automation='jobAdDetails']");
                var jobTitleBlock = body.QuerySelectorAll("*[data-automation='job-detail-title']");
                var advertiserBlock = body.QuerySelectorAll("*[data-automation='advertiser-name']");
                foreach (var paragraph in detailsBlock)
                {
                    sb.AppendLine(paragraph.TextContent);
                }
                JobAdvert ja = new JobAdvert()
                {
                    SourceUrl = new Uri(url),
                    JobTitle = jobTitleBlock[0].TextContent,
                    Advertiser = advertiserBlock[0].TextContent,
                    Details = sb.ToString()
                };
                return new AdvertProcessResult { Success = true, Content = ja };
            }
        }

        //private async Task<AdvertProcessResult> ProcessIndeedAdvert(string url)
        //{
        //    throw new NotImplementedException();
        //    using (HttpClient client = new HttpClient())
        //    {
        //        var content = await LoadContent(url);
        //        var parser = new HtmlParser();
        //        var document = await parser.ParseDocumentAsync(content);
        //        var body = document.Body;
        //        var sb = new StringBuilder();
        //        var detailsBlock = body.QuerySelectorAll("#jobDescriptionText");
        //        var jobTitleBlock = body.QuerySelectorAll("*[data-testid='jobsearch-JobInfoHeader-title']");
        //        var advertiserBlock = body.QuerySelectorAll("*[data-testid='inlineHeader-companyName']");
        //        foreach (var paragraph in detailsBlock)
        //        {
        //            sb.AppendLine(paragraph.TextContent);
        //        }
        //        JobAdvert ja = new JobAdvert()
        //        {
        //            SourceUrl = new Uri(url),
        //            JobTitle = jobTitleBlock[0].TextContent,
        //            Advertiser = advertiserBlock[0].TextContent,
        //            Details = sb.ToString()
        //        };
        //        return new AdvertProcessResult { Success = true, Content = ja };
        //    }
        //}

    }
}
