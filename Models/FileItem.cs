using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public class FileItem
    {
        private string _fileName;
        private string _imageSource;

        public FileItem(string fileName, string imageSource) 
        {
            FileName = fileName;
            ImageSource = imageSource;

        }

        public string FileName { get { return _fileName; } set { _fileName = value; } }
        public string ImageSource { get { return _imageSource; } set { _imageSource = value; } }
    }
}
