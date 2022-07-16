using SPICA.PICA.Commands;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrGfx.Texture
{
    [TypeChoice(0x20000009u, typeof(GfxTextureCube))]
    [TypeChoice(0x20000011u, typeof(GfxTextureImage))]
    public class GfxTexture : GfxObject
    {
        public int Height;
        public int Width;

        public uint GLFormat;
        public uint GLType;

        public int MipmapSize;

        private uint TextureObj;
        private uint LocationFlag;

        public PICATextureFormat HwFormat;

        public static GfxTexture FromH3D(CtrH3D.Texture.H3DTexture texture)
        {
            GfxTextureImage image = new GfxTextureImage();
            image.Image = new GfxTextureImageData();
            image.Name = texture.Name;
            image.Width = texture.Width;
            image.Height = texture.Height;
            image.GLFormat = 26458;
            image.GLType = 0;
            image.HwFormat = texture.Format;
            image.MipmapSize = texture.MipmapSize;
            image.Image.Width = texture.Width;
            image.Image.Height = texture.Height;
            image.Image.BitsPerPixel = GetBPP(texture.Format);
            image.Image.RawBuffer = texture.RawBuffer;
            image.LocationFlag = 0;
            image.Header.MagicNumber = 0x424F5854;

            return image;
        }

        public CtrH3D.Texture.H3DTexture ToH3D()
        {
            var Tex = new CtrH3D.Texture.H3DTexture()
            {
                Name = this.Name,
                Width = this.Width,
                Height = this.Height,
                Format = this.HwFormat,
                MipmapSize = (byte)this.MipmapSize
            };

            if (this is GfxTextureCube)
            {
                Tex.RawBufferXPos = ((GfxTextureCube)this).ImageXPos.RawBuffer;
                Tex.RawBufferXNeg = ((GfxTextureCube)this).ImageXNeg.RawBuffer;
                Tex.RawBufferYPos = ((GfxTextureCube)this).ImageYPos.RawBuffer;
                Tex.RawBufferYNeg = ((GfxTextureCube)this).ImageYNeg.RawBuffer;
                Tex.RawBufferZPos = ((GfxTextureCube)this).ImageZPos.RawBuffer;
                Tex.RawBufferZNeg = ((GfxTextureCube)this).ImageZNeg.RawBuffer;
            }
            else
            {
                Tex.RawBuffer = ((GfxTextureImage)this).Image.RawBuffer;
            }
            return Tex;
        }

        static int GetBPP(PICATextureFormat format)
        {
            int[] FmtBPP = new int[] { 32, 24, 16, 16, 16, 16, 16, 8, 8, 8, 4, 4, 4, 8 };
            return FmtBPP[(int)format];
        }
    }
}
