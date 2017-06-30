using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimilarImages
{
    class ImageInfo
    {
        private int _index;

        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }
        private string _imgVector;

        public string ImgVector
        {
            get { return _imgVector; }
            set { _imgVector = value; }
        }
    }
}
