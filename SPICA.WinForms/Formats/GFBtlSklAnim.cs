using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.GFL;
using SPICA.Formats.GFL.Motion;

using System.IO;

namespace SPICA.WinForms.Formats
{
    class GFBtlSklAnim
    {
        public static H3D OpenAsH3D(Stream Input, GFPackage.Header Header, H3DDict<H3DBone> Skeleton)
        {
            H3D Output = new H3D();

            //Skeletal Animations
            Input.Seek(Header.Entries[0].Address, SeekOrigin.Begin);

            GF1MotionPack MotPack = new GF1MotionPack(Input);

            foreach (GF1Motion Mot in MotPack)
            {
                H3DAnimation SklAnim = Mot.ToH3DSkeletalAnimation(Skeleton);

                SklAnim.Name = $"Motion_{Mot.Index}";

                Output.SkeletalAnimations.Add(SklAnim);
            }

            //Material Animations
            Input.Seek(Header.Entries[1].Address, SeekOrigin.Begin);

            GFPackage.Header PackHeader = GFPackage.GetPackageHeader(Input);

            foreach (GFPackage.Entry Entry in PackHeader.Entries)
            {
                Input.Seek(Entry.Address, SeekOrigin.Begin);

                if (Entry.Length > 0)
                {
                    byte[] Data = new byte[Entry.Length];

                    Input.Read(Data, 0, Data.Length);

                    H3D MatAnims = H3D.Open(Data);

                    Output.Merge(MatAnims);
                }
            }

            return Output;
        }
    }
}
