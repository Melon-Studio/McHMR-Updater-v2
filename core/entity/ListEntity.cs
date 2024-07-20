using System.Collections.Generic;

//本类的最大作用就是用来返回在获取白名单列表和哈希文件列表时返回的多个数据
namespace McHMR_Updater_v2.core.entity
{
    internal class ListEntity
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
}
