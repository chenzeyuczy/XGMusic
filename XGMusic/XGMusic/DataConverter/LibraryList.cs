using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;
using Windows.Storage;
using Lib = XGMusic.DataConverter.MusicLibrary<XGMusic.DataConverter.MusicInfor>;

namespace XGMusic.DataConverter
{
    public class LibraryList : IEnumerable<Lib>
    {
        static public LibraryList Load()
        {
            LibraryList libList = new LibraryList();
            var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile file = null;
            try
            {
                file = folder.GetFileAsync(FILE_NAME).AsTask<StorageFile>().Result;
            }
            catch (Exception ex)
            {
                LogOut.Log.LogOut(ex.Message);
                file = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/MusicLibraris.xml")).AsTask<StorageFile>().Result;
            }
            XDocument xdoc = null;
            using (var stream = file.OpenStreamForReadAsync().Result)
            {
                try
                {
                    xdoc = XDocument.Load(stream);
                }
                catch (Exception ex)
                {
                    xdoc = new XDocument();
                }
                stream.Dispose();
            }
            foreach (var lib in xdoc.Descendants().Elements(Element_LIB_NAME))
            {
                Lib tmpLib = new Lib();
                tmpLib.Name = (String)lib.Attribute("Name");
                foreach (var item in lib.Elements(Element_ITEM_NAME))
                {
                    MusicInfor music = new MusicInfor((String)item.Attribute("Name"))
                    {
                        TotalTime = (String)item.Attribute("TotalTime"),
                        Title = (String)item.Attribute("Title"),
                        Singer = (String)item.Attribute("Singer"),
                        ThumbnailImage = (String)item.Attribute("ThumbnailImage")
                    };
                    tmpLib.ItemsList.Add(music);
                }
                libList.LibList.Add(tmpLib);
            }
            return libList;
        }

        private const string FILE_NAME = "MusicLibraris.xml";
        private const string Element_LIB_NAME = "MusicLibrary";
        private const string Element_ITEM_NAME = "MusicInfor";

        private ObservableCollection<Lib> _libList = new ObservableCollection<Lib>();
        public ObservableCollection<Lib> LibList { get { return _libList; } }

        public void Save()
        {
            StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            using (var stream = folder.OpenStreamForWriteAsync(FILE_NAME, CreationCollisionOption.ReplaceExisting).Result)
            {
                XDocument xdoc = this.ToXDocument();
                xdoc.Save(stream);
            }
        }

        public XDocument ToXDocument()
        {
            XDocument xdoc = new XDocument();
            xdoc.Add(new XElement("MusicLibraris"));
            foreach (Lib lib in _libList)
            {
                XElement xlib = new XElement(Element_LIB_NAME,
                    new XAttribute("Name", lib.Name));
                foreach (MusicInfor music in lib)
                {
                    XElement xmusic = new XElement(Element_ITEM_NAME,
                        new XAttribute("Name", music.Name),
                        new XAttribute("TotalTime", music.TotalTime),
                        new XAttribute("Title", music.TotalTime),
                        new XAttribute("Singer", music.Singer),
                        new XAttribute("ThumbnailImage", music.ThumbnailImage));
                    xlib.Add(xmusic);
                }
                xdoc.Element("MusicLibraris").Add(xlib);
            }
            return xdoc;
        }

        public void AddLibrary(Lib item)
        {
            _libList.Add(item);
        }

        public void RemoveLibrary(Lib item)
        {
            _libList.Remove(item);
        }

        #region 枚举相关
        public IEnumerator<Lib> GetEnumerator()
        {
            return _libList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _libList.GetEnumerator();
        }
        #endregion
    }
}
