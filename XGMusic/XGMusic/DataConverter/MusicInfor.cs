using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XGMusic.DataConverter
{
    public class MusicInfor : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                try
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
                catch (Exception ex)
                {
                    // TODO: 例外処理
                    LogOut.Log.LogOut(ex.Message);
                }
            }
        }

        public MusicInfor(string name, string token)
        {
            this.Name = name;
            this.Token = token;
        }

        public MusicInfor(string name)
        {
            this.Name = name;
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        private string _totalTime = "";
        public string TotalTime
        {
            get { return _totalTime; }
            set
            {
                _totalTime = value;
                OnPropertyChanged("TotalTime");
            }
        }

        private string _token = "{00000000-0000-0000-0000-00000000}";
        public string Token
        {
            get { return _token; }
            set
            {
                _token = value;
                OnPropertyChanged("Token");
            }
        }

        private string _title = "";
        public string Title
        {
            get
            {
                if (_title == "")
                {
                    return Name;
                }
                else
                {
                    return _title;
                }
            }
            set
            {
                _title = value;
                OnPropertyChanged("Title");
            }
        }

        private string _singer = "Unknown";
        public string Singer
        {
            get
            {
                return _singer;
            }
            set
            {
                _singer = value;
                OnPropertyChanged("Singer");
            }
        }

        private string _thumbnailImage = "";
        public string ThumbnailImage
        {
            get { return _thumbnailImage; }
            set
            {
                _thumbnailImage = value;
                OnPropertyChanged("ThumbnailImage");
            }
        }
    }
}
