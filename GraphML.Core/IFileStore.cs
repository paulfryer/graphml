using System.Threading.Tasks;

namespace GraphML.Core
{
    public interface IFileStore
    {
        Task SaveFile(string fileName, string prefix, string body);
    }
}