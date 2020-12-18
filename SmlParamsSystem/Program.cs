using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LoggingSystem;

namespace SmlParamsSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            clsSmlPrm clsZ = new clsSmlPrm();
            clsZ.SmlParams(int.Parse(args[0]), args[1]);

        }
    }
}
