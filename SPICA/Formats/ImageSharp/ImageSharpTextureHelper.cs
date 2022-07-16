using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SPICA
{
    public class ImageSharpTextureHelper
    {
        /// <summary>
        /// Exports a uncompressed rgba8 image given the file path, data, width and height
        /// </summary>
        public static void ExportFile(string filePath, byte[] data, int width, int height)
        {
            var file = Image.LoadPixelData<Rgba32>(data, width, height);
            file.Save(filePath);
        }

        public static byte[] DropShadows(byte[] data, int width, int height)
        {
            var file = Image.LoadPixelData<Rgba32>(data, width, height);
            file.Mutate(x => x.GaussianBlur());
            byte[] newData = file.GetSourceInBytes();
            //dispose file
            file.Dispose();
            return newData;
        }

        /// <summary>
        /// Gets the uncompressed rgba8 image given the file path, width and height
        /// </summary>
        public static byte[] GetRgba(string filePath)
        {
            var file = Image.Load<Rgba32>(filePath);
            byte[] data = file.GetSourceInBytes();
            //dispose file
            file.Dispose();
            return data;
        }

        /// <summary>
        /// Gets the uncompressed rgba8 image given the file path.
        /// The given width and height will resize the output of the rgba data.
        /// </summary>
        public static byte[] GetRgba(string filePath, int width, int height)
        {
            var file = Image.Load<Rgba32>(filePath);
            if (file.Width != width || file.Height != height)
                Resize(file, width, height);

            var data = file.GetSourceInBytes();
            //dispose file
            file.Dispose();
            return data;
        }

        /// <summary>
        /// Resizes the given image with a new width and height using the Lanczos3 resampler algoritim.
        /// </summary>
        public static void Resize(Image<Rgba32> baseImage, int newWidth, int newHeight) {
            baseImage.Mutate(context => context.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
        }

        /// <summary>
        /// Generates mipmaps with the given mipmap count from the image provided.
        /// </summary>
        public static Image<Rgba32>[] GenerateMipmaps(Image<Rgba32> baseImage, uint mipLevelCount)
        {
            Image<Rgba32>[] mipLevels = new Image<Rgba32>[mipLevelCount];
            mipLevels[0] = baseImage;
            int i = 1;

            int currentWidth = baseImage.Width;
            int currentHeight = baseImage.Height;
            while ((currentWidth != 1 || currentHeight != 1) && i < mipLevelCount)
            {
                int newWidth = Math.Max(1, currentWidth / 2);
                int newHeight = Math.Max(1, currentHeight / 2);
                Image<Rgba32> newImage = baseImage.Clone(context => context.Resize(new ResizeOptions()
                {
                    Size = new Size(newWidth, newHeight),
                    //Many textures require this to be disabled as they can have 0 alpha, full rgba color
                    //As the alpha can be used as a mask or sort of specular channel
                    PremultiplyAlpha = false,
                }));
                Debug.Assert(i < mipLevelCount);
                mipLevels[i] = newImage;

                i++;
                currentWidth = newWidth;
                currentHeight = newHeight;
            }

            Debug.Assert(i == mipLevelCount);

            return mipLevels;
        }
    }
}
