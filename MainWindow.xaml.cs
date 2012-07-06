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
        private List<PdfImg> images;
        private string sourcePDF;
        private Config configuration;

        public MainWindow()
        {
            InitializeComponent();

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
            }

            this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Config));
            TextWriter tw = new StreamWriter("app_config.xml");
            serializer.Serialize(tw, configuration);
            tw.Close();
        }

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
                                        FileStream fs = new FileStream(imageName, FileMode.Create, FileAccess.Write);
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

                linkBox.ItemsSource = configuration.imageLinks;
                dataGrid1.ItemsSource = images;
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
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
                                    for(int i = 0; i < images.Count; i++)
                                    {
                                        IStructuralEquatable eqa1 = xObject.Stream.Value;
                                        if (images[i].replace && eqa1.Equals(images[i].sourceImg, StructuralComparisons.StructuralEqualityComparer))
                                        {
                                            byte[] sourceImage = xObject.Stream.Value;
                                            MemoryStream sourceMemStream = new MemoryStream();
                                            sourceMemStream.Write(sourceImage, 0, sourceImage.Length);
                                            System.Drawing.Image sourceImageInst = System.Drawing.Image.FromStream(sourceMemStream);

                                            // Read image from file
                                            FileStream fs = new FileStream(@"C:\Users\Adam\Downloads\vw.jpg", FileMode.Open, FileAccess.Read);
                                            BinaryReader br = new BinaryReader(fs);
                                            byte[] imageBytes = br.ReadBytes((int)fs.Length);
                                            br.Close();
                                            fs.Close();

                                            // Create a resized image
                                            MemoryStream memStream = new MemoryStream();
                                            memStream.Write(imageBytes, 0, imageBytes.Length);
                                            System.Drawing.Image image = System.Drawing.Image.FromStream(memStream);
                                            System.Drawing.Image.GetThumbnailImageAbort myCallback = new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback);
                                            System.Drawing.Image thumb = image.GetThumbnailImage(sourceImageInst.Width, sourceImageInst.Height, myCallback, IntPtr.Zero);

                                            // Convert back to a stream from an image object
                                            MemoryStream ms = new MemoryStream();
                                            thumb.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                            xObject.Stream.Value = ms.ToArray();               

                                            // Find the bounding rectangle of the image
                                            double originx = 100;
                                            double originy = 100;
                                            int width = xObject.Elements.GetInteger(PdfImage.Keys.Width);
                                            int height = xObject.Elements.GetInteger(PdfImage.Keys.Height);

                                            // Add a hyperlink at the location of the image
                                            PdfRectangle rect = new PdfRectangle(new PdfSharp.Drawing.XPoint(originx, originy), new PdfSharp.Drawing.XPoint(originx+width, originy+height));
                                            page.AddWebLink(rect, images[i].link);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            document.Save("try2.pdf");
            MessageBox.Show("PDF File successfully saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static bool ThumbnailCallback() { return false; }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            Window1 configWindow = new Window1(configuration);
            configWindow.ShowDialog();
            configuration = configWindow.configObj;
        }

    }
}
