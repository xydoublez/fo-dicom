using System;
using System.IO;
using Dicom;
using Dicom.Network;
using Dicom.Log;

namespace ZYStoreScp
{
    class Program {
        /// <summary>
        /// 存储路径如e:\\dicom_files\\
        /// </summary>
        static string StoragePath = @".\DICOM";
        /// <summary>
        /// 端口
        /// </summary>
        static  int Port = 102;
        static  string AETitle = "ZYPACS";
        static string storename = "";
        static string strHospitalId = "-1";
        static string strConfigModality = "";
        private static  System.Collections.Hashtable unique(string[] ss)
        {
            System.Collections.Hashtable ht = new System.Collections.Hashtable();
            foreach (string s in ss)
            {
                if (!ht.ContainsValue(s))
                {
                    ht.Add(s, s);
                }
            }
            return ht;
            
        }
		static void Main(string[] args) {
            //string s = "PATIENT_ID,PATIENT_NAME,PATIENT_BRITHDATE,PATIENT_SEX,PATIENT_COMMENTS,OTHER_PATIENT_ID,OTHER_PATIENT_NAME,STUDY_INSTANCE_UID,STUDY_ID,ACCESSION_NUMBER,STUDY_DATE,STUDY_TIME,STUDY_DESCRIPTION,PAITENT_AGE,REFERRING_PHYSICIAN_NAME,ADDITIONAL_PATIENT_HISTORY,STATION_NAME,SERIES_INSTANCE_UID,SERIES_NUMBER,SERIES_DATE,SERIES_TIME,SERIES_DESCRIPTION,BODY_PART_EXMINED,OPERATORS_NAME,PROTOCOL_NAME,SOPINSTANCE_UID,SOPCLASS_UID,INSTANCE_NUMBER,ACQUISITION_NUMBER,ACQUISITION_DATE,ACQUISTION_TIME,REFERENCE_FILE,MODALITY,STUDY_INSTANCE_UID,SERIES_INSTANCE_UID,SOPINSTANCE_UID";
            //string[] ss = s.Split(',');
            //StringBuilder sb = new StringBuilder();
            //System.Collections.Hashtable ht = unique(ss);

            //foreach(System.Collections.DictionaryEntry  h  in ht)
            //{
                
            //    sb.Append(" ht.Add(\"").Append(h.Value).Append("\", dataSet.Get<string>(DicomTag.PatientID));\n");
            //}
           
            if (args.Length != 6)
            {
                Console.WriteLine("参数不正确!  STORENAME 端口 AETITLE STOREPATH HOSPITAL_ID CONFIG_MODALITY!");
                Console.ReadKey();
                return;
            }
            storename = args[0];
            int.TryParse(args[1], out Port);
            AETitle = args[2];
            StoragePath = args[3];
            strHospitalId = args[4];
            strConfigModality = args[5];
			// initialize NLog logging
			//var config = new LoggingConfiguration();

			//var target = new ColoredConsoleTarget();
			//target.Layout = "${message}";
			//config.AddTarget("Console", target);
			//config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Info, target));

			//LogManager.Configuration = config;


			// preload dictionary to prevent timeouts
			var dict = DicomDictionary.Default;


			// start DICOM server on port 104
            try
            {
                var server = new DicomServer<CStoreSCP>(Port);
            }
            catch
            {
                //System.Windows.Forms.MessageBox.Show("端口不正确或端口已经被占用!" + "\n");
                Console.WriteLine("端口不正确或端口已经被占用");
                return;
            }

			// end process
            Console.Title = "   服务名称: "+storename + " 端口号: " + Port.ToString() + " AETITLE: " + AETitle;
			Console.WriteLine("Press <return> to end...");
			Console.ReadLine();
		}

		class CStoreSCP : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider {
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

			public CStoreSCP(Stream stream, Logger log) : base(stream, log) {
			}

			public void OnReceiveAssociationRequest(DicomAssociation association) {
                if (association.CalledAE != AETitle)
                {
                    SendAssociationReject(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
                    return;
                }

				foreach (var pc in association.PresentationContexts) {
					if (pc.AbstractSyntax == DicomUID.Verification)
						pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
					else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None)
						pc.AcceptTransferSyntaxes(AcceptedImageTransferSyntaxes);
				}

				SendAssociationAccept(association);
			}

			public void OnReceiveAssociationReleaseRequest() {
				SendAssociationReleaseResponse();
			}

			public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason) {
			}

			public void OnConnectionClosed(int errorCode) {
			}
            /// <summary>
            /// 获取单个DCM文件的键值对信息
            /// </summary>
            /// <param name="dataSet">数据信息</param>
            /// <param name="filepath">存储物理路径</param>
            /// <returns></returns>
            private System.Collections.Hashtable GetDcmHt(DicomDataset dataSet,string filepath)
            {
                System.Collections.Hashtable ht = new System.Collections.Hashtable();
                ht.Add("ACQUISTION_TIME", dataSet.Get<string>(DicomTag.AcquisitionTime)==null?"":dataSet.Get<string>(DicomTag.AcquisitionTime));
                ht.Add("ACCESSION_NUMBER", dataSet.Get<string>(DicomTag.AccessionNumber));
                ht.Add("STUDY_INSTANCE_UID", dataSet.Get<string>(DicomTag.StudyInstanceUID));
                ht.Add("STUDY_ID", dataSet.Get<string>(DicomTag.StudyID));
                ht.Add("STUDY_DESCRIPTION", dataSet.Get<string>(DicomTag.StudyDescription));
                ht.Add("SOPCLASS_UID", dataSet.Get<string>(DicomTag.SOPClassUID));
                ht.Add("SERIES_DESCRIPTION", dataSet.Get<string>(DicomTag.SeriesDescription));
                ht.Add("ACQUISITION_DATE", dataSet.Get<string>(DicomTag.AcquisitionDate));
                ht.Add("STATION_NAME", dataSet.Get<string>(DicomTag.StationName));
                ht.Add("MODALITY", dataSet.Get<string>(DicomTag.Modality));
                ht.Add("ACQUISITION_NUMBER", dataSet.Get<string>(DicomTag.AcquisitionNumber));
                ht.Add("PATIENT_NAME", dataSet.Get<string>(DicomTag.PatientName));
                ht.Add("PATIENT_SEX", dataSet.Get<string>(DicomTag.PatientSex));
                ht.Add("PATIENT_BRITHDATE", dataSet.Get<string>(DicomTag.PatientBirthDate));
                ht.Add("OTHER_PATIENT_ID", dataSet.Get<string>(DicomTag.OtherPatientIDs));
                ht.Add("OPERATORS_NAME", dataSet.Get<string>(DicomTag.OperatorsName));
                ht.Add("STUDY_DATE", dataSet.Get<string>(DicomTag.StudyDate));
                ht.Add("PROTOCOL_NAME", dataSet.Get<string>(DicomTag.ProtocolName));
                ht.Add("SOPINSTANCE_UID", dataSet.Get<string>(DicomTag.SOPInstanceUID));
                ht.Add("SERIES_DATE", dataSet.Get<string>(DicomTag.SeriesDate));
                ht.Add("INSTANCE_NUMBER", dataSet.Get<string>(DicomTag.InstanceNumber));
                ht.Add("SERIES_TIME", dataSet.Get<string>(DicomTag.SeriesTime));
                ht.Add("ADDITIONAL_PATIENT_HISTORY", dataSet.Get<string>(DicomTag.AdditionalPatientHistory));
                ht.Add("PATIENT_ID", dataSet.Get<string>(DicomTag.PatientID));
                ht.Add("SERIES_INSTANCE_UID", dataSet.Get<string>(DicomTag.SeriesInstanceUID));
                ht.Add("PAITENT_AGE", dataSet.Get<string>(DicomTag.PatientAge));
                ht.Add("SERIES_NUMBER", dataSet.Get<string>(DicomTag.SeriesNumber));
                ht.Add("REFERRING_PHYSICIAN_NAME", dataSet.Get<string>(DicomTag.ReferringPhysicianName));
                ht.Add("OTHER_PATIENT_NAME", dataSet.Get<string>(DicomTag.OtherPatientIDs));
                ht.Add("PATIENT_COMMENTS", dataSet.Get<string>(DicomTag.PatientComments));
                ht.Add("BODY_PART_EXAMINED", dataSet.Get<string>(DicomTag.BodyPartExamined));
                ht.Add("STUDY_TIME", dataSet.Get<string>(DicomTag.StudyTime));
                ht.Add("REFERENCE_FILE", filepath);
                return ht;
                
            }
			public DicomCStoreResponse OnCStoreRequest(DicomCStoreRequest request) {
				var studyUid = request.Dataset.Get<string>(DicomTag.StudyInstanceUID);
				var instUid = request.SOPInstanceUID.UID;

                var path = Path.GetFullPath(Program.StoragePath);
                //文件路径格式 modality studydate patient_id
                path = Path.Combine(path, request.Dataset.Get<string>(DicomTag.StudyDate) + "\\" + request.Dataset.Get<string>(DicomTag.PatientID));
				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);

				path = Path.Combine(path, instUid) + ".dcm";

				request.File.Save(path);
                dbUtility updatedb = new dbUtility(strHospitalId, strConfigModality);
                System.Collections.Hashtable ht = GetDcmHt(request.Dataset, path);
                updatedb.UpdateDb(ht);
				return new DicomCStoreResponse(request, DicomStatus.Success);
			}

			public void OnCStoreRequestException(string tempFileName, Exception e) {
				// let library handle logging and error response
			}

			public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request) {
				return new DicomCEchoResponse(request, DicomStatus.Success);
			}
		}
	}
}
