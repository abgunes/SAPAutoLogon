using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using KeePassLib;

namespace SAPAutoLogon
{
    /// <summary>
    /// Keeps the SAP logo in a database as a KeePass custom icon under a fixed
    /// UUID, so entries created by the import can use it. The icon is the embedded
    /// SAP.png, stored byte for byte. Never throws.
    /// </summary>
    internal static class SapIconHelper
    {
        /// <summary>Side length of the fallback PNG. The source image is larger, so it is only ever scaled down.</summary>
        private const int IconSize = 64;

        /// <summary>Side length of the menu and list image.</summary>
        private const int MenuSize = 16;

        private static readonly byte[] IconUuidBytes = new byte[]
        {
            188, 126, 229, 26, 127, 40, 108, 79, 160, 194, 97, 231, 3, 230, 60, 207
        };

        /// <summary>The UUID the SAP icon is stored under in every database.</summary>
        public static PwUuid IconUuid
        {
            get { return new PwUuid((byte[])IconUuidBytes.Clone()); }
        }

        /// <summary>
        /// Returns the UUID of the SAP custom icon in db, adding it when the database
        /// does not have it yet. An icon already stored under the UUID is reused as it is.
        /// Returns PwUuid.Zero when db is null or anything goes wrong; db is then unchanged.
        /// Does not set db.Modified; the caller's UpdateUI does.
        /// </summary>
        public static PwUuid EnsureIcon(PwDatabase db)
        {
            try
            {
                if (db == null || db.CustomIcons == null)
                {
                    return PwUuid.Zero;
                }

                PwUuid uuid = IconUuid;
                foreach (PwCustomIcon existing in db.CustomIcons)
                {
                    if (existing != null && existing.Uuid.Equals(uuid))
                    {
                        return uuid;
                    }
                }

                byte[] png = GetEmbeddedPng();
                if (png == null)
                {
                    png = CreatePng();
                }
                if (png == null || png.Length == 0)
                {
                    return PwUuid.Zero;
                }

                db.CustomIcons.Add(new PwCustomIcon(uuid, png));
                db.UINeedsIconUpdate = true;
                return uuid;
            }
            catch
            {
                return PwUuid.Zero;
            }
        }

        /// <summary>
        /// The embedded SAP.png exactly as it is in the file, or null when the
        /// resource is missing or the bytes do not decode as an image.
        /// </summary>
        private static byte[] GetEmbeddedPng()
        {
            try
            {
                byte[] data = Properties.Resources.sap_icon_16;
                if (data == null || data.Length == 0)
                {
                    return null;
                }

                using (MemoryStream ms = new MemoryStream(data))
                using (Image image = Image.FromStream(ms))
                {
                    if (image.Width <= 0 || image.Height <= 0)
                    {
                        return null;
                    }
                }

                return data;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// The 16x16 menu image from the embedded SAP.png: a 16x16 PNG pixel for pixel,
        /// any other size scaled to 16x16 (see <see cref="CreateMenuImage(byte[])"/>).
        /// Always a new 32bpp ARGB bitmap the caller owns. Falls back to the scaled
        /// logo when the PNG is missing or does not decode. Null only when both
        /// fail. Never throws.
        /// </summary>
        public static Image CreateMenuImage()
        {
            byte[] png = null;
            try
            {
                png = Properties.Resources.sap_icon_16;
            }
            catch
            {
                // Use the scaled logo instead.
            }
            return CreateMenuImage(png);
        }

        /// <summary>
        /// The 16x16 menu image for the given PNG bytes: a 16x16 image is copied pixel
        /// for pixel, any other size is scaled to 16x16 in high quality (aspect ratio
        /// kept, centred on a transparent canvas). Bytes that are null or do not
        /// decode give the scaled logo. Null only when both fail. Never throws.
        /// </summary>
        internal static Image CreateMenuImage(byte[] png)
        {
            try
            {
                if (png != null && png.Length > 0)
                {
                    using (MemoryStream ms = new MemoryStream(png))
                    using (Bitmap decoded = new Bitmap(ms))
                    {
                        if (decoded.Width == MenuSize && decoded.Height == MenuSize)
                        {
                            return CopyPixels(decoded);
                        }
                        if (decoded.Width > 0 && decoded.Height > 0)
                        {
                            return ScaleToMenuSize(decoded);
                        }
                    }
                }
            }
            catch
            {
                // Use the scaled logo instead.
            }

            try
            {
                return SAPAutoLogonExt.CreateMenuIcon();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// source scaled to 16x16 in high quality, as a new 32bpp ARGB bitmap. The aspect
        /// ratio is kept: a non-square source is centred on the transparent canvas.
        /// </summary>
        private static Bitmap ScaleToMenuSize(Bitmap source)
        {
            double factor = Math.Min((double)MenuSize / source.Width, (double)MenuSize / source.Height);
            int w = Math.Max(1, Math.Min(MenuSize, (int)Math.Round(source.Width * factor)));
            int h = Math.Max(1, Math.Min(MenuSize, (int)Math.Round(source.Height * factor)));
            Rectangle target = new Rectangle((MenuSize - w) / 2, (MenuSize - h) / 2, w, h);

            Bitmap scaled = new Bitmap(MenuSize, MenuSize, PixelFormat.Format32bppArgb);
            try
            {
                using (Graphics g = Graphics.FromImage(scaled))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    using (ImageAttributes attributes = new ImageAttributes())
                    {
                        attributes.SetWrapMode(WrapMode.TileFlipXY);
                        g.DrawImage(source, target, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                    }
                }
                return scaled;
            }
            catch
            {
                scaled.Dispose();
                throw;
            }
        }

        /// <summary>
        /// A 32bpp ARGB copy of source with the same pixels, independent of the stream
        /// source was decoded from. Copies raw pixel data: drawing through Graphics would
        /// round semi-transparent pixels.
        /// </summary>
        private static Bitmap CopyPixels(Bitmap source)
        {
            Rectangle rect = new Rectangle(0, 0, source.Width, source.Height);
            Bitmap copy = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            try
            {
                BitmapData from = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    BitmapData to = copy.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        int rowBytes = source.Width * 4;
                        byte[] row = new byte[rowBytes];
                        for (int y = 0; y < source.Height; y++)
                        {
                            Marshal.Copy(new IntPtr(from.Scan0.ToInt64() + (long)y * from.Stride), row, 0, rowBytes);
                            Marshal.Copy(row, 0, new IntPtr(to.Scan0.ToInt64() + (long)y * to.Stride), rowBytes);
                        }
                    }
                    finally
                    {
                        copy.UnlockBits(to);
                    }
                }
                finally
                {
                    source.UnlockBits(from);
                }
                return copy;
            }
            catch
            {
                copy.Dispose();
                throw;
            }
        }

        /// <summary>The 512x512 logo scaled to size x size, owned by the caller; null on failure. Never throws.</summary>
        public static Image CreateLogoImage(int size)
        {
            try
            {
                if (size <= 0)
                {
                    return null;
                }
                return ScaleLogo(size);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Fallback: the 512x512 logo scaled down to a 64x64 PNG.</summary>
        private static byte[] CreatePng()
        {
            using (Bitmap scaled = ScaleLogo(IconSize))
            using (MemoryStream ms = new MemoryStream())
            {
                scaled.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        /// <summary>The 512x512 logo scaled to size x size in high quality, as a new 32bpp ARGB bitmap.</summary>
        private static Bitmap ScaleLogo(int size)
        {
            using (Bitmap source = Properties.Resources.sap_image)
            {
                Bitmap scaled = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                try
                {
                    using (Graphics g = Graphics.FromImage(scaled))
                    {
                        g.Clear(Color.Transparent);
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        using (ImageAttributes attributes = new ImageAttributes())
                        {
                            // Without this the edge pixels blend with transparent ones and leave faint fringes.
                            attributes.SetWrapMode(WrapMode.TileFlipXY);
                            g.DrawImage(source, new Rectangle(0, 0, size, size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                        }
                    }
                    return scaled;
                }
                catch
                {
                    scaled.Dispose();
                    throw;
                }
            }
        }
    }
}
