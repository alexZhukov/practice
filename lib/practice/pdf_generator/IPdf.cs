using System;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace CreatePDFApp
{
    class IPdf
    {
        private  Rectangle pagesize=new Rectangle(800f, 1200f);
        private BaseFont _basefont = BaseFont.CreateFont("c:/windows/fonts/tahoma.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
        private float verticalSpace =30;
        private float startX = 20;
        private  float startY;
        private Document doc;
        public string path;
        private float fontSize = 24;


        public IPdf() {
           startY = pagesize.Height - verticalSpace;

        }
        public IPdf(string Path)
        {
            path = Path;
            startY = pagesize.Height - verticalSpace;
        }
        public void writeErrors(string filename , string [] errorsText) {
            try
            {
                doc = new Document(pagesize, 0f, 0f, 0f, 0f);
                PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(path + "/" + filename + ".pdf", FileMode.Create));
                doc.Open();
                PdfContentByte canvas = writer.DirectContent;
                canvas.BeginText();
                canvas.SetFontAndSize(_basefont, fontSize);
                float y = startY;
                foreach (string str in errorsText)
                {
                    int length = str.Length;
                    int tempLength;
                    if (str.Length > 60) { tempLength = 60; } else { tempLength = length; }
                    canvas.ShowTextAligned(PdfContentByte.ALIGN_LEFT, "- " + str.Substring(0,tempLength), startX, y, 0f);
                    y -= verticalSpace;
                    length -= 60;
                    while (length >0)
                    {
                        if (length > 60) { tempLength = 60; } else { tempLength = length; }
                            canvas.ShowTextAligned(PdfContentByte.ALIGN_LEFT, str.Substring(str.Length - length,tempLength), startX, y, 0f);
                            y -= verticalSpace;
                            length -= 60;
                        }
                }
                canvas.EndText();
                doc.Close();
            }
            catch(Exception ex) {
                System.IO.File.AppendAllText(path+"/"+"logFile.txt",ex.ToString());
            }
        }
    }
}/* Пример работы
            IPdf test = new IPdf(".");
            string[] str2 = new string[5];
            for (int j = 0; j < str2.Length;j++ )
            {
                str2[j] = "I'm string number " + (j+1).ToString()+"МОЯ БАЙ!";
            }
             string fileName=Console.ReadLine();
            while (!string.IsNullOrEmpty(fileName)){
            test.writeErrors(fileName , str2);
            fileName = Console.ReadLine();
            }*/