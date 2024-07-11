using Microsoft.AspNetCore.StaticFiles;

namespace DCElectricWebAPI.Modules
{
    public class FileModule
    {
        public static string GetMimeMapping(string FileName)
        {

            //just going to default the entire thing to pdf.... just because.

            var provider = new FileExtensionContentTypeProvider();
            string contentType;
            if (!provider.TryGetContentType(FileName, out contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;

        }

    }
}
 
