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
using Lib = XGMusic.DataConverter.MusicLibrary<XGMusic.DataConverter.MusicInfor>;

// “用户控件”项模板在 http://go.microsoft.com/fwlink/?LinkId=234236 上提供

namespace XGMusic.UI
{
    public sealed partial class AddLibraryPopup : UserControl
    {
        public AddLibraryPopup()
        {
            this.InitializeComponent();
        }

        public bool IsOpen
        {
            get { return MainPopup.IsOpen; }
            set { MainPopup.IsOpen = value; }
        }

        private void BtnYes_Click_1(object sender, RoutedEventArgs e)
        {
            Lib lib = new Lib();
            lib.Name = TxtLibraryName.Text;
            (Application.Current as App).LibraryListData.LibList.Add(lib);
            IsOpen = false;
        }
    }
}
