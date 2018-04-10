using Windows.Storage.Streams;
using Windows.UI.Core;

using Windows.UI.Xaml.Media;
using Windows.Foundation;
using System.Threading;
using Windows.UI.Notifications;
using System.IO.IsolatedStorage;
using System.IO;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.OneDrive.Sdk;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Graphics.Imaging;
using System.Collections.Generic;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using TestOneDriveSdk_001.Helpers;
using Microsoft.OneDrive.Sdk;



using Microsoft;
using System.Diagnostics;
using Windows.UI.Popups;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Controls.Maps;
using TestOneDriveSdk_001.Helpers;
using Tweetinvi.Models;
using Tweetinvi;

namespace TestOneDriveSdk_001
{
    public static class GeoCodeCalc
    {

        //  private IOneDriveClient _client;
        public const double EarthRadiusInMiles = 3956.0;
        public const double EarthRadiusInKilometers = 6367.0;

        public static double ToRadian(double val) { return val * (Math.PI / 180); }
        public static double DiffRadian(double val1, double val2) { return ToRadian(val2) - ToRadian(val1); }

        public static double CalcDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double radius = GeoCodeCalc.EarthRadiusInMiles;

            radius = GeoCodeCalc.EarthRadiusInKilometers;
            return radius * 2 * Math.Asin(Math.Min(1, Math.Sqrt((Math.Pow(Math.Sin((DiffRadian(lat1, lat2)) / 2.0), 2.0) + Math.Cos(ToRadian(lat1)) * Math.Cos(ToRadian(lat2)) * Math.Pow(Math.Sin((DiffRadian(lng1, lng2)) / 2.0), 2.0)))));
        }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // FBSession sess = FBSession.ActiveSession;
        private IOneDriveClient _client;
        // LiveConnectClient liveClient;
        DateTime start;
        int linesCounter = 0;
        GeolocationAccessStatus accessStatus;
        Geolocator gl;
        Geoposition pos;
        MapIcon curPoint;
        MapPolyline line;
        List<Geoposition> gpList;
        List<BasicGeoposition> bgpList;
        bool track;

        double distance = 0;
        double goal = 1;

        static string Access_Token = "254710807-9ZN3N4F17rmPJLrKjLDtnmF4lCvnEp5Y2myDZBCw";
        static string Access_Token_Secret = "zlwHRWwklihadXNbHtTzd9NsG9cINEx1FnxlM3LItvavc";
        static string Consumer_key = "1hlQfsybu0NiP1ZUBNnWu9ljt";
        static string Consumer_secret = "6QgZRCvQzpj4t39Omq16aqKGPrlF5eXfDi5Uyzo64ksDkWuQ9x";

        private readonly string[] _scopes =
      {
            "onedrive.readwrite",
            "onedrive.appfolder",
            "wl.signin",
        };
        private IOneDriveClient _client1;
        private string _savedId;
        private AccountSession _session;

        public MainPage()
        {
            InitializeComponent();
            InitializeData();
            //InitTwitterCredentials();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
          //  NavigationCacheMode = NavigationCacheMode.Enabled;


        }




        private async void InitializeData()
        {
            loadGoal();

            goaltextbox.Text = goal.ToString();

            accessStatus = await Geolocator.RequestAccessAsync();
            if (accessStatus == GeolocationAccessStatus.Allowed)
            {
                gl = new Geolocator() { MovementThreshold = 5, DesiredAccuracyInMeters = 5, ReportInterval = 1000 };
                pos = await gl.GetGeopositionAsync();
                curPoint = new MapIcon()
                {
                    NormalizedAnchorPoint = new Point(0.5, 0.5),
                    Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/LocationLogo.png")),
                    ZIndex = 2,
                    Location = pos.Coordinate.Point
                };
                gpList = new List<Geoposition>() { pos };
                bgpList = new List<BasicGeoposition>() { pos.Coordinate.Point.Position };
                line = new MapPolyline()
                {
                    StrokeThickness = 2,
                    StrokeColor = ((SolidColorBrush)Resources["SystemControlHighlightAccentBrush"]).Color,
                    ZIndex = 1,
                    Path = new Geopath(bgpList)
                };
                map.MapElements.Add(curPoint);
                track = false;
                gl.PositionChanged += OnPositionChanged;

            }
        }
        private static void InitTwitterCredentials()
        {
            var creds = new TwitterCredentials(Access_Token, Access_Token_Secret, Consumer_key, Consumer_secret);
            Auth.SetUserCredentials(Consumer_key, Consumer_secret, Access_Token, Access_Token_Secret);
            Auth.ApplicationCredentials = creds;

        }  
        private async void locatorButton_Click(object sender, RoutedEventArgs e)
        {
            if (pos == null)
                pos = await gl.GetGeopositionAsync();
            map.Center = pos.Coordinate.Point;
            map.ZoomLevel = 18;
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                goal = double.Parse(goaltextbox.Text.ToString());                
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog("Please enter the correct goal.", "");
                dialog.ShowAsync();
                txtBoxAge.Focus(FocusState.Keyboard);
                return;
            }

            saveGoal();
        }

        private void saveGoal()
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication();

            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream("goal.txt", FileMode.OpenOrCreate, isoStore))
            {
                using (StreamWriter writer = new StreamWriter(isoStream))
                {
                    writer.Write(goal);
                    writer.WriteLine();
                    writer.Write(txtBoxAge.Text);
                    writer.WriteLine();
                    writer.Write(txtBoxHeight.Text);
                    writer.WriteLine();
                    writer.Write(txtBoxWeight.Text);
                    writer.WriteLine();
                    writer.Write(radMale.IsChecked);
                    writer.WriteLine();
                    writer.Write(comboFactor.SelectedIndex);
                }
            }
        }

        private void loadGoal()
        {

            IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication();

            if (isoStore.FileExists("goal.txt"))
            {
                using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream("goal.txt", FileMode.Open, isoStore))
                {
                    using (StreamReader reader = new StreamReader(isoStream))
                    {
                        try{
                            string goal_string = reader.ReadLine();
                            goal = double.Parse(goal_string);
                            txtBoxAge.Text = reader.ReadLine();
                            txtBoxHeight.Text = reader.ReadLine();
                            txtBoxWeight.Text = reader.ReadLine();
                            if(bool.Parse(reader.ReadLine()))
                            {
                                radMale.IsChecked = true;
                                radFemale.IsChecked = false;
                            }
                            else
                            {
                                radMale.IsChecked = false;
                                radFemale.IsChecked = true;
                            }
                            comboFactor.SelectedIndex = Int32.Parse(reader.ReadLine());
                        }
                        catch (Exception){
                        }
                    }
                }
            }
        }


        private void incButton_Click(object sender, RoutedEventArgs e)
        {
            if (map.ZoomLevel < map.MaxZoomLevel)
                map.ZoomLevel++;
        }
        private void decButton_Click(object sender, RoutedEventArgs e)
        {
            if (map.ZoomLevel > map.MinZoomLevel)
                map.ZoomLevel--;
        }
        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void startButton_Click(object sender, RoutedEventArgs e)
        {

            switch (accessStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    start = DateTime.Now;
                    var timer = new Timer(WriteInfo, null, 0, 1000);
                    linesCounter++;
                    map.MapElements.Add(line);
                    track = true;
                    startButton.IsEnabled = false;
                    pauseButton.IsEnabled = true;
                    break;
                case GeolocationAccessStatus.Denied:
                    break;
                case GeolocationAccessStatus.Unspecified:
                    break;
            }
        }
        private async void WriteInfo(object state)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBlock.Text = "Start Time: " + start.ToString() + "\n" + "Elapsed Time: " + DateTime.Now.Subtract(start).ToString() + "\n";
            });
        }

        private void ShowToastNotification(string title, string stringContent)
        {
            ToastNotifier ToastNotifier = ToastNotificationManager.CreateToastNotifier();
            Windows.Data.Xml.Dom.XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            Windows.Data.Xml.Dom.XmlNodeList toastNodeList = toastXml.GetElementsByTagName("text");
            toastNodeList.Item(0).AppendChild(toastXml.CreateTextNode(title));
            toastNodeList.Item(1).AppendChild(toastXml.CreateTextNode(stringContent));
            Windows.Data.Xml.Dom.IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
            Windows.Data.Xml.Dom.XmlElement audio = toastXml.CreateElement("audio");
            audio.SetAttribute("src", "ms-winsoundevent:Notification.SMS");

            ToastNotification toast = new ToastNotification(toastXml);
            toast.ExpirationTime = DateTime.Now.AddSeconds(4);
            ToastNotifier.Show(toast);
        }

        async private void OnPositionChanged(Geolocator sender, PositionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                pos = e.Position;
                curPoint.Location = pos.Coordinate.Point;
                map.MapElements[0] = curPoint;
                if (track)
                {
                    distance += GeoCodeCalc.CalcDistance(pos.Coordinate.Point.Position.Latitude, pos.Coordinate.Point.Position.Longitude, bgpList[bgpList.Count - 1].Latitude, bgpList[bgpList.Count - 1].Longitude);
                    distanceBlock.Text = "Distance = " + distance.ToString() + " kilometers";


                    gpList.Add(pos);

                    bgpList.Add(pos.Coordinate.Point.Position);

                    line.Path = new Geopath(bgpList);
                    map.MapElements[linesCounter] = line;

                    if (distance >= goal)
                    {
                        //notification
                        ShowToastNotification("Goal Reached", "You have reached your goal!!!");
                    }
                }
            });
        }
        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            bgpList.Clear();
            track = false;
            startButton.IsEnabled = true;
            pauseButton.IsEnabled = false;
        }

        // 4/4/2018
        /*   
                private async void setUploadButton_Click(object sender, RoutedEventArgs e)
                {
                    InitTwitterCredentials();

                    Exception error = null;

                    try
                    {
                        _session = null;
                        _client = OneDriveClientExtensions.GetClientUsingOnlineIdAuthenticator(
                              _scopes);

                       _session = await _client.AuthenticateAsync();
                        Debug.WriteLine($"Token: {_session.AccessToken}");

                        var dialog = new MessageDialog("You are authenticated!", "Success!");

                        await dialog.ShowAsync();
                        uploadFile();
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }

                    if (error != null)
                    {
                        var dialog = new MessageDialog(error.ToString(), "Sorry!");
                        await dialog.ShowAsync();
                    }
                }
        */

        public async void CopyFile(StorageFile fileToCopy, StorageFile filetoReplace)
        {
            await fileToCopy.CopyAndReplaceAsync(filetoReplace);
        }

        private async void setUploadButton_Click(object sender, RoutedEventArgs e)
        {
            //InitTwitterCredentials();

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null && file.Path.Equals("") == false)
            {
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile saveFile = await storageFolder.CreateFileAsync("save.png",
                        Windows.Storage.CreationCollisionOption.ReplaceExisting);

                CopyFile(file, saveFile);

                // Application now has read/write access to the picked file

                var bitmap = new BitmapImage(new Uri(file.Path));

                var Stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

                bitmap.SetSource(Stream);
                img.Source = bitmap;
                this.textBlock.Text = "Picked photo: " + file.Name;
            }
            
            

            /*
            using (var inputStream = await file.OpenSequentialReadAsync())
            {
                var readStream = inputStream.AsStreamForRead();
                var byteArray = new byte[readStream.Length];
                await readStream.ReadAsync(byteArray, 0, byteArray.Length);
                
            }
            */
            /*IRandomAccessStream random = await RandomAccessStreamReference.CreateFromUri(new Uri(file.Path)).OpenReadAsync();
            Windows.Graphics.Imaging.BitmapDecoder decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random);
            Windows.Graphics.Imaging.PixelDataProvider pixelData = await decoder.GetPixelDataAsync();
            byte[] bytes = pixelData.DetachPixelData();


            Tweet.PublishTweet(file.Path);
            Tweet.PublishTweetWithImage(file.Name.ToString(), bytes);*/

            // ----------- STORE THIS BITMAP TO SERVER ------ //

            /*var wbm = new WriteableBitmap(600, 800);
            await wbm.SetSourceAsync(bitmap);
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            if (folder != null)
            {
                StorageFile fileToSave = await folder.CreateFileAsync("imagefile" + ".jpg", CreationCollisionOption.ReplaceExisting);
                using (var storageStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, storageStream);
                    var pixelStream = wbm.PixelBuffer.AsStream();
                    var pixels = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixels, 0, pixels.Length);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)wbm.PixelWidth, (uint)wbm.PixelHeight, 48, 48, pixels);
                    await encoder.FlushAsync();
                }
            }*/
        }

        public async void getlink()
        {
            if (_client == null
                || _session?.AccessToken == null)
            {
                var dialog = new MessageDialog("Please authenticate first!", "Sorry!");
                await dialog.ShowAsync();
                return;
            }

            if (string.IsNullOrEmpty(_savedId))
            {
                var dialog =
                    new MessageDialog(
                        "For the purpose of this demo, save a file first using the Upload File button",
                        "No file saved");
                await dialog.ShowAsync();
                return;
            }

            Exception error = null;
            Permission link = null;
           ;

            try
            {
                link = await _client.Drive.Items[_savedId].CreateLink("view").Request().PostAsync();
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                var dialog = new MessageDialog(error.Message, "Error!");
                await dialog.ShowAsync();
             
                return;
            }

            Debug.WriteLine("RETRIEVED LINK ---------------------");
            Debug.WriteLine(link.Link.WebUrl);
            var successDialog = new MessageDialog(
                $"The link was copied to the Debug window: {link.Link.WebUrl}",
                "No file saved");
            await successDialog.ShowAsync();
        }


        public async void uploadFile()
        {
            if (_client == null
               || _session?.AccessToken == null)
            {
                var dialog = new MessageDialog("Please authenticate first!", "Sorry!");
                await dialog.ShowAsync();
                return;
            }

            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            picker.FileTypeFilter.Add("*");
            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                //ShowBusy(true);

                Exception error = null;

                try
                {
                    using (var stream = await file.OpenStreamForReadAsync())
                    {
                        var item =
                            await
                                _client.Drive.Special.AppRoot.ItemWithPath(file.Name)
                                    .Content.Request()
                                    .PutAsync<Item>(stream);


                        _savedId = item.Id;
                        var successDialog =
                            new MessageDialog(
                                $"Uploaded file has ID {item.Id}. You can now use the Get Link button to retrieve a direct link to the file",
                                "Success");
                        await successDialog.ShowAsync();


                       
                    }

                  //  ShowBusy(false);
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                if (error != null)
                {
                    var dialog = new MessageDialog(error.Message, "Error!");
                    await dialog.ShowAsync();
                    // ShowBusy(false);
              
                    



                }

                if(error==null)
                {
                    /*IRandomAccessStream random = await RandomAccessStreamReference.CreateFromUri(new Uri(_savedId.Trim())).OpenReadAsync();
                    Windows.Graphics.Imaging.BitmapDecoder decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random);
                    Windows.Graphics.Imaging.PixelDataProvider pixelData = await decoder.GetPixelDataAsync();
                    byte[] bytes = pixelData.DetachPixelData(); */
                    

                    var Stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

                    var image = new BitmapImage();
                    image.SetSource(Stream);
                    img.Source = image;

                    using (var inputStream = await file.OpenSequentialReadAsync())
                    {
                        var readStream = inputStream.AsStreamForRead();
                        var byteArray = new byte[readStream.Length];
                        await readStream.ReadAsync(byteArray, 0, byteArray.Length);

                        Tweet.PublishTweetWithImage(file.Name.ToString(), byteArray);


                    }
                }
            }
        }
        private void GoalButton_Click(object sender, RoutedEventArgs e)
        {
            //do whatever you need.
            Canvas.SetZIndex(GoalGrid, 3);
            Canvas.SetZIndex(MapGrid, 1);
            Canvas.SetZIndex(UploadGrid, 1);
            Page_Title.Text = "Goal";
        }

        private void GoalButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }


        private void MapButton_Click(object sender, RoutedEventArgs e)
        {
            //do whatever you need.
            Canvas.SetZIndex(MapGrid, 3);
            Canvas.SetZIndex(GoalGrid, 1);
            Canvas.SetZIndex(UploadGrid, 1);
            Page_Title.Text = "Map";
        }

        private void MapButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }


        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            //do whatever you need.
            Canvas.SetZIndex(UploadGrid, 3);
            Canvas.SetZIndex(GoalGrid, 1);
            Canvas.SetZIndex(MapGrid, 1);
            Page_Title.Text = "Upload";



            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            try
            {
                Windows.Storage.StorageFile saveFile = await storageFolder.GetFileAsync("save.png");

                var bitmap = new BitmapImage(new Uri(saveFile.Path));

                var Stream = await saveFile.OpenAsync(Windows.Storage.FileAccessMode.Read);

                bitmap.SetSource(Stream);
                img.Source = bitmap;
            }
            catch (Exception)
            {

            }
            
        }

        private void UploadButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void calculateCalory(object sender, RoutedEventArgs e)
        {
            double value_BMR;

            int weight = 0;
            int height = 0;
            int age = 0;

            try
            {
                age = Convert.ToInt32(txtBoxAge.Text);
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog("Please enter the correct age.", "");
                dialog.ShowAsync();
                txtBoxAge.Focus(FocusState.Keyboard);
                return;
            }

            try
            {
                height = Convert.ToInt32(txtBoxHeight.Text);
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog("Please enter the correct height.", "");
                dialog.ShowAsync();
                txtBoxHeight.Focus(FocusState.Keyboard);
                return;
            }

            try
            {
                weight = Convert.ToInt32(txtBoxWeight.Text);
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog("Please enter the correct weight.", "");
                dialog.ShowAsync();
                txtBoxWeight.Focus(FocusState.Keyboard);
                return;
            }



            if (radMale.IsChecked == true)
            {
                value_BMR = 66 + (13.7 * weight) + (5 * height) - (6.8 * age);
            }
            else
            {
                value_BMR = 665 + (9.6 * weight) + (1.8 * height) - (4.7 * age);
            }

            switch(comboFactor.SelectedIndex)
            {
                case 0:
                    caloryResult.Text = ((int)(1.2 * value_BMR)).ToString() + "calories \n per day";
                    return;
                case 1:
                    caloryResult.Text = ((int)(1.375 * value_BMR)).ToString() + "calories \n per day";
                    return;
                case 2:
                    caloryResult.Text = ((int)(1.55 * value_BMR)).ToString() + "calories \n per day";
                    return;
                case 4:
                    caloryResult.Text = ((int)(1.725 * value_BMR)).ToString() + "calories \n per day";
                    return;
                case 5:
                    caloryResult.Text = ((int)(1.9 * value_BMR)).ToString() + "calories \n per day";
                    return;
            }
        }


    }


}