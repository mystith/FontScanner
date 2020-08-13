using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace FontScanner
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0 || !Directory.Exists(args[0]))
                Console.WriteLine("Please pass a valid directory as an argument.");
            
            using(Image img = Image.FromFile(args[0]))
            using (Bitmap bmp = img as Bitmap)
                GenerateFont(BitmapToBoolean(bmp), bmp.Width, bmp.Height);
        }

        public static unsafe void GenerateFont(bool[,] image, int imgW, int imgH)
        {
            List<List<Point>> characters = new List<List<Point>>();
            
            for (int y = 0; y < imgH; y++)
            {
                for (int x = 0; x < imgW; x++)
                {
                    if (image[x, y])
                    {
                        List<Point> points = new List<Point>();
                        Discover(ref points, ref image, x, y, imgW, imgH);
                        characters.Add(points);
                    }
                }
            }
            
            foreach (List<Point> points in characters)
            {
                int x = int.MaxValue;
                int y = int.MaxValue;
                int width = 0;
                int height = 0;

                foreach (Point p in points)
                {
                    if (p.X < x) x = p.X;
                    if (p.Y < y) y = p.Y;
                    if (p.X > width) width = p.X;
                    if (p.Y > height) height = p.Y;
                }

                width -= x - 1;
                height -= y - 1;

                bool[,] chr = new bool[width, height];
                foreach (Point p in points)
                    chr[p.X - x, p.Y - y] = true;

                List<int> finished = new List<int>();
                
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(x + " " + y);
                for (int iy = 0; iy < height; iy++)
                {
                    for (int ix = 0; ix < width; ix++)
                    {
                        sb.Append(chr[ix, iy] ? 1 : 0);
                        Console.Write(chr[ix, iy] ? '█' : ' ');
                    }

                    sb.AppendLine();
                    Console.WriteLine();
                }

                string final = sb.ToString();
                if (finished.Contains(final.GetHashCode())) 
                    continue;
                
                Console.WriteLine("Character? ");
                
                finished.Add(final.GetHashCode());
                File.WriteAllText(Console.ReadKey().KeyChar + ".txt", final);
                Console.WriteLine();
            }
        }

        //Searches for connected points to a certain point (up, down, left, right, diagonals)
        public static unsafe void Discover(ref List<Point> points, ref bool[,] data, int x, int y, int w, int h)
        {
            points.Add(new Point(x, y));

            //Search rows above, around, and below point
            for (int yo = -1; yo <= 1; yo++)
            {
                //Ensure no OOB
                if (y + yo > h) continue;
                if (y + yo < 0) continue;

                //
                for (int xo = -1; xo <= 1; xo++)
                {
                    if (x + xo > w) continue;
                    if (x + xo < 0) continue;
                    if (xo == 0 && yo == 0) continue;
                    
                    if (data[x + xo, y + yo])
                    {
                        data[x + xo, y + yo] = false;
                        Discover(ref points, ref data, x + xo, y + yo, w, h);
                    }
                }
            }
        }
        
        private static unsafe bool[,] BitmapToBoolean(Bitmap img)
        {
            int bpp = 3;
            BitmapData data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            bool[,] boolImage = new bool[img.Width, img.Height];
            
            byte* Scan0 = (byte*) data.Scan0;
            for (int y = 0; y < img.Height; y++)
            {
                byte* row = Scan0 + y * data.Stride;
                for (int x = 0; x < img.Width; x++)
                {
                    double gray = 0.3 * row[x * bpp + 2] + 0.59 * row[x * bpp + 1] + 0.11 * row[x * bpp];
                    boolImage[x, y] = gray > 187;
                }
            }
            
            img.UnlockBits(data);
            return boolImage;
        }
    }
}