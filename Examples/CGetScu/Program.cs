using Dicom;
using Dicom.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CGetScu
{
    class Program
    {
        static string StoragePath = @".\DICOM";
        static void Main(string[] args)
        {
            var client = new DicomClient();
            client.Options = new DicomServiceOptions { IgnoreAsyncOps = true };

            //var pc = DicomPresentationContext.GetScpRolePresentationContext(DicomUID.CTImageStorage);
            //client.AdditionalPresentationContexts.Add(pc);
            client.NegotiateAsyncOps(100, 100);
            var counter = 0;
            var locker = new object();
            client.OnCStoreRequest = request =>
            {
                lock (locker)
                {
                    var studyUid = request.Dataset.Get<string>(DicomTag.StudyInstanceUID);
                    var instUid = request.SOPInstanceUID.UID;

                    var path = Path.GetFullPath(Program.StoragePath);
                    //文件路径格式 modality studydate patient_id
                    path = Path.Combine(path, request.Dataset.Get<string>(DicomTag.StudyDate) + "\\" + request.Dataset.Get<string>(DicomTag.PatientID));
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    path = Path.Combine(path, instUid) + ".dcm";
                    request.File.Save(path);
                    ++counter;
                }

                return new DicomCStoreResponse(request, DicomStatus.Success);
            };

            var get = new DicomCGetRequest("1.2.840.113619.2.55.3.2609388324.145.1222836278.84");

            var handle = new ManualResetEventSlim();
            get.OnResponseReceived = (request, response) =>
            {
                
                if (response.Remaining == 0)
                {
                    handle.Set();
                }
            };
            client.AddRequest(get);
            client.Send("localhost", 12346, false, "SCU", "COMMON");
            handle.Wait();

        }
    }
}
