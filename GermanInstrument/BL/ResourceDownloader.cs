using GermanInstrument.Model;
using GermanInstrument.StringExtensions;
using System.IO;
using System.Net;
using System.Net.Http;

namespace GermanInstrument.BL;

public class ResourceDownloader
{
    HttpClient client = new HttpClient(new HttpClientHandler()
    {
        AllowAutoRedirect = true,
        UseCookies = true,
        CookieContainer = new CookieContainer()
    });

    public async Task DownloadResource(IEnumerable<Product> products, string folder, IProgress<double> indicator)
    {
        int total = products.Count();
        int current = 0;

        foreach (var product in products)
        {

            try
            {
                await DownloadProductResources(folder, product);
            }
            catch
            {

            }

            indicator?.Report((double)++current / total);
        }
    }

    private async Task DownloadProductResources(string folder, Product product)
    {
        var images = product.Images.Distinct().ToArray();

        foreach (var image in images)
        {
            string localPath = Path.Combine(folder, "products", image.CreateMD5() + Path.GetExtension(image));

            if (!File.Exists(localPath))
            {
                var dir = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                try
                {
                    var bytes = await client.GetByteArrayAsync(image);
                    await File.WriteAllBytesAsync(localPath, bytes);
                }
                catch
                {
                    throw;
                }
            }
        }

        var instructions = product.Instructions;

        foreach (var pdf in instructions)
        {
            string localPath = Path.Combine(folder, "pdf", pdf.Uri.CreateMD5() + Path.GetExtension(pdf.Uri));

            if (!File.Exists(localPath))
            {
                var dir = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                try
                {
                    await (new WebClient().DownloadFileTaskAsync(pdf.Uri, localPath));
                }
                catch
                {
                    throw;
                }
            }
        }

    }
}