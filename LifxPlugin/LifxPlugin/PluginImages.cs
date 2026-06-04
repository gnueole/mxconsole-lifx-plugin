namespace Loupedeck.LifxPlugin
{
    using System;

    internal static class PluginImages
    {
        public static readonly BitmapColor PurpleColor = new BitmapColor(0x82, 0x00, 0xFF);
        public static readonly BitmapColor BlackColor = BitmapColor.Black;

        public static BitmapImage CreateButtonImage(PluginImageSize imageSize, string text, BitmapColor? textColor = null, BitmapColor? bgColor = null)
        {
            if (imageSize == PluginImageSize.None)
            {
                return null;
            }

            var tc = textColor ?? PurpleColor;
            var bg = bgColor ?? BlackColor;

            using (var builder = new BitmapBuilder(imageSize))
            {
                builder.Clear(bg);

                var fontSize = BitmapBuilder.GetDefaultFontSize(imageSize);
                var lineHeight = BitmapBuilder.GetDefaultLineHeight(imageSize);
                var spaceHeight = BitmapBuilder.GetDefaultSpaceHeight(imageSize);

                builder.DrawText(text, tc, fontSize, lineHeight, spaceHeight);

                return builder.ToImage();
            }
        }

        public static BitmapImage CreateBulbButtonImage(PluginImageSize imageSize, bool isGroup, BitmapColor? textColor = null, BitmapColor? bgColor = null)
        {
            if (imageSize == PluginImageSize.None)
            {
                return null;
            }

            var tc = textColor ?? PurpleColor;
            var bg = bgColor ?? BlackColor;

            using (var builder = new BitmapBuilder(imageSize))
            {
                builder.Clear(bg);

                int w = builder.Width;
                int h = builder.Height;

                // Center the bulb icon in the middle of the button
                float centerX = w / 2f;
                float centerY = h / 2f;
                float bulbSize = Math.Min(w, h) * 0.65f;

                if (isGroup)
                {
                    DrawTwoBulbs(builder, centerX, centerY, bulbSize, tc);
                }
                else
                {
                    DrawBulb(builder, centerX, centerY, bulbSize, tc);
                }

                return builder.ToImage();
            }
        }

        private static void DrawBulb(BitmapBuilder builder, float x, float y, float size, BitmapColor color)
        {
            // Glass part (thickened by drawing multiple concentric circles)
            float bulbRadius = size * 0.28f;
            float bulbCenterY = y - size * 0.08f;
            builder.DrawCircle(x, bulbCenterY, bulbRadius, color);
            builder.DrawCircle(x, bulbCenterY, bulbRadius - 0.7f, color);
            builder.DrawCircle(x, bulbCenterY, bulbRadius - 1.4f, color);

            // Metal base (thickened by drawing concentric rectangles)
            float baseWidth = size * 0.20f;
            float baseHeight = size * 0.12f;
            float baseX = x - baseWidth / 2f;
            float baseY = bulbCenterY + bulbRadius - size * 0.04f;
            builder.DrawRectangle((int)baseX, (int)baseY, (int)baseWidth, (int)baseHeight, color);
            builder.DrawRectangle((int)baseX + 1, (int)baseY + 1, (int)baseWidth - 2, (int)baseHeight - 2, color);

            // Base threads (thickened to 2.5f)
            builder.DrawLine(baseX, baseY + baseHeight * 0.33f, baseX + baseWidth, baseY + baseHeight * 0.33f, color, 2.5f);
            builder.DrawLine(baseX + baseWidth * 0.2f, baseY + baseHeight * 0.66f, baseX + baseWidth * 0.8f, baseY + baseHeight * 0.66f, color, 2.5f);

            // Filament (thickened to 2.5f)
            float filY = bulbCenterY + bulbRadius * 0.2f;
            builder.DrawLine(x - bulbRadius * 0.3f, filY, x + bulbRadius * 0.3f, filY, color, 2.5f);
            builder.DrawLine(x - bulbRadius * 0.3f, filY, x - bulbRadius * 0.1f, filY - bulbRadius * 0.3f, color, 2.5f);
            builder.DrawLine(x + bulbRadius * 0.3f, filY, x + bulbRadius * 0.1f, filY - bulbRadius * 0.3f, color, 2.5f);

            // Rays (thickened to 3f)
            float rayLength = size * 0.08f;
            float startDist = bulbRadius + size * 0.04f;

            // Top
            builder.DrawLine(x, bulbCenterY - startDist, x, bulbCenterY - startDist - rayLength, color, 3f);
            // Left & Right
            builder.DrawLine(x - startDist, bulbCenterY, x - startDist - rayLength, bulbCenterY, color, 3f);
            builder.DrawLine(x + startDist, bulbCenterY, x + startDist + rayLength, bulbCenterY, color, 3f);
            // Diagonals
            builder.DrawLine(x - startDist * 0.7f, bulbCenterY - startDist * 0.7f, x - (startDist + rayLength) * 0.7f, bulbCenterY - (rayLength + startDist) * 0.7f, color, 3f);
            builder.DrawLine(x + startDist * 0.7f, bulbCenterY - startDist * 0.7f, x + (startDist + rayLength) * 0.7f, bulbCenterY - (rayLength + startDist) * 0.7f, color, 3f);
        }

        private static void DrawTwoBulbs(BitmapBuilder builder, float x, float y, float size, BitmapColor color)
        {
            // Left bulb slightly lower and offset
            DrawBulb(builder, x - size * 0.18f, y + size * 0.04f, size * 0.75f, color);

            // Right bulb slightly higher and offset
            DrawBulb(builder, x + size * 0.18f, y - size * 0.04f, size * 0.75f, color);
        }

        public static BitmapImage CreateColorWheelImage(PluginImageSize imageSize, string text = null, BitmapColor? bgColor = null)
        {
            if (imageSize == PluginImageSize.None)
            {
                return null;
            }

            var bg = bgColor ?? BlackColor;

            using (var builder = new BitmapBuilder(imageSize))
            {
                builder.Clear(bg);

                int w = builder.Width;
                int h = builder.Height;

                int centerX = w / 2;
                int centerY = h / 2;
                int outerRadius = (int)(Math.Min(w, h) * 0.35); 
                int strokeWidth = (int)(Math.Min(w, h) * 0.11); 

                // Full 360-degree color wheel restored
                for (int i = 0; i < 36; i++)
                {
                    float startAngle = i * 10f;
                    float sweepAngle = 10.5f; 
                    double hue = i * 10.0;

                    if (BitmapColor.TryParseHslaColor(hue, 1.0, 0.5, 255, out var arcColor))
                    {
                        builder.DrawArc(centerX, centerY, outerRadius, startAngle, sweepAngle, arcColor, strokeWidth);
                    }
                }

                if (!string.IsNullOrEmpty(text))
                {
                    var tc = PurpleColor;
                    int fontSize = (int)(w * 0.13f); 
                    int lineHeight = (int)(fontSize * 1.2f);
                    int spaceHeight = (int)(fontSize * 0.3f);
                    
                    int boxW = (int)(outerRadius * 2 * 0.8f);
                    int boxH = (int)(outerRadius * 2 * 0.8f);
                    int boxX = centerX - boxW / 2;
                    int boxY = centerY - boxH / 2;

                    builder.DrawText(text, boxX, boxY, boxW, boxH, tc, fontSize, lineHeight, spaceHeight, "Brown Logitech Pan Light");
                }

                return builder.ToImage();
            }
        }

        public static BitmapImage CreateBrightnessGaugeImage(PluginImageSize imageSize, string text = null, BitmapColor? bgColor = null)
        {
            if (imageSize == PluginImageSize.None)
            {
                return null;
            }

            var bg = bgColor ?? BlackColor;

            using (var builder = new BitmapBuilder(imageSize))
            {
                builder.Clear(bg);

                int w = builder.Width;
                int h = builder.Height;

                int centerX = w / 2;
                int centerY = h / 2;
                int outerRadius = (int)(Math.Min(w, h) * 0.35); 
                int strokeWidth = (int)(Math.Min(w, h) * 0.11); 

                // Draw 27 segments graduating from a visible base yellow (30%) to full gold/yellow (100%)
                for (int i = 0; i < 27; i++)
                {
                    float startAngle = 135f + i * 10f;
                    float sweepAngle = 10.5f; 
                    float t = 0.3f + 0.7f * (i / 26f); // color fraction starting at 30% for symmetry

                    int r = (int)(255 * t);
                    int g = (int)(220 * t);
                    int b = (int)(50 * t);
                    var arcColor = new BitmapColor(r, g, b);

                    builder.DrawArc(centerX, centerY, outerRadius, startAngle, sweepAngle, arcColor, strokeWidth);
                }

                if (!string.IsNullOrEmpty(text))
                {
                    var tc = PurpleColor;
                    int fontSize = (int)(w * 0.13f); 
                    int lineHeight = (int)(fontSize * 1.2f);
                    int spaceHeight = (int)(fontSize * 0.3f);
                    
                    int boxW = (int)(outerRadius * 2 * 0.8f);
                    int boxH = (int)(outerRadius * 2 * 0.8f);
                    int boxX = centerX - boxW / 2;
                    int boxY = centerY - boxH / 2;

                    builder.DrawText(text, boxX, boxY, boxW, boxH, tc, fontSize, lineHeight, spaceHeight, "Brown Logitech Pan Light");
                }

                return builder.ToImage();
            }
        }

        public static BitmapImage CreateWarmthWheelImage(PluginImageSize imageSize, BitmapColor? bgColor = null)
        {
            if (imageSize == PluginImageSize.None)
            {
                return null;
            }

            var bg = bgColor ?? BlackColor;

            using (var builder = new BitmapBuilder(imageSize))
            {
                builder.Clear(bg);

                int w = builder.Width;
                int h = builder.Height;

                int centerX = w / 2;
                int centerY = h / 2;
                int outerRadius = (int)(Math.Min(w, h) * 0.35); 
                int strokeWidth = (int)(Math.Min(w, h) * 0.11); 

                // Smooth gradient around the 270-degree arc:
                // Inverted so it is warm orange (255, 100, 0) on the left (i=0) to cold blue (100, 180, 255) on the right (i=26)
                for (int i = 0; i < 27; i++)
                {
                    float startAngle = 135f + i * 10f;
                    float sweepAngle = 10.5f; 
                    
                    float t = (26 - i) / 26f;

                    int r = (int)(100 + (255 - 100) * t);
                    int g = (int)(180 + (100 - 180) * t);
                    int b = (int)(255 + (0 - 255) * t);
                    var arcColor = new BitmapColor(r, g, b);

                    builder.DrawArc(centerX, centerY, outerRadius, startAngle, sweepAngle, arcColor, strokeWidth);
                }

                return builder.ToImage();
            }
        }

        public static BitmapImage CreatePowerButtonImage(PluginImageSize imageSize, BitmapColor? textColor = null, BitmapColor? bgColor = null)
        {
            if (imageSize == PluginImageSize.None)
            {
                return null;
            }

            var tc = textColor ?? PurpleColor;
            var bg = bgColor ?? BlackColor;

            using (var builder = new BitmapBuilder(imageSize))
            {
                builder.Clear(bg);

                int w = builder.Width;
                int h = builder.Height;

                int centerX = w / 2;
                int centerY = h / 2;
                int radius = (int)(Math.Min(w, h) * 0.28);
                int strokeWidth = (int)(Math.Min(w, h) * 0.06);
                if (strokeWidth < 1)
                {
                    strokeWidth = 1;
                }

                // Draw circle arc for the power symbol (from 300 degrees to 240 degrees, leaving the top open)
                builder.DrawArc(centerX, centerY, radius, 300f, 300f, tc, (float)strokeWidth);

                // Draw vertical line from center top downwards
                int lineStartY = centerY - (int)(radius * 1.2);
                int lineEndY = centerY;
                builder.DrawLine((float)centerX, (float)lineStartY, (float)centerX, (float)lineEndY, tc, (float)strokeWidth);

                return builder.ToImage();
            }
        }
    }
}
