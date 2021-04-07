using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using GraphML.Core;

namespace GraphML.AWS
{
    public class S3FileStore : IFileStore
    {
        public S3FileStore(IAmazonS3 s3, string bucket)
        {
            S3 = s3;
            Bucket = bucket;
        }

        public IAmazonS3 S3 { get; }
        public string Bucket { get; }

        public async Task SaveFile(string fileName, string prefix, string body)
        {
            var key = $"{prefix}/{fileName}";
            await S3.PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = Bucket,
                    Key = key,
                    ContentType = "text/csv",
                    ContentBody = body
                });
        }
    }
}