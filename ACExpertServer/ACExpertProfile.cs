using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACExpertServer
{
    ///<summary>
    /// A profile business object for storing Expert Advisor parameters
    /// Usage: save/restore and trasmit EA parameters within the ACExpert Client and Server
    ///</summary>
    public class ACExpertProfile
    {
        public string Name { get; set; }
        public Dictionary<string, double> Variables { get; }
    }
}
