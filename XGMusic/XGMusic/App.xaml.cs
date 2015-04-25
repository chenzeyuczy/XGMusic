using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using GalaSoft.MvvmLight.Threading;
using XGMusic.DataConverter;
using Windows.UI.ApplicationSettings;
using Windows.UI.Popups;
using Windows.Storage;
using Windows.Storage.FileProperties;
using XGMusic.Utility;
using Windows.UI.Xaml.Controls.Primitives;

namespace XGMusic
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.Resuming += App_Resuming;
        }

        void App_Resuming(object sender, object e)
        {
            LibraryListData.Save();

        }

        private LibraryList _libListData = null;
        public LibraryList LibraryListData { get { return _libListData; } set { _libListData = value; } }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            if (LibraryListData == null)
            {
                LibraryListData = LibraryList.Load();
            }
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
                DispatcherHelper.Initialize();
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            LibraryListData.Save();
            deferral.Complete();
        }

        protected async override void OnFileActivated(FileActivatedEventArgs args)
        {
            if (LibraryListData == null)
            {
                LibraryListData = LibraryList.Load();
            }

            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.Navigate(typeof(MainPage));
                Window.Current.Content = rootFrame;
                Window.Current.Activate();
            }

            MainPage page = rootFrame.Content as MainPage;

            foreach (var file in args.Files)
            {
                string token = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file);
                StorageFile musicFile = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync(token);
                MusicInfor music = new MusicInfor(file.Name, token);
                StorageItemThumbnail thumbnail = musicFile.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 1024u).AsTask<StorageItemThumbnail>().Result;
                if (!(thumbnail.OriginalWidth == thumbnail.OriginalHeight && thumbnail.OriginalHeight == 1024u))
                {
                    var thumbnailStream = thumbnail.CloneStream();
                    await PictureHelper.SavePicture(thumbnailStream, file.Name + ".jpg");
                    music.ThumbnailImage = string.Format("{0}.jpg", file.Name);
                }
                page.AddMusicInTempList(music);
            }
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            //注册设置面板
            SettingsPane.GetForCurrentView().CommandsRequested += App_CommandsRequested;
        }

        void App_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            SettingsCommand generalCommand = new SettingsCommand("About", "关于本播放器", FuncSettingCommandPopUp);
            args.Request.ApplicationCommands.Add(generalCommand);
            generalCommand = new SettingsCommand("SetBackground", "界面换肤", FuncSettingBackground);
            args.Request.ApplicationCommands.Add(generalCommand);
        }

        void FuncSettingBackground(IUICommand command)
        {
            MainPage page = (Window.Current.Content as Frame).Content as MainPage;
            page.SetBackground();
        }

        void FuncSettingCommandPopUp(IUICommand command)
        {
            var popup = new Popup();
            switch (command.Id.ToString())
            {
                case "About":
                    popup.Child = new XGMusic.UI.AboutPage();
                    break;
                default:
                    return;
            }
            WindowActivatedEventHandler activated = (s, e) => { if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated) { popup.IsOpen = false; } };
            popup.Opened += (s, e) => { Window.Current.Activated += activated; };
            popup.Closed += (s, e) => { Window.Current.Activated -= activated; };
            popup.IsLightDismissEnabled = true;
            popup.Width = 320;
            popup.Height = Window.Current.Bounds.Height;
            popup.HorizontalOffset = Window.Current.Bounds.Width - popup.Width;
            popup.VerticalOffset = 0;
            popup.IsOpen = true;

            var popupChild = popup.Child as Page;
            popupChild.Width = popup.Width;
            popupChild.Height = popup.Height;
        }
    }
}
