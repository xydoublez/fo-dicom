using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using System.Collections;
using System.IO;

namespace MRConvert
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                
                if (true)
                {
                    System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
                    w.Start();
                    //string path = args[0];
                    string path = @"E:\dicom_files\CT\20141228\4";
                    string[] files = System.IO.Directory.GetFiles(path);
                    ArrayList list = new ArrayList();
                    foreach (string file in files)
                    {
                        try {
                            DicomFile f= DicomFile.Open(file);
                            var ff = f.ChangeTransferSyntax(DicomTransferSyntax.ExplicitVRLittleEndian);
                            //var ff = f.ChangeTransferSyntax(DicomTransferSyntax.JPEGLSLossless);
                            MemoryStream ms = new MemoryStream();
                            ff.Save(ms);
                            list.Add(ms);
                            ms.Close();
                            //ff.Save(@"E:\dicom_files\CT\20141228\3\"+ Path.GetFileName(file));
                            //ff.Save(file + "_lzq");



                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.WriteLine(file+":"+ex.Message);
                        }

                    }
                    w.Stop();
                    //Console.WriteLine("耗时："+w.ElapsedMilliseconds/1000+"秒");
                    System.Diagnostics.Trace.WriteLine("耗时：" + w.ElapsedMilliseconds + "秒");
                    Console.ReadLine();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine(e.Message);
            }
            finally
            {
                Environment.Exit(0);
            }
        }
    }
}
