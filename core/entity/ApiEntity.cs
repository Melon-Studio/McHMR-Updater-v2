﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McHMR_Updater_v2.core.entity
{
    public class ApiEntity
    {
        public string apiUrl { get; set; }     
        public string serverName { get; set; }
        public string version { get; set; }
        public string launcher { get; set; }
        public string token { get; set; }
    }
}
