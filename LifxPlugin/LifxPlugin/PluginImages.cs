namespace Loupedeck.LifxPlugin
{
    using System;

    internal static class PluginImages
    {
        public static readonly BitmapColor PurpleColor = new BitmapColor(0x82, 0x00, 0xFF);
        public static readonly BitmapColor BlackColor = BitmapColor.Black;

        public static BitmapImage CreateButtonImage(PluginImageSize imageSize, string text, BitmapColor? textColor = null, BitmapColor? bgColor = null)
        {
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
            // Glass part
            float bulbRadius = size * 0.28f;
            float bulbCenterY = y - size * 0.08f;
            builder.DrawCircle(x, bulbCenterY, bulbRadius, color);

            // Metal base
            float baseWidth = size * 0.20f;
            float baseHeight = size * 0.12f;
            float baseX = x - baseWidth / 2f;
            float baseY = bulbCenterY + bulbRadius - size * 0.04f;
            builder.DrawRectangle((int)baseX, (int)baseY, (int)baseWidth, (int)baseHeight, color);

            // Base threads
            builder.DrawLine(baseX, baseY + baseHeight * 0.33f, baseX + baseWidth, baseY + baseHeight * 0.33f, color, 1f);
            builder.DrawLine(baseX + baseWidth * 0.2f, baseY + baseHeight * 0.66f, baseX + baseWidth * 0.8f, baseY + baseHeight * 0.66f, color, 1f);

            // Filament
            float filY = bulbCenterY + bulbRadius * 0.2f;
            builder.DrawLine(x - bulbRadius * 0.3f, filY, x + bulbRadius * 0.3f, filY, color, 1f);
            builder.DrawLine(x - bulbRadius * 0.3f, filY, x - bulbRadius * 0.1f, filY - bulbRadius * 0.3f, color, 1f);
            builder.DrawLine(x + bulbRadius * 0.3f, filY, x + bulbRadius * 0.1f, filY - bulbRadius * 0.3f, color, 1f);

            // Rays
            float rayLength = size * 0.08f;
            float startDist = bulbRadius + size * 0.04f;

            // Top
            builder.DrawLine(x, bulbCenterY - startDist, x, bulbCenterY - startDist - rayLength, color, 1.2f);
            // Left & Right
            builder.DrawLine(x - startDist, bulbCenterY, x - startDist - rayLength, bulbCenterY, color, 1.2f);
            builder.DrawLine(x + startDist, bulbCenterY, x + startDist + rayLength, bulbCenterY, color, 1.2f);
            // Diagonals
            builder.DrawLine(x - startDist * 0.7f, bulbCenterY - startDist * 0.7f, x - (startDist + rayLength) * 0.7f, bulbCenterY - (rayLength + startDist) * 0.7f, color, 1.2f);
            builder.DrawLine(x + startDist * 0.7f, bulbCenterY - startDist * 0.7f, x + (startDist + rayLength) * 0.7f, bulbCenterY - (rayLength + startDist) * 0.7f, color, 1.2f);
        }

        private static void DrawTwoBulbs(BitmapBuilder builder, float x, float y, float size, BitmapColor color)
        {
            // Left bulb slightly lower and offset
            DrawBulb(builder, x - size * 0.18f, y + size * 0.04f, size * 0.75f, color);

            // Right bulb slightly higher and offset
            DrawBulb(builder, x + size * 0.18f, y - size * 0.04f, size * 0.75f, color);
        }

        public static BitmapImage CreateColorWheelImage(PluginImageSize imageSize, BitmapColor? bgColor = null)
        {
            var bg = bgColor ?? BlackColor;

            using (var builder = new BitmapBuilder(imageSize))
            {
                builder.Clear(bg);

                int w = builder.Width;
                int h = builder.Height;

                int centerX = w / 2;
                int centerY = h / 2;
                int outerRadius = (int)(Math.Min(w, h) * 0.42); 
                int strokeWidth = (int)(Math.Min(w, h) * 0.14); 

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

                return builder.ToImage();
            }
        }

        public static BitmapImage CreateBrightnessGaugeImage(PluginImageSize imageSize, BitmapColor? bgColor = null)
        {
            var bg = bgColor ?? BlackColor;

            using (var builder = new BitmapBuilder(imageSize))
            {
                builder.Clear(bg);

                int w = builder.Width;
                int h = builder.Height;

                int numBars = 5;
                float totalWidth = w * 0.70f;
                float barWidth = totalWidth * 0.12f;
                float barSpacing = (totalWidth - (numBars * barWidth)) / (numBars - 1);
                
                float startX = (w - totalWidth) / 2f;
                float maxHeight = h * 0.60f;
                float minHeight = h * 0.20f;
                float bottomY = (h + maxHeight) / 2f;

                for (int i = 0; i < numBars; i++)
                {
                    float t = (i + 1) / (float)numBars;
                    
                    float barHeight = minHeight + i * (maxHeight - minHeight) / (numBars - 1);
                    float barX = startX + i * (barWidth + barSpacing);
                    float barY = bottomY - barHeight;

                    int r = (int)(0x82 * t);
                    int g = 0;
                    int b = (int)(0xFF * t);
                    var barColor = new BitmapColor(r, g, b);

                    builder.FillRectangle((int)barX, (int)barY, (int)barWidth, (int)barHeight, barColor);
                }

                return builder.ToImage();
            }
        }
    }
}

