using GermanInstrument.Model;
using GermanInstrument.StringExtensions;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GermanInstrument.BL;

public class ProductParser : ParserBase
{
    public static readonly string HOST = "https://german-instrument.ru";

    public async Task<List<Product>> ParseProducts(IEnumerable<ProductSearchEntry> searchData, IProgress<double> progress = null)
    {
        var parsedProducts = new List<Product>();
        int total = searchData.Count();
        int current = 0;

        foreach (var entry in searchData)
        {
            var product = await ParseProduct(entry);
            parsedProducts.Add(product);
            progress?.Report((double)++current / total);
        }

        return parsedProducts;
    }

    private async Task<Product> ParseProduct(ProductSearchEntry sd)
    {
        var product = new Product()
        {
            Model = sd.Sku,
            Sku = sd.Sku,
            Name = sd.PriceListProductName,
            EAN = sd.EAN
        };

        await SearchProduct(product);

        if (!string.IsNullOrWhiteSpace(product.Uri))
        {
            await ParseProductDetails(product);
        }

        return product;
    }

    private async Task SearchProduct(Product product)
    {
        string searchUri = "https://german-instrument.ru/index.php?route=product/search&description=true&search=" + product.Sku;

        var doc = await GetDocument(searchUri);

        var productItem = doc.DocumentNode
            .SelectSingleNode($"//img[contains(@src, '/{product.Sku}-250x150')]/..");

        if (productItem != null)
        {
            product.Uri = productItem.GetAttributeValue("href", null).Replace("&amp;", "&");
        }
        else
        {
            Debug.WriteLine($"Product not found: {product.Sku}");
        }
    }

    private async Task ParseProductDetails(Product product)
    {
        var doc = await GetDocument(product.Uri);
        if (doc == null)
        {
            return;
        }
        string skuOnPage = doc.DocumentNode.SelectSingleNode("//span[@itemprop='sku']")?.InnerText.TrimHtml();
        if (!skuOnPage.Equals(product.Sku, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        //example: https://german-instrument.ru/image/cache//data/product/knipex/KN-7803150-600x400.jpg
        if (doc.DocumentNode.SelectNodes("//div[@class='image-additional']//img") != null)
        {
            var images = doc.DocumentNode.SelectNodes("//div[@class='image-additional']//img").ToArray();
            if (images != null)
            {
                var fullSizeImages = images.Select(img => ClearImageCache(img.GetAttributeValue("src", null))).ToArray();
                product.Images.AddRange(fullSizeImages);
            }
        }
        else
        {
            var mainImageHref = doc.DocumentNode.SelectSingleNode("//*[@id='zoom1']").GetAttributeValue("href", null);
            var mainImageClear = ClearImageCache(mainImageHref);
            product.Images.Add(mainImageClear);
        }

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            product.Name = doc.DocumentNode.SelectSingleNode("//h1[@itemprop]")?.InnerText.TrimHtml();
        }
        if (string.IsNullOrWhiteSpace(product.EAN))
        {
            product.EAN = doc.DocumentNode.SelectSingleNode("//li[contains(text(), 'Штрих-код:')]")?.InnerText
                .TrimHtml()
                .Replace("Штрих-код:", string.Empty);
        }

        var manufacturer = doc.DocumentNode.SelectSingleNode("//span[@itemprop='brand']")?
            .InnerText
            .TrimHtml()
            .ToLower();

        product.Manufacturer = Regex.Replace(manufacturer, @"\b\w", m => m.Value.ToUpper());

        product.DescriptionMarkup = doc.DocumentNode.SelectSingleNode("//div[@id='tab-description']/div/div")?.InnerHtml?.TrimHtml() ?? string.Empty;
        product.DescriptionMarkup = product.DescriptionMarkup.Replace("\n", string.Empty).Replace("\t", string.Empty);

        product.CategoryName = doc.DocumentNode.SelectSingleNode("//div[@class='breadcrumb']/span/span")?.InnerText.TrimHtml();

        product.IsParsed = true;
    }

    private string ClearImageCache(string src)
    {
        var uri = Regex.Replace(src, "/cache/", "/");
        uri = Regex.Replace(uri, @"-\d+x\d+.jpg", ".jpg");
        uri = Regex.Replace(uri, "/{2,}", "/");
        uri = uri.Insert(uri.IndexOf(':') + 1, "/");
        return uri;
    }
}
