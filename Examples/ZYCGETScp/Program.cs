using System;
using System.Collections.Generic;

using Dicom;
using Dicom.Network;
using Dicom.Log;
using System.IO;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;

namespace ZYCGETScp
{
    class Program
    {
        /// <summary>
        /// 验证方法
        /// </summary>
        /// <param name="info"></param>
        /// <returns>0则验证成功，非0则验证失败</returns>
        [DllImport("MsunLicenseVerify.dll", EntryPoint = "MsunVerify", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static int MsunVerify(string info, string priKeyFile, string pubKeyFile);
        static void Main(string[] args)
        {
#if RELEASE
            if (!File.Exists("License.txt"))
            {
                MessageBox.Show("本系统未经过山东众阳软件产品正版认证，请告之相关负责人联系我公司进行产品的认证工作。谢谢!");
                return;
            }
            string info = System.IO.File.ReadAllText("License.txt");
            var result = MsunVerify(info, "pri", "pub");
            if (result == 0)
            {
                // MessageBox.Show("验证成功");
            }
            else
            {
                MessageBox.Show("本系统未经过山东众阳软件产品正版认证，请告之相关负责人联系我公司进行产品的认证工作。谢谢!");
                return;
            }
#endif
            LogManager.SetImplementation(ConsoleLogManager.Instance);
            int port = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["port"]);
            var cmoveScp = DicomServer.Create<CGetScp>(port);
            Console.WriteLine("服务成功运行,在"+port.ToString()+"端口.......");
            Console.ReadLine();

        }

    }
    /// <summary>
    /// C-GET 服务提供类
    /// </summary>
    class CGetScp : DicomService, IDicomServiceProvider, IDicomCGetProvider, IDicomCEchoProvider
    {
        private static DicomTransferSyntax[] AcceptedTransferSyntaxes =
            new DicomTransferSyntax[]
                {
                        DicomTransferSyntax
                            .ExplicitVRLittleEndian,
                        DicomTransferSyntax
                            .ExplicitVRBigEndian,
                        DicomTransferSyntax
                            .ImplicitVRLittleEndian
                };

        private static DicomTransferSyntax[] AcceptedImageTransferSyntaxes =
            new DicomTransferSyntax[]
                    {
                    // Lossless
                    DicomTransferSyntax
                        .JPEGLSLossless,
                    DicomTransferSyntax
                        .JPEG2000Lossless,
                    DicomTransferSyntax
                        .JPEGProcess14SV1,
                    DicomTransferSyntax
                        .JPEGProcess14,
                    DicomTransferSyntax
                        .RLELossless,

                    // Lossy
                    DicomTransferSyntax
                        .JPEGLSNearLossless,
                    DicomTransferSyntax
                        .JPEG2000Lossy,
                    DicomTransferSyntax
                        .JPEGProcess1,
                    DicomTransferSyntax
                        .JPEGProcess2_4,

                    // Uncompressed
                    DicomTransferSyntax
                        .ExplicitVRLittleEndian,
                    DicomTransferSyntax
                        .ExplicitVRBigEndian,
                    DicomTransferSyntax
                        .ImplicitVRLittleEndian
                    };
        public CGetScp(INetworkStream stream, Encoding fallbackEncoding, Logger log) : base(stream, fallbackEncoding, log)
        {

            //this.Options = new DicomServiceOptions { IgnoreAsyncOps = true };
        }

        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }

        public IEnumerable<DicomCGetResponse> OnCGetRequest(DicomCGetRequest request)
        {
            IList<DicomCGetResponse> rsp = new List<DicomCGetResponse>();
            try
            {
                DataSet ds = DataBase.GetImage(request.Dataset.Get<string>(DicomTag.StudyInstanceUID));
                if (ds != null && ds.Tables[0].Rows.Count > 0)
                {
                    int len = ds.Tables[0].Rows.Count;
                    int cnt = 0;
                    foreach (DataRow r in ds.Tables[0].Rows)
                    {
                        DicomCStoreRequest cstorerq = new DicomCStoreRequest(r["REFERENCEFILE"].ToString());
                        cstorerq.OnResponseReceived = (rq, rs) =>
                        {
                            if (rs.Status != DicomStatus.Pending)
                            {

                            }
                            if (rs.Status == DicomStatus.Success)
                            {
                                DicomCGetResponse rsponse = new DicomCGetResponse(request, DicomStatus.Pending);
                                rsponse.Remaining = --len;
                                rsponse.Completed = ++cnt;
                                rsponse.Warnings = 0;
                                rsponse.Failures = 0;
                                rsp.Add(rsponse);
                             

                            }

                        };
                        this.SendRequest(cstorerq);



                    }

                }

                rsp.Add(new DicomCGetResponse(request, DicomStatus.Success));
                return rsp;
            }
            catch(Exception ex)
            {
                Log.Add("接收请求出错", ex.Message + ex.StackTrace);
                rsp.Add(new DicomCGetResponse(request, DicomStatus.QueryRetrieveOutOfResources));
                return rsp;
            }
        }

        public void OnConnectionClosed(Exception exception)
        {
            throw new NotImplementedException();
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            throw new NotImplementedException();
        }

        public void OnReceiveAssociationReleaseRequest()
        {
            SendAssociationReleaseResponse();
        }

        public void OnReceiveAssociationRequest(DicomAssociation association)
        {
            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification)
                    pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                else
                    if (pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelFIND ||
                        pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelGET ||
                        pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelFIND ||
                        pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelGET
                        )
                {
                    //未添加Transfer Syntax限制
                    pc.SetResult(DicomPresentationContextResult.Accept);
                }
                else
                        if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None)
                    pc.AcceptTransferSyntaxes(AcceptedImageTransferSyntaxes);
            }
            SendAssociationAccept(association);
        }
    }
}
