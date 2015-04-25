using System.Threading.Tasks;

namespace XGMusic.Model
{
    public interface IDataService
    {
        Task<DataItem> GetData();
    }
}