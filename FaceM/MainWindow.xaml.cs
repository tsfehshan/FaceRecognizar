using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace FaceM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IFaceServiceClient faceServiceClient = new FaceServiceClient("88ce56f3dbae4c67a5c090088b5a0d3b");
        public MainWindow()
        {
            InitializeComponent();
			CheckIfAGroupExist();
			//MakeRequest();
		}
        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";
            bool? result = openDlg.ShowDialog(this);

            if (!(bool)result)
            {
                return;
            }

            string filePath = openDlg.FileName;

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            FacePhoto.Source = bitmapSource;

            OutPut op = await UploadAndIdentifyFaces(filePath);
            Dictionary<string, string> faceNames = op.faceNames;
            Title = "Detecting...";
            //FaceRectangle[] faceRects = await UploadAndDetectFaces(filePath);
            Face[] faces = op.faces;
            FaceRectangle[] faceRects = faces?.Select(face => face.FaceRectangle).ToArray();
            Title = String.Format("Detection Finished. {0} face(s) detected", faceRects?.Length);

            if (faceRects?.Length > 0)
            {
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                double dpi = bitmapSource.DpiX;
                double resizeFactor = 96 / dpi;

                foreach (var faceRect in faceRects)
                {
                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(Brushes.Red, 2),
                        new Rect(
                            faceRect.Left * resizeFactor,
                            faceRect.Top * resizeFactor,
                            faceRect.Width * resizeFactor,
                            faceRect.Height * resizeFactor
                            )
                    );
                }

                foreach (var item in faceNames)
                {
                    var f = faces.SingleOrDefault(p => p.FaceId.ToString() == item.Key);
                    if (f != null)
                    {
                        drawingContext.DrawText(
                          new FormattedText(item.Value,
                             CultureInfo.GetCultureInfo("en-us"),
                             FlowDirection.LeftToRight,
                             new Typeface("Verdana"),
                             10, Brushes.White),
                             new Point(f.FaceRectangle.Left * resizeFactor, f.FaceRectangle.Top * resizeFactor));
                    }
                }

                drawingContext.Close();
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * resizeFactor),
                    (int)(bitmapSource.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);
                FacePhoto.Source = faceWithRectBitmap;

            }
        }

		private async void CheckIfAGroupExist()
		{
			var PG = await faceServiceClient.ListPersonGroupsAsync();
			var APG = await faceServiceClient.GetPersonGroupTrainingStatusAsync("test_school_15_12_melbourne_2016");
			if (PG.Any(s => s.PersonGroupId == "test_school_15_12_melbourne_2016"))
			{
			}
		}
		private async Task<Face[]> UploadAndGetFaces(string imageFilePath)
        {
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    var faces = await faceServiceClient.DetectAsync(imageFileStream);
                    return faces.ToArray();
                }
            }
            catch (Exception)
            {
                return new Face[0];
            }
        }


        private async Task<FaceRectangle[]> UploadAndDetectFaces(string imageFilePath)
        {
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    var faces = await faceServiceClient.DetectAsync(imageFileStream);
                    var faceRects = faces.Select(face => face.FaceRectangle);
                    return faceRects.ToArray();
                }
            }
            catch (Exception)
            {
                return new FaceRectangle[0];
            }
        }

        private async Task<OutPut> UploadAndIdentifyFaces(string imageFilePath)
        {
            //string testImageFile = @"D:\Tab\TabApu.jpg";
            OutPut op = new OutPut();

            string testImageFile = imageFilePath;
            Dictionary<string, string> faceNames = new Dictionary<string, string>();

            using (Stream s = File.OpenRead(testImageFile))
            {
                var faces = await faceServiceClient.DetectAsync(s);
                op.faces = faces;
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                if (faceIds.Count() > 10)
                {
					
					int i=0;
                    var faceCounter = faceIds.Count();
                    while (faceCounter > 0)
                    {
						Task.Delay(3000).Wait();
						//var start = i * 10 + 1;
						//var end = i * 10 + 10;
						var results = await faceServiceClient.IdentifyAsync("test_school_15_12_melbourne_2016", faceIds.Skip(i*10).Take(10).ToArray());
                        faceCounter = faceCounter - 10;
                        i++;
                        foreach (var identifyResult in results)
                        {
		                       Task.Delay(3000).Wait(); 

							Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                            if (identifyResult.Candidates.Length == 0)
                            {
                                Console.WriteLine("No one identified");
                            }
                            else
                            {
                                // Get top 1 among all candidates returned
                                var candidateId = identifyResult.Candidates[0].PersonId;
								
								var person = await faceServiceClient.GetPersonAsync("test_school_15_12_melbourne_2016", candidateId);
                                Console.WriteLine("Identified as {0}", person.Name);
                                faceNames.Add(identifyResult.FaceId.ToString(), person.Name);
                            }
                        }

                    }
                }
                else
                {
					Task.Delay(3000).Wait();
					var results = await faceServiceClient.IdentifyAsync("test_school_15_12_melbourne_2016", faceIds);
	                int counter = 0;

					foreach (var identifyResult in results)
                    {
							Task.Delay(3000).Wait();
						
						Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                        if (identifyResult.Candidates.Length == 0)
                        {
                            Console.WriteLine("No one identified");
                        }
                        else
                        {
                            // Get top 1 among all candidates returned
                            var candidateId = identifyResult.Candidates[0].PersonId;
							var person = await faceServiceClient.GetPersonAsync("test_school_15_12_melbourne_2016", candidateId);
                            Console.WriteLine("Identified as {0}", person.Name);
                            faceNames.Add(identifyResult.FaceId.ToString(), person.Name);
                        }
                    }
                }


            }
            op.faceNames = faceNames;
            return op;

        }


        private async void MakeRequest()
        {
            string[] s1 = new string[7] { "Dipu", "Gopu", "Mos", "Sam", "Tab", "Tos", "Zob" };
            string personGroupId = "myfamily";
            await faceServiceClient.CreatePersonGroupAsync(personGroupId, "My Family");


            foreach (var item in s1)
            {
                // Define Tabassum
                CreatePersonResult f = await faceServiceClient.CreatePersonAsync(
                    // Id of the person group that the person belonged to
                    personGroupId,
                    // Name of the person
                    item
                );
                var path = @"D:\Images\" + item;
                foreach (string imagePath in Directory.GetFiles(path, "*.jpg"))
                {
                    using (Stream s = File.OpenRead(imagePath))
                    {
                        // Detect faces in the image and add to Anna
                        await faceServiceClient.AddPersonFaceAsync(
                            personGroupId, f.PersonId, s);
                    }
                }
            }

            //// Define Tabassum
            //CreatePersonResult friend1 = await faceServiceClient.CreatePersonAsync(
            //	// Id of the person group that the person belonged to
            //	personGroupId,
            //	// Name of the person
            //	"Tabassum"
            //);
            //CreatePersonResult friend2 = await faceServiceClient.CreatePersonAsync(
            //	// Id of the person group that the person belonged to
            //	personGroupId,
            //	// Name of the person
            //	"Gopal"
            //);

            //const string friend1ImageDir = @"D:\Tab\";

            //foreach (string imagePath in Directory.GetFiles(friend1ImageDir, "*.jpg"))
            //{
            //	using (Stream s = File.OpenRead(imagePath))
            //	{
            //		// Detect faces in the image and add to Anna
            //		await faceServiceClient.AddPersonFaceAsync(
            //			personGroupId, friend1.PersonId, s);
            //	}
            //}
            //const string friend2ImageDir = @"D:\Gopu\";

            //foreach (string imagePath in Directory.GetFiles(friend2ImageDir, "*.jpg"))
            //{
            //	using (Stream s = File.OpenRead(imagePath))
            //	{
            //		// Detect faces in the image and add to Anna
            //		await faceServiceClient.AddPersonFaceAsync(
            //			personGroupId, friend2.PersonId, s);
            //	}
            //}


            await faceServiceClient.TrainPersonGroupAsync(personGroupId);

        }

        public class OutPut
        {
            public Face[] faces;
            public Dictionary<string, string> faceNames;
        }
    }
}
