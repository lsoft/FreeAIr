using FreeAIr.Helper;
using HtmlAgilityPack;
using PuppeteerSharp;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace FreeAIr.Seaarch
{
    public class GoogleSearcher
    {
        public const string GoogleMainPageUrl = "https://www.google.com";
        private readonly string? _pageImagesFolder;

        public GoogleSearcher(
            string? pageImagesFolder
            )
        {
            _pageImagesFolder = pageImagesFolder;
        }

        public async Task<List<SearchResult>> SearchAsync(
            string searchTerm,
            int itemCountNeeded
            )
        {
            var result = new List<SearchResult>();

            await new BrowserFetcher().DownloadAsync();
            await using var browser = await Puppeteer.LaunchAsync(
                new LaunchOptions
                {
                    Args =
                    [
                        "--disable-blink-features=AutomationControlled", // Скрывает признак автоматизации
                        "--no-sandbox",
                        "--disable-setuid-sandbox"
                    ]
                }
                );

            var page = await browser.NewPageAsync();

            await page.EvaluateExpressionOnNewDocumentAsync(
                @"delete navigator.__proto__.constructor.prototype._id"
                );

            await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            await page.SetViewportAsync(
                new ViewPortOptions
                {
                    Width = 2560,
                    Height = 2800
                });

            await page.GoToAsync(GoogleMainPageUrl);

            await Task.Delay(500); //я тучка тучка тучка, а вовсе не медведь!

            //начинаем вводить поисковый запрос
            foreach (var ch in searchTerm)
            {
                await page.Keyboard.TypeAsync(ch.ToString());
                await Task.Delay(100); //я тучка тучка тучка, а вовсе не медведь!
            }

            //ищем кнопку начать поиск
            var doSearchButtons = await page.XPathAsync("//input [@class='gNO89b']");
            if (doSearchButtons is null || doSearchButtons.Length == 0)
            {
                return result;
            }
            var doSearchButton = doSearchButtons[1];

            //фокусируемся на кнопке начать поиск
            await doSearchButton.FocusAsync();

            await Task.Delay(500); //я тучка тучка тучка, а вовсе не медведь!

            await SavePageImageAsync(searchTerm, page, null);

            //нажимаем кнопку начать поиск
            await page.Keyboard.PressAsync("Enter");

            var rootUrl = page.Url;

            var itemCount = 0;
            for (var pageIndex = 0; pageIndex < (itemCountNeeded / 5); pageIndex++)
            {
                await page.WaitForNavigationAsync(
                    new NavigationOptions
                    {
                        WaitUntil = new[]
                        {
                            WaitUntilNavigation.DOMContentLoaded
                        },
                        Timeout = 5_000
                    }
                    );
                //ждём, пока все сетевые запросы не завершатся
                //await page.WaitForNetworkIdleAsync();

                await SavePageImageAsync(searchTerm, page, pageIndex);

                var html = await page.GetContentAsync();

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var titleNodes = htmlDoc.DocumentNode.SelectNodes(".//h3[@class='LC20lb MBeuO DKV0Md']");
                if (titleNodes == null)
                {
                    return result;
                }

                foreach (var titleNode in titleNodes)
                {
                    var title = WebUtility.HtmlDecode(
                        titleNode.InnerText.RemoveAcuteAccent()
                        );

                    var linkNode = titleNode.ParentNode?.ParentNode?.SelectSingleNode(".//a[@jsname='UWckNb']");
                    if (linkNode is null)
                    {
                        return result;
                    }
                    var link = WebUtility.HtmlDecode(
                        linkNode.Attributes["href"].Value
                        );

                    var detail = string.Empty;
                    var fullNode = titleNode?.ParentNode?.ParentNode?.ParentNode?.ParentNode?.ParentNode?.ParentNode;
                    var detailNode = fullNode?.SelectSingleNode(".//div[contains(@style, '-webkit-line-clamp:')]");
                    if (detailNode is not null)
                    {
                        detail = WebUtility.HtmlDecode(
                            detailNode.InnerText.RemoveAcuteAccent()
                            );
                    }


                    result.Add(new SearchResult(title, detail, link));

                    itemCount++;
                    if (itemCount >= itemCountNeeded)
                    {
                        return result;
                    }
                }
                //var anchorNodes = htmlDoc.DocumentNode.SelectNodes("//div [@class='kb0PBd A9Y9g']");
                //if (anchorNodes == null)
                //{
                //    return result;
                //}

                //foreach (var anchorNode in anchorNodes)
                //{
                //    var titleNode = anchorNode.ParentNode?.SelectSingleNode(".//h3[@class='LC20lb MBeuO DKV0Md']");
                //    if (titleNode is null)
                //    {
                //        return result;
                //    }
                //    var title = WebUtility.HtmlDecode(
                //        titleNode.InnerText
                //        );

                //    var detailNode = anchorNode.ParentNode?.SelectSingleNode(".//div[@style='-webkit-line-clamp:2']");
                //    if (detailNode is null)
                //    {
                //        return result;
                //    }
                //    var detail = WebUtility.HtmlDecode(
                //        detailNode.InnerText
                //        );

                //    var linkNode = anchorNode.ParentNode?.SelectSingleNode(".//a[@jsname='UWckNb']");
                //    if (linkNode is null)
                //    {
                //        return result;
                //    }
                //    var link = WebUtility.HtmlDecode(
                //        linkNode.Attributes["href"].Value
                //        );


                //    result.Add(new SearchResult(title, detail, link));

                //    itemCount++;
                //    if (itemCount >= itemNeeded)
                //    {
                //        return result;
                //    }
                //}

                pageIndex++;

                var nextPageButtons = await page.XPathAsync("//a [@class='LLNLxf']");
                if (nextPageButtons is null || nextPageButtons.Length == 0)
                {
                    return result;
                }

                var nextPageButton = nextPageButtons[0];

                await Task.Delay(5_000); //я тучка тучка тучка, а вовсе не медведь!

                await nextPageButton.FocusAsync();
                await page.Keyboard.PressAsync("Enter");
            }

            return result;
        }

        private async Task SavePageImageAsync(
            string searchTerm,
            IPage page,
            int? pageIndex
            )
        {
            if (string.IsNullOrEmpty(_pageImagesFolder))
            {
                return;
            }

            var imageFileName = WebUtility.HtmlEncode(searchTerm);
            if (imageFileName.Length > 16)
            {
                imageFileName = imageFileName.Substring(0, 16);
            }

            var imageFilePath = Path.Combine(
                _pageImagesFolder,
                imageFileName + (pageIndex.HasValue ? pageIndex.Value : string.Empty) + ".jpg"
                );

            await page.ScreenshotAsync(imageFilePath);
        }
    }
}
