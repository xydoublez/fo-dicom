using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using Dicom.Network;
using Dicom.Log;
using System.IO;
namespace WorklistSCP
{

    /// <summary>
    /// configuration for this demo
    /// </summary>
    class Config 
    {
        public const string ENCODING = "ISO IR 144";
        public const int    LOCALE_PORT = 12346;
        public const int    MAX_PDU_LENGTH = 16384;
        public const string LOCAL_AE = "WorklistSCP";
        public const string IMPLEMENTATION_VERSION_NAME = "WorklistSCP";
        public const string IMPLEMENTATION_CLASS_UID = "1.2.392.0000000.1.2";
    }

    class Program
    {


        static void Main(string[] args)
        {
            MyImplementation m = new MyImplementation();
            m.Run();
        }
    }


    class MyImplementation
    {
        Logger logger;

        public void Run()
        {
            

            // initialize NLog logging
            //var config = new LoggingConfiguration();
            //var target = new ColoredConsoleTarget();
            //target.Layout = "${message}";
            //config.AddTarget("Console", target);
            //config.LoggingRules.Add(new LoggingRule("Dicom.Network", NLog.LogLevel.Info, target));
            //LogManager.Configuration = config;
            //logger = LogManager.GetLogger("Dicom.Network");


            // preload dictionary to prevent timeouts
            var dict = DicomDictionary.Default;


            // start DICOM server 
            // the server will run until server.Dispose() is called
            // during this time the TCP port wil be open and it will accept new connections
            try
            {
                DicomServer<MyDicomServiceProvider> server = new DicomServer<MyDicomServiceProvider>(Config.LOCALE_PORT);
                server.Logger = logger;
                server.Options = new DicomServiceOptions();
                server.Options.LogDataPDUs = true;
                server.Options.LogDimseDatasets = true;
                //server.Options.MaximumPDULength = Config.MAX_PDU_LENGTH;
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
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
        public MyDicomServiceProvider(Stream stream, Logger log) : base(stream, log)
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
            // check if the called AE is our program
            if (association.CalledAE != Config.LOCAL_AE) {
                // the SCU want to contact another AE --> reject
                SendAssociationReject(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
                return;
            }
                
            // check if the calling AE is allowed to contact our program
            if (association.CallingAE == "NotAllowedAE") {
                // the SCU is a not allowed program --> reject
                SendAssociationReject(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CallingAENotRecognized);
                return;
            }

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
            Random rnd = new Random();

            DicomCFindResponse[] res = new DicomCFindResponse[rnd.Next(2,15)];
            for (int i=0; i<res.Length-1; i++)
                res[i] = CreateFindResponse(request);
            
            // the last entry has status Success
            // it does not contain any data, so we are done
            res[res.Length-1] = new DicomCFindResponse(request, DicomStatus.Success);
            return res;
        }


        /// <summary>
        /// for our sample implementation, this function creates a random response matching the given request
        /// </summary>
        /// <param name="request">received C-FIND RQ</param>
        /// <returns>one C-FIND RSP with random data</returns>
        private DicomCFindResponse CreateFindResponse(DicomCFindRequest request)
        {
            // check the request for supported return fields
            bool bReqPatientName = (request.Dataset.First(i => i.Tag == DicomTag.PatientName) != null);
            bool bReqPatientID = (request.Dataset.First(i => i.Tag == DicomTag.PatientID) != null);
            bool bReqPatientBirth = (request.Dataset.First(i => i.Tag == DicomTag.PatientBirthDate) != null);
            bool bReqPatientSex = (request.Dataset.First(i => i.Tag == DicomTag.PatientSex) != null);
            bool bReqPatientAddr = (request.Dataset.First(i => i.Tag == DicomTag.PatientAddress) != null);
            bool bReqPatientPhone = (request.Dataset.First(i => i.Tag == DicomTag.PatientTelephoneNumbers) != null);
            bool bReqStudyUID = (request.Dataset.First(i => i.Tag == DicomTag.StudyInstanceUID) != null);
            bool bReqProcDesc = (request.Dataset.First(i => i.Tag == DicomTag.RequestedProcedureDescription) != null);
            bool bReqAdmissionID = (request.Dataset.First(i => i.Tag == DicomTag.AdmissionID) != null);
            bool bReqRequProcID = (request.Dataset.First(i => i.Tag == DicomTag.RequestedProcedureID) != null);
            bool bReqRequProcLoc = (request.Dataset.First(i => i.Tag == DicomTag.RequestedProcedureLocation) != null);
            bool bReqRequProcComm = (request.Dataset.First(i => i.Tag == DicomTag.RequestedProcedureComments) != null);
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
                    startDateRange = reqElem.Get<DicomDateRange>(0);
                    // we do not provide results from the past
                    if (startDateRange.Minimum < DateTime.Today) startDateRange.Minimum = DateTime.Today;
                }
                bReqStartTime = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepStartTime) != null);
                bReqEndDate = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepEndDate) != null);
                bReqEndTime = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepEndTime) != null);
                bReqPerfPhysican = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledPerformingPhysicianName) != null);
                bReqProcStepDesc = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepDescription) != null);
                bReqProcStepID = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepID) != null);
                bReqSchedStation = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledStationName) != null);
                bReqProcStepLoc = (reqSeq.Items[0].First(i => i.Tag == DicomTag.ScheduledProcedureStepLocation) != null);
                bReqPreMed = (reqSeq.Items[0].First(i => i.Tag == DicomTag.PreMedication) != null);
                bReqComments = (reqSeq.Items[0].First(i => i.Tag == DicomTag.CommentsOnTheScheduledProcedureStep) != null);
            }

            Random rand = new Random();
            // now bild a response to our request 
            // all responses with data have status Pending, because the last response with status Success never has data
            DicomCFindResponse resp = new DicomCFindResponse(request, DicomStatus.Pending);
            resp.Dataset = new DicomDataset();
            resp.Dataset.Add(DicomTag.SpecificCharacterSet, Config.ENCODING);
            if (bReqPatientName)
            {
                string[] sz = new string[] {"Tarkowski^Andrei^Arsenjewitsch=Тарковский^Андрей^Арсеньевич", 
                                            "Smith^John", 
                                            "Сталин^Иосиф^Виссарионович",
                                            "Romanow^Pjotr^Alexejewitsch =Романов^Пётр^Алексе́евич"};
                resp.Dataset.Add(new DicomPersonName(DicomTag.PatientName, DicomEncoding.GetEncoding(Config.ENCODING), sz[rand.Next(0, sz.Length)]));
            }

            if (bReqPatientID)
            {
                string sz = Convert.ToChar(rand.Next(65, 91)) + Convert.ToChar(rand.Next(65, 91)) + rand.Next(0, 999999).ToString("D6");
                resp.Dataset.Add(new DicomLongString(DicomTag.PatientID, DicomEncoding.GetEncoding(Config.ENCODING), sz));
            }

            if (bReqPatientBirth)
            {
                int age = 365 + rand.Next(0, 85 * 365);
                resp.Dataset.Add(new DicomDate(DicomTag.PatientBirthDate, DateTime.Today.Subtract(TimeSpan.FromDays(age))));
            }

            if (bReqPatientSex)
            {
                if (rand.Next(0, 2) == 0)
                {
                    resp.Dataset.Add(new DicomCodeString(DicomTag.PatientSex, "M"));
                }
                else
                {
                    resp.Dataset.Add(new DicomCodeString(DicomTag.PatientSex, "F"));
                }
            }

            if (bReqPatientAddr)
            {
                string[] sz = new string[] {"Москва, Красная Площадь", 
                                            "Hamburg, Reeperbahn", 
                                            "Paris, La Tour Eiffel",
                                            "Не́вский проспе́кт, Санкт-Петербург"};
                resp.Dataset.Add(new DicomLongString(DicomTag.PatientAddress, DicomEncoding.GetEncoding(Config.ENCODING), sz[rand.Next(0, sz.Length)]));
            }

            if (bReqPatientPhone)
            {
                string sz = "++" + rand.Next(100, 999).ToString("D3") + "-" + rand.Next(1000, 9999).ToString("D4") + "-" + rand.Next(100000, 999999).ToString("D6");
                resp.Dataset.Add(new DicomShortString(DicomTag.PatientTelephoneNumbers, DicomEncoding.GetEncoding(Config.ENCODING), sz));
            }

            if (bReqStudyUID)
            {
                string sz = "10.155.666." + rand.Next(1, 999).ToString("D") + "." + rand.Next(1, 999).ToString("D") + "." + rand.Next(1, 999).ToString("D");
                resp.Dataset.Add(new DicomUniqueIdentifier(DicomTag.StudyInstanceUID, sz));
            }

            if (bReqProcDesc)
            {
                resp.Dataset.Add(new DicomLongString(DicomTag.RequestedProcedureDescription, DicomEncoding.GetEncoding(Config.ENCODING), "No description available - Отсутствует описание"));
            }

            if (bReqAdmissionID)
            {
                string sz = Convert.ToChar(rand.Next(65, 91)) + Convert.ToChar(rand.Next(65, 91)) + rand.Next(1, 1000000).ToString("D6") + "-" + Convert.ToChar(rand.Next(65, 91));
                resp.Dataset.Add(new DicomLongString(DicomTag.AdmissionID, DicomEncoding.GetEncoding(Config.ENCODING), sz));
            }

            if (bReqRequProcID)
            {
                string sz = DateTime.Today.ToString("yyyy") + "-" + rand.Next(1, 1000000).ToString("D6");
                resp.Dataset.Add(new DicomShortString(DicomTag.RequestedProcedureID, DicomEncoding.GetEncoding(Config.ENCODING), sz));
            }

            if (bReqRequProcLoc)
            {
                string sz = "Room " + Convert.ToChar(rand.Next(65, 72));
                resp.Dataset.Add(new DicomLongString(DicomTag.RequestedProcedureLocation, DicomEncoding.GetEncoding(Config.ENCODING), sz));
            }

            if (bReqRequProcLoc)
            {
                resp.Dataset.Add(new DicomLongText(DicomTag.RequestedProcedureComments, DicomEncoding.GetEncoding(Config.ENCODING), "No comment - Нет комментариев"));
            }

            DicomSequence respSeq = new DicomSequence(DicomTag.ScheduledProcedureStepSequence, new DicomDataset());
            resp.Dataset.Add(respSeq);

            if (bReqModality)
            {
                if (szModalityFilter.Length == 0)
                {
                    // not used as filter --> define own modalities
                    szModalityFilter = new string[] { "ES", "MG", "NM", "RF", "ECG" };
                }
                respSeq.Items[0].Add(new DicomCodeString(DicomTag.Modality, szModalityFilter[rand.Next(0, szModalityFilter.Length)]));
            }

            if (bReqScheduledAETitle)
            {
                respSeq.Items[0].Add(new DicomApplicationEntity(DicomTag.ScheduledStationAETitle, szScheduledAETitles[rand.Next(0, szScheduledAETitles.Length)]));
            }

            DateTime start;
            if (startDateRange == null) startDateRange = new DicomDateRange(DateTime.Today, DateTime.Today.AddDays(10));
            if (startDateRange.Minimum == startDateRange.Maximum)
            {
                // fixed day specified --> use this one
                start = startDateRange.Minimum;
            }
            else if (startDateRange.Maximum == DateTime.MaxValue)
            {
                // minimum, but no maximum --> 0...9 days after minimum
                start = startDateRange.Minimum.AddDays(rand.Next(0, 10));
            }
            else
            {
                // minimum and maximum --> random day in this range
                start = startDateRange.Minimum.AddDays(rand.Next(0, (startDateRange.Maximum - startDateRange.Minimum).Days + 1));
            }
            start = start.AddHours(rand.Next(0, 24));
            start = start.AddMinutes(rand.Next(0, 60));
            start = start.AddSeconds(rand.Next(0, 60));

            if (bReqStartDate)
            {
                respSeq.Items[0].Add(new DicomDate(DicomTag.ScheduledProcedureStepStartDate, start));
            }

            if (bReqStartTime)
            {
                respSeq.Items[0].Add(new DicomTime(DicomTag.ScheduledProcedureStepStartTime, start));
            }

            DateTime end = start.AddHours(rand.Next(0, 2)).AddMinutes(rand.Next(0, 60)).AddSeconds(rand.Next(0, 60));
            if (bReqEndDate)
            {
                respSeq.Items[0].Add(new DicomDate(DicomTag.ScheduledProcedureStepEndDate, end));
            }

            if (bReqEndTime)
            {
                respSeq.Items[0].Add(new DicomTime(DicomTag.ScheduledProcedureStepEndTime, end));
            }

            if (bReqPerfPhysican)
            {
                string[] sz = new string[] {"Skaryna^Francysk=Скарына^Францішак ", 
                                            "Botkin^Sergei^Petrowitsch=Бо́ткин^Серге́й^Петро́вич"};
                respSeq.Items[0].Add(new DicomPersonName(DicomTag.ScheduledPerformingPhysicianName, DicomEncoding.GetEncoding(Config.ENCODING), sz[rand.Next(0, sz.Length)]));
            }

            if (bReqProcStepDesc)
            {
                respSeq.Items[0].Add(new DicomLongString(DicomTag.ScheduledProcedureStepDescription, DicomEncoding.GetEncoding(Config.ENCODING), "No Procedure Step Description - Нет Описание процедуры шаг"));
            }

            if (bReqProcStepID)
            {
                string sz = "PSID_";
                for (int i = 0; i < 5; i++) sz = sz + Convert.ToChar(rand.Next(0x0410, 0x430));
                respSeq.Items[0].Add(new DicomShortString(DicomTag.ScheduledProcedureStepID, DicomEncoding.GetEncoding(Config.ENCODING), sz));
            }

            if (bReqSchedStation)
            {
                string sz = "Station_" + Convert.ToChar(rand.Next(0x0410, 0x430));
                respSeq.Items[0].Add(new DicomShortString(DicomTag.ScheduledStationName, DicomEncoding.GetEncoding(Config.ENCODING), sz));
            }

            if (bReqProcStepLoc)
            {
                string sz = "Room_" + Convert.ToChar(rand.Next(0x0410, 0x430));
                respSeq.Items[0].Add(new DicomShortString(DicomTag.ScheduledProcedureStepLocation, DicomEncoding.GetEncoding(Config.ENCODING), sz));
            }

            if (bReqPreMed)
            {
                respSeq.Items[0].Add(new DicomLongString(DicomTag.PreMedication, DicomEncoding.GetEncoding(Config.ENCODING), "No Medication - Нет лекарств"));
            }

            if (bReqComments)
            {
                respSeq.Items[0].Add(new DicomLongText(DicomTag.CommentsOnTheScheduledProcedureStep, DicomEncoding.GetEncoding(Config.ENCODING), "No comment - нет комментариев"));
            }

            return resp;
        }
    }
}
