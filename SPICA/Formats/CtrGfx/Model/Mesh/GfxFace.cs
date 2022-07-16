using System.Collections.Generic;

namespace SPICA.Formats.CtrGfx.Model.Mesh
{
    public class GfxFace
    {
        public readonly List<GfxFaceDescriptor> FaceDescriptors;

        private uint[] BufferObjs; //One for each FaceDescriptor
        private uint Flags = 228;
        private uint CommandAlloc;

        public GfxFace()
        {
            FaceDescriptors = new List<GfxFaceDescriptor>();
            BufferObjs = new uint[1];
        }

        public void Setup()
        {
            BufferObjs = new uint[FaceDescriptors.Count];
        }
    }
}
