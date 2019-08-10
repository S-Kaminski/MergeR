using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergeR_BGWorker
{
    public class Tiles
    {
        public string Path { get; set; }
        public string TileName { get; set; }
        public string DirectoryName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Image PartImage { get; set; }

        public Tiles(string _path, int x, int y, string dirName, string tileName)
        {
            Path = _path;
            PartImage = Image.FromFile(Path);
            X = x;
            Y = y;
            DirectoryName = dirName;
            TileName = tileName;
        }
    }
}
