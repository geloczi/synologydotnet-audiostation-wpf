using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

public static class ImageHelper
{
    public static ImageCodecInfo[] Codecs = ImageCodecInfo.GetImageEncoders();
    private static ImageCodecInfo _jpgCodec = GetEncoderInfo("image/jpeg");

    /// <summary>
    /// Returns the image codec with the given mime type
    /// </summary>
    public static ImageCodecInfo GetEncoderInfo(string mimeType) => Codecs.First(x => x.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase));

    public static Size GetFitSize(Size tofit, Size box)
    {
        if (tofit.Height > box.Height || tofit.Width > box.Width)
        {
            int y = (int)Math.Round(box.Width / (tofit.Width / (float)tofit.Height));

            if (y <= box.Height)
            {
                return new Size(box.Width, y);
            }
            else
            {
                int x = (int)Math.Round((tofit.Width * box.Height) / (float)tofit.Height);
                return new Size(x, box.Height);
            }
        }
        else
        {
            return tofit;
        }
    }

    /// <summary>
    /// Resizes the specified image to fit in the specified size (box). Keeps aspect ratio. 
    /// </summary>
    /// <param name="img">The image.</param>
    /// <param name="box">The box to fit in.</param>
    /// <param name="mode">The quality mode.</param>
    /// <returns></returns>
    public static Image FitImageIn(Image img, Size box, PixelOffsetMode mode)
    {
        if (img.Size.Height > box.Height || img.Size.Width > box.Width)
        {
            int y = (int)Math.Round(box.Width / (img.Width / (float)img.Height));
            if (y <= box.Height)
            {
                return Resize(img, new Size(box.Width, y), false);
            }
            else
            {
                int x = (int)Math.Round((img.Width * box.Height) / (float)img.Height);
                return Resize(img, new Size(x, box.Height), mode, false);
            }
        }
        return (Image)img.Clone(); // Because the caller expects a new instance
    }

    /// <summary>
    /// Resizes the specified image to the specified size.
    /// </summary>
    /// <param name="imgToResize">The img to resize.</param>
    /// <param name="size">The size.</param>
    /// <param name="keepAspectRatio">if set to <c>true</c> [keep aspect ratio].</param>
    /// <returns></returns>
    public static Image Resize(Image imgToResize, Size size, bool keepAspectRatio)
    {
        return Resize(imgToResize, size, PixelOffsetMode.HighQuality, keepAspectRatio);
    }

    /// <summary>
    /// Resizes the specified image to the specified size.
    /// </summary>
    /// <param name="imgToResize">The img to resize.</param>
    /// <param name="size">The size.</param>
    /// <param name="mode">The mode.</param>
    /// <param name="keepAspectRatio">if set to <c>true</c> [keep aspect ratio].</param>
    /// <returns></returns>
    public static Image Resize(Image imgToResize, Size size, PixelOffsetMode mode, bool keepAspectRatio)
    {
        int destWidth;
        int destHeight;

        if (keepAspectRatio)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;
            float nPercentW = ((float)size.Width / (float)sourceWidth);
            float nPercentH = ((float)size.Height / (float)sourceHeight);
            float nPercent = nPercentH < nPercentW ? nPercentH : nPercentW;
            destWidth = (int)Math.Round((sourceWidth * nPercent));
            destHeight = (int)Math.Round((sourceHeight * nPercent));
        }
        else
        {
            destWidth = size.Width;
            destHeight = size.Height;
        }

        var b = new Bitmap(destWidth, destHeight);
        using (var g = Graphics.FromImage((Image)b))
        {
            g.PixelOffsetMode = mode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
        }
        return (Image)b;
    }

    /// <summary>
    /// Gets a dynamic quality value for an image, depending on the image resolution. 
    /// </summary>
    /// <param name="imageSize">Size of the image.</param>
    /// <param name="minQuality">The minimum quality from 1 to 100.</param>
    /// <param name="maxQuality">The maximum quality from 1 to 100.</param>
    /// <param name="minQualitySize">Resolution for minimum quality. Images with or under this resolution will be assigned with minimum quality.</param>
    /// <param name="maxQualitySize">Resolution for maximum quality. Images with or under this resolution will be assigned with maximum quality.</param>
    /// <returns></returns>
    public static int GetDynamicQuality(Size imageSize, int minQuality, int maxQuality, int minQualitySize, int maxQualitySize)
    {
        int res = imageSize.Height * imageSize.Width;
        int q = (int)Math.Round(maxQuality - (((maxQuality - minQuality) / (float)(minQualitySize - maxQualitySize)) * (res - maxQualitySize)));
        if (q >= maxQuality)
            return maxQuality;
        else
            return q <= minQuality ? minQuality : q;
    }

    /// <summary>
    /// Converts the image to another image using JPG format.
    /// </summary>
    /// <param name="img">The img.</param>
    /// <param name="quality">The quality.</param>
    /// <returns></returns>
    public static Image ConvertJPG(Image img, int quality)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            ConvertJPG(img, quality, ms);
            return Image.FromStream(ms);
        }
    }

    /// <summary>
    /// Converts the image to a stream using JPG format.
    /// </summary>
    /// <param name="img">The img.</param>
    /// <param name="quality">The quality.</param>
    /// <param name="targetStream">The target stream.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">quality must be between 0 and 100.</exception>
    public static void ConvertJPG(Image img, int quality, Stream targetStream)
    {
        if (quality < 0 || quality > 100)
            throw new ArgumentOutOfRangeException("quality must be between 0 and 100.");
        using (var qualityParam = new EncoderParameter(Encoder.Quality, quality))
        using (var encoderParams = new EncoderParameters(1))
        {
            encoderParams.Param[0] = qualityParam;
            using (var bitmap = new Bitmap(img)) //To fix "A generic error occurred in GDI+."
                bitmap.Save(targetStream, _jpgCodec, encoderParams);
        }
    }

    /// <summary>
    /// Saves the image to a JPG file.
    /// </summary>
    /// <param name="img">The img.</param>
    /// <param name="quality">The quality.</param>
    /// <param name="path">The path.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">quality must be between 0 and 100.</exception>
    public static void SaveJpg(Image img, int quality, string path)
    {
        if (quality < 0 || quality > 100)
            throw new ArgumentOutOfRangeException("quality must be between 0 and 100.");
        using (var qualityParam = new EncoderParameter(Encoder.Quality, quality))
        using (var encoderParams = new EncoderParameters(1))
        {
            encoderParams.Param[0] = qualityParam;
            using (var bitmap = new Bitmap(img)) //To fix "A generic error occurred in GDI+."
                bitmap.Save(path, _jpgCodec, encoderParams);
        }
    }

    /// <summary>
    /// Saves the specified image to stream in JPG format.
    /// </summary>
    /// <param name="img">The img.</param>
    /// <param name="quality">The quality.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentOutOfRangeException">quality must be between 0 and 100.</exception>
    public static void SaveJpg(Image img, int quality, Stream stream)
    {
        if (quality < 0 || quality > 100)
            throw new ArgumentOutOfRangeException("quality must be between 0 and 100.");
        using (var qualityParam = new EncoderParameter(Encoder.Quality, quality))
        using (var encoderParams = new EncoderParameters(1))
        {
            encoderParams.Param[0] = qualityParam;
            using (var bitmap = new Bitmap(img)) //To fix "A generic error occurred in GDI+."
                bitmap.Save(stream, _jpgCodec, encoderParams);
        }
    }

    /// <summary>
    /// Multiplies the specified image horizontal.
    /// </summary>
    /// <param name="baseimage">The baseimage.</param>
    /// <param name="count">The count.</param>
    /// <returns></returns>
    public static Image Multiply(Image baseimage, int count)
    {
        if (count >= 1)
        {
            int nwidth = baseimage.Width * count;
            var bitmap = new Bitmap(nwidth, baseimage.Height);
            using (var canvas = Graphics.FromImage(bitmap))
            {
                canvas.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                for (int i = 0; i < count; ++i)
                    canvas.DrawImage(baseimage, baseimage.Width * i, 0, baseimage.Width, baseimage.Height);
                canvas.Save();
            }
            return (Image)bitmap;
        }
        return count == 1 ? baseimage : null;
    }

    /// <summary>
    /// Images to byte array.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <returns></returns>
    public static byte[] ImageToByteArray(Image image, ImageFormat imageFormat)
    {
        byte[] result = null;
        using (var ms = new MemoryStream())
        {
            image.Save(ms, imageFormat);
            result = ms.ToArray();
        }
        return result;
    }

    /// <summary>
    /// Images from byte array.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns></returns>
    public static Image ImageFromByteArray(byte[] bytes)
    {
        using (var ms = new MemoryStream(bytes))
            return Image.FromStream(ms);
    }

    /// <summary>
    /// Draws a progressbar using the specified images.
    /// </summary>
    /// <param name="activeImage">Image for the value part.</param>
    /// <param name="inactiveImage">Image for the empty part.</param>
    /// <param name="value">The count.</param>
    /// <returns></returns>
    public static Image DrawProgressBar(Image activeImage, Image inactiveImage, int value, int max)
    {
        if (value > max)
            throw new ArgumentException("Value must less or equal to [max].", "value");

        int nwidth = activeImage.Width * max;
        var result = new Bitmap(nwidth, activeImage.Height);
        using (var canvas = Graphics.FromImage(result))
        {
            canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
            int i = 0;
            for (i = 0; i < value; ++i)
                canvas.DrawImage(activeImage, activeImage.Width * i, 0, activeImage.Width, activeImage.Height);

            if (i < max)
                for (int j = i; j < max; ++j)
                    canvas.DrawImage(inactiveImage, activeImage.Width * j, 0, inactiveImage.Width, inactiveImage.Height);
            canvas.Save();
        }
        return (Image)result;
    }

    /// <summary>
    /// Changes the aspect ratio by cropping the image height or width from center.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="ratio">The ratio.</param>
    /// <param name="mode">The mode.</param>
    /// <returns></returns>
    public static Image CropToAspectRatio(Image image, double ratio, PixelOffsetMode mode = PixelOffsetMode.HighQuality)
    {
        double sourceRatio = image.Width / (double)image.Height;

        if (sourceRatio > ratio)
        {
            /* Crop horizontal */
            int width = (int)(image.Height * ratio);

            // Align center
            int offset = (int)((image.Width / 2) - (width / 2));

            // Crop
            Bitmap b = new Bitmap(width, image.Height);
            using (Graphics g = Graphics.FromImage((Image)b))
            {
                g.PixelOffsetMode = mode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, new Rectangle(0, 0, width, image.Height), offset, 0, width, image.Height, GraphicsUnit.Pixel);
            }
            return b;
        }
        else if (sourceRatio < ratio)
        {
            /* Crop vertical */
            int height = (int)(image.Width / ratio);

            // Align center
            int offset = (int)((image.Height / 2) - (height / 2));

            // Crop
            Bitmap b = new Bitmap(image.Width, height);
            using (Graphics g = Graphics.FromImage((Image)b))
            {
                g.PixelOffsetMode = mode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, new Rectangle(0, 0, image.Width, height), 0, offset, image.Width, height, GraphicsUnit.Pixel);
            }
            return b;
        }

        return image;
    }

    /// <summary>
    /// Crops from center and always returns the specified size by zooming the original image.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="size">The size.</param>
    /// <returns></returns>
    public static Image CropFromCenter(Image image, Size size, PixelOffsetMode mode = PixelOffsetMode.HighQuality)
    {
        double targetRatio = size.Width / (double)size.Height;
        using (var img1 = CropToAspectRatio(image, targetRatio, mode))
            return Resize(img1, size, mode, false);
    }
}
