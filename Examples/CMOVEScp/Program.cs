using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dicom;
using Dicom.IO;
using Dicom.Network;
using Dicom.Log;
using System.IO;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZYCMOVEScp
{

    //DICOM3.0协议第7部分第8章中DIMSE协议并未规定请求方和实现方如何来进行具体操作
    //此处定义的DcmCFindCallback代理由用户自己来实现接收到C-FIND-RQ后的操作

    public delegate IList<DicomDataset> DcmCMoveCallback(DicomCMoveRequest request);

    //要想提供C-FIND SCP服务，需要继承DicomService类，该类中实现了DICOM协议的基础框架，
    //另外还需要实现IDicomCFindProvider接口,用于实现具体的C-FIND SCP服务。
    class ZSCMoveSCP : DicomService, IDicomServiceProvider, IDicomCMoveProvider,IDicomCEchoProvider
    {
        string cmove_store_ip = System.Configuration.ConfigurationManager.AppSettings["cmove_store_ip"].ToString();
        string cmove_store_port = System.Configuration.ConfigurationManager.AppSettings["cmove_store_port"].ToString();
        //不发送图像的modality
        string not_send_modalitys = System.Configuration.ConfigurationManager.AppSettings["not_send_modalitys"]==null ? "" : System.Configuration.ConfigurationManager.AppSettings["not_send_modalitys"].ToString();
             
        public ZSCMoveSCP(INetworkStream stream, Encoding fallbackEncoding, Logger log):base(stream,fallbackEncoding,log)
        {

        }
        #region C-MOVE
        //public static DcmCMoveCallback OnZSCMoveRequest;
        public virtual IEnumerable<DicomCMoveResponse> OnCMoveRequest(DicomCMoveRequest request)
        {
            DicomStatus status = DicomStatus.Success;
            IList<DicomCMoveResponse> rsp = new List<DicomCMoveResponse>();
            /*----to do------*/
            //添加查询数据库的代码，即根据request的条件提取指定的图像
            //然后将图像信息添加到rsp响应中

            //创建C-STORE-SCU，发起C-STORE-RQ
            //IList<DicomDataset> queries;
            DicomClient clt = new DicomClient();
            DataSet ds = DataBase.GetImage(request.Dataset.Get<string>(DicomTag.StudyInstanceUID));
            string modality = "";
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                int len = ds.Tables[0].Rows.Count;
                int cnt = 0;
                try
                {
                    foreach (DataRow r in ds.Tables[0].Rows)
                    {
                        DicomCStoreRequest cstorerq = new DicomCStoreRequest(r[0].ToString());
                        modality = cstorerq.Dataset.Get<string>(DicomTag.Modality);
                        cstorerq.OnResponseReceived = (rq, rs) =>
                        {
                            if (rs.Status != DicomStatus.Pending)
                            {

                            }
                            if (rs.Status == DicomStatus.Success)
                            {
                                DicomCMoveResponse rsponse = new DicomCMoveResponse(request, DicomStatus.Pending);
                                rsponse.Remaining = --len;
                                rsponse.Completed = ++cnt;
                                rsponse.Warnings = 0;
                                rsponse.Failures = 0;
                           
                            SendResponse(rsponse);
                          

                        }

                        };
                        clt.AddRequest(cstorerq);

                    }

                    //注意：这里给出的IP地址与C-MOVE请求的IP地址相同，意思就是说C-MOVE SCP需要向C-MOVE SCU发送C-STORE-RQ请求
                    //将查询到的图像返回给C-MOVE SCU
                    //所以四尺C-STORE-RQ中的IP地址与C-MOVE SCU相同，但是端口不同，因为同一个端口不能被绑定多次。
                    
                    if (not_send_modalitys.IndexOf(modality)>-1) {
                        //不发送图像
                    }
                    else
                    {
                        clt.Send(cmove_store_ip, Convert.ToInt32(cmove_store_port), false, this.Association.CalledAE, request.DestinationAE);
                        Log.Add("发送图像", " ip:" + cmove_store_ip + "port:" + cmove_store_port+"modality:"+ modality);
                    }
                }
                catch(Exception e)
                {
                    Log.Add("onCmoveRequest", e.Message + e.StackTrace);
                    DicomCMoveResponse rs = new DicomCMoveResponse(request, DicomStatus.StorageStorageOutOfResources);
                    rsp.Add(rs);
                    return rsp;
                }
            }
            else
            {
                rsp.Add(new DicomCMoveResponse(request, DicomStatus.NoSuchObjectInstance));
                return rsp;
            }
            rsp.Add(new DicomCMoveResponse(request, DicomStatus.Success));
            return rsp;


        }
        #endregion

        //下面这部分是A-ASSOCIATE服务的相关实现，此处为了简单只实现了连接请求服务
        #region ACSE-Service

        //A-ASSOCIATE-RQ:
        public void OnReceiveAssociationRequest(DicomAssociation association)
        {


            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification)
                    pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                else
                    if (pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelFIND ||
                        pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelMOVE ||
                        pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelFIND ||
                        pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelMOVE
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

        //A-RELEASE-RQ
        public void OnReceiveAssociationReleaseRequest()
        {
            SendAssociationReleaseResponse();
        }
        //A-ABORT
        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
        }
        //CONNECTION CLOSED
        public void OnConnectionClosed(int errorCode)
        {
        }

        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }

        public void OnConnectionClosed(Exception exception)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Transfer Syntaxes

        private static DicomTransferSyntax[] AcceptedTransferSyntaxes = new DicomTransferSyntax[] {
				DicomTransferSyntax.ExplicitVRLittleEndian,
				DicomTransferSyntax.ExplicitVRBigEndian,
				DicomTransferSyntax.ImplicitVRLittleEndian
			};

        private static DicomTransferSyntax[] AcceptedImageTransferSyntaxes = new DicomTransferSyntax[] {
				// Lossless
				DicomTransferSyntax.JPEGLSLossless,
				DicomTransferSyntax.JPEG2000Lossless,
				DicomTransferSyntax.JPEGProcess14SV1,
				DicomTransferSyntax.JPEGProcess14,
				DicomTransferSyntax.RLELossless,
			
				// Lossy
				DicomTransferSyntax.JPEGLSNearLossless,
				DicomTransferSyntax.JPEG2000Lossy,
				DicomTransferSyntax.JPEGProcess1,
				DicomTransferSyntax.JPEGProcess2_4,

				// Uncompressed
				DicomTransferSyntax.ExplicitVRLittleEndian,
				DicomTransferSyntax.ExplicitVRBigEndian,
				DicomTransferSyntax.ImplicitVRLittleEndian
			};
        #endregion
    }
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
            var cmoveScp = new DicomServer<ZSCMoveSCP>(12345);
            Console.WriteLine("服务成功运行。。。。。。。。");
            Console.ReadLine();

        }
    }
}
