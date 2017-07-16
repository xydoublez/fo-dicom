
using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using Dicom.Network;
using System.IO;
using System.Data;
using Dicom.Log;
using System.Text;

namespace ZyWorkListScp
{

    /// <summary>
    /// configuration for this demo
    /// </summary>
    class Config 
    {
       
    }
   
    class Program
    {

        public static string strModality = "CT";
        public static  string ENCODING = "GB18030";
        public static  int LOCALE_PORT = 12346;
        public static  uint MAX_PDU_LENGTH = 16384;
        public static  string LOCAL_AE = "WorklistSCP";
        public static string REMOTE_AE = "WorklistSCU";
        public static string IMPLEMENTATION_VERSION_NAME = "ZyWorklistScp";
        public static  string IMPLEMENTATION_CLASS_UID = "1.2.392.0000000.1.2";
        public static string NEW_MODALITY = "";
        /// <summary>
        /// 名字是否传拼音
        /// </summary>
        public static bool b_NAME_IS_PY = true;
        static void Main(string[] args)
        {
            try
            {
                Config();
                MyImplementation m = new MyImplementation();
                m.Run();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        /// <summary>
        /// 获取配置信息
        /// </summary>
        static void Config()
        {
            strModality = System.Configuration.ConfigurationManager.AppSettings["MODALITY"].ToString();
            LOCALE_PORT = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["PORT"]);
            REMOTE_AE = System.Configuration.ConfigurationManager.AppSettings["AETITLE"].ToString();
            ENCODING = System.Configuration.ConfigurationManager.AppSettings["ENCODING"].ToString();
            NEW_MODALITY = System.Configuration.ConfigurationManager.AppSettings["NEW_MODALITY"].ToString();
            if (System.Configuration.ConfigurationManager.AppSettings["NAME_IS_PY"].ToString() != "true")
            {
                b_NAME_IS_PY = false;
            }
        }
    }


    class MyImplementation
    {
       Dicom.Log.Logger logger;

        public void Run()
        {

            Console.Title = "ZyWorklistScp " + Program.strModality + " " + Program.LOCALE_PORT + " " + Program.REMOTE_AE + " " + Program.ENCODING;
            // initialize NLog logging
            //var config = new LoggingConfiguration();
            //var target = new ColoredConsoleTarget();
            //target.Layout = "${message}";
            //config.AddTarget("Console", target);
            //config.LoggingRules.Add(new LoggingRule("Dicom.Network", NLog.LogLevel.Info, target));
            //NLog.LogManager.Configuration = config;
            //var logManager = new Dicom.Log.NLogManager();
            //logger =logManager.GetLogger("Dicom.Network");
            

            // preload dictionary to prevent timeouts
            var dict = DicomDictionary.Default;


            // start DICOM server 
            // the server will run until server.Dispose() is called
            // during this time the TCP port wil be open and it will accept new connections
            try
            {
                DicomServer<MyDicomServiceProvider> server = new DicomServer<MyDicomServiceProvider>(Program.LOCALE_PORT);
                server.Options.LogDataPDUs = true;
                server.Options.LogDimseDatasets = true;
                server.Options.MaxDataBuffer = Program.MAX_PDU_LENGTH;
                Console.WriteLine("众阳Worklist服务成功运行！");
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
                Console.WriteLine("启动失败！");
            }
            // end process
            Console.WriteLine("Press <return> to end...");
            Console.ReadLine();
        }
    }


    /// <summary>
    /// this class implements the handling of events created by the server
    /// the server creates a new object for each accepted connection 
    /// 
    /// this class implements IDicomCFindProvider and IDicomCEchoProvider
    /// because we can handle two type of messages: Worklist C-FIND and C-ECHO RQ
    /// </summary>
    class MyDicomServiceProvider : DicomService, IDicomServiceProvider, IDicomCFindProvider, IDicomCEchoProvider
    {
        /// <summary>array of supported transfer syntaxes by our implementation</summary>
        private static DicomTransferSyntax[] AcceptedTransferSyntaxes = new DicomTransferSyntax[] {
			DicomTransferSyntax.ExplicitVRLittleEndian,
			DicomTransferSyntax.ExplicitVRBigEndian,
			DicomTransferSyntax.ImplicitVRLittleEndian
		};

        /// <summary>
        ///  constructor, called by DicomServer when a new connection is created
        /// </summary>
        public MyDicomServiceProvider(INetworkStream stream, Encoding fallbackEncoding, Logger log) : base(stream, fallbackEncoding, log)
        {
        }

        /// <summary>
        /// called whenever a connection is opened and the ASSOCIATE-RQ PDU has been received
        /// this PDU contains information of SCU and the requested type of communication
        /// we jave to check the information and decide if we want to accept or to reject the connection 
        /// reject: send ASSOCIATE-RQ PDU 
        /// accept: send ASSOCIATE-AC PDU 
        /// </summary>
        /// <param name="association">association object, containing the information from the ASSOCIATE-RQ</param>
        public void OnReceiveAssociationRequest(DicomAssociation association)
        {
            //// check if the called AE is our program
            //if (association.CalledAE != Program.LOCAL_AE) {
            //    // the SCU want to contact another AE --> reject
            //    SendAssociationReject(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
            //    return;
            //}
                
            //// check if the calling AE is allowed to contact our program
            //if (association.CallingAE !=Program.REMOTE_AE) {
            //    // the SCU is a not allowed program --> reject
            //    SendAssociationReject(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CallingAENotRecognized);
            //    return;
            //}

            // check the proposed presentation contexts
            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification) {
                    // abstract syntax is verification 
                    // check transfer syntaxes for verification 
                    pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                } else if (pc.AbstractSyntax == DicomUID.ModalityWorklistInformationModelFIND) {
                    // abstract syntax is worklist C-FIND
                    // check transfer syntaxes for worklist C-FIND
                    pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                } else {
                    // not supported abstract syntax
                    pc.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                }
            }

            // everything fine --> send accept
            SendAssociationAccept(association);
        }

        /// <summary>
        /// called whenever a A-RELEASE-RQ PDU has been received
        /// this PDU does not caontain any valuable information
        /// the only useful response is: send A-RELEASE-RP PDU 
        /// </summary>
        public void OnReceiveAssociationReleaseRequest()
        {
            SendAssociationReleaseResponse();
        }
        
        /// <summary>
        /// called whenever a A-ABORT PDU has been received
        /// this indicates an interrupted communication
        /// no special handling is needed
        /// </summary>
        /// <param name="source">Abort source as received in A-ABORT PDU </param>
        /// <param name="reason">Abort reason as received in A-ABORT PDU </param>
        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
        }

        /// <summary>
        /// called whenever the TCP/IP connection has been closed
        /// no special handling is needed
        /// </summary>
        public void OnConnectionClosed(int errorCode)
        {
        }


        /// <summary>
        /// a complete request has been received (C-ECHO RQ)
        /// C-ECHO RQ does not contain any valuable data 
        /// so we just create the response 
        /// </summary>
        /// <param name="request">the received C-ECHO RQ</param>
        /// <returns>a C-ECHO RSP object to be send to the SCU</returns>
        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }


        /// <summary>
        /// a complete C-FIND RQ has been received
        /// the C-FIND RQ defines search criteria (fields filled with data) and the return values (fields available in request)
        /// 
        /// a C-FIND SCP now filters entries from the internal database which are matching the request (search criteria).
        /// from the entries found, C-FIND response messages are created
        /// the responses have the same structure (fields) as the request
        /// but the this time thie fields are filled with data
        /// 
        /// this sample implementation does not have a database and creates random responses
        /// </summary>
        /// <param name="request">the received C-FIND RQ</param>
        /// <returns>a list of C-FIND RSP messages with the information found in the internal database</returns>
        public IEnumerable<DicomCFindResponse> OnCFindRequest(DicomCFindRequest request)
        {
            // this sample code does not really created responses based on the request
            // we simply return some entries as response
            // but anyway the shows the basic idea
            dbUtility db = new dbUtility();
            DataSet ds = db.GetRegInfo(" and modality='" + Program.strModality+"' and datediff(day,registration_date,getdate())<=3 ");
            DicomCFindResponse[] res;
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                res = new DicomCFindResponse[ds.Tables[0].Rows.Count+1];
            }
            else
            {
                res = new DicomCFindResponse[1];
            }
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {

                for (int i = 0; i < res.Length-1; i++)
                    res[i] = CreateFindResponse(request, GetHashTableFromRow(ds.Tables[0].Rows[i],ds.Tables[0].Columns));
                res[res.Length-1] = new DicomCFindResponse(request, DicomStatus.Success);
            }
            else
            {
                res[res.Length-1] = new DicomCFindResponse(request, DicomStatus.Success);
            }
            // the last entry has status Success
            // it does not contain any data, so we are done
            
            return res;
        }
        private System.Collections.Hashtable GetHashTableFromRow(DataRow row, DataColumnCollection cols)
        {
            System.Collections.Hashtable ht = new System.Collections.Hashtable();
            for (int i = 0; i < cols.Count; i++)
            {
                ht.Add(cols[i].ColumnName, row.ItemArray[i]);
            }
            return ht;
            
        }

        /// <summary>
        /// for our sample implementation, this function creates a random response matching the given request
        /// </summary>
        /// <param name="request">received C-FIND RQ</param>
        /// <returns>one C-FIND RSP with random data</returns>
        private DicomCFindResponse CreateFindResponse(DicomCFindRequest request,System.Collections.Hashtable ht)
        {
            // check the request for supported return fields
            
            bool bReqPatientName = (request.Dataset.First(i => i.Tag == DicomTag.PatientName) != null);
            bool bReqPatientID = (request.Dataset.First(i => i.Tag == DicomTag.PatientID) != null);
            bool bReqPatientBirth = (request.Dataset.First(i => i.Tag == DicomTag.PatientBirthDate) != null);
            
            bool bReqPatientAge = false;
            bool bReqPatientSex = (request.Dataset.First(i => i.Tag == DicomTag.PatientSex) != null);
            //bool bReqPatientAddr = (request.Dataset.First(i => i.Tag == DicomTag.PatientAddress) != null);
            bool bReqPatientAddr = true;
            //bool bReqPatientPhone = (request.Dataset.First(i => i.Tag == DicomTag.PatientTelephoneNumbers) != null);
            bool bReqPatientPhone = true;
            bool bReqStudyUID = (request.Dataset.First(i => i.Tag == DicomTag.StudyInstanceUID) != null);
            //bool bReqProcDesc = (request.Dataset.First(i => i.Tag == DicomTag.RequestedProcedureDescription) != null);
            bool bReqProcDesc = true;
            bool bReqAdmissionID = (request.Dataset.First(i => i.Tag == DicomTag.AdmissionID) != null);
            bool bReqRequProcID = (request.Dataset.First(i => i.Tag == DicomTag.RequestedProcedureID) != null);
            //bool bReqRequProcLoc = (request.Dataset.First(i => i.Tag == DicomTag.RequestedProcedureLocation) != null);
            bool bReqRequProcLoc = true;
           // bool bReqRequProcComm = (request.Dataset.First(i => i.Tag == DicomTag.RequestedProcedureComments) != null);
            bool bReqRequProcComm = true;
            bool bReqModality = false;
            string[] szModalityFilter = new string[0];
            bool bReqScheduledAETitle = false;
            string[] szScheduledAETitles = new string[0]; ;
            bool bReqStartDate = false;
            DicomDateRange startDateRange = null;
            bool bReqStartTime = false;
            bool bReqEndDate = false;
            bool bReqEndTime = false;
            bool bReqPerfPhysican = false;
            bool bReqProcStepDesc = false;
            bool bReqProcStepID = false;
            //ScheduledProcedureStepStatus SCHEDULED 
            bool bReqProcStepStatus = false;
            bool bReqSchedStation = false;
            bool bReqProcStepLoc = false;
            bool bReqPreMed = false;
            bool bReqComments = false;

            DicomSequence reqSeq = (DicomSequence)request.Dataset.First(i => i.Tag == DicomTag.ScheduledProcedureStepSequence);
            if (reqSeq != null && reqSeq.Items[0] != null)
            {
                DicomElement reqElem;

                // get the modality element
                reqElem = (DicomElement)reqSeq.Items[0].First(i => i.Tag == DicomTag.Modality);
                if (reqElem != null)
                {
                    // modality is requested
                    bReqModality = true;
                    if (reqElem.Count > 0)
                    {
                        // there is also one or more filter values (we support multiple search values here)
                        szModalityFilter = reqElem.Get<string[]>();
                    }
                }

                // get the scheduled AE title
                reqElem = (DicomElement)reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledStationAETitle);
                if (reqElem != null)
                {
                    //  AE title is requested
                    bReqScheduledAETitle = true;
                    if (reqElem.Count > 0)
                    {
                        szScheduledAETitles = reqElem.Get<string[]>();
                    }
                    else
                    {
                        // no scheduled AE title in request --> use sender AE title as filter
                        szScheduledAETitles = new string[1];
                        szScheduledAETitles[0] = Association.CallingAE;
                    }
                }

                // get the scheduled start date
                reqElem = (DicomElement)reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepStartDate);
                if (reqElem != null)
                {
                    bReqStartDate = true;
                    // we support range matching --> so try to find a range
                    try
                    {
                        startDateRange = reqElem.Get<DicomDateRange>(0);
                        // we do not provide results from the past
                        if (startDateRange.Minimum < DateTime.Today) startDateRange.Minimum = DateTime.Today;
                    }
                    catch { }
                }
                bReqStartTime = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepStartTime) != null);
                //bReqEndDate = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepEndDate) != null);
                bReqEndDate = true;
                //bReqEndTime = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepEndTime) != null);
                bReqEndTime = true;
                bReqPerfPhysican = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledPerformingPhysicianName) != null);
                
                bReqProcStepDesc = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepDescription) != null);
                //bReqProcStepID = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepID) != null);
                bReqProcStepID = true;
                bReqProcStepStatus = true;
                bReqSchedStation = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledStationName) != null);
                bReqProcStepLoc = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepLocation) != null);
                bReqPreMed = (reqSeq.Items[0].First(i => i.Tag == DicomTag.PreMedication) != null);
                //bReqComments = (reqSeq.Items[0].First(i => i.Tag == DicomTag.CommentsOnTheScheduledProcedureStep) != null);
                bReqComments = true;
            }

            //Random rand = new Random();
            // now bild a response to our request 
            // all responses with data have status Pending, because the last response with status Success never has data
            DicomCFindResponse resp = new DicomCFindResponse(request, DicomStatus.Pending);
            resp.Dataset = new DicomDataset();
            resp.Dataset.Add(DicomTag.SpecificCharacterSet, Program.ENCODING);
            //得到病人姓名
            if (bReqPatientName)
            {
                if (Program.b_NAME_IS_PY)
                {
                    resp.Dataset.Add(new DicomPersonName(DicomTag.PatientName, DicomEncoding.GetEncoding(Program.ENCODING),Utility.GetPy( ht["PATIENT_NAME"].ToString())));
                }
                else
                {
                    resp.Dataset.Add(new DicomPersonName(DicomTag.PatientName, DicomEncoding.GetEncoding(Program.ENCODING), ht["PATIENT_NAME"].ToString()));
                }
            }
            //检查号
            if (bReqPatientID)
            {
                resp.Dataset.Add(new DicomLongString(DicomTag.PatientID, DicomEncoding.GetEncoding(Program.ENCODING), ht["PROCESS_NUM"].ToString()));
            }
            //病人出生日期
            if (bReqPatientBirth)
            {
                resp.Dataset.Add(new DicomDate(DicomTag.PatientBirthDate,Convert.ToDateTime(ht["PATIENT_BIRTHDAY"]).ToString("yyyyMMdd")));
            }
            //年龄
            if (bReqPatientAge)
            {
                resp.Dataset.Add(new DicomAgeString(DicomTag.PatientAge, ht["AGE"].ToString()));
            }
            if (bReqPatientSex)
            {
                if (ht["PATIENT_SEX_ID"].ToString()=="1")
                {
                    resp.Dataset.Add(new DicomCodeString(DicomTag.PatientSex, "M"));
                }
                else
                {
                    resp.Dataset.Add(new DicomCodeString(DicomTag.PatientSex, "F"));
                }
            }
            //病人地址
            if (bReqPatientAddr)
            {
                resp.Dataset.Add(new DicomLongString(DicomTag.PatientAddress, DicomEncoding.GetEncoding(Program.ENCODING),ht["PATIENT_ADDRESS"].ToString()));
            }
            //病人电话
            if (bReqPatientPhone)
            {
                resp.Dataset.Add(new DicomShortString(DicomTag.PatientTelephoneNumbers, DicomEncoding.GetEncoding(Program.ENCODING), ht["PATIENT_PHONENUM"].ToString()));
            }
            //studyuid
            if (bReqStudyUID)
            {
                
                resp.Dataset.Add(new DicomUniqueIdentifier(DicomTag.StudyInstanceUID, ht["PROCESS_NUM"].ToString()));
            }

            if (bReqProcDesc)
            {
                resp.Dataset.Add(new DicomLongString(DicomTag.RequestedProcedureDescription, DicomEncoding.GetEncoding(Program.ENCODING), " "));
            }
            if (bReqProcStepStatus)
            {
                resp.Dataset.Add(new DicomLongString(DicomTag.ScheduledProcedureStepStatus, DicomEncoding.GetEncoding(Program.ENCODING), "SCHEDULED"));
            }
            if (bReqAdmissionID)
            {

                resp.Dataset.Add(new DicomLongString(DicomTag.AdmissionID, DicomEncoding.GetEncoding(Program.ENCODING), ht["PROCESS_NUM"].ToString()));
            }

            if (bReqRequProcID)
            {

                resp.Dataset.Add(new DicomShortString(DicomTag.RequestedProcedureID, DicomEncoding.GetEncoding(Program.ENCODING), ht["PROCESS_NUM"].ToString()));
            }

            if (bReqRequProcLoc)
            {
                
                resp.Dataset.Add(new DicomLongString(DicomTag.RequestedProcedureLocation, DicomEncoding.GetEncoding(Program.ENCODING), " " ));
            }

            if (bReqRequProcLoc)
            {
                resp.Dataset.Add(new DicomLongText(DicomTag.RequestedProcedureComments, DicomEncoding.GetEncoding(Program.ENCODING), " "));
            }

            DicomSequence respSeq = new DicomSequence(DicomTag.ScheduledProcedureStepSequence, new DicomDataset());
            resp.Dataset.Add(respSeq);
            //Modality
            if (bReqModality)
            {
               
                if (Program.NEW_MODALITY.Trim() != "")
                {
                    respSeq.Items[0].Add(new DicomCodeString(DicomTag.Modality, Program.NEW_MODALITY));
                }
                else
                {
                    respSeq.Items[0].Add(new DicomCodeString(DicomTag.Modality, ht["MODALITY"].ToString()));
                }
            }

            if (bReqScheduledAETitle)
            {
                respSeq.Items[0].Add(new DicomApplicationEntity(DicomTag.ScheduledStationAETitle, Program.REMOTE_AE));
            }

            //申请开始日期
            if (bReqStartDate)
            {
                respSeq.Items[0].Add(new DicomDate(DicomTag.ScheduledProcedureStepStartDate,Convert.ToDateTime(ht["REGISTRATION_DATE"]).ToString("yyyyMMdd")));
            }
            //申请开始时间
            if (bReqStartTime)
            {
                respSeq.Items[0].Add(new DicomTime(DicomTag.ScheduledProcedureStepStartTime, Convert.ToDateTime(ht["REGISTRATION_DATE"]).ToString("hhmmss")));
            }

            
            //申请结束日期
            if (bReqEndDate)
            {
                respSeq.Items[0].Add(new DicomDate(DicomTag.ScheduledProcedureStepEndDate, Convert.ToDateTime(ht["REGISTRATION_DATE"]).ToString("yyyyMMdd")));
            }
            //申请结束时间
            if (bReqEndTime)
            {
                respSeq.Items[0].Add(new DicomTime(DicomTag.ScheduledProcedureStepEndTime, Convert.ToDateTime(ht["REGISTRATION_DATE"]).ToString("hhmmss")));
            }
            //操作技师
            if (bReqPerfPhysican)
            {
            
                respSeq.Items[0].Add(new DicomPersonName(DicomTag.ScheduledPerformingPhysicianName, DicomEncoding.GetEncoding(Program.ENCODING), ht["REG_OPERATOR_NAME"].ToString()));
            }

            if (bReqProcStepDesc)
            {
                respSeq.Items[0].Add(new DicomLongString(DicomTag.ScheduledProcedureStepDescription, DicomEncoding.GetEncoding(Program.ENCODING), " "));
            }

            if (bReqProcStepID)
            {
                
                respSeq.Items[0].Add(new DicomShortString(DicomTag.ScheduledProcedureStepID, DicomEncoding.GetEncoding(Program.ENCODING),ht["PROCESS_NUM"].ToString()));
            }

            if (bReqSchedStation)
            {
               
                respSeq.Items[0].Add(new DicomShortString(DicomTag.ScheduledStationName, DicomEncoding.GetEncoding(Program.ENCODING), " "));
            }

            if (bReqProcStepLoc)
            {
                
                respSeq.Items[0].Add(new DicomShortString(DicomTag.ScheduledProcedureStepLocation, DicomEncoding.GetEncoding(Program.ENCODING), " "));
            }

            if (bReqPreMed)
            {
                respSeq.Items[0].Add(new DicomLongString(DicomTag.PreMedication, DicomEncoding.GetEncoding(Program.ENCODING), " "));
            }

            if (bReqComments)
            {
                respSeq.Items[0].Add(new DicomLongText(DicomTag.CommentsOnTheScheduledProcedureStep, DicomEncoding.GetEncoding(Program.ENCODING), "no coment"));
            }
            
            return resp;
        }

        public void OnConnectionClosed(Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}

