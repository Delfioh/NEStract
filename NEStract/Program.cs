using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NEStract
{
    class Program
    {

        struct NESHeader
        {
            public byte[] id;
            public byte PRG_count;
            public byte CHR_count;
        }

        static void Main(string[] args)
        {

            string filename;
            string outfilename;
            FileStream romfile;
            BinaryReader reader;


            //Expected first 4 bytes of a regular NES ROM
            byte[] correct_id = new byte[] { 0x4E, 0x45, 0x53, 0x1A }; 

            //Color Palette to be used when creating output images, actually defaults to a predefined one
            //Custom Palettes to be added at a later time
            Color[] palette = new Color[4];
            palette[0] = Color.Black;
            palette[1] = Color.LightGray;
            palette[2] = Color.Gray;
            palette[3] = Color.DarkGray;

            Console.WriteLine("-- NEStract --");
            Console.WriteLine("A tool for ripping sprite data from CHR banks in NES ROM files.");
            Console.WriteLine("2016 - Delfioh - https://github.com/delfioh \n");

            if (args.Length > 0)
            {
                filename = args[0];
            }
            else
            {
                Console.WriteLine("No filename specified.");
                Console.WriteLine("Usage: NESTract filename");
                return;
            }
 
            if (File.Exists(filename))
            {
                romfile = File.OpenRead(filename);
                reader = new BinaryReader(romfile);
                outfilename = Path.GetFileNameWithoutExtension(filename);
            }
            else
            {
                Console.WriteLine("Specified file does not exist");
                return;
            }

            //Reading NES ROM Header            
            NESHeader header = new NESHeader();
            header.id = reader.ReadBytes(4);
            header.PRG_count = reader.ReadByte();
            header.CHR_count = reader.ReadByte();

            if (CompareByteArray(correct_id, header.id))
            {
                Console.WriteLine("Valid NES ROM file");
                Console.WriteLine("PRG Banks: {0}", header.PRG_count);
                Console.WriteLine("CHR Banks: {0}", header.CHR_count);
            }
            else
            {
                Console.WriteLine("Not a valid NES ROM file");
                return;
            }

            if (header.CHR_count > 0)
            {
                //Set file read position to first CHR bank in file
                romfile.Seek(16 + header.PRG_count * 16384, SeekOrigin.Begin);

                for (int i = 0; i < header.CHR_count; i++)
                {
                    Console.WriteLine("Processing CHR bank {0}", i);

                    Bitmap bmp = new Bitmap(128, 256);

                    for (int y = 0; y < 32; y++)
                    {
                        for (int x = 0; x < 16; x++)
                        {
                            byte[] channel_A = reader.ReadBytes(8);
                            byte[] channel_B = reader.ReadBytes(8);
                            Bitmap sprite = DecodeSprite(channel_A, channel_B, palette);
                            OverwriteBitmap(sprite, ref bmp, x * 8, y * 8);

                        }
                    }

                    bmp.Save(outfilename + "_" + i + ".bmp");
                }

                Console.WriteLine("Done");
            }
            else
            {
                Console.WriteLine("No CHR banks in ROM file");
            }
                             
        }

        static Bitmap DecodeSprite(byte[] channel_A, byte[] channel_B, Color[] palette)
        {
            Bitmap bmp = new Bitmap(8, 8);
            for (int y = 0; y < 8; y++)
            {
                byte mask = 0x80;
                byte shift = 7;
                for (int x = 0; x < 8; x++)
                {
                    byte color_val = (byte)(((channel_A[y] & mask) >> shift) + ((channel_B[y] & mask) >> shift) * 2);
                    mask >>= 1;
                    shift--;
                    
                    bmp.SetPixel(x, y, palette[color_val]);
                }
            }

            return bmp;
        }

        static void OverwriteBitmap(Bitmap src, ref Bitmap dst, int x, int y)
        {
            Graphics graphics = Graphics.FromImage(dst);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.DrawImage(src, new Point(x, y));
        }

        static bool CompareByteArray(byte[] arr1, byte[] arr2)
        {
            if (arr1.Length != arr2.Length) return false;

            bool equal = true;
            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i])
                {
                    equal = false;
                    break;
                }
            }
            return equal;
        }
    }
}
