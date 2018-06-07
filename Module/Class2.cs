using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Forms;
namespace GroundCS.Module
{
    public class Class2
    {
        StreamWriter TargetFile;
        
        private Bitmap bitmap;

        public string OpenData(string LTarget)
        {
            string prop = null;
            string FILE_NAME = LTarget;
            try
            {
                
                System.IO.StreamReader objReader = new System.IO.StreamReader(FILE_NAME);
                prop = objReader.ReadToEnd();
                objReader.Close();
                return prop;
            }
            catch
            {
                MessageBox.Show("No File Can Be Opened");

            }
            return prop;
        }

        public void SaveData(string Prop, string LTarget)
        {
            if (System.IO.File.Exists(LTarget) == true)
            {
                System.IO.File.Delete(LTarget);
            }
            try
            {
                TargetFile = new StreamWriter(LTarget, true);
            }
            catch
            {
                //MessageBox.Show("Error opening " + LTarget);
            }

            try
            {
                TargetFile.Write(Prop);
            }
            catch
            {
                //MessageBox.Show("Error writing file");
            }

            TargetFile.Close();
           
        }
        public void Download(string imageUrl)
        {
            try
            {
                WebClient client = new WebClient();
                Stream stream = client.OpenRead(imageUrl);
                bitmap = new Bitmap(stream);
                stream.Flush();
                stream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public Bitmap GetImage()
        {
            return bitmap;
        }
        public void SaveImage(string filename, ImageFormat format)
        {
            if (bitmap != null)
            {
                bitmap.Save(filename, format);
            }
        }
    }
}
