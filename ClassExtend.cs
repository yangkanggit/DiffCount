using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiffCountByBeyond
{
    public static class ClassExtend
    {
        public static bool fileInMask(this String filePath,string masks)
        {
            string fileEnd = System.IO.Path.GetExtension(filePath);
            string[] maskArray = masks.Split('|');
            if (maskArray.Length > 0)
            {
                foreach(var mask in maskArray)
                {
                    if (mask.Equals(fileEnd))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static UInt32 GetNumFirst(this String str)
        {
            int numStartPos = 0, numCount = 0;
            for (int i=0;i<str.Length;i++)
            {
                if (str[i] >= '0' && str[i] <= '9')
                {
                    if (numCount == 0)
                    {
                        numStartPos = i;
                    }
                    numCount++;
                }
                else if (numCount == 0)
                {
                    continue;
                }
                else
                {
                    break;
                }
            }
            string strnum = str.Substring(numStartPos, numCount);
            return UInt32.Parse(strnum);
        }
    }
}
