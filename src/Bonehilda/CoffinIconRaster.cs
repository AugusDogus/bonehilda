using System;

namespace Bonehilda
{
    // An orthographic miniature, rasterized once per tier at registration. Kept
    // independent of Unity so the exact artwork can be reviewed without a game.
    internal sealed class CoffinIconRaster
    {
        private const int Size = 384;
        private readonly byte[] pixels = new byte[Size * Size * 4];
        private readonly double[] depth = new double[Size * Size];
        private struct V
        {
            internal double X, Y, Z;
            internal V(double x, double y, double z) { X = x; Y = y; Z = z; }
        }
        private struct Ink
        {
            internal double R, G, B;
            internal int Grain;
            internal Ink(double r, double g, double b, int grain = 0) { R = r; G = g; B = b; Grain = grain; }
        }
        // Flat head, broad shoulders, long taper to a narrow foot. No roof apex.
        private static readonly V[] Shape = {
            new V(-.29, 0, 0), new V(.29, 0, 0), new V(.57, 1.64, 0),
            new V(.35, 2.28, 0), new V(-.35, 2.28, 0), new V(-.57, 1.64, 0)
        };
        private static readonly Ink Wood = new Ink(108, 70, 42, 1);
        private static readonly Ink Dark = new Ink(39, 27, 22, 1);
        private static readonly Ink Bone = new Ink(209, 195, 151);
        private static readonly Ink Gold = new Ink(206, 151, 57);

        internal static byte[] Render(int stars, int outputSize)
        {
            var image = new CoffinIconRaster();
            for (var i = 0; i < image.depth.Length; i++) image.depth[i] = double.NegativeInfinity;
            image.Draw(stars);
            // Premultiplied averaging keeps the silhouette clean on dark UI cells.
            var result = new byte[outputSize * outputSize * 4];
            var samples = Size / outputSize;
            for (var y = 0; y < outputSize; y++)
            for (var x = 0; x < outputSize; x++)
            {
                double r = 0, g = 0, b = 0, alpha = 0;
                for (var sy = 0; sy < samples; sy++)
                for (var sx = 0; sx < samples; sx++)
                {
                    var p = ((y * samples + sy) * Size + x * samples + sx) * 4;
                    var a = image.pixels[p + 3] / 255.0;
                    alpha += a; r += image.pixels[p] * a; g += image.pixels[p + 1] * a; b += image.pixels[p + 2] * a;
                }
                var target = (y * outputSize + x) * 4;
                if (alpha <= 0) continue;
                result[target] = (byte)(r / alpha); result[target + 1] = (byte)(g / alpha); result[target + 2] = (byte)(b / alpha);
                result[target + 3] = (byte)(255 * alpha / (samples * samples));
            }
            return result;
        }

        private void Draw(int stars)
        {
            var velvet = stars == 0 ? new Ink(143, 31, 33, 2) : stars == 1 ? new Ink(60, 49, 43, 2) : new Ink(42, 65, 119, 2);
            // Soft contact shadow, transparent outside the silhouette.
            for (var y = 14; y < 59; y++)
            for (var x = 107; x < 263; x++)
            {
                var radius = Math.Pow((x - 185) / 78.0, 2) + Math.Pow((y - 36) / 23.0, 2);
                if (radius >= 1) continue;
                var p = (y * Size + x) * 4;
                pixels[p + 3] = (byte)(48 * (1 - radius) * (1 - radius));
            }
            Extrude(1, -.32, .26, Wood);
            // Raised ivory bevel and dark inset give the front a physical rim.
            Extrude(.965, .26, .295, Bone);
            Extrude(.917, .296, .319, Dark);
            Extrude(.86, .320, .335, velvet);
            // Slender gilt inner border, with its center inset in the tier color.
            Face(Scaled(.825, .340), new Ink(139, 110, 66));
            Face(Scaled(.803, .344), velvet);

            // Coffin furniture: side handles, hinges, nail heads and a front pull.
            for (var i = 0; i < 2; i++)
            {
                var y = .56 + i * .83;
                Box(.415 + i * .05, y, -.02, .032, .20, .15, Gold);
                Box(-.35 - i * .13, y, .352, .10, .045, .018, Gold);
                Disc(-.35 - i * .13, y, .375, .012, .012, Dark);
            }
            Disc(.28, 1.03, .365, .045, .065, Gold);
            Disc(.28, 1.035, .368, .024, .039, Dark);
            Box(.28, 1.08, .38, .045, .025, .025, Gold);

            // Carved skull and crossed bones read as a coffin even in a 64px cell.
            Line(-.17, 1.03, .17, 1.26, .032, .357, Bone);
            Line(.17, 1.03, -.17, 1.26, .032, .358, Bone);
            foreach (var endpoint in new[] { new V(-.17, 1.03, 0), new V(.17, 1.26, 0), new V(.17, 1.03, 0), new V(-.17, 1.26, 0) })
                Disc(endpoint.X, endpoint.Y, .36, .037, .035, Bone);
            Disc(0, 1.57, .36, .156, .179, new Ink(58, 38, 26));
            Disc(-.006, 1.59, .366, .144, .167, Bone);
            Box(0, 1.425, .37, .18, .105, .014, Bone);
            Disc(-.059, 1.602, .386, .037, .046, Dark);
            Disc(.054, 1.602, .386, .037, .046, Dark);
            Face(new[] { new V(0, 1.55, .389), new V(-.023, 1.49, .389), new V(.023, 1.49, .389) }, Dark);
            for (var i = -1; i <= 1; i++) Box(i * .042, 1.412, .389, .009, .056, .004, Dark);

            // Small stars belong to the object, not an unrelated UI badge.
            for (var i = 0; i < stars; i++) Star((i - (stars - 1) * .5) * .17, .43, .36, .071);
            for (var i = 0; i < Shape.Length; i++)
            {
                var p = Shape[i];
                Disc(p.X * .944, (p.Y - 1.14) * .944 + 1.14, .327, .016, .016, Gold);
            }
        }

        private static V[] Scaled(double scale, double z)
        {
            var points = new V[Shape.Length];
            for (var i = 0; i < points.Length; i++) points[i] = new V(Shape[i].X * scale, (Shape[i].Y - 1.14) * scale + 1.14, z);
            return points;
        }
        private void Extrude(double scale, double back, double front, Ink ink)
        {
            var a = Scaled(scale, front); var b = Scaled(scale, back);
            Face(a, ink);
            for (var i = 0; i < a.Length; i++)
            {
                var next = (i + 1) % a.Length;
                Face(new[] { a[i], b[i], b[next], a[next] }, ink);
            }
        }
        private void Box(double x, double y, double z, double width, double height, double thickness, Ink ink)
        {
            var a = new V(x - width / 2, y - height / 2, z + thickness / 2);
            var b = new V(x + width / 2, y - height / 2, z + thickness / 2);
            var c = new V(x + width / 2, y + height / 2, z + thickness / 2);
            var d = new V(x - width / 2, y + height / 2, z + thickness / 2);
            Face(new[] { a, b, c, d }, ink);
            Face(new[] { b, new V(b.X, b.Y, z - thickness / 2), new V(c.X, c.Y, z - thickness / 2), c }, ink);
            Face(new[] { d, c, new V(c.X, c.Y, z - thickness / 2), new V(d.X, d.Y, z - thickness / 2) }, ink);
        }
        private void Disc(double x, double y, double z, double rx, double ry, Ink ink)
        {
            var ring = new V[20];
            for (var i = 0; i < ring.Length; i++) ring[i] = new V(x + Math.Cos(i * Math.PI / 10) * rx, y + Math.Sin(i * Math.PI / 10) * ry, z);
            Face(ring, ink);
        }
        private void Line(double x1, double y1, double x2, double y2, double width, double z, Ink ink)
        {
            var length = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            var dx = -(y2 - y1) * width / length; var dy = (x2 - x1) * width / length;
            Face(new[] { new V(x1 + dx, y1 + dy, z), new V(x1 - dx, y1 - dy, z), new V(x2 - dx, y2 - dy, z), new V(x2 + dx, y2 + dy, z) }, ink);
        }
        private void Star(double x, double y, double z, double radius)
        {
            var center = new V(x, y, z);
            for (var i = 0; i < 10; i++)
            {
                var angle = Math.PI / 2 + i * Math.PI / 5;
                var next = angle + Math.PI / 5;
                var r = i % 2 == 0 ? radius : radius * .43;
                var r2 = i % 2 == 0 ? radius * .43 : radius;
                Triangle(center, new V(x + Math.Cos(angle) * r, y + Math.Sin(angle) * r, z), new V(x + Math.Cos(next) * r2, y + Math.Sin(next) * r2, z), Gold, 1.12);
            }
        }
        private void Face(V[] vertices, Ink ink)
        {
            var a = vertices[0]; var b = vertices[1]; var c = vertices[2];
            var nx = (b.Y - a.Y) * (c.Z - a.Z) - (b.Z - a.Z) * (c.Y - a.Y);
            var ny = (b.Z - a.Z) * (c.X - a.X) - (b.X - a.X) * (c.Z - a.Z);
            var nz = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            var length = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            var light = .52 + .54 * Math.Max(0, (-nx * .42 + ny * .65 + nz * .63) / Math.Max(.000001, length));
            for (var i = 1; i < vertices.Length - 1; i++) Triangle(a, vertices[i], vertices[i + 1], ink, light);
        }
        private static V Project(V p)
        {
            var u = .8660254 * p.X - .5 * p.Z;
            var v = -.1710101 * p.X + .9396926 * (p.Y - 1.14) - .2961981 * p.Z;
            // The miniature leans diagonally like the game's object thumbnails.
            return new V(192 + (u * .965926 + v * .258819) * 143, 194 + (-u * .258819 + v * .965926) * 143,
                .4698463 * p.X + .3420201 * p.Y + .8137977 * p.Z);
        }
        private static double Edge(V a, V b, double x, double y) { return (b.X - a.X) * (y - a.Y) - (b.Y - a.Y) * (x - a.X); }
        private void Triangle(V a, V b, V c, Ink ink, double light)
        {
            var pa = Project(a); var pb = Project(b); var pc = Project(c);
            var area = Edge(pa, pb, pc.X, pc.Y);
            if (Math.Abs(area) < .00001) return;
            var minX = Math.Max(0, (int)Math.Floor(Math.Min(pa.X, Math.Min(pb.X, pc.X))));
            var maxX = Math.Min(Size - 1, (int)Math.Ceiling(Math.Max(pa.X, Math.Max(pb.X, pc.X))));
            var minY = Math.Max(0, (int)Math.Floor(Math.Min(pa.Y, Math.Min(pb.Y, pc.Y))));
            var maxY = Math.Min(Size - 1, (int)Math.Ceiling(Math.Max(pa.Y, Math.Max(pb.Y, pc.Y))));
            for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var wa = Edge(pb, pc, x + .5, y + .5) / area;
                var wb = Edge(pc, pa, x + .5, y + .5) / area;
                var wc = 1 - wa - wb;
                if (wa < -.00001 || wb < -.00001 || wc < -.00001) continue;
                var z = wa * pa.Z + wb * pb.Z + wc * pc.Z;
                var index = y * Size + x;
                if (z < depth[index] - .000001) continue;
                depth[index] = z;
                var worldX = wa * a.X + wb * b.X + wc * c.X;
                var worldY = wa * a.Y + wb * b.Y + wc * c.Y;
                var noise = ((x * 13 + y * 7) % 11 - 5) * .6;
                var grain = ink.Grain == 1 ? Math.Sin(worldX * 108 + Math.Sin(worldY * 9) * 1.3) * 7 : ink.Grain == 2 ? Math.Cos(worldX * 17) * 5 + Math.Sin(worldY * 27) * 2 : 0;
                pixels[index * 4] = Clamp(ink.R * light + grain + noise);
                pixels[index * 4 + 1] = Clamp(ink.G * light + grain + noise);
                pixels[index * 4 + 2] = Clamp(ink.B * light + grain + noise);
                pixels[index * 4 + 3] = 255;
            }
        }
        private static byte Clamp(double value) { return (byte)Math.Max(0, Math.Min(255, value)); }
    }
}
