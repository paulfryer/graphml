using System.IO;
using System.Threading.Tasks;

namespace GraphML.Core
{
    public class LocalFileStore : IFileStore
    {
        public async Task SaveFile(string fileName, string prefix, string body)
        {
            if (!Directory.Exists(prefix))
                Directory.CreateDirectory(prefix);
            await File.WriteAllTextAsync($"{prefix}/{fileName}", body);
        }
    }
}