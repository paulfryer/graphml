using System.IO;
using System.Threading.Tasks;

namespace GraphML.Core
{
    public class LocalFileStore : IFileStore
    {

        public async Task SaveFile(string graphId, string folder, string fileName, string body)
        {
            if (!Directory.Exists(graphId))
                Directory.CreateDirectory(graphId);

            if (!Directory.Exists($"{graphId}/{folder}"))
                Directory.CreateDirectory($"{graphId}/{folder}");
            await File.WriteAllTextAsync($"{graphId}/{folder}/{fileName}", body);
        }
    }
}