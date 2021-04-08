using System.Threading.Tasks;

namespace GraphML.Core
{
    public interface IFileStore
    {
        Task SaveFile(string graphId, string folder, string fileName, string body);
    }
}