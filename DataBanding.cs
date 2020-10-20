using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DiffCountByBeyond.Services
{
    public class DataBanding : BindBase
    {

        public ObservableCollection<ItemAndLine> ItemAndLines { get; set; } = new ObservableCollection<ItemAndLine>();
        private string basePath;
        private string newPath;
        private string baseFileName;
        private string newFileName;
        private string fileMask;

        public string BasePath { get => basePath; set => SetProperty(ref basePath, value); }
        public string NewPath { get => newPath; set => SetProperty(ref newPath, value); }
        public string BaseFileName { get => baseFileName; set => SetProperty(ref baseFileName, value); }
        public string NewFileName { get => newFileName; set => SetProperty(ref newFileName, value); }
        public string FileMask { get => fileMask; set => SetProperty(ref fileMask, value); }


    }

    public enum Items
    {
        非代码Base孤立行,
        非代码New孤立行,
        非代码差异行,
        代码Base孤立行,
        代码New孤立行,
        代码差异行,
        代码差异行和New新增行总数,
        代码不同行总数,
        不相同行总数
    }

    public class ItemAndLine : BindBase
    {

        private Items items;
        private UInt64 lines;
        private string remarks;

        public ItemAndLine(Items items, UInt64 lines,string remarks)
        {
            this.Items = items;
            this.Lines = lines;
            this.remarks = remarks;
        }

        public Items Items
        {
            get => items; set => SetProperty(ref items, value);
        }
        public UInt64 Lines
        {
            get => lines; set => SetProperty(ref lines, value);
        }
        public string Remarks { get => remarks; set => SetProperty(ref remarks, value); }
    }
}
