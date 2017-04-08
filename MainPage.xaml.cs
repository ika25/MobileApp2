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

namespace TestOneDriveSdk_001
{
    public sealed partial class MainPage : Page
    {
        private readonly string[] _scopes =
        {
            "onedrive.readonly",
            "onedrive.appfolder",
            "wl.signin",
        };

        private IOneDriveClient _client;
        private string _savedId;
        private AccountSession _session;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void AuthenticateClick(object sender, RoutedEventArgs e)
        {
            ShowBusy(true);
            Exception error = null;

            try
            {
                _session = null;

                // Using the OnlineIdAuthenticator
                _client = OneDriveClientExtensions.GetClientUsingOnlineIdAuthenticator(
                    _scopes);

                // Using the WebAuthenticationBroker
                //_client = OneDriveClientExtensions.GetClientUsingWebAuthenticationBroker(
                //    "000000004C172C3F",
                //    _scopes);

                _session = await _client.AuthenticateAsync();
                Debug.WriteLine($"Token: {_session.AccessToken}");

                var dialog = new MessageDialog("You are authenticated!", "Success!");
                await dialog.ShowAsync();
                ShowBusy(false);
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                var dialog = new MessageDialog("Problem when authenticating!", "Sorry!");
                await dialog.ShowAsync();
                ShowBusy(false);
            }
        }

        private async void BrowseSubfolderClick(object sender, RoutedEventArgs e)
        {
            if (_client == null
                || _session?.AccessToken == null)
            {
                var dialog = new MessageDialog("Please authenticate first!", "Sorry!");
                await dialog.ShowAsync();
                return;
            }

            var path = FolderPathText.Text;

            if (string.IsNullOrEmpty(path))
            {
                var dialog = new MessageDialog(
                    "Please enter a path to a folder, for example Apps/OneDriveSample",
                    "Error!");
                await dialog.ShowAsync();
                return;
            }

            path = path.Replace("\\", "/");

            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            await GetFolder(_client.Drive.Root.ItemWithPath(path), true);
        }

        private async void DownloadFileClick(object sender, RoutedEventArgs e)
        {
            if (_client == null
                || _session?.AccessToken == null)
            {
                var dialog = new MessageDialog("Please authenticate first!", "Sorry!");
                await dialog.ShowAsync();
                return;
            }

            Exception error = null;
            Item foundFile = null;
            Stream contentStream = null;

            var path = DownloadFilePathText.Text;

            if (string.IsNullOrEmpty(path))
            {
                var dialog =
                    new MessageDialog(
                        "Please enter a path to an existing file, for example Apps/OneDriveSample/Test.jpg",
                        "Error!");
                await dialog.ShowAsync();
                return;
            }

            path = path.Replace("\\", "/");

            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            try
            {
                var request = _client.Drive.Root.ItemWithPath(path);
                foundFile = await request.Request().GetAsync();

                if (foundFile == null)
                {
                    var dialog = new MessageDialog($"Not found: {DownloadFilePathText.Text}");
                    await dialog.ShowAsync();
                    ShowBusy(false);
                    return;
                }

                contentStream = await request.Content.Request().GetAsync();

                if (contentStream == null)
                {
                    var dialog = new MessageDialog($"Content not found: {DownloadFilePathText.Text}");
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                var dialog = new MessageDialog(error.Message, "Error!");
                await dialog.ShowAsync();
                ShowBusy(false);
                return;
            }

            // Save the retrieved stream to the local drive

            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = foundFile.Name
            };

            var extension = Path.GetExtension(foundFile.Name);

            picker.FileTypeChoices.Add(
                $"{extension} files",
                new List<string>
                {
                    extension
                });

            var targetFile = await picker.PickSaveFileAsync();

            using (var targetStream = await targetFile.OpenStreamForWriteAsync())
            {
                using (var writer = new BinaryWriter(targetStream))
                {
                    contentStream.Position = 0;

                    using (var reader = new BinaryReader(contentStream))
                    {
                        byte[] bytes;

                        do
                        {
                            bytes = reader.ReadBytes(1024);
                            writer.Write(bytes);
                        }
                        while (bytes.Length == 1024);
                    }
                }
            }

            var successDialog = new MessageDialog("Done saving the file!", "Success");
            await successDialog.ShowAsync();
            ShowBusy(false);
        }

        private async void GetAppRootClick(object sender, RoutedEventArgs e)
        {
            if (_client == null
                || _session?.AccessToken == null)
            {
                var dialog = new MessageDialog("Please authenticate first!", "Sorry!");
                await dialog.ShowAsync();
                return;
            }

            await GetFolder(_client.Drive.Special.AppRoot, true);
        }

        private async Task GetFolder(IItemRequestBuilder builder, bool childrenToo)
        {
            ShowBusy(true);
            Exception error = null;
            IChildrenCollectionPage children = null;

            try
            {
                var root = await builder.Request().GetAsync();

                if (childrenToo)
                {
                    children = await builder.Children.Request().GetAsync();
                }

                DisplayHelper.ShowContent(
                    "SHOW FOLDER ++++++++++++++++++++++",
                    root,
                    children,
                    async message =>
                    {
                        var dialog = new MessageDialog(message);
                        await dialog.ShowAsync();
                    });

                ShowBusy(false);
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                var dialog = new MessageDialog(error.Message, "Error!");
                await dialog.ShowAsync();
                ShowBusy(false);
                return;
            }
        }

        private async void GetLinkClick(object sender, RoutedEventArgs e)
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
            ShowBusy(true);

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
                ShowBusy(false);
                return;
            }

            Debug.WriteLine("RETRIEVED LINK ---------------------");
            Debug.WriteLine(link.Link.WebUrl);
            var successDialog = new MessageDialog(
                $"The link was copied to the Debug window: {link.Link.WebUrl}",
                "No file saved");
            await successDialog.ShowAsync();
            ShowBusy(false);
        }

        private async void GetRootFolderClick(object sender, RoutedEventArgs e)
        {
            if (_client == null
                || _session?.AccessToken == null)
            {
                var dialog = new MessageDialog("Please authenticate first!", "Sorry!");
                await dialog.ShowAsync();
                return;
            }

            await GetFolder(_client.Drive.Root, true);
        }

        private async void LogOffClick(object sender, RoutedEventArgs e)
        {
            if (_client == null
                || _session?.AccessToken == null)
            {
                var dialog = new MessageDialog("Please authenticate first!", "Sorry!");
                await dialog.ShowAsync();
                return;
            }

            Exception error = null;
            ShowBusy(true);

            try
            {
                await _client.SignOutAsync();
            }
            catch (Exception ex)
            {
                error = ex;
            }

            if (error != null)
            {
                var dialog = new MessageDialog(error.Message, "Error!");
                await dialog.ShowAsync();
                ShowBusy(false);
                return;
            }

            _session = null;
            _savedId = null;
            _client = null;

            var successDialog = new MessageDialog("You are now logged off", "Success");
            await successDialog.ShowAsync();
            ShowBusy(false);
        }

        private void ShowBusy(bool isBusy)
        {
            Progress.IsActive = isBusy;
            PleaseWaitCache.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void UploadFileClick(object sender, RoutedEventArgs e)
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
                ShowBusy(true);

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

                        // Save for the GetLink demo
                        _savedId = item.Id;

                        var successDialog =
                            new MessageDialog(
                                $"Uploaded file has ID {item.Id}. You can now use the Get Link button to retrieve a direct link to the file",
                                "Success");
                        await successDialog.ShowAsync();
                    }

                    ShowBusy(false);
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                if (error != null)
                {
                    var dialog = new MessageDialog(error.Message, "Error!");
                    await dialog.ShowAsync();
                    ShowBusy(false);
                }
            }
        }

        private async void MoveClick(object sender, RoutedEventArgs e)
        {
            if (_client == null
                || _session?.AccessToken == null)
            {
                var dialog = new MessageDialog("Please authenticate first!", "Sorry!");
                await dialog.ShowAsync();
                return;
            }

            // TODO error handling and progress

            var newLocation = await _client.Drive.Root.Request().GetAsync();

            var updateItem = new Item
            {
                ParentReference = new ItemReference
                {
                    Id = newLocation.Id
                }
            };

            var itemWithUpdates = await _client
                .Drive
                .Items[_savedId]
                .Request()
                .UpdateAsync(updateItem);
        }

        private async void RenameClick(object sender, RoutedEventArgs e)
        {
            if (_client == null
                || _session?.AccessToken == null)
            {
                var dialog = new MessageDialog("Please authenticate first!", "Sorry!");
                await dialog.ShowAsync();
                return;
            }

            // TODO error handling and progress

            var updateItem = new Item
            {
                Name = NewFileNameText.Text
            };

            var itemWithUpdates = await _client
                .Drive
                .Items[_savedId]
                .Request()
                .UpdateAsync(updateItem);
        }

        private async void CopyClick(object sender, RoutedEventArgs e)
        {
            if (_client == null
                || _session?.AccessToken == null)
            {
                var dialog = new MessageDialog("Please authenticate first!", "Sorry!");
                await dialog.ShowAsync();
                return;
            }

            // TODO error handling and progress

            var newLocation = await _client.Drive.Root.Request().GetAsync();
            
            // Get the file to access its meta info
            var file = await _client.Drive.Items[_savedId].Request().GetAsync();
            var newName = Path.GetFileNameWithoutExtension(file.Name)
                + "-"
                + DateTime.Now.Ticks
                + Path.GetExtension(file.Name);

            var itemStatus = await _client
                .Drive
                .Items[_savedId]
                .Copy(
                    newName,
                    new ItemReference
                    {
                        Id = newLocation.Id
                    })
                .Request()
                .PostAsync();

            // TODO restore when bug is fixed
            var newItem = await itemStatus.CompleteOperationAsync(
                null,
                CancellationToken.None);

            var successDialog = new MessageDialog(
                $"The item has been copied with ID {newItem.Id}", 
                "Done!");
            await successDialog.ShowAsync();
        }

        private async void NewFolderClick(object sender, RoutedEventArgs e)
        {
            if (_client == null
                || _session?.AccessToken == null)
            {
                var dialog = new MessageDialog("Please authenticate first!", "Sorry!");
                await dialog.ShowAsync();
                return;
            }

            // TODO error handling and progress

            var newFolder = new Item
            {
                Name = NewFolderNameText.Text,
                Folder = new Folder()
            };

            var newFolderCreated = _client.Drive
                .Special.AppRoot
                .Children
                .Request()
                .AddAsync(newFolder);

            var successDialog = new MessageDialog(
                $"The folder has been created with ID {newFolderCreated.Id}",
                "Done!");
            await successDialog.ShowAsync();
        }
    }
}