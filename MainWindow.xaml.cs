using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace PdfImageChanger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Properties

        private List<PdfImg> images;
        private string sourcePDF;
        private Config configuration;
        private List<string> deletables;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
            parseConfig();
            deletables = new List<string>();

            this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
        }

        #endregion

        #region Events Stuff

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveConfig();

            if (deletables != null)
            {
                configuration.imageLinks = null;
                dataGrid1.ItemsSource = null;
                for (int i = 0; i < deletables.Count; i++)
                {
                    if (File.Exists(deletables[i]))
                    {
                        try
                        {
                            File.Delete(deletables[i]);
                        }
                        catch (IOException)
                        { }
                    }
                }
            }
        }

        private static bool ThumbnailCallback() { return false; }

        #endregion

        #region Button Events

        /// <summary>
        /// Open a new PDF document
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".pdf"; // Default file extension
            dlg.Filter = "Pdf documents (.pdf)|*.pdf"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                images = new List<PdfImg>();

                // Open document
                sourcePDF = dlg.FileName;
                label1.Content = sourcePDF;
                PdfDocument document = PdfReader.Open(sourcePDF);
                int imageCount = 0;
                List<string> imageList = new List<string>();

                // Iterate pages
                foreach (PdfPage page in document.Pages)
                {
                    // Get resources dictionary
                    PdfDictionary resources = page.Elements.GetDictionary("/Resources");
                    if (resources != null)
                    {
                        // Get external objects dictionary
                        PdfDictionary xObjects = resources.Elements.GetDictionary("/XObject");
                        if (xObjects != null)
                        {
                            ICollection<PdfItem> items = xObjects.Elements.Values;
                            // Iterate references to external objects
                            foreach (PdfItem item in items)
                            {
                                PdfReference reference = item as PdfReference;
                                if (reference != null)
                                {
                                    PdfDictionary xObject = reference.Value as PdfDictionary;
                                    if (xObject != null && xObject.Elements.GetString("/Subtype") == "/Image" && xObject.Elements.GetName("/Filter") == "/DCTDecode")
                                    {
                                        byte[] stream = xObject.Stream.Value;
                                        string imageName = String.Format("Image{0}.jpeg", imageCount++);
                                        if (!deletables.Contains(imageName))
                                        {
                                            deletables.Add(imageName);
                                        }

                                        using (FileStream fs = new FileStream(imageName, FileMode.Create, FileAccess.Write))
                                        {
                                            BinaryWriter bw = new BinaryWriter(fs);
                                            bw.Write(stream);
                                            bw.Close();

                                            PdfImg newImage = new PdfImg();
                                            newImage.replace = false;
                                            newImage.imageCount = imageCount;
                                            newImage.filename = System.AppDomain.CurrentDomain.BaseDirectory + imageName;
                                            newImage.sourceImg = xObject.Stream.Value;
                                            images.Add(newImage);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                dataGrid1.ItemsSource = images;
            }

            button2.Focus();
        }

        /// <summary>
        /// Save the PDF document(s)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < configuration.distributors.Count; i++)
            {
                PdfDocument document = PdfReader.Open(sourcePDF);
                foreach (PdfPage page in document.Pages)
                {
                    PdfDictionary resources = page.Elements.GetDictionary("/Resources");
                    if (resources != null)
                    {
                        PdfDictionary xObjects = resources.Elements.GetDictionary("/XObject");
                        if (xObjects != null)
                        {
                            ICollection<PdfItem> items = xObjects.Elements.Values;
                            foreach (PdfItem item in items)
                            {
                                PdfReference reference = item as PdfReference;
                                if (reference != null)
                                {
                                    PdfDictionary xObject = reference.Value as PdfDictionary;
                                    if (xObject != null && xObject.Elements.GetString("/Subtype") == "/Image" && xObject.Elements.GetName("/Filter") == "/DCTDecode")
                                    {
                                        for (int j = 0; j < images.Count; j++)
                                        {
                                            IStructuralEquatable eqa1 = xObject.Stream.Value;
                                            if (images[j].replace && eqa1.Equals(images[j].sourceImg, StructuralComparisons.StructuralEqualityComparer))
                                            {
                                                byte[] sourceImage = xObject.Stream.Value;
                                                MemoryStream sourceMemStream = new MemoryStream();
                                                sourceMemStream.Write(sourceImage, 0, sourceImage.Length);
                                                System.Drawing.Image sourceImageInst = System.Drawing.Image.FromStream(sourceMemStream);

                                                // Read image from file
                                                using (FileStream fs = new FileStream(configuration.distributors[i].logo, FileMode.Open, FileAccess.Read))
                                                {
                                                    BinaryReader br = new BinaryReader(fs);
                                                    byte[] imageBytes = br.ReadBytes((int)fs.Length);
                                                    br.Close();

                                                    // Create a resized image
                                                    MemoryStream memStream = new MemoryStream();
                                                    memStream.Write(imageBytes, 0, imageBytes.Length);
                                                    System.Drawing.Image image = System.Drawing.Image.FromStream(memStream);
                                                    System.Drawing.Image.GetThumbnailImageAbort myCallback = new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback);
                                                    System.Drawing.Image thumb = image.GetThumbnailImage(sourceImageInst.Width, sourceImageInst.Height, myCallback, IntPtr.Zero);
                                                    memStream.Close();

                                                    // Convert back to a stream from an image object
                                                    MemoryStream ms = new MemoryStream();
                                                    thumb.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                                    xObject.Stream.Value = ms.ToArray();
                                                    ms.Close();
                                                }

                                                sourceMemStream.Close();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Add hyperlinks to the images at the bottom of the first page
                for (int j = 0; j < configuration.imageLinks.Count; j++)
                {
                    using (var pdfGfx = XGraphics.FromPdfPage(document.Pages[0]))
                    {
                        XRect rect = new XRect(new PdfSharp.Drawing.XPoint(configuration.imageLinks[j].xStart, configuration.imageLinks[j].yStart), new PdfSharp.Drawing.XPoint(configuration.imageLinks[j].xStart + configuration.imageLinks[j].width, configuration.imageLinks[j].yStart + configuration.imageLinks[j].height));
                        XRect tRect = pdfGfx.Transformer.WorldToDefaultPage(rect);
                        PdfRectangle rc = new PdfSharp.Pdf.PdfRectangle(tRect);
                        document.Pages[0].AddWebLink(rc, configuration.imageLinks[j].url);
                    }

                    //using (var pdfGfx = XGraphics.FromPdfPage(pageZero))
                    //{
                    //    XRect rect1 = new XRect(new PdfSharp.Drawing.XPoint(configuration.imageLinks[j].xStart, configuration.imageLinks[j].yStart), new PdfSharp.Drawing.XPoint(configuration.imageLinks[j].xStart + configuration.imageLinks[j].width, configuration.imageLinks[j].yStart + configuration.imageLinks[j].height));
                    //    pdfGfx.DrawRectangle(XBrushes.Pink, rect1);
                    //}
                }

                // Add contact information to the first page
                XFont nameFont = new XFont("Verdana", 10, XFontStyle.Bold);
                XFont contactFont = new XFont("Verdana", 8, XFontStyle.Regular);
                int targetCenter = 375;

                using (var pdfGfx = XGraphics.FromPdfPage(document.Pages[0]))
                {
                    XSize nameStringSize = pdfGfx.MeasureString(configuration.distributors[i].contactName, nameFont);
                    XSize contactStringSize1 = pdfGfx.MeasureString(configuration.distributors[i].contactContact1, contactFont);
                    XSize contactStringSize2 = pdfGfx.MeasureString(configuration.distributors[i].contactContact2, contactFont);

                    int nameStart = targetCenter - (int)Math.Round((double)nameStringSize.Width / 2);
                    int contactStart1 = targetCenter - (int)Math.Round((double)contactStringSize1.Width / 2);
                    int contactStart2 = targetCenter - (int)Math.Round((double)contactStringSize2.Width / 2);

                    pdfGfx.DrawString(configuration.distributors[i].contactName, nameFont, XBrushes.White, nameStart, 95 + nameStringSize.Height * 0.6, XStringFormats.Default);
                    pdfGfx.DrawString(configuration.distributors[i].contactContact1, contactFont, XBrushes.White, contactStart1, 105 + contactStringSize1.Height * 0.6, XStringFormats.Default);
                    pdfGfx.DrawString(configuration.distributors[i].contactContact2, contactFont, XBrushes.White, contactStart2, 115 + contactStringSize2.Height * 0.6, XStringFormats.Default);
                }

                document.Save(configuration.distributors[i].name + ".pdf");
            }

            MessageBox.Show("PDF file(s) successfully saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Open the Edit form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            Window1 configWindow = new Window1(configuration);
            configWindow.ShowDialog();
            if (configWindow.DialogResult == true)
            {
                configuration = configWindow.configObj;
                saveConfig();
            }
        }

        /// <summary>
        /// Get out of here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion        

        #region Methods

        /// <summary>
        /// Parse the serialized config object.  If the file doesn't exist then initialize it.
        /// </summary>
        private void parseConfig()
        {
            if (File.Exists("app_config.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Config));
                TextReader tr = new StreamReader("app_config.xml");
                configuration = (Config)serializer.Deserialize(tr);
                tr.Close();
            }
            else
            {
                configuration = new Config();
                configuration.imageLinks = new List<ImageLink>();
                configuration.distributors = new List<Distributor>();

                ImageLink link1 = new ImageLink();
                link1.prettyName = "Otter Brine, Inc.";
                link1.url = @"http://www.otterbine.com";
                link1.width = 58;
                link1.height = 40;
                link1.xStart = 24;
                link1.yStart = 729;
                configuration.imageLinks.Add(link1);

                link1 = new ImageLink();
                link1.prettyName = "Foley United";
                link1.url = @"http://www.foleyunited.com/Home";
                link1.width = 70;
                link1.height = 40;
                link1.xStart = 93;
                link1.yStart = 729;
                configuration.imageLinks.Add(link1);

                link1 = new ImageLink();
                link1.prettyName = "True Surface";
                link1.url = @"http://www.true-surface.com";
                link1.width = 87;
                link1.height = 40;
                link1.xStart = 180;
                link1.yStart = 729;
                configuration.imageLinks.Add(link1);

                link1 = new ImageLink();
                link1.prettyName = "Turf Care U.S.";
                link1.url = @"http://turfcare-us.lely.com/en/our-products/broadcast-spreader/land-wheel-wfr-en-wgr";
                link1.width = 60;
                link1.height = 40;
                link1.xStart = 282;
                link1.yStart = 729;
                configuration.imageLinks.Add(link1);

                link1 = new ImageLink();
                link1.prettyName = "Paraide";
                link1.url = @"http://www.paraide.com";
                link1.width = 63;
                link1.height = 40;
                link1.xStart = 350;
                link1.yStart = 729;
                configuration.imageLinks.Add(link1);

                link1 = new ImageLink();
                link1.prettyName = "Standard Gold";
                link1.url = @"http://www.standardgolf.com";
                link1.width = 90;
                link1.height = 40;
                link1.xStart = 421;
                link1.yStart = 729;
                configuration.imageLinks.Add(link1);

                link1 = new ImageLink();
                link1.prettyName = "SGM Industries";
                link1.url = @"http://www.sgmindustries.com/products";
                link1.width = 56;
                link1.height = 40;
                link1.xStart = 530;
                link1.yStart = 729;
                configuration.imageLinks.Add(link1);

                Distributor dist1 = new Distributor();
                dist1.contactName = "Adam Tebbe";
                dist1.contactContact1 = "20 Pierce Rd., Watertown MA 02472";
                dist1.contactContact2 = "adam.tebbe@gmail.com";
                dist1.logo = @"C:\Users\Adam\Downloads\vw.jpg";
                dist1.name = "Me";
                configuration.distributors.Add(dist1);
            }
        }

        private void saveConfig()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Config));
            TextWriter tw = new StreamWriter("app_config.xml");
            serializer.Serialize(tw, configuration);
            tw.Close();
        }

        #endregion

    }
}
