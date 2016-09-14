using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACExpertServer;

namespace TestACExpert
{
    class Program
    {
        static void Main(string[] args)
        {
            ACExpertProfile t_profile = new ACExpertProfile();

            t_profile.Variables.Add("StopLoss", 1.2345);
        }
    }
}
