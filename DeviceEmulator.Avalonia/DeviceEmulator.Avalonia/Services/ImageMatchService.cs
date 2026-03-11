using System;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace DeviceEmulator.Services
{
    /// <summary>
    /// Image-based template matching service for Surface Automation.
    /// Uses SkiaSharp for image processing and NCC (Normalized Cross-Correlation) for matching.
    /// </summary>
    public static class ImageMatchService
    {
        private static readonly string CapturedImagesDir;

        static ImageMatchService()
        {
            CapturedImagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captured_images");
            if (!Directory.Exists(CapturedImagesDir))
                Directory.CreateDirectory(CapturedImagesDir);
        }

        /// <summary>
        /// Directory where captured template images are stored.
        /// </summary>
        public static string ImagesDirectory => CapturedImagesDir;

        // ═══════════════════════════════════════════════════════════════
        //  SCREEN CAPTURE (in-memory)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Captures the entire screen and returns it as an SKBitmap.
        /// </summary>
        public static SKBitmap? CaptureScreen()
        {
            try
            {
                // Use a temp file approach (same as existing TakeScreenshot) then load
                var tempPath = Path.Combine(Path.GetTempPath(), $"screen_capture_{Guid.NewGuid():N}.png");

                if (PlatformAutomation.IsMacOS)
                {
                    PlatformAutomation.RunShell("screencapture", $"-x \"{tempPath}\"");
                }
                else if (PlatformAutomation.IsWindows)
                {
                    PlatformAutomation.TakeScreenshot(tempPath);
                }
                else
                {
                    return null;
                }

                if (!File.Exists(tempPath)) return null;

                var bytes = File.ReadAllBytes(tempPath);
                try { File.Delete(tempPath); } catch { }

                return SKBitmap.Decode(bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageMatchService] CaptureScreen error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Captures a region of the screen and returns as SKBitmap.
        /// </summary>
        public static SKBitmap? CaptureRegion(int x, int y, int w, int h)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"region_capture_{Guid.NewGuid():N}.png");

                if (PlatformAutomation.IsMacOS)
                {
                    PlatformAutomation.RunShell("screencapture", $"-x -R{x},{y},{w},{h} \"{tempPath}\"");
                }
                else if (PlatformAutomation.IsWindows)
                {
                    PlatformAutomation.TakeRegionScreenshot(x, y, w, h, tempPath);
                }
                else
                {
                    return null;
                }

                if (!File.Exists(tempPath)) return null;

                var bytes = File.ReadAllBytes(tempPath);
                try { File.Delete(tempPath); } catch { }

                return SKBitmap.Decode(bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageMatchService] CaptureRegion error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Captures the region around the current cursor position.
        /// </summary>
        public static SKBitmap? CaptureAtCursor(int halfSize = 32)
        {
            try
            {
                var posStr = PlatformAutomation.GetCursorPosition();
                var parts = posStr.Split(',');
                if (parts.Length < 2 || !int.TryParse(parts[0].Trim(), out int cx) || !int.TryParse(parts[1].Trim(), out int cy))
                    return null;

                int x = Math.Max(0, cx - halfSize);
                int y = Math.Max(0, cy - halfSize);
                return CaptureRegion(x, y, halfSize * 2, halfSize * 2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageMatchService] CaptureAtCursor error: {ex.Message}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  SAVE / LOAD templates
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Saves an SKBitmap as a PNG file and returns the path.
        /// </summary>
        public static string SaveTemplateImage(SKBitmap bitmap, string? fileName = null)
        {
            fileName ??= $"img_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString()[..6]}.png";
            var filePath = Path.Combine(CapturedImagesDir, fileName);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);

            Console.WriteLine($"[ImageMatchService] Saved template: {filePath}");
            return filePath;
        }

        /// <summary>
        /// Loads a template image from file.
        /// </summary>
        public static SKBitmap? LoadTemplateImage(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                return SKBitmap.Decode(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageMatchService] LoadTemplate error: {ex.Message}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  NCC TEMPLATE MATCHING
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Result of a template matching operation.
        /// </summary>
        public struct MatchResult
        {
            public bool Found;
            public int X, Y, Width, Height;
            public double Confidence;
            public int CenterX => X + Width / 2;
            public int CenterY => Y + Height / 2;
        }

        /// <summary>
        /// Finds a template image on the screen using NCC (Normalized Cross-Correlation).
        /// Returns the best match location if confidence exceeds threshold.
        /// Uses grayscale comparison and step-based scanning for performance.
        /// </summary>
        public static MatchResult FindOnScreen(string templatePath, double minConfidence = 0.8)
        {
            var result = new MatchResult { Found = false };

            var template = LoadTemplateImage(templatePath);
            if (template == null)
            {
                Console.WriteLine($"[ImageMatchService] Template not found: {templatePath}");
                return result;
            }

            var screen = CaptureScreen();
            if (screen == null)
            {
                Console.WriteLine("[ImageMatchService] Failed to capture screen");
                template.Dispose();
                return result;
            }

            try
            {
                result = MatchTemplate(screen, template, minConfidence);
                result.Width = template.Width;
                result.Height = template.Height;
            }
            finally
            {
                template.Dispose();
                screen.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Core NCC template matching algorithm.
        /// Converts to grayscale then slides the template across the source image.
        /// Uses a step of 2 pixels for initial scan, then refines around the best match.
        /// </summary>
        public static MatchResult MatchTemplate(SKBitmap source, SKBitmap template, double minConfidence)
        {
            var result = new MatchResult { Found = false };

            int sw = source.Width, sh = source.Height;
            int tw = template.Width, th = template.Height;

            if (tw > sw || th > sh) return result;

            // Convert to grayscale byte arrays for fast access
            var srcGray = ToGrayscale(source);
            var tplGray = ToGrayscale(template);

            // Pre-compute template stats
            double tplSum = 0, tplSumSq = 0;
            int tplPixels = tw * th;
            for (int i = 0; i < tplPixels; i++)
            {
                double v = tplGray[i];
                tplSum += v;
                tplSumSq += v * v;
            }
            double tplMean = tplSum / tplPixels;
            double tplStdDev = Math.Sqrt(tplSumSq / tplPixels - tplMean * tplMean);

            if (tplStdDev < 1e-6) return result; // flat template

            double bestNCC = -1;
            int bestX = 0, bestY = 0;

            // Phase 1: Coarse scan (step=2)
            int step = 2;
            for (int y = 0; y <= sh - th; y += step)
            {
                for (int x = 0; x <= sw - tw; x += step)
                {
                    double ncc = ComputeNCC(srcGray, sw, tplGray, tw, th, x, y, tplMean, tplStdDev);
                    if (ncc > bestNCC)
                    {
                        bestNCC = ncc;
                        bestX = x;
                        bestY = y;
                    }
                }
            }

            // Phase 2: Refine around best match (step=1) in a small window
            if (bestNCC > minConfidence * 0.7)
            {
                int refineRange = step + 1;
                int rxStart = Math.Max(0, bestX - refineRange);
                int ryStart = Math.Max(0, bestY - refineRange);
                int rxEnd = Math.Min(sw - tw, bestX + refineRange);
                int ryEnd = Math.Min(sh - th, bestY + refineRange);

                for (int y = ryStart; y <= ryEnd; y++)
                {
                    for (int x = rxStart; x <= rxEnd; x++)
                    {
                        double ncc = ComputeNCC(srcGray, sw, tplGray, tw, th, x, y, tplMean, tplStdDev);
                        if (ncc > bestNCC)
                        {
                            bestNCC = ncc;
                            bestX = x;
                            bestY = y;
                        }
                    }
                }
            }

            if (bestNCC >= minConfidence)
            {
                result.Found = true;
                result.X = bestX;
                result.Y = bestY;
                result.Width = tw;
                result.Height = th;
                result.Confidence = bestNCC;
                Console.WriteLine($"[ImageMatchService] Match found at ({bestX},{bestY}) confidence={bestNCC:F3}");
            }
            else
            {
                Console.WriteLine($"[ImageMatchService] No match above threshold. Best={bestNCC:F3}");
            }

            return result;
        }

        /// <summary>
        /// Computes Normalized Cross-Correlation at a specific position.
        /// </summary>
        private static double ComputeNCC(byte[] src, int srcWidth,
                                          byte[] tpl, int tplW, int tplH,
                                          int offsetX, int offsetY,
                                          double tplMean, double tplStdDev)
        {
            int tplPixels = tplW * tplH;
            double srcSum = 0;
            double srcSumSq = 0;
            double crossSum = 0;

            for (int ty = 0; ty < tplH; ty++)
            {
                int srcRowIdx = (offsetY + ty) * srcWidth + offsetX;
                int tplRowIdx = ty * tplW;

                for (int tx = 0; tx < tplW; tx++)
                {
                    double sv = src[srcRowIdx + tx];
                    double tv = tpl[tplRowIdx + tx];

                    srcSum += sv;
                    srcSumSq += sv * sv;
                    crossSum += sv * tv;
                }
            }

            double srcMean = srcSum / tplPixels;
            double srcStdDev = Math.Sqrt(srcSumSq / tplPixels - srcMean * srcMean);

            if (srcStdDev < 1e-6) return 0;

            return (crossSum / tplPixels - srcMean * tplMean) / (srcStdDev * tplStdDev);
        }

        /// <summary>
        /// Converts an SKBitmap to a grayscale byte array (0-255 per pixel).
        /// </summary>
        private static byte[] ToGrayscale(SKBitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            var gray = new byte[w * h];

            // Ensure BGRA8888 format for consistent pixel access
            using var converted = bmp.ColorType == SKColorType.Bgra8888 ? bmp : bmp.Copy(SKColorType.Bgra8888);
            var pixels = converted.GetPixelSpan();

            for (int i = 0; i < w * h; i++)
            {
                int byteIdx = i * 4;
                int b = pixels[byteIdx];
                int g = pixels[byteIdx + 1];
                int r = pixels[byteIdx + 2];
                // ITU-R BT.601 luminance
                gray[i] = (byte)(0.299 * r + 0.587 * g + 0.114 * b);
            }

            return gray;
        }

        // ═══════════════════════════════════════════════════════════════
        //  EDGE-BASED COMPONENT BOUNDARY DETECTION
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Represents a detected UI component boundary on screen.
        /// </summary>
        public struct ComponentBounds
        {
            public bool Found;
            public int X, Y, Width, Height;
        }

        /// <summary>
        /// Detects the UI component boundary at the given screen position using edge detection.
        /// 
        /// Algorithm:
        /// 1. Capture a local region around the cursor (to keep it fast)
        /// 2. Compute Sobel edge magnitude on the grayscale image
        /// 3. From the cursor position, ray-cast outward in 4 directions (up/down/left/right)
        ///    to find the nearest strong edges (likely component borders)
        /// 4. Return the bounding rectangle
        /// </summary>
        public static ComponentBounds DetectComponentBounds(int screenX, int screenY)
        {
            var result = new ComponentBounds { Found = false };

            try
            {
                // Capture a generous region around cursor (400px in each direction)
                int regionHalf = 400;
                int rx = Math.Max(0, screenX - regionHalf);
                int ry = Math.Max(0, screenY - regionHalf);
                int rw = regionHalf * 2;
                int rh = regionHalf * 2;

                var regionBmp = CaptureRegion(rx, ry, rw, rh);
                if (regionBmp == null) return result;

                try
                {
                    int w = regionBmp.Width, h = regionBmp.Height;
                    // cursor position relative to captured region
                    int cx = screenX - rx;
                    int cy = screenY - ry;

                    if (cx < 0 || cx >= w || cy < 0 || cy >= h) return result;

                    // Step 1: Convert to grayscale
                    var gray = ToGrayscale(regionBmp);

                    // Step 2: Compute Sobel edge magnitude
                    var edges = ComputeSobelMagnitude(gray, w, h);

                    // Step 3: Determine adaptive threshold
                    // Use the 75th percentile of edge values in the region as threshold
                    int edgeThreshold = ComputeAdaptiveThreshold(edges, w, h, cx, cy, 150);

                    // Minimum threshold to avoid noise
                    edgeThreshold = Math.Max(edgeThreshold, 25);

                    // Step 4: Ray-cast from cursor to find boundaries
                    int minSize = 16;
                    int maxReach = 350;

                    // Ray-cast LEFT
                    int left = cx;
                    for (int x = cx - 1; x >= Math.Max(0, cx - maxReach); x--)
                    {
                        if (edges[cy * w + x] > edgeThreshold) { left = x; break; }
                    }

                    // Ray-cast RIGHT
                    int right = cx;
                    for (int x = cx + 1; x < Math.Min(w, cx + maxReach); x++)
                    {
                        if (edges[cy * w + x] > edgeThreshold) { right = x; break; }
                    }

                    // Ray-cast UP
                    int top = cy;
                    for (int y = cy - 1; y >= Math.Max(0, cy - maxReach); y--)
                    {
                        if (edges[y * w + cx] > edgeThreshold) { top = y; break; }
                    }

                    // Ray-cast DOWN
                    int bottom = cy;
                    for (int y = cy + 1; y < Math.Min(h, cy + maxReach); y++)
                    {
                        if (edges[y * w + cx] > edgeThreshold) { bottom = y; break; }
                    }

                    // Validate: ensure minimum size
                    int detectedW = right - left;
                    int detectedH = bottom - top;

                    if (detectedW < minSize || detectedH < minSize)
                    {
                        // Fallback: use a small default region
                        left = cx - 32;
                        right = cx + 32;
                        top = cy - 16;
                        bottom = cy + 16;
                        detectedW = 64;
                        detectedH = 32;
                    }

                    // Add small padding (2px around the detected edges)
                    int pad = 2;
                    left = Math.Max(0, left - pad);
                    top = Math.Max(0, top - pad);
                    right = Math.Min(w - 1, right + pad);
                    bottom = Math.Min(h - 1, bottom + pad);

                    // Convert back to screen coordinates
                    result.Found = true;
                    result.X = rx + left;
                    result.Y = ry + top;
                    result.Width = right - left + 1;
                    result.Height = bottom - top + 1;

                    // Clamp max size (avoid capturing entire screen)
                    if (result.Width > 500) { result.X = screenX - 100; result.Width = 200; }
                    if (result.Height > 300) { result.Y = screenY - 20; result.Height = 40; }
                }
                finally
                {
                    regionBmp.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageMatchService] DetectComponentBounds error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Computes Sobel edge magnitude for each pixel.
        /// Returns an int array where higher values = stronger edges.
        /// </summary>
        private static int[] ComputeSobelMagnitude(byte[] gray, int w, int h)
        {
            var mag = new int[w * h];

            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    // 3x3 Sobel kernels
                    int gx = -gray[(y - 1) * w + (x - 1)] - 2 * gray[y * w + (x - 1)] - gray[(y + 1) * w + (x - 1)]
                           + gray[(y - 1) * w + (x + 1)] + 2 * gray[y * w + (x + 1)] + gray[(y + 1) * w + (x + 1)];

                    int gy = -gray[(y - 1) * w + (x - 1)] - 2 * gray[(y - 1) * w + x] - gray[(y - 1) * w + (x + 1)]
                           + gray[(y + 1) * w + (x - 1)] + 2 * gray[(y + 1) * w + x] + gray[(y + 1) * w + (x + 1)];

                    mag[y * w + x] = (int)Math.Sqrt(gx * gx + gy * gy);
                }
            }

            return mag;
        }

        /// <summary>
        /// Computes an adaptive edge threshold based on the local neighborhood around the cursor.
        /// Uses the 75th percentile of edge magnitudes within the search radius.
        /// </summary>
        private static int ComputeAdaptiveThreshold(int[] edges, int w, int h, int cx, int cy, int radius)
        {
            // Collect edge values in the neighborhood
            var values = new System.Collections.Generic.List<int>(radius * 4);

            int xStart = Math.Max(1, cx - radius), xEnd = Math.Min(w - 2, cx + radius);
            int yStart = Math.Max(1, cy - radius), yEnd = Math.Min(h - 2, cy + radius);

            // Sample along horizontal and vertical lines through cursor (faster than full area)
            for (int x = xStart; x <= xEnd; x++)
                values.Add(edges[cy * w + x]);
            for (int y = yStart; y <= yEnd; y++)
                values.Add(edges[y * w + cx]);

            if (values.Count == 0) return 30;

            values.Sort();
            // 70th percentile
            int idx = (int)(values.Count * 0.70);
            return values[Math.Min(idx, values.Count - 1)];
        }

        /// <summary>
        /// Lists all captured template image paths.
        /// </summary>
        public static string[] GetCapturedImages()
        {
            if (!Directory.Exists(CapturedImagesDir)) return Array.Empty<string>();
            return Directory.GetFiles(CapturedImagesDir, "*.png");
        }
    }
}
