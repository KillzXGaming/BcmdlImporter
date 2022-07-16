using SPICA.Formats.CtrGfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BcmdlImporter

{
    public struct TextureMeta
    {
        public string Format { get; set; }
        public int MipCount { get; set; }
        public GfxDict<GfxMetaData> MetaData { get; set; }
    }
}
