using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XGMusic.DataConverter
{
    /// <summary>
    /// 循环模式
    /// </summary>
    public enum LoopMode
    {
        Loop,
        OneTimeLoop,
        SingleLoop,
        Random
    }

    /// <summary>
    /// 音乐库
    /// </summary>
    public class MusicLibrary<T> : IEnumerable<T>, INotifyPropertyChanged
    {
        private ObservableCollection<T> _playList = new ObservableCollection<T>();
        public ObservableCollection<T> ItemsList { get { return _playList; } }
        private LoopMode _loopMode;
        public LoopMode LoopMode
        {
            get { return _loopMode; }
            set
            {
                _loopMode = value;
            }
        }

        public string Name { get; set; }
        public int Count { get { return _playList.Count; } }

        public MusicLibrary()
        {
            this.IndexChanged += MusicLibrary_IndexChanged;
        }

        void MusicLibrary_IndexChanged(object o, MusicLibrary<T>.IndexChangeEventArgs e)
        {
            OnPropertyChanged("CurrentOne");
        }

        public T CurrentOne
        {
            get { return GetCurrentOne(); }
        }

        #region 增删改
        //添加音乐
        public void AddMusic(T item)
        {
            _playList.Add(item);
            Index = _playList.Count - 1;
        }

        //插入音乐
        public void InsertMusic(int index, T item)
        {
            _playList.Insert(index, item);
        }

        //删除音乐
        public void Remove(int index)
        {
            _index--;
            _playList.RemoveAt(index);
        }

        public void Remove(T music)
        {
            _index--;
            _playList.Remove(music);
        }
        //清空播放列表
        public void Clear()
        {
            _playList.Clear();
        }

        private int _index = -1;
        public int Index
        {
            get { return _index; }
            set
            {
                int oldIndex = _index;
                _index = value;
                OnIndexChanged(_index, oldIndex);
                OnPropertyChanged("Index");
            }
        }

        public bool IsLast
        {
            get
            {
                if (GetNextOne() != null)
                    return false;
                else
                    return true;
            }
        }
        #endregion

        public bool IsFirst
        {
            get
            {
                if (GetForwardOne() != null)
                    return false;
                else
                    return true;
            }
        }

        #region 获得下一曲, 上一曲, 和当前正在播放的曲目
        public T GetNextOne()
        {
            int index = Index;
            if (index == -1)
            {
                return default(T);
            }
            switch (_loopMode)
            {
                case LoopMode.Loop:
                    if (index == _playList.Count - 1)
                        index = 0;
                    else
                    {
                        index = Index + 1;
                    }
                    break;
                case LoopMode.OneTimeLoop:
                    if (index == _playList.Count - 1)
                        return default(T);
                    else
                    {
                        index = Index + 1;
                    }
                    break;
                case LoopMode.Random:
                    Random r = new Random();
                    index = r.Next(0, _playList.Count);
                    break;
                case LoopMode.SingleLoop:
                    break;
            }
            Index = index;
            return _playList[index];
        }

        public T GetForwardOne()
        {
            int index = Index;
            if (index == -1)
            {
                return default(T);
            }
            switch (_loopMode)
            {
                case LoopMode.Loop:
                    if (index == 0)
                        index = _playList.Count - 1;
                    else
                    {
                        index = Index - 1;
                    }
                    break;
                case LoopMode.OneTimeLoop:
                    if (index == 0)
                        return default(T);
                    else
                    {
                        index = Index - 1;
                    }
                    break;
                case LoopMode.Random:
                    Random r = new Random();
                    index = r.Next(0, _playList.Count);
                    break;
                case LoopMode.SingleLoop:
                    break;
            }
            Index = index;
            return _playList[index];
        }

        public T GetCurrentOne()
        {
            if (Index == -1 || _playList.Count == 0)
            {
                return default(T);
            }
            else
            {
                return _playList[Index];
            }
        }
        #endregion

        public delegate void IndexChangedEventHandler(Object o, IndexChangeEventArgs e);
        public event IndexChangedEventHandler IndexChanged;
        private void OnIndexChanged(int newIndex, int oldIndex)
        {
            if (IndexChanged != null)
            {
                IndexChanged(this, new IndexChangeEventArgs(newIndex, oldIndex));
            }
        }
        /// <summary>
        /// Index变更之后的参数, 包含一个新的Index和旧的Index
        /// </summary>
        public class IndexChangeEventArgs : EventArgs
        {
            public readonly int NewIndex;
            public readonly int OldIndex;
            public IndexChangeEventArgs(int newIndex, int oldIndex)
            {
                this.NewIndex = newIndex;
                this.OldIndex = oldIndex;
            }
        }

        #region 枚举相关
        public IEnumerator<T> GetEnumerator()
        {
            return _playList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _playList.GetEnumerator();
        }
        #endregion

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
                }
            }
        }
    }
}
