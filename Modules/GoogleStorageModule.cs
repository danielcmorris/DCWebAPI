using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using System.Diagnostics;
using System.IO;
using System.Text;
using static Google.Apis.Storage.v1.StorageService;

namespace DCElectricWebAPI.Modules
{
    public class GoogleStorageModule
    {



        private readonly GoogleCredential googleCredential;
        private readonly ServiceAccountCredential serviceAccountCredential;

        private MemoryStream Key;

        /// <summary>
        /// You can override the standard key with any other key
        /// </summary>
        /// <param name="key"></param>
        public GoogleStorageModule(MemoryStream key)
        {

            Key = key;

        }

        public GoogleStorageModule()
        {

            this.googleCredential = this.googleCred();
            this.serviceAccountCredential = this.serviceCred();
        }




        private MemoryStream getKey()
        {

            if (!(Key is null)) return Key;

            string json = @"{
                              ""type"": ""service_account"",
                              ""project_id"": ""imagestorage-271519"",
                              ""private_key_id"": ""659c20dd5b9086b2b06c30462f28209e81c09afa"",
                              ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQDQrb3TszxKrpD4\nRe/n/rwEbUTOAVNz2siFhDRnts+mKwbhzCb8GY6SDXF9TD762FBivxSC3DaTKjhF\nSZ3RJ7VM4xLb2ZlBG+EmrHb0LGJKypgKmg7Z72VKV4UwIOlkkN6kQzMq48NW8epP\n2GZReqv3Sd97NT8Lvvm+29wM06oV0bqX/WKUg7b0MXha8odHDs7jLOVBESfJ7SAI\n3X/FMsRidKNLVKcaXMpvz9mIPhBDwx/c+DsZQlxcc22adoraPWYBS01rUx5RJobW\n/nvm83WoMyT0jEDdbQJQtsnYdE/g71/Auw/brzZj2s+lnvnnf/nptu+MN8tXYn5j\nJ4sLNqTtAgMBAAECggEAE3W4R4cl3rMDuttOwXwsTV9hNLLD9QBYhbbr6iYOnCjU\nBfdzRTwe4vjU9gHHt723VVYLVB60Cio8QB3a1TfWPNrKFe1nUL6IUwJvP1rqOZ5F\n6msual1cPUAHIBNZoHKwCHJp0ZyWyUNa/eIovH1rju55JDS1ceN3x7gZ/6o3aLxQ\ndGDGOeSJMfpPA9ZN3LEcizPl1wjuFAnzI3zYlOwnd0sWVM8a17zFU/Cw9ekeK0JO\n3gXLKqB/UZKSvnG11XVovFYqJGxhKzfWGmk7V4nYxDID+Yxiitjavd9OVDkoQX12\ng38JIKbJ2Ha1vnCNfX64Frd5Vkwh6qOEkdxaiNUoIwKBgQD0nZc2FIjBMHneP6Yp\nzIZTPLS2REXl67XxqoPaMupTTqkloPedLro3lunEjMTF/+0Se7ufDAYS8Ss8tgeH\nXZPdCN9f6jqZu5V+SlNvFg7QuBzUPg29NexlU2R8zWXBxzH7ISFuNRWBD9WXDddZ\nBB3x7irDIbl0fHxLYBAKNKVitwKBgQDaY/1i0m0kyJN5QpjYQNVL0lmPuy+Ic3ZR\n+ecfE/WHUnp5SuHBWw2TDEDA0xcZyNUQBqAw4ksN/2dCQRek4BLlkzPsjuBHcbse\nHI+qXgcVqEdms5a+KZc1Tn+4ZsrtaTwKwQEBppqb6ZR9uSGg2CXrBQL8vF2IJWvQ\nZCvIFQ6BewKBgHIEHl4Lti5t/O/VtQqYlSepDQZDzly7wEOTWf/TaZtI99hdLe0q\nwYt1oSKHBpTPlF3gJHSesxoTJTcYFWxH9sq4/v0C/St43tZNqJQHsQIiPvXCsr61\nqqkT9KujRUHMKgTGzYFD5vEQQ1s0DWMlYxvIClCHoJymBX1QmklXfpsDAoGAO3w+\n2XsNpZZIrR/ZuBW4w4VRRdgrs1QX525VaN6r4ycxGvatlVgy01nkDWGyCiDtAAd9\n/LK21OI7gw1t3kf2fbfhzc6BApTU7ffDnhksftauVCgYqEV13Vw7Z8QABDjE1P0L\nRCLYPB2ch740jbYyZdignqzEIUBoJIQUdNRfdh8CgYBMbjlf3gx774r3pIm6LhRF\nlQNJba1JgVhyFIWeeigyZeHKjj2BRzPOUFWeMbsbZS5+z6AQBpBvotLXFbloNoGk\nxezIOVnHDwLnkrjKsRgwlqfiUOqQ1zAakvyHScC+f9HpMS2EUIg7omHwGtKSLiVa\nt9+dsT1K+42/u1Gue4Q+IA==\n-----END PRIVATE KEY-----\n"",
                              ""client_email"": ""imagestorage@imagestorage-271519.iam.gserviceaccount.com"",
                              ""client_id"": ""109745029450579208330"",
                              ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                              ""token_uri"": ""https://oauth2.googleapis.com/token"",
                              ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                              ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/imagestorage%40imagestorage-271519.iam.gserviceaccount.com""
                            }
                           ";





            var key = new MemoryStream(Encoding.UTF8.GetBytes(json ?? ""));

            return key;


        }
        private ServiceAccountCredential serviceCred()
        {

            Stream key = this.getKey();
            //  prepare a credential from google  (not approved yet, just set up)
            var credential = GoogleCredential.FromStream(key)
                                .CreateScoped("https://www.googleapis.com/auth/devstorage.full_control")
                                .UnderlyingCredential as ServiceAccountCredential; ;

            //define exactly what kind of authority we want from google
            var scp = new List<string>();
            scp.Add(Scope.DevstorageReadWrite);
            var scopes = scp.ToString();

            // at this point, we have an object that contains google's response.  either an OK with a token, or a denial
            return credential;
        }

        private GoogleCredential googleCred()
        {
            Stream key = this.getKey();
            //  prepare a credential from google  (not approved yet, just set up)
            var credential = GoogleCredential.FromStream(key);

            //define exactly what kind of authority we want from google
            var scp = new List<string>();
            scp.Add(Scope.DevstorageReadWrite);
            var scopes = scp.ToString();
            // string[] scopes = new string { Scope.DevstorageReadWrite   }; // Full access

            //send that request along with our security account in the credential.
            credential = credential.CreateScoped(scp);

            // at this point, we have an object that contains google's response.  either an OK with a token, or a denial
            return credential;
        }


        public string UploadFile(string bucketName, MemoryStream objectBody, string objectName, string fileName = "")
        {

            string mimeType = FileModule.GetMimeMapping(objectName);
            if (fileName == "") fileName = objectName;

            var obj = new Google.Apis.Storage.v1.Data.Object
            {
                Bucket = bucketName,
                Name = objectName,
                ContentType = mimeType

            };


            var storage = StorageClient.Create(this.googleCredential);
            Google.Apis.Storage.v1.Data.Object retval;
            try
            {
                retval = storage.UploadObject(obj, objectBody);
                var patchObject = new Google.Apis.Storage.v1.Data.Object
                {
                    Bucket = bucketName,
                    Name = objectName,
                    ContentDisposition = $@"attachment; filename=""{fileName}"""
                };
                storage.PatchObject(patchObject);
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
                return e.Message;
            }

            string gsURI = "gs://" + bucketName + "/" + retval.Name;

            return gsURI;


        }

        public string UploadFile(string bucketName, Stream objectBody, string objectName, string fileName = "", bool raiseError = false)
        {

            string mimeType = FileModule.GetMimeMapping(objectName);
            if (fileName == "") fileName = objectName;

            var obj = new Google.Apis.Storage.v1.Data.Object
            {
                Bucket = bucketName,
                Name = objectName,
                ContentType = mimeType

            };


            var storage = StorageClient.Create(this.googleCredential);
            Google.Apis.Storage.v1.Data.Object retval;
            try
            {
                retval = storage.UploadObject(obj, objectBody);
                var patchObject = new Google.Apis.Storage.v1.Data.Object
                {
                    Bucket = bucketName,
                    Name = objectName,
                    ContentDisposition = $@"attachment; filename=""{fileName}"""
                };
                storage.PatchObject(patchObject);
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
                if (!raiseError)
                {
                    return e.Message;
                }
                else
                {
                    throw e;
                }
            }

            string gsURI = "gs://" + bucketName + "/" + retval.Name;

            return gsURI;


        }

        public async Task<string> UploadFileAsync(string bucketName, MemoryStream objectBody, string objectName, string fileName = "")
        {

            string mimeType = FileModule.GetMimeMapping(objectName);
            if (fileName == "") fileName = objectName;

            var obj = new Google.Apis.Storage.v1.Data.Object
            {
                Bucket = bucketName,
                Name = objectName,
                ContentType = mimeType

            };


            var storage = StorageClient.Create(this.googleCredential);
            Google.Apis.Storage.v1.Data.Object retval;
            try
            {
                retval = storage.UploadObject(obj, objectBody);
                var patchObject = new Google.Apis.Storage.v1.Data.Object
                {
                    Bucket = bucketName,
                    Name = objectName,
                    ContentDisposition = $@"attachment; filename=""{fileName}"""
                };
                storage.PatchObject(patchObject);
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
                return e.Message;
            }

            string gsURI = "gs://" + bucketName + "/" + retval.Name;

            return gsURI;


        }


        public string SignedURL(string bucket, string objectName, double minutes = 10, string contentType = "image/jpeg")
        {

            var urlSigner = UrlSigner.FromServiceAccountCredential(this.serviceAccountCredential);
            string url = urlSigner.Sign(
               bucket,
               objectName,
               TimeSpan.FromMinutes(minutes),
               HttpMethod.Get
            );

            return url;
        }

        public Google.Apis.Storage.v1.Data.Object GetMeta(string bucketName, string FileID)
        {
            var storage = StorageClient.Create(this.googleCredential);
            try
            {
                var retval = storage.GetObject(bucketName, FileID);
                return retval;
            }
            catch (Exception err)
            {
                var e = new Google.Apis.Storage.v1.Data.Object();
                e.Size = 0;
                e.Metadata.Add("ERROR", err.Message);
                return e;
            }


        }
        public MemoryStream GetFile(string bucketName, string FileID)
        {
            MemoryStream fileBody = new MemoryStream();
            var storage = StorageClient.Create(this.googleCredential);
            try
            {
                storage.DownloadObject(bucketName, FileID, fileBody);
                fileBody.Position = 0;
                return fileBody;

            }
            catch (Exception e)
            {
                // Debug.Write(e.Message);

                var ms = new MemoryStream();
                return ms;
            }


        }

        //Method from: https://cloud.google.com/storage/docs/samples/storage-list-files-with-prefix#storage_list_files_with_prefix-csharp
        public IEnumerable<Google.Apis.Storage.v1.Data.Object> ListFilesWithPrefix(string bucketName, string prefix, string delimiter = null)
        {
            var storage = StorageClient.Create(this.googleCredential);
            var options = new ListObjectsOptions { Delimiter = delimiter };
            var storageObjects = storage.ListObjects(bucketName, prefix, options);
            Console.WriteLine($"Objects in bucket {bucketName} with prefix {prefix}:");
            foreach (var storageObject in storageObjects)
            {
                Console.WriteLine(storageObject.Name);
            }
            return storageObjects;
        }

        //public bool checkIfBucketExists(string bucketName)
        //{
        //    var storage = StorageClient.Create(this.googleCredential);
        //    Bucket bucket = null;
        //    try
        //    {
        //        bucket = storage.GetBucket(bucketName);
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //    if(bucket == null)
        //    {
        //        return false;
        //    }
        //    return true;
        //}

        //public bool createBucket(string bucketName)
        //{
        //    var storage = StorageClient.Create(this.googleCredential);
        //    storage.CreateBucket()
        //}


        //public MemoryStream GetFile(string FileID)
        //{
        //    GoogleDriveModule dm = new GoogleDriveModule();

        //    try
        //    {
        //        var file = dm.DownloadGoogleFile(FileID);
        //        return file.File;

        //    }
        //    catch (Exception e)
        //    {
        //        // Debug.Write(e.Message);

        //        var ms = new MemoryStream();
        //        return ms;
        //    }


        //}

        public string Delete(string bucketName, string objectName)
        {
            var storage = StorageClient.Create(this.googleCredential);
            var obj = new Google.Apis.Storage.v1.Data.Object() { Bucket = bucketName, Name = objectName };
            try
            {
                storage.DeleteObject(obj);
                return "Success";
            }
            catch (Exception e)
            {
                return e.Message;
            }


        }
    }
}
