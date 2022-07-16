﻿using System;
using System.Runtime.InteropServices;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SPICA
{
    class RG_ETC1
    {
        public static byte[] encodeETC(Image<Rgba32> b)
        {
            int width = b.Width;
            int height = b.Height;
            int[] pixels = new int[width * height];
            init();

            int i, j;

            var mem = new MemoryStream();
            BinaryWriter o = new BinaryWriter(mem);

            for (i = 0; i < height; i += 8)
            {
                for (j = 0; j < width; j += 8)
                {
                    int x, y;

                    Rgba32[] temp = new Rgba32[16];
                    int pi = 0;
                    for (x = i; x < i + 4; x++)
                        for (y = j; y < j + 4; y++)
                            temp[pi++] = b[y, x];

                    o.Write(GenETC1(temp));


                    temp = new Rgba32[16];
                    pi = 0;
                    for (x = i; x < i + 4; x++)
                        for (y = j + 4; y < j + 8; y++)
                            temp[pi++] = b[y, x];

                    o.Write(GenETC1(temp));


                    temp = new Rgba32[16];
                    pi = 0;
                    for (x = i + 4; x < i + 8; x++)
                        for (y = j; y < j + 4; y++)
                            temp[pi++] = b[y, x];

                    o.Write(GenETC1(temp));


                    temp = new Rgba32[16];
                    pi = 0;
                    for (x = i + 4; x < i + 8; x++)
                        for (y = j + 4; y < j + 8; y++)
                            temp[pi++] = b[y, x];

                    o.Write(GenETC1(temp));
                }
            }

            return mem.ToArray();
        }

        public static byte[] GenETC1(Rgba32[] colors)
        {
            uint[] pixels = new uint[colors.Length];

            for (int i = 0; i < colors.Length; i++)
            {
                pixels[i] = (uint)((colors[i].A << 24) | (colors[i].B << 16) | (colors[i].G << 8) | colors[i].R);
            }

            IntPtr ptr = encode_etc1(pixels, (int)ETC1_Quality.med, false);
            byte[] result = new byte[8];
            Marshal.Copy(ptr, result, 0, 8);
            ReleaseMemory(ptr);

            byte[] block = result;//pack_etc1_block(pixels);

            byte[] ne = new byte[8];
            ne[0] = block[7];
            ne[1] = block[6];
            ne[2] = block[5];
            ne[3] = block[4];
            ne[4] = block[3];
            ne[5] = block[2];
            ne[6] = block[1];
            ne[7] = block[0];

            return ne;
        }

        public static byte[] encodeETCa4(Image<Rgba32> b)
        {
            int width = b.Width;
            int height = b.Height;
            int[] pixels = new int[width * height];
            init();

            int i, j;

            var mem = new MemoryStream();
            BinaryWriter o = new BinaryWriter(mem);

            for (i = 0; i < height; i += 8)
            {
                for (j = 0; j < width; j += 8)
                {
                    int x, y;

                    Rgba32[] temp = new Rgba32[16];
                    int pi = 0;
                    for (x = i; x < i + 4; x++)
                        for (y = j; y < j + 4; y++)
                            temp[pi++] = b[y, x];

                    for (int ax = 0; ax < 4; ax++)
                        for (int ay = 0; ay < 4; ay++)
                        {
                            int a = (temp[ax + ay * 4].A >> 4);
                            ay++;
                            a |= (temp[ax + ay * 4].A >> 4) << 4;
                            o.Write((byte)a);
                        }

                    o.Write(GenETC1(temp));

                    temp = new Rgba32[16];
                    pi = 0;
                    for (x = i; x < i + 4; x++)
                        for (y = j + 4; y < j + 8; y++)
                            temp[pi++] = b[y, x];

                    for (int ax = 0; ax < 4; ax++)
                        for (int ay = 0; ay < 4; ay++)
                        {
                            int a = (temp[ax + ay * 4].A >> 4);
                            ay++;
                            a |= (temp[ax + ay * 4].A >> 4) << 4;
                            o.Write((byte)a);
                        }

                    o.Write(GenETC1(temp));

                    temp = new Rgba32[16];
                    pi = 0;
                    for (x = i + 4; x < i + 8; x++)
                        for (y = j; y < j + 4; y++)
                            temp[pi++] = b[y, x];

                    for (int ax = 0; ax < 4; ax++)
                        for (int ay = 0; ay < 4; ay++)
                        {
                            int a = (temp[ax + ay * 4].A >> 4);
                            ay++;
                            a |= (temp[ax + ay * 4].A >> 4) << 4;
                            o.Write((byte)a);
                        }

                    o.Write(GenETC1(temp));

                    temp = new Rgba32[16];
                    pi = 0;
                    for (x = i + 4; x < i + 8; x++)
                        for (y = j + 4; y < j + 8; y++)
                            temp[pi++] = b[y, x];

                    for (int ax = 0; ax < 4; ax++)
                        for (int ay = 0; ay < 4; ay++)
                        {
                            int a = (temp[ax + ay * 4].A >> 4);
                            ay++;
                            a |= (temp[ax + ay * 4].A >> 4) << 4;
                            o.Write((byte)a);
                        }

                    o.Write(GenETC1(temp));
                }
            }

            return mem.ToArray();
        }

        public enum ETC1_Quality
        {
            low = 0,
            med = 1,
            high = 2
        }

        [DllImport("RG_ETC1.dll")]
        public static extern void init();

        [DllImport("RG_ETC1.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr encode_etc1(uint[] pSrc_pixels_rgba, int quality, bool dither);

        [DllImport("RG_ETC1.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ReleaseMemory(IntPtr ptr);
    }
}