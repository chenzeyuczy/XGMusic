using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using XGMusic.DataConverter;
using XGMusic.FormatConverter;
using XGMusic.Utility;
using XGMusic.ViewModel;

namespace XGMusic
{
    public sealed partial class MainPage
    {
        private StorageFile storage;

        /// <summary>
        /// Gets the view's ViewModel.
        /// </summary>
        public MainViewModel Vm
        {
            get
            {
                return (MainViewModel)DataContext;
            }
        }

        public MainPage()
        {
            InitializeComponent();

            //数据载入
            _libList = (Application.Current as App).LibraryListData;
            _musicLib = _libList.LibList.First();// as MusicLibrary<MusicInfor>;
            //_libList = LibraryList.Load();
            ListViewLibs.ItemsSource = _libList.LibList;
            ListViewLibs.SelectedIndex = 0;

            //后台播放相关
            MediaControl.PlayPauseTogglePressed += MediaControl_PlayPauseTogglePressed;
            MediaControl.PlayPressed += MediaControl_PlayPressed;
            MediaControl.PausePressed += MediaControl_PausePressed;
            MediaControl.StopPressed += MediaControl_StopPressed;

            //滑块相关
            TimelineSlider.ValueChanged += timelineSlider_ValueChanged;
            TimelineSlider.PointerPressed += slider_PointerPressed;
            TimelineSlider.PointerCaptureLost += slider_PointerCaptureLost;

            //音乐库相关
            _musicLib.IndexChanged += _musicLib_IndexChanged;
            //音量初始化
            MusicPlayer.Volume = SliderVolume.Value;
            MusicPlayer.IsMuted = ButtonIsMuted.IsChecked == true ? true : false;

            PopupMsg.BtnYes.Click += BtnYes_Click;
            this.Tapped += MainPage_Tapped;

            //缩略图
            ImageThumbnail.SetBinding(Image.SourceProperty, new Binding() { Source = _musicLib, Path = new PropertyPath("CurrentOne"), Converter = new MusicInforToImageSource() });

            //背景
            NormalPanel.Background = new ImageBrush() { ImageSource = MainPageBackground, Stretch = Stretch.UniformToFill };
            var file = ApplicationData.Current.LocalFolder.CreateFileAsync("Background.png", CreationCollisionOption.OpenIfExists).AsTask<StorageFile>().Result;
            using (var stream = file.OpenAsync(FileAccessMode.Read).AsTask<IRandomAccessStream>().Result)
            {
                MainPageBackground.SetSource(stream);
                //stream.Dispose();
            }

            //snap
            this.SizeChanged += MainPage_SizeChanged;
            DataTransferManager.GetForCurrentView().DataRequested += MainPage_DataRequested;
        }

        protected override void LoadState(object state)
        {
            var casted = state as MainPageState;

            if (casted != null)
            {
                Vm.Load(casted.LastVisit);
            }
        }

        protected override object SaveState()
        {
            return new MainPageState
            {
                LastVisit = DateTime.Now
            };
        }

        #region

        //private ObservableCollection<string> _fileName = new ObservableCollection<string>();
        private MusicLibrary<MusicInfor> _musicLib = null;//new MusicLibrary<MusicInfor>();
        private LibraryList _libList = null;
       
        void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            switch (Windows.UI.ViewManagement.ApplicationView.Value)
            {
                case Windows.UI.ViewManagement.ApplicationViewState.Filled:
                    NormalPanel.ColumnDefinitions[1].Width = new GridLength(0);
                    NormalPanel.ColumnDefinitions[2].Width = new GridLength(0);
                    break;
                case Windows.UI.ViewManagement.ApplicationViewState.Snapped:
                    NormalPanel.ColumnDefinitions[1].Width = new GridLength(0);
                    NormalPanel.ColumnDefinitions[2].Width = new GridLength(330);
                    break;
                default:
                    NormalPanel.ColumnDefinitions[1].Width = new GridLength(150);
                    NormalPanel.ColumnDefinitions[2].Width = new GridLength(390);
                    break;
            }
        }

        private BitmapImage MainPageBackground = new BitmapImage();

        private Windows.UI.Xaml.Media.Imaging.BitmapImage DefaultThumbnailImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/Images/DefaultThumbnailImage.jpg"));

        private void _musicLib_IndexChanged(object o, MusicLibrary<MusicInfor>.IndexChangeEventArgs e)
        {
            var music = _musicLib.GetCurrentOne();
            if (ListViewMusics.ItemsSource == _musicLib.ItemsList)
            {
                ListViewMusics.SelectedItem = music; //e.NewIndex == -1 ? 0 : e.NewIndex;
                ListViewMusics.ScrollIntoView(ListViewMusics.SelectedItem);
            }
            if (music != null)
            {
                LoadMusicStream(music);
                MusicPlayer.Play();
            }
            //if (e.NewIndex >= ListViewMusics.Items.Count )
            //{
            //    ListViewMusics.SelectedIndex = ListViewMusics.Items.Count - 1;
            //}else
            //{
            //    ListViewMusics.SelectedIndex = e.NewIndex;
            //}
        }

        private MusicInfor CurrentMusic { get; set; }
        public void LoadMusicStream(MusicInfor music)
        {
            if (music == null)
            {
                return;
            }
            if (music == CurrentMusic && !IsMediaFailed)
                return;
            CurrentMusic = music;
            try
            {
                StorageFile file = null;
                try
                {
                    file = KnownFolders.MusicLibrary.GetFileAsync(music.Name).AsTask<StorageFile>().Result;
                }
                catch (Exception ex)
                {
                    LogOut.Log.LogOut(ex.Message);
                    file = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync(music.Token).AsTask<StorageFile>().Result;                    
                }
                using (var stream = file.OpenAsync(FileAccessMode.Read).AsTask<IRandomAccessStream>().Result)
                {
                    var props = file.Properties.GetMusicPropertiesAsync().AsTask<Windows.Storage.FileProperties.MusicProperties>().Result;
                    TimeSpan time = props.Duration;
                    music.Title = props.Title;
                    music.TotalTime = string.Format("{0:00}:{1:00}", time.Minutes, time.Seconds);
                    music.Singer = props.Artist;
                    MusicPlayer.SetSource(stream, "");
                    stream.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogOut.Log.LogOut(ex.Message);
                MusicPlayer.Stop();
                ShowPopup(ButtonPlay, "是否移出播放列表", "对不起, 没在音乐库中找到对应的文件, 是否从列表中移除?");
            }
        }

        #region 后台播放
        //后台播放
        void MediaControl_StopPressed(object sender, object e)
        {
            MusicPlayer.Stop();
        }

        void MediaControl_PausePressed(object sender, object e)
        {
            MusicPlayer.Pause();
        }

        void MediaControl_PlayPressed(object sender, object e)
        {
            MusicPlayer.Play();
        }

        void MediaControl_PlayPauseTogglePressed(object sender, object e)
        {
            if (MediaControl.IsPlaying == true)
            {
                MusicPlayer.Pause();
            }
            else
            {
                MusicPlayer.Play();
            }
        }
        #endregion

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //StorageFile file = e.Parameter as StorageFile;
            //if (file != null)
            //{
            //    AddAndPlayMusic(file);
            //}
            ButtonPlay.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }

        public void AddMusic(MusicInfor file)
        {
            _musicLib.ItemsList.Add(file);
            _libList.Save();
        }

        public void AddMusicInTempList(string name, string token)
        {
            MusicInfor file = new MusicInfor(name, token);
            ListViewLibs.SelectedIndex = 0;
            _musicLib.ItemsList.Add(file);
            _libList.Save();
        }
        public void AddMusicInTempList(MusicInfor file)
        {
            ListViewLibs.SelectedIndex = 0;
            _musicLib.ItemsList.Add(file);
            _libList.Save();
        }

        #region 按钮按下
        private void ButtonPlay_Click_1(object sender, RoutedEventArgs e)
        {
            if (_musicLib.Index == -1)
            {
                ButtonPlay.IsChecked = false;
            }

            switch (MusicPlayer.CurrentState)
            {
                case MediaElementState.Playing:
                    MusicPlayer.Pause();
                    break;
                case MediaElementState.Paused:
                case MediaElementState.Stopped:
                    MusicPlayer.Play();
                    break;
                default:
                    LoadMusicStream(_musicLib.GetCurrentOne());
                    MusicPlayer.Play();
                    break;
            }
        }

        private void ButtonStop_Click_1(object sender, RoutedEventArgs e)
        {
            MusicPlayer.Stop();
            //ButtonPlay.IsChecked = false;
        }

        private void ButtonNext_Click_1(object sender, RoutedEventArgs e)
        {
            if (_musicLib.Index == -1)
            {
                return;
            }
            var music = _musicLib.GetNextOne();
            if (music != null)
            {
                LoadMusicStream(music);
                MusicPlayer.Play();
            }
        }

        private void ButtonBack_Click_1(object sender, RoutedEventArgs e)
        {
            if (_musicLib.Index == -1)
            {
                return;
            }
            var music = _musicLib.GetForwardOne();
            if (music != null)
            {
                LoadMusicStream(music);
                MusicPlayer.Play();
            }
        }

        //打开文件按钮
        private async void ButtonOpenFiles_Click_1(object sender, RoutedEventArgs e)
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            openPicker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
            openPicker.FileTypeFilter.Add(".mp3");
            openPicker.FileTypeFilter.Add(".mp4");
            openPicker.FileTypeFilter.Add(".wmv");
            try
            {
                Windows.UI.ViewManagement.ApplicationView.TryUnsnap();
                var files = await openPicker.PickMultipleFilesAsync();
                foreach (var file in files)
                {
                    string token = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file);
                    MusicInfor music = new MusicInfor(file.Name, token);
                    StorageItemThumbnail thumbnail = file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 1024u).AsTask<StorageItemThumbnail>().Result;
                    if (!(thumbnail.OriginalWidth == thumbnail.OriginalHeight && thumbnail.OriginalHeight == 1024u))
                    {
                        var thumbnailStream = thumbnail.CloneStream();
                        await PictureHelper.SavePicture(thumbnailStream, file.Name + ".jpg");
                        music.ThumbnailImage = string.Format("{0}.jpg", file.Name);
                    }
                    this.AddMusicInTempList(music);
                }
            }
            catch (Exception ex)
            {
                LogOut.Log.LogOut(ex.Message);
            }
        }
        #endregion

        /// <summary>
        /// 当选择框的循环模式变更之后
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboLoopMode_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ComboLoopMode.SelectedItem != null)
                {
                    switch (ComboLoopMode.SelectedIndex)
                    {
                        case 0:
                            _musicLib.LoopMode = LoopMode.Loop;
                            break;
                        case 1:
                            _musicLib.LoopMode = LoopMode.OneTimeLoop;
                            break;
                        case 2:
                            _musicLib.LoopMode = LoopMode.Random;
                            break;
                        case 3:
                            _musicLib.LoopMode = LoopMode.SingleLoop;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogOut.Log.LogOut(ex.Message);
            }
        }

        #region 定时器相关
        private DispatcherTimer _timer;

        private void SetupTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(TimelineSlider.StepFrequency);
            StartTimer();
        }

        private void _timer_Tick(object sender, object e)
        {
            if (!_sliderpressed)
            {
                TimelineSlider.Value = MusicPlayer.Position.TotalSeconds;
                LabTime.Text = string.Format("{0:00}:{1:00}", MusicPlayer.Position.Minutes, MusicPlayer.Position.Seconds);
            }
        }

        private void StartTimer()
        {
            _timer.Tick += _timer_Tick;
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer.Stop();
            _timer.Tick -= _timer_Tick;
        }
        #endregion

        #region 滑动条相关
        private bool _sliderpressed = false;

        void slider_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _sliderpressed = true;
        }

        void slider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            MusicPlayer.Position = TimeSpan.FromSeconds(TimelineSlider.Value);
            _sliderpressed = false;
        }

        void timelineSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!_sliderpressed)
            {
                MusicPlayer.Position = TimeSpan.FromSeconds(e.NewValue);
            }

            //_player.Position = TimeSpan.FromSeconds(e.NewValue);
        }
        #endregion

        #region MediaElement事件
        void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            //音量初始化
            MusicPlayer.Volume = SliderVolume.Value;
            MusicPlayer.IsMuted = ButtonIsMuted.IsChecked == true ? true : false;

            double absvalue = (int)Math.Round(
                MusicPlayer.NaturalDuration.TimeSpan.TotalSeconds,
                MidpointRounding.AwayFromZero);

            TimelineSlider.Maximum = absvalue;

            TimelineSlider.StepFrequency = 0.2;
            //SliderFrequency(MusicPlayer.NaturalDuration.TimeSpan);

            SetupTimer();

            LabMusicName.Text = string.Format("{0}", _musicLib.GetCurrentOne().Title);
            LabMusicSinger.Text = string.Format("歌手: {0}", _musicLib.GetCurrentOne().Singer);
            //缩略图
            //var thumbnail = new BitmapImage(new Uri(_musicLib.GetCurrentOne().ThumbnailImage));
        }

        void OnCurrentStateChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MusicPlayer.CurrentState == MediaElementState.Playing)
                {
                    if (_sliderpressed)
                    {
                        _timer.Stop();
                    }
                    else
                    {
                        _timer.Start();
                    }
                    // 播放按钮的样式
                    ButtonPlay.IsChecked = true;
                }

                if (MusicPlayer.CurrentState == MediaElementState.Paused)
                {
                    _timer.Stop();
                    // 播放按钮的样式
                    ButtonPlay.IsChecked = false;
                }

                if (MusicPlayer.CurrentState == MediaElementState.Stopped)
                {
                    _timer.Stop();
                    TimelineSlider.Value = 0;
                    // 播放按钮的样式
                    ButtonPlay.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                LogOut.Log.LogOut(ex.Message);
            }
        }

        void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            StopTimer();
            TimelineSlider.Value = 0.0;
            LabTime.Text = "00:00";
            ButtonNext_Click_1(null, null);
        }

        private bool _isMediaFailed = false;
        public bool IsMediaFailed
        {
            get
            {
                if (_isMediaFailed)
                {
                    _isMediaFailed = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            set { _isMediaFailed = value; }
        }
        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            // get HRESULT from event args 
            IsMediaFailed = true;
            ButtonPlay_Click_1(null, null);
        }
        #endregion

        #region 音量控制
        private void SliderVolume_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (MusicPlayer != null)
            {
                MusicPlayer.Volume = SliderVolume.Value / 100d;
            }
        }

        private void ButtonIsMuted_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (MusicPlayer != null)
            {
                MusicPlayer.IsMuted = ButtonIsMuted.IsChecked == true ? true : false;
            }
        }
        #endregion

        //Page上的键盘按下
        private void Page_KeyUp_1(object sender, KeyRoutedEventArgs e)
        {
            //如果是循环列表的按键路由
            if (e.OriginalSource == ComboLoopMode)
            {
                e.Handled = true;
                return;
            }

            switch (e.Key)
            {
                case Windows.System.VirtualKey.Up:
                    SliderVolume.Value++;
                    SliderVolume.Focus(Windows.UI.Xaml.FocusState.Keyboard);
                    break;
                case Windows.System.VirtualKey.Down:
                    SliderVolume.Value--;
                    SliderVolume.Focus(Windows.UI.Xaml.FocusState.Keyboard);
                    break;
                case Windows.System.VirtualKey.Left:
                    TimelineSlider.Value--;
                    TimelineSlider.Focus(Windows.UI.Xaml.FocusState.Keyboard);
                    break;
                case Windows.System.VirtualKey.Right:
                    TimelineSlider.Value++;
                    TimelineSlider.Focus(Windows.UI.Xaml.FocusState.Keyboard);
                    break;
                default:
                    break;
            }
            e.Handled = true;
        }

        private void ListViewLibs_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            //必须有一项处于选中状态
            if (e.AddedItems.Count == 0 && e.RemovedItems.Count >= 0)
            {
                ListViewLibs.SelectedItem = e.RemovedItems.First();
            }

            if (ListViewLibs.SelectedItem != null)
            {
                //_musicLib = ListViewLibs.SelectedItem as MusicLibrary<MusicInfor>;
                ListViewMusics.ItemsSource = (ListViewLibs.SelectedItem as MusicLibrary<MusicInfor>).ItemsList;

                if (ListViewLibs.SelectedItem == _musicLib)
                {
                    ListViewMusics.SelectedIndex = _musicLib.Index;
                }
                //显示删除按钮
                if (ListViewLibs.SelectedIndex != 0)//选中项不是临时列表的时候, 可以删除
                {
                    BtnRemoveLib.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    BtnAddMusic.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
                else//选中项为临时列表的时候, 不可以删除, Music可以追加到列表中
                {
                    BtnRemoveLib.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    BtnAddMusic.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
            }
        }

        private void ListViewMusics_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            //必须有一项处于选中状态
            if (e.AddedItems.Count == 0 && e.RemovedItems.Count >= 0)
            {
                ListViewMusics.SelectedItem = e.RemovedItems.First();
            }

            if (ListViewMusics.SelectedItem != null)
            {
                //if (ListViewLibs.SelectedItem == _musicLib)
                //{
                _musicLib = ListViewLibs.SelectedItem as MusicLibrary<MusicInfor>;
                _musicLib.IndexChanged += _musicLib_IndexChanged;
                ImageThumbnail.SetBinding(Image.SourceProperty, new Binding() { Source = _musicLib, Path = new PropertyPath("CurrentOne"), Converter = new MusicInforToImageSource() });
                _musicLib.Index = ListViewMusics.SelectedIndex;
                ListViewMusics.ScrollIntoView(_musicLib.GetCurrentOne());
                //}
                //显示删除按钮
                //BtnRemoveMusic.Visibility = Windows.UI.Xaml.Visibility.Visible;
                //打开AppBar
                //BottomAppBar.IsOpen = true;
            }
            else
            {
                //隐藏删除按钮
                //BtnRemoveMusic.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        #region 增删曲目/库
        private void BtnRemoveLib_Click_1(object sender, RoutedEventArgs e)
        {
            _libList.LibList.Remove(ListViewLibs.SelectedItem as MusicLibrary<MusicInfor>);
            _libList.Save();
        }

        //从播放列表移除
        private void BtnRemoveMusic_Click_1(object sender, RoutedEventArgs e)
        {
            MusicInfor music = (sender as Button).DataContext as MusicInfor;
            if (_musicLib.GetCurrentOne() == music)
            {
                if (_musicLib.Index>-1)
                    _musicLib.Index = -1;
                MusicPlayer.Stop();
                ButtonPlay.IsChecked = false;
                MusicPlayer.Source = null;
            }
            _musicLib.Remove(music);
            _libList.Save();
        }

        private void BtnAddLib_Click_1(object sender, RoutedEventArgs e)
        {
            Windows.UI.Xaml.Media.GeneralTransform buttonTransform = (sender as FrameworkElement).TransformToVisual(null);
            Windows.Foundation.Point point = buttonTransform.TransformPoint(new Point());
            PopupAddLib.SetValue(Canvas.LeftProperty, point.X - 150);
            PopupAddLib.SetValue(Canvas.TopProperty, point.Y - 120);
            PopupAddLib.IsOpen = true;
        }
        #endregion

        #region popup显示提示信息
        void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            _musicLib.Remove(ListViewMusics.SelectedIndex);//ItemsList.Remove(ListViewMusics.SelectedItem as MusicInfor);
            _libList.Save();
            PopupMsg.IsOpen = false;
        }

        public void ShowPopup(FrameworkElement element, string title, string msg)
        {
            PopupMsg.Title = title;
            PopupMsg.Text = msg;
            Windows.UI.Xaml.Media.GeneralTransform buttonTransform = element.TransformToVisual(null);
            Windows.Foundation.Point point = buttonTransform.TransformPoint(new Point());
            PopupMsg.SetValue(Canvas.LeftProperty, point.X - 130);
            PopupMsg.SetValue(Canvas.TopProperty, point.Y - 220);
            PopupMsg.IsOpen = true;
            PopupAudio.Play();
        }
        public void ShowPopup(FrameworkElement element, string title, string msg, int hOffset)
        {
            PopupMsg.Title = title;
            PopupMsg.Text = msg;
            Windows.UI.Xaml.Media.GeneralTransform buttonTransform = element.TransformToVisual(null);
            Windows.Foundation.Point point = buttonTransform.TransformPoint(new Point());
            PopupMsg.SetValue(Canvas.LeftProperty, point.X + hOffset);
            PopupMsg.SetValue(Canvas.TopProperty, point.Y - 220);
            PopupMsg.IsOpen = true;
            PopupAudio.Play();
        }

        void MainPage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _point = e.GetPosition(this);
        }

        private Point _point = new Point();
        #endregion

        //播放歌曲
        private void BtnItemPlay_Click_1(object sender, RoutedEventArgs e)
        {
            MusicInfor music = (sender as FrameworkElement).DataContext as MusicInfor;
            ListViewMusics.SelectedItem = music;
            LoadMusicStream(music);
            MusicPlayer.Play();
        }

        //导出缩略图
        private async void BtnSaveAsThumbnail_Click_1(object sender, RoutedEventArgs e)
        {
            MusicInfor music = _musicLib.GetCurrentOne();
            if (music != null)
            {
                string thumbnailPath = music.ThumbnailImage;
                if (string.IsNullOrEmpty(thumbnailPath))
                {
                    thumbnailPath = string.Format("ms-appx:///Assets/Images/DefaultThumbnailImage.jpg");
                }
                else
                {
                    thumbnailPath = string.Format("ms-appdata:///local/Images/{0}", thumbnailPath);
                }
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(thumbnailPath));
                if (file == null)
                    return;
                PictureHelper.SaveAsPicture(file);
            }
        }

        private void BtnPlay_Click_1(object sender, RoutedEventArgs e)
        {
            if (ListViewMusics.SelectedItem == null)
            {
                ListViewMusics.SelectedIndex = 0;
            }
            LoadMusicStream(ListViewMusics.SelectedItem as MusicInfor);
            MusicPlayer.Play();
        }

        ////更改缩略图
        private async void BtnChangeThumbnail_Click_1(object sender, RoutedEventArgs e)
        {
            MusicInfor music = ListViewMusics.SelectedItem as MusicInfor;
            //<---
            //music.ThumbnailImage = "";//"ms-appx:///Assets/Images/DefaultThumbnailImage.jpg";
            //if (_musicLib.GetCurrentOne() == music)
            //{
            //    ImageThumbnail.Source = DefaultThumbnailImage;
            //}
            //--->
            if (music != null)
            {
                try
                {
                    var picker = new Windows.Storage.Pickers.FileOpenPicker();
                    picker.FileTypeFilter.Add(".jpg");
                    picker.FileTypeFilter.Add(".png");
                    picker.FileTypeFilter.Add(".bmp");
                    picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                    StorageFile file = await picker.PickSingleFileAsync();
                    if (file == null)
                        return;
                    else
                    {
                        string newImageName = music.Name + file.FileType;
                        var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Images", CreationCollisionOption.OpenIfExists);

                        await file.CopyAsync(folder, newImageName, NameCollisionOption.ReplaceExisting);
                        music.ThumbnailImage = string.Format("{0}", newImageName);
                        ImageThumbnail.SetBinding(Image.SourceProperty, new Binding() { Source = _musicLib, Path = new PropertyPath("CurrentOne"), Converter = new MusicInforToImageSource() });
                    }
                }
                catch (Exception ex)
                {
                    LogOut.Log.LogOut(ex.Message);
                    PopupAudio.Stop();
                    PopupAudio.Play();
                }
            }
        }

        public async void SetBackground()
        {
            string background = "Background.png";
            //string backgroundSetted = "BackgroundSetted.png";
            await PictureHelper.CopyPicture(null, background);
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(background, CreationCollisionOption.OpenIfExists);
            using (IRandomAccessStream bkStream = await file.OpenAsync(FileAccessMode.Read))
            {
                var image = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                //image.CreateOptions = Windows.UI.Xaml.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                image.SetSource(bkStream);


                NormalPanel.Background = new ImageBrush() { ImageSource = image, Stretch = Stretch.UniformToFill };
                //StorageFile outFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(backgroundSetted, CreationCollisionOption.OpenIfExists);
                //await file.CopyAndReplaceAsync(outFile);
                //IRandomAccessStream bkSettedStream = await outFile.OpenAsync(FileAccessMode.Read);
                //image.SetSource(bkStream);
                bkStream.Dispose();
                //bkSettedStream.Dispose();
            }
        }

        private void BtnAddFav_Click_1(object sender, RoutedEventArgs e)
        {
            MusicInfor music = (sender as Button).DataContext as MusicInfor;
            if (music == null)
            {
                return;
            }
            CurrentMusic = music;
            try
            {
                StorageFile file = KnownFolders.MusicLibrary.GetFileAsync(music.Name).AsTask<StorageFile>().Result;
            }
            catch (Exception ex)
            {
                LogOut.Log.LogOut(ex.Message);
                ShowPopup(sender as FrameworkElement, "是否移出播放列表", "对不起, 非音乐库文件无法追加到个人列表中, 是否从临时列表中移除?");
                return;
            }
            Windows.UI.Xaml.Media.GeneralTransform buttonTransform = (sender as FrameworkElement).TransformToVisual(null);
            Windows.Foundation.Point point = buttonTransform.TransformPoint(new Point());
            PopupAddFav.SetValue(Canvas.LeftProperty, point.X - 180);
            PopupAddFav.SetValue(Canvas.TopProperty, point.Y - 105);
            PopupAddFav.Music = music;
            PopupAddFav.IsOpen = true;
        }

        private void BtnChangeBackground_Click_1(object sender, RoutedEventArgs e)
        {
            SetBackground();
        }

        #endregion

        private void btn_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb == null)
                return;
            switch (cb.SelectedIndex)
            {
                case 0:
                    break;
                case 1:
                    BtnAddFav_Click_1(null, null);
                    break;
                case 2:
                    Shared_Click(null, null);
                    break;
            }
        }

        private void Shared_Click(object sender, RoutedEventArgs e)
        {
            //添加文件
            MusicInfor music = (sender as FrameworkElement).DataContext as MusicInfor;
            StorageFile file = null;
            try
            {
                file = KnownFolders.MusicLibrary.GetFileAsync(music.Name).AsTask<StorageFile>().Result;
            }
            catch (Exception ex)
            {
                LogOut.Log.LogOut(ex.Message);
                file = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync(music.Token).AsTask<StorageFile>().Result;
            }
            this.storage = file ;
            DataTransferManager.ShowShareUI();
        }

        /// <summary>
        /// 分享
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void MainPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var defl = args.Request.GetDeferral();
            // 设置数据包
            DataPackage dp = new DataPackage();
            dp.Properties.Title = "名称 :" + this.storage.Name;
            dp.Properties.Description = "创建日期: " + this.storage.DateCreated + "  音乐位置 :" + this.storage.Path;
            dp.SetStorageItems(new List<StorageFile> {this.storage});
            args.Request.Data = dp;
            // 报告操作完成
            defl.Complete();
        }

        private void TimelineSlider_ValueChanged_1(object sender, RangeBaseValueChangedEventArgs e)
        {

        }

        private void TimelineSlider_ValueChanged_2(object sender, RangeBaseValueChangedEventArgs e)
        {

        }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }
    }

    public class MainPageState
    {
        public DateTime LastVisit
        {
            get;
            set;
        }
    }
}