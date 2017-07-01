using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.Structure;

namespace SimilarImages
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        OpenFileDialog openDialog1 = new OpenFileDialog();

        DirectoryInfo directoryInfo = new DirectoryInfo(@"..\..\..\Imagini");
        string FilePath = @"..\..\..\Results\Results 6x6+Sobel.txt";

        #region Butoane
        private void Button_DeschideImaginea(object sender, RoutedEventArgs e)
        {
            openDialog1.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                                 "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                                 "Portable Network Graphic (*.png)|*.png";
            openDialog1.FilterIndex = 1;

            if (openDialog1.ShowDialog() == true)
            {
                try
                {
                    InputImg.Source = new BitmapImage(new Uri(openDialog1.FileName));

                    btnCautăSemnături.IsEnabled = true;

                    lblInputImg.Visibility = Visibility.Visible;
                    lblFirstImg.Visibility = Visibility.Hidden;
                    lblSecondImg.Visibility = Visibility.Hidden;
                    lblThirdImg.Visibility = Visibility.Hidden;

                    FirstImg.Visibility = Visibility.Hidden;
                    SecondImg.Visibility = Visibility.Hidden;
                    ThirdImg.Visibility = Visibility.Hidden;

                    txtFristImg.Visibility = Visibility.Hidden;
                    rctFistImg.Visibility = Visibility.Hidden;
                    txtSecondImg.Visibility = Visibility.Hidden;
                    rctSecondImg.Visibility = Visibility.Hidden;
                    txtThirdImg.Visibility = Visibility.Hidden;
                    rctThirdImg.Visibility = Visibility.Hidden;

                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        private void Button_CautăSemnături(object sender, RoutedEventArgs e)
        {
            kNN_FromFileForAllToAll();
        }
        private void Button_CreareVectori(object sender, RoutedEventArgs e)
        {
            CreareVectori();
            btnScriereFișier.IsEnabled = true;
        }

        private void Button_ScriereFișier(object sender, RoutedEventArgs e)
        {
            WriteResults();
        }


        #endregion
              
        #region Write and load Images in txt

        public string[] ImagesList;
        private List<ImageInfo> images = new List<ImageInfo>();
        private List<string> imgVectors = new List<string>();

        public void GetImages(DirectoryInfo directoryInfo)
        {
            var dirFiles = directoryInfo.GetFiles();
            ImagesList = new string[dirFiles.Length];

            for (int i = 0; i < dirFiles.Length; i++)
            {
                ImagesList[i] = dirFiles[i].FullName;
            }
        }

        public void WriteResults()
        {
            var orderedImages = (from i in images
                                 orderby i.Index
                                 select i.ImgVector).ToList();
            using (StreamWriter file = new StreamWriter(FilePath, true))
            {

                foreach (var item in orderedImages)
                {
                    file.WriteLine(item);
                }
            }
        }
        private void CreareVectori()
        {
            GetImages(directoryInfo);

            List<Task> tasks = new List<Task>();
            var nrImages = directoryInfo.GetFiles().Length;
            for (int i = 0; i < nrImages; i++)
            {
                int k = i;
                tasks.Add(new Task(() => GenerateVectors(k)));
            }
            foreach (var item in tasks)
            {
                item.Start();
            }
            Task.WaitAll();
        }

        private void GenerateVectors(int imgIndex)
        {
            var imagineDePrelucrare = new Bitmap(ImagesList[imgIndex]);
            double[] a = CreateVectorBlackPixels(imagineDePrelucrare);
            var imagineDePrelucrareSobel = SobelConvertor(imagineDePrelucrare);
            double[] b = CreateVectorWhitePixels(imagineDePrelucrareSobel);

            System.Text.StringBuilder vector = new System.Text.StringBuilder();

            foreach (var item in b)
            {
                vector.Append(item + " ");
            }
            foreach (var item in a)
            {
                vector.Append(item + " ");
            }
            vector.Append("+" + imagineDePrelucrare.Width + "+" + imagineDePrelucrare.Height);
            ImageInfo image = new ImageInfo();
            image.Index = imgIndex;
            image.ImgVector = vector.ToString();
            images.Add(image);

            imgVectors.Add(vector.ToString());
        }
        public double[] CreateVectorBlackPixels(Bitmap img)
        {
            //Sursa : https://stackoverflow.com/questions/13625891/cut-an-image-into-9-pieces-c-sharp
            var imgarray = new Bitmap[36];
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    var index = i * 6 + j;
                    imgarray[index] = new Bitmap(img.Width / 6, img.Height / 6);
                    var graphics = Graphics.FromImage(imgarray[index]);
                    graphics.DrawImage(img, new Rectangle(0, 0, img.Width / 6, img.Height / 6), new Rectangle(i * img.Width / 6, j * img.Height / 6, img.Width / 6, img.Height / 6), GraphicsUnit.Pixel);
                    graphics.Dispose();
                }
            }
            double[] vectorPixels = new double[imgarray.Length];

            for (int i = 0; i < vectorPixels.Length; i++)
            {
                vectorPixels[i] = BlackPixelsNumber(imgarray[i]);
            }

            return vectorPixels;
        }
        public double[] CreateVectorWhitePixels(Bitmap img)
        {
            var imgarray = new Bitmap[36];
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    var index = i * 6 + j;
                    imgarray[index] = new Bitmap(img.Width / 6, img.Height / 6);
                    var graphics = Graphics.FromImage(imgarray[index]);
                    graphics.DrawImage(img, new Rectangle(0, 0, img.Width / 6, img.Height / 6), new Rectangle(i * img.Width / 6, j * img.Height / 6, img.Width / 6, img.Height / 6), GraphicsUnit.Pixel);
                    graphics.Dispose();
                }
            }
            double[] vectorPixels = new double[imgarray.Length];

            for (int i = 0; i < vectorPixels.Length; i++)
            {
                vectorPixels[i] = WhitePixelsNumber(imgarray[i]);
            }

            return vectorPixels;
        }
        private int BlackPixelsNumber(Bitmap img)
        {
            //Sursa : http://csharphelper.com/blog/2016/10/count-pixels-of-different-colors-in-c/
            int blackPixels = 0;
            for (int i = 0; i < img.Height; i++)
            {
                for (int j = 0; j < img.Width; j++)
                {
                    if (img.GetPixel(j, i) == Color.FromArgb(255, 0, 0, 0))
                        blackPixels++;
                }
            }
            return blackPixels;
        }
        private int WhitePixelsNumber(Bitmap img)
        {
            int whitePixels = 0;
            for (int i = 0; i < img.Height; i++)
            {
                for (int j = 0; j < img.Width; j++)
                {
                    if (img.GetPixel(j, i) != Color.FromArgb(255, 0, 0, 0))
                        whitePixels++;
                }
            }
            return whitePixels;
        }

        #endregion

        #region KNN and Eucliadian Norm
        private void kNN_FromFileForAllToAll()
        {
            double a;
            double min = 100, min1 = 50, min2 = 20;
            int k = 0, k1 = 0, k2 = 0;

            var dirFiles = directoryInfo.GetFiles();
            var totalImages = File.ReadAllLines(FilePath);

            string[] vectors = new string[totalImages.Count()];
            int[] imgWidth = new int[totalImages.Count()];
            int[] imgHeight = new int[totalImages.Count()];

            int index = 0;
            foreach (var line in totalImages)
            {
                string[] stringSplit = line.Split('+');
                vectors[index] = stringSplit[0].Substring(0, stringSplit[0].Length - 1);

                imgWidth[index] = Int32.Parse(stringSplit[1]);
                imgHeight[index] = Int32.Parse(stringSplit[2]);
                index++;
            }

            var imaginePrincipala = new Bitmap(openDialog1.FileName);
            var imaginePrincipalaCuSobel = SobelConvertor(imaginePrincipala);

            double[] vectorPixelsImaginePrincipala = CreateVectorBlackPixels(imaginePrincipala);
            double[] vectorPixelsImaginePrincipalaCuSobel = CreateVectorWhitePixels(imaginePrincipalaCuSobel);
            double[] vectorPixels = vectorPixelsImaginePrincipalaCuSobel.Concat(vectorPixelsImaginePrincipala).ToArray();

            var imgWidthPrincipal = imaginePrincipala.Width;
            var imgHeightPrincipal = imaginePrincipala.Height;

            var nrImages = vectors.Count();
            for (int i = 0; i < nrImages; i++)
            {
                double[] vectorPixels1 = Array.ConvertAll(vectors[i].Split(null), Double.Parse);
                a = GetEuclidianNorm(vectorPixels, vectorPixels1, imgWidthPrincipal, imgHeightPrincipal, imgWidth[i], imgHeight[i]);
                if (a != 0)
                {
                    if (a < min)
                    {
                        if (a < min1)
                            if (a < min2)
                            {
                                k2 = i;
                                min = min1;
                                min1 = min2;
                                min2 = a;
                            }
                            else
                            {
                                k1 = i;
                                min = min1;
                                min1 = a;
                            }
                        else
                        {
                            k = i;
                            min = a;
                        }
                    }
                }
            }

            FirstImg.Source = new BitmapImage(new Uri(dirFiles[k2].FullName));
            SecondImg.Source = new BitmapImage(new Uri(dirFiles[k1].FullName));
            ThirdImg.Source = new BitmapImage(new Uri(dirFiles[k].FullName));

            FirstImg.Visibility = Visibility.Visible;
            SecondImg.Visibility = Visibility.Visible;
            ThirdImg.Visibility = Visibility.Visible;

            lblFirstImg.Visibility = Visibility.Visible;
            lblSecondImg.Visibility = Visibility.Visible;
            lblThirdImg.Visibility = Visibility.Visible;

            txtFristImg.Text = min2.ToString();
            txtFristImg.Visibility = Visibility.Visible;
            rctFistImg.Visibility = Visibility.Visible;
            txtSecondImg.Text = min1.ToString();
            txtSecondImg.Visibility = Visibility.Visible;
            rctSecondImg.Visibility = Visibility.Visible;
            txtThirdImg.Text = min.ToString();
            txtThirdImg.Visibility = Visibility.Visible;
            rctThirdImg.Visibility = Visibility.Visible;
        }
        private double GetEuclidianNorm(double[] vectorPixels, double[] vectorPixels1, int Img1Width, int Img1Height, int Img2Width, int Img2Height)
        {
            double a = 0;

            double nr1deImpartit = Img1Width / 6 * Img1Height / 6;
            double nr2deImpartit = Img2Width / 6 * Img2Height / 6;

            for (int i = 0; i < vectorPixels.Length; i++)
            {
                a += Math.Abs(vectorPixels[i] / nr1deImpartit - vectorPixels1[i] / nr2deImpartit);
            }

            return a;
        }
        #endregion

        #region Converters

        private Bitmap SobelConvertor(Bitmap imagineDePrelucrare)
        {
            Image<Gray, float> imgr2 = new Image<Gray, float>(imagineDePrelucrare);
            Image<Gray, float> sobel = imgr2.Sobel(0, 1, 3).Add(imgr2.Sobel(1, 0, 3)).AbsDiff(new Gray(0.0));

            return sobel.ToBitmap();
        }
        #endregion
    }

}
