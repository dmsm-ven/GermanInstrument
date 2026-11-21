using GermanInstrument.Model;
using GermanInstrument.StringExtensions;
using HtmlAgilityPack;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace GermanInstrument.BL;

public class ExportFormatter
{
    private readonly string yandexDiskRoot;
    private readonly IReadOnlyList<Product> products;
    private readonly int startId;

    public ExportFormatter(IEnumerable<Product> products, int startId, string yandexDiskRoot)
    {
        this.products = products.ToList();
        this.startId = startId;
        this.yandexDiskRoot = yandexDiskRoot;
    }

    public string GetGeneralExport()
    {
        var sb = new StringBuilder();

        int id = startId;
        foreach (var product in products)
        {
            string mainImage = GetMainImage(product);

            string productType = product.Name
                .Replace($" {product.Manufacturer} ", " ", StringComparison.OrdinalIgnoreCase)
                .Replace($" {product.Model} ", " ", StringComparison.OrdinalIgnoreCase);
            productType = Regex.Replace(productType, " {2,}", " ").Trim();

            string caption = $"{product.Model} {product.Manufacturer} {productType}";
            string keyword = Regex.Replace(Transliteration.Front(caption, true), "-{2,}", "-").Trim('-');
            string meta_title = $"{caption} купить в Санкт-Петербурге";
            string meta_desc = $"{meta_title} с доставкой по России";

            sb
                .AppendTab(id.ToString())   //product_id
                .AppendTab(product.Name)   //name(ru)
                .AppendTab(product.CategoryName)   //categories
                .AppendTab(product.Sku)   //sku
                .AppendTab(string.Empty)   //upc
                .AppendTab(string.Empty)   //ean
                .AppendTab(string.Empty)   //jan
                .AppendTab(string.Empty)   //isbn
                .AppendTab(string.Empty)   //mpn
                .AppendTab(string.Empty)   //location
                .AppendTab("0")   //quantity
                .AppendTab(product.Model)   //model
                .AppendTab(product.Manufacturer)   //manufacturer
                .AppendTab(mainImage)   //image_name
                .AppendTab("yes")   //shipping
                .AppendTab("0")   //price
                .AppendTab("0")   //points
                .AppendTab(string.Empty)   //date_added
                .AppendTab(string.Empty)   //date_modified
                .AppendTab(string.Empty)   //date_available
                .AppendTab("0")   //weight
                .AppendTab("kg")   //weight_unit
                .AppendTab("0")    //length
                .AppendTab("0")    //width
                .AppendTab("0")    //height
                .AppendTab("mm")   //length_unit
                .AppendTab("true")   //status
                .AppendTab("0")   //tax_class_id
                .AppendTab(keyword)   //seo_keyword
                .AppendTab(string.Empty)   //description(ru)
                .AppendTab("0")   //category_show(ru)
                .AppendTab("0")   //main_product(ru)
                .AppendTab(meta_title)   //meta_title(ru)
                .AppendTab(meta_desc)   //meta_description(ru)
                .AppendTab(string.Empty)   //meta_keywords(ru)
                .AppendTab("10")   //stock_status_id
                .AppendTab("0,1,2,3,4,5,6,7,8")   //store_ids
                .AppendTab("0:,1:,2:,3:,4:,5:,6:,7:,8:")   //layout
                .AppendTab(string.Empty)   //related_ids
                .AppendTab(string.Empty)   //adjacent_ids
                .AppendTab(string.Empty)   //tags(ru)
                .AppendTab("1")   //sort_order
                .AppendTab("true")   //subtract
                .AppendLine("1"); //minimum

            id++;
        }

        var result = sb.ToString();
        return result;
    }

    public string GetAdditionalImagesExport()
    {
        var sb = new StringBuilder();

        int id = startId;
        foreach (var product in products)
        {
            int sort_order = 0;
            foreach (var image in product.Images.Skip(1))
            {
                string imagePath = $"catalog/{product.ManufacturerFtpPath}/products/" + image.CreateMD5() + Path.GetExtension(image);
                sb.AppendLine($"{id}\t{imagePath}\t{sort_order++}");
            }

            id++;
        }

        var result = sb.ToString();
        return result;
    }

    public string GetDescriptionSql()
    {
        var sb = new StringBuilder();

        sb
            .AppendLine("UPDATE oc_product_description")
            .AppendLine("SET description = case product_id");

        int id = startId;
        foreach (var product in products)
        {
            sb.AppendLine($"WHEN '{id++}' THEN '{HttpUtility.HtmlEncode(BuildDescriptionForProduct(product))}'");
        }
        sb.AppendLine("END");
        sb.AppendLine($"WHERE product_id >= {startId}");

        var result = sb.ToString().TrimHtml() + ";";
        return result;
    }

    public string GetPdfSql()
    {
        var sb = new StringBuilder();

        sb.AppendLine("INSERT INTO oc_product_pdf (product_id, name, path) VALUES");

        int id = startId;
        foreach (var p in products)
        {
            foreach (var pdf in p.Instructions)
            {
                string pdfMd5Name = pdf.Uri.CreateMD5() + ".pdf";
                string path = $"{yandexDiskRoot}/{p.ManufacturerFtpPath}/{pdfMd5Name}";
                sb.AppendLine($"({id}, '{pdf.Name}', '{path}'),");
            }
            id++;
        }

        var result = sb.ToString().Trim('\r', '\n', '\t', ' ', ',') + ";";

        return result;
    }

    private string GetMainImage(Product product)
    {
        string? firstImage = product.Images.FirstOrDefault();

        if (firstImage != null)
        {
            return $"catalog/{product.ManufacturerFtpPath}/products/" + firstImage.CreateMD5() + Path.GetExtension(firstImage);
        }
        else
        {
            return "catalog/placeholder.jpg";
        }
    }

    private string BuildDescriptionForProduct(Product product)
    {
        var sb = new StringBuilder();

        sb.AppendLine(product.DescriptionMarkup);

        string result = sb.ToString();

        var doc = new HtmlDocument();
        doc.LoadHtml(result);

        if (IsDocumentHasInvalidTags(doc))
        {
            //throw new FormatException($"В описании товара '{product.Name ?? "<Без названия>"}' есть недопустимые теги: <a>, <img> или <iframe>");
        }

        return result;
    }

    private bool IsDocumentHasInvalidTags(HtmlDocument doc)
    {
        string[] invalidTags = new string[] { "img", "iframe", "a" };

        foreach (var tag in invalidTags)
        {
            if (doc.DocumentNode.SelectSingleNode($"//{tag}") != null)
            {
                return true;
            }
        }

        return false;
    }

}
