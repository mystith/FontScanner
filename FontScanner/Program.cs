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
            //Ensure proper syntax
            if (args.Length == 0 || !Directory.Exists(args[0]))
            {
                Console.WriteLine("Please pass a valid directory as an argument.");
                Console.WriteLine("    Arguments: fontscanner [directory] (boolean)");
                Console.WriteLine("    boolean: saves fonts as text files of 1 and 0, rather than PNG files.");
                return;
            }

            //Loop over directory
            foreach (string file in Directory.GetFiles(args[0]))
            {
                //Generate font
                using (Image img = Image.FromFile(file))
                using (Bitmap bmp = img as Bitmap)
                    GenerateFont(BitmapToBoolean(bmp), bmp.Width, bmp.Height,
                        args.Length > 1 && args[1].ToLower() != "boolean");
            }
        }

        //Scan boolmap for text.
        public static unsafe void GenerateFont(bool[,] image, int imgW, int imgH, bool booleanOutput)
        {
            List<List<Point>> characters = new List<List<Point>>();
            
            //Scan boolmap for white pixels, if found then discover character from that pixel.
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
            
            HashSet<int> completed = new HashSet<int>();
            //Take points from characters discovered and create boolmaps from those, and save them to text / PNG file.
            foreach (List<Point> points in characters)
            {
                bool[,] character = BoolMap(points);
                int hash = character.GetHashCode();

                if (completed.Contains(hash)) continue;

                completed.Add(hash);
                
                //Save in either boolean text file form or png form.
                if (booleanOutput)
                {
                    SaveTxt(character);
                    PrintBoolMap(character);
                    Console.WriteLine();
                    
                    //Read valid char.
                    char c;
                    while (Path.GetInvalidFileNameChars().Contains(c = Console.ReadKey().KeyChar)) ;
                    
                    File.Move(character.GetHashCode() + ".txt",  c + ".txt");
                } else
                {
                    SavePng(character);
                    PrintBoolMap(character);
                    Console.WriteLine();
                    
                    //Read valid char.
                    char c;
                    while (Path.GetInvalidFileNameChars().Contains(c = Console.ReadKey().KeyChar)) ;
                    
                    File.Move(character.GetHashCode() + ".png",  c + ".png");
                }
            }
        }

        //Print boolmap to console.
        public static void PrintBoolMap(bool[,] character)
        {
            int width = character.GetLength(0);
            int height = character.GetLength(1);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    Console.Write(character[x, y] ? '█' : ' ');
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        //Create a 2D array of booleans (BoolMap) from a list of points.
        public static bool[,] BoolMap(List<Point> points)
        {
            int x = int.MaxValue;
            int y = int.MaxValue;
            int width = 0;
            int height = 0;

            //Find bounds of character.
            foreach (Point p in points)
            {
                if (p.X < x) x = p.X;
                if (p.Y < y) y = p.Y;
                if (p.X > width) width = p.X;
                if (p.Y > height) height = p.Y;
            }

            //Correct width and height. Why is the - 1 there? I do not know, but it does not work without.
            width -= x - 1;
            height -= y - 1;

            //Create boolmap from points.
            bool[,] chr = new bool[width, height];
            foreach (Point p in points)
                chr[p.X - x, p.Y - y] = true;

            return chr;
        }

        //Save boolmap to text file of 1s and 0s, based on true / false.
        public static void SaveTxt(bool[,] chr)
        {
            int width = chr.GetLength(0);
            int height = chr.GetLength(1);
            
            //Write pixels into a string.
            StringBuilder sb = new StringBuilder();
            for (int iy = 0; iy < height; iy++)
            {
                for (int ix = 0; ix < width; ix++)
                    sb.Append(chr[ix, iy] ? 1 : 0);
                
                sb.AppendLine();
            }
            
            //Save string to hash.txt.
            string final = sb.ToString();
            
            int hash = chr.GetHashCode();
            File.WriteAllText(hash + ".txt", final);
        }
        
        //Save boolmap as a PNG, false = black, true = white.
        public static void SavePng(bool[,] chr)
        {
            int width = chr.GetLength(0);
            int height = chr.GetLength(1);
            
            //Create hash from boolmap
            int hash = chr.GetHashCode();
            using (Bitmap bmp = new Bitmap(width, height))
            {
                //Draw to bitmap using boolmap as reference.
                using (Graphics g = Graphics.FromImage(bmp))
                    for (int iy = 0; iy < height; iy++)
                    for (int ix = 0; ix < width; ix++)
                            g.FillRectangle(new SolidBrush(Color.White), ix, iy, 1, 1);
                
                //Save bitmap to hash.png.
                bmp.Save(hash + ".png");
            }
        }

        //Searches for connected points to a certain point (up, down, left, right, diagonals).
        public static unsafe void Discover(ref List<Point> points, ref bool[,] data, int x, int y, int w, int h)
        {
            points.Add(new Point(x, y));

            //Search rows above, around, and below point.
            for (int yo = -1; yo <= 1; yo++)
            {
                //Ensure no OOB
                if (y + yo > h) continue;
                if (y + yo < 0) continue;

                //Search columns within row, in the left, center, and right of the row.
                for (int xo = -1; xo <= 1; xo++)
                {
                    //Ensure no OOB and no search of pixel passed in x, y args.
                    if (x + xo > w) continue;
                    if (x + xo < 0) continue;
                    if (xo == 0 && yo == 0) continue;
                    
                    //Check if pixel is white. If it is, set the pixel passed as argument to black, and recurse.
                    if (data[x + xo, y + yo])
                    {
                        data[x, y] = false;
                        Discover(ref points, ref data, x + xo, y + yo, w, h);
                    }
                }
            }
        }
        
        //Convert a bitmap image into a grayscale image of booleans, either black [0] or white [1].
        public static unsafe bool[,] BitmapToBoolean(Bitmap img)
        {
            //Set constant bytes per pixel + lock bitmap for unsafe operations.
            const int bpp = 3;
            BitmapData data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            bool[,] boolImage = new bool[img.Width, img.Height];
            
            //Loop over pixels row by row, column by column.
            byte* Scan0 = (byte*) data.Scan0;
            for (int y = 0; y < img.Height; y++)
            {
                byte* row = Scan0 + y * data.Stride;
                for (int x = 0; x < img.Width; x++)
                {
                    //Get grayscale value of pixel based on human eyesight, set boolean based on if values > 187 (arbitrary value).
                    double gray = 0.3 * row[x * bpp + 2] + 0.59 * row[x * bpp + 1] + 0.11 * row[x * bpp];
                    boolImage[x, y] = gray > 187;
                }
            }
            
            //Unlock bitmap, return final 2D array of booleans.
            img.UnlockBits(data);
            return boolImage;
        }
    }
}