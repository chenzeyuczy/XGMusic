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

// “用户控件”项模板在 http://go.microsoft.com/fwlink/?LinkId=234236 上提供

namespace XGMusic.UI
{
    public sealed partial class MessagePopup : UserControl
    {
        string defTitle = "是否移出播放列表";
        string defText = "对不起, 没在音乐库中找到对应的文件, 是否从列表中移除?";
        public MessagePopup()
        {
            this.InitializeComponent();

            LabText.Text = defText;
            LabTitle.Text = defTitle;
        }

        public Button BtnYes { get { return btnYes; } }

        public bool IsOpen
        {
            get { return popup.IsOpen; }
            set { popup.IsOpen = value; }
        }

        public string Title
        {
            get
            {
                return LabTitle.Text;
            }
            set
            {
                LabTitle.Text = value;
            }
        }

        public string Text
        {
            get
            {
                return LabText.Text;
            }
            set
            {
                LabText.Text = value;
            }
        }
    }
}
