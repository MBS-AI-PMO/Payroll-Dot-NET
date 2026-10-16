using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;

namespace PayslipMgtSystem.Services.Pdf
{
    /// <summary>
    /// Page decoration used when rendering HTML to PDF. The presets reproduce the layouts the
    /// individual services used before they were consolidated, so generated documents look the same.
    /// </summary>
    public sealed record PdfLayout
    {
        /// <summary>Company logo drawn in the top-left corner (file in wwwroot).</summary>
        public string Logo { get; init; } = "solo.png";

        /// <summary>Draw the logo before the HTML content instead of after it.</summary>
        public bool LogoBeforeContent { get; init; }

        /// <summary>Image drawn rotated in the page centre behind the content, or null for none.</summary>
        public string? Watermark { get; init; }

        /// <summary>Watermark opacity (0–1); null keeps the PDF default (fully opaque).</summary>
        public float? WatermarkOpacity { get; init; }

        /// <summary>Draw the watermark before the HTML content instead of after it.</summary>
        public bool WatermarkBeforeContent { get; init; }

        /// <summary>Payslips emailed in bulk and PDFs downloaded by admins.</summary>
        public static readonly PdfLayout Standard = new() { Watermark = "solowatermark.png" };

        /// <summary>Income tax certificates emailed in bulk.</summary>
        public static readonly PdfLayout ItcEmail = new() { Watermark = "solo.png", WatermarkOpacity = 0.3f };

        /// <summary>Documents emailed one at a time from the admin pages.</summary>
        public static readonly PdfLayout IndividualEmail = new()
        {
            LogoBeforeContent = true,
            Watermark = "solowatermark.png",
            WatermarkOpacity = 0.3f,
            WatermarkBeforeContent = true
        };

        /// <summary>PDFs archived to disk by the background workers.</summary>
        public static readonly PdfLayout Plain = new();
    }

    /// <summary>Converts HTML templates into A4 PDF documents.</summary>
    public class PdfRenderer
    {
        private readonly IWebHostEnvironment _environment;

        static PdfRenderer()
        {
            // iTextSharp's XML worker needs Windows-1252, which .NET Core only provides via the code-pages provider
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public PdfRenderer(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public byte[] Render(string html, PdfLayout layout)
        {
            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 25, 25, 30, 30);
            var writer = PdfWriter.GetInstance(document, stream);
            writer.CloseStream = false;
            document.Open();

            if (layout.LogoBeforeContent)
                AddLogo(document, layout.Logo);
            if (layout.WatermarkBeforeContent)
                AddWatermark(document, writer, layout);

            using (var reader = new StringReader(html))
            {
                XMLWorkerHelper.GetInstance().ParseXHtml(writer, document, reader);
            }

            if (!layout.LogoBeforeContent)
                AddLogo(document, layout.Logo);
            if (!layout.WatermarkBeforeContent)
                AddWatermark(document, writer, layout);

            document.Close();
            return stream.ToArray();
        }

        private string? ImagePath(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;
            var path = Path.Combine(_environment.WebRootPath, fileName);
            return File.Exists(path) ? path : null;
        }

        private void AddLogo(Document document, string fileName)
        {
            var path = ImagePath(fileName);
            if (path == null)
                return;

            var logo = Image.GetInstance(path);
            logo.ScaleToFit(200f, 200f);
            logo.SetAbsolutePosition(25, document.PageSize.Height - logo.ScaledHeight - 30);
            document.Add(logo);
        }

        private void AddWatermark(Document document, PdfWriter writer, PdfLayout layout)
        {
            var path = ImagePath(layout.Watermark);
            if (path == null)
                return;

            var watermark = Image.GetInstance(path);
            watermark.ScaleToFit(400f, 300f);
            watermark.RotationDegrees = 45;
            watermark.SetAbsolutePosition(
                (document.PageSize.Width - watermark.ScaledWidth) / 2,
                (document.PageSize.Height - watermark.ScaledHeight) / 2);

            var state = new PdfGState();
            if (layout.WatermarkOpacity.HasValue)
                state.FillOpacity = layout.WatermarkOpacity.Value;

            var canvas = writer.DirectContentUnder;
            canvas.SetGState(state);
            canvas.AddImage(watermark);
        }
    }
}
