using System;
using Dicom;
using Dicom.Network;
using System.Threading;

namespace WorklistSCU
{
    class Program
    {
        static void Main(string[] args)
        {
            MyWorklistSCU m = new MyWorklistSCU();
            m.Run();
        }
    }


    /// <summary>
    /// configuration for this demo
    /// </summary>
    static class Config
    {
        public const string ENCODING = "GB18030";
        public const string LOCAL_AE = "WorklistSCU";
        public const string REMOTE_AE = "WorklistSCP";
        public const string REMOTE_IP = "127.0.0.1";
        public const int    REMOTE_PORT = 5001;
        public const string IMPLEMENTATION_VERSION_NAME = "WorklistSCU";
        public const string IMPLEMENTATION_CLASS_UID = "1.2.392.0000000.1.1";
        public const uint   MAX_PDU_LENGTH = 16 * 1024;
    }

    /// <summary>
    /// this class contains all we need for a DICOM Worklist SCU implementation
    /// </summary>
    class MyWorklistSCU
    {
        //Logger logger;

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

            // set Implementation Version string
            // is sent in dicom messages to specify the type of sender 
            // can be skipped (fo-dicom defaults are used in this case)
            DicomImplementation.Version = Config.IMPLEMENTATION_VERSION_NAME;
            DicomImplementation.ClassUID = new DicomUID(Config.IMPLEMENTATION_CLASS_UID, "Implementation Class UID", DicomUidType.Unknown);

            // now we create the Worklist C-FIND request
            DicomCFindRequest req = new DicomCFindRequest(DicomQueryRetrieveLevel.Worklist, DicomPriority.Medium);
            //req.SOPClassUID = DicomUID.ModalityWorklistInformationModelFIND;
            
            // set an event handler for our request
            // this handler will get the response (answer received from SCP)
            req.OnResponseReceived = this.ResponseReceived;

            // now add the fields to our request
            // this request contains information on search criteria (all field which are filled with data)
            // and the return parameters (all fields defined in the messge are requested as return values)
            req.Dataset.Add(DicomTag.SpecificCharacterSet, Config.ENCODING);
            req.Dataset.Add(DicomTag.AccessionNumber, "");
            req.Dataset.Add(DicomTag.ReferringPhysicianName, "");
            req.Dataset.Add(DicomTag.PatientName, "");
            req.Dataset.Add(DicomTag.PatientID, "");
            req.Dataset.Add(DicomTag.PatientBirthDate, "");
            req.Dataset.Add(DicomTag.PatientSex, "");
            req.Dataset.Add(DicomTag.PatientAddress, "");
            req.Dataset.Add(DicomTag.PatientTelephoneNumbers, "");
            req.Dataset.Add(DicomTag.StudyInstanceUID, "");
            req.Dataset.Add(DicomTag.RequestedProcedureDescription, "");
            req.Dataset.Add(DicomTag.AdmissionID, "");
            DicomSequence seq = new DicomSequence(DicomTag.ScheduledProcedureStepSequence, new DicomDataset());
            req.Dataset.Add(seq);
            seq.Items[0].Add(DicomTag.Modality, "");
            seq.Items[0].Add(DicomTag.ScheduledStationAETitle, Config.LOCAL_AE);
            seq.Items[0].Add(DicomTag.ScheduledProcedureStepStartDate, new DicomDateRange(DateTime.Today, DateTime.Today.Add(TimeSpan.FromDays(1))));
            seq.Items[0].Add(DicomTag.ScheduledProcedureStepStartTime, "");
            seq.Items[0].Add(DicomTag.ScheduledProcedureStepEndDate, "");
            seq.Items[0].Add(DicomTag.ScheduledProcedureStepEndTime, "");
            seq.Items[0].Add(DicomTag.ScheduledPerformingPhysicianName, "");
            seq.Items[0].Add(DicomTag.ScheduledProcedureStepDescription, "");
            seq.Items[0].Add(DicomTag.ScheduledProcedureStepID, "");
            seq.Items[0].Add(DicomTag.ScheduledStationName, "");
            seq.Items[0].Add(DicomTag.ScheduledProcedureStepLocation, "");
            seq.Items[0].Add(DicomTag.PreMedication, "");
            seq.Items[0].Add(DicomTag.CommentsOnTheScheduledProcedureStep, "");
            req.Dataset.Add(DicomTag.RequestedProcedureID, "");
            req.Dataset.Add(DicomTag.ReasonForTheRequestedProcedure, "");
            req.Dataset.Add(DicomTag.RequestedProcedureLocation, "");
            req.Dataset.Add(DicomTag.RequestedProcedureComments, "");
            req.Dataset.Add(DicomTag.ReasonForTheImagingServiceRequestRETIRED, "");

            // create and configure the DicomClient object
            // this will handle the communication
            var client = new DicomClient();
            //client.Logger = logger;
            client.Options = new DicomServiceOptions();
            //client.Options.MaximumPDULength = Config.MAX_PDU_LENGTH;
            client.Options.LogDataPDUs = true;
            client.Options.LogDimseDatasets = true;
            Console.Title = "WorkListScu";

            try {
               // logger.Info("ThreadID: " + Thread.CurrentThread.ManagedThreadId.ToString() + " - Start Sending");
                // add the request to the client object 
                client.AddRequest(req);
                // send the request to the SCU
                // this function will return, when the communication has been finished!
                client.Send(Config.REMOTE_IP, Config.REMOTE_PORT, false, Config.LOCAL_AE, Config.REMOTE_AE);
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }

            // communication finished, keep the console open to show the results
            Console.WriteLine("Press <return> to end...");
            Console.ReadLine();
        }


		public void ResponseReceived(DicomCFindRequest request, DicomCFindResponse response)
        {
            // this handler is called for each received response
            // one request can create o..n response messages
            // implement here storing of the onswers
            //logger.Info("ThreadID: " + Thread.CurrentThread.ManagedThreadId.ToString() + " - Received Response");
        }
    }
}
