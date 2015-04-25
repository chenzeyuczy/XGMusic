using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using XGMusic.DataConverter;

// “用户控件”项模板在 http://go.microsoft.com/fwlink/?LinkId=234236 上提供

namespace XGMusic.UI
{
    public sealed partial class AddFavPopup : UserControl
    {
        public AddFavPopup()
        {
            this.InitializeComponent();
        }

        public bool IsOpen
        {
            get { return MainPopup.IsOpen; }
            set
            {
                List<string> listSource = new List<string>();
                var listlib = (Application.Current as App).LibraryListData.LibList;
                for (int i = 1; i < listlib.Count; i++)
                {
                    if (listlib[i].Name != "临时列表")
                    {
                        listSource.Add(listlib[i].Name);
                    }
                }
                ComboLibrary.ItemsSource = listSource;
                if (listSource.Count >= 0)
                {
                    ComboLibrary.SelectedIndex = 0;
                }
                MainPopup.IsOpen = value;
            }
        }
        public MusicInfor Music { get; set; }

        private void BtnYes_Click_1(object sender, RoutedEventArgs e)
        {
            if (Music != null)
            {
                XGMusic.DataConverter.MusicInfor music = new DataConverter.MusicInfor(Music.Name) { Singer = Music.Singer, Title = Music.Title, ThumbnailImage = Music.ThumbnailImage, TotalTime = Music.Token };
                (Application.Current as App).LibraryListData.LibList[ComboLibrary.SelectedIndex + 1].ItemsList.Add(music);
                (Application.Current as App).LibraryListData.Save();
            }
            IsOpen = false;
        }
    }
}
