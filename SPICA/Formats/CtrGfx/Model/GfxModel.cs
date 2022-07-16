using SPICA.Formats.CtrGfx.Model.Material;
using SPICA.Formats.CtrGfx.Model.Mesh;
using SPICA.PICA.Commands;
using SPICA.Serialization.Attributes;

using System.Collections.Generic;

namespace SPICA.Formats.CtrGfx.Model
{
    [TypeChoice(0x40000012u, typeof(GfxModel))]
    [TypeChoice(0x40000092u, typeof(GfxModelSkeletal))]
    public class GfxModel : GfxNodeTransform
    {
        public List<GfxMesh> Meshes;

        public GfxDict<GfxMaterial> Materials;

        public List<GfxShape> Shapes;

        public GfxDict<GfxMeshNodeVisibility> MeshNodeVisibilities;

        public GfxModelFlags Flags;

        public PICAFaceCulling FaceCulling;

        public int LayerId;

        public GfxModel()
        {
            Meshes = new List<GfxMesh>();

            Materials = new GfxDict<GfxMaterial>();

            Shapes = new List<GfxShape>();

            MeshNodeVisibilities = new GfxDict<GfxMeshNodeVisibility>();

            this.Header.MagicNumber = 0x4C444D43;
            this.Header.Revision = 150994944;
        }
    }
}
