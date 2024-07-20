using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McHMR_Updater_v2.core.entity;
public class ListEntity
{
    public List<HashEntity> hashList
    {
        get; set;
    }
    public string whiteList
    {
        get; set;
    }
}
