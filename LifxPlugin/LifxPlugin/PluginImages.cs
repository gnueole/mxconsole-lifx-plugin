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

        public static BitmapImage CreateBulbButtonImage(PluginImageSize imageSize, string text, bool isGroup, BitmapColor? textColor = null, BitmapColor? bgColor = null)
        {
            var tc = textColor ?? PurpleColor;
            var bg = bgColor ?? BlackColor;

            using (var builder = new BitmapBuilder(imageSize))
            {
                builder.Clear(bg);

                int w = builder.Width;
                int h = builder.Height;

                // Position the bulb icon in the upper part
                float centerX = w / 2f;
                float centerY = h * 0.38f;
                float bulbSize = Math.Min(w, h) * 0.52f;

                if (isGroup)
                {
                    DrawTwoBulbs(builder, centerX, centerY, bulbSize, tc);
                }
                else
                {
                    DrawBulb(builder, centerX, centerY, bulbSize, tc);
                }

                // Strip redundant prefixes like "Toggle" or "Select" to keep the button label clean
                var cleanText = text;
                if (cleanText.StartsWith("Toggle\n"))
                {
                    cleanText = cleanText.Substring("Toggle\n".Length);
                }
                else if (cleanText.StartsWith("Toggle "))
                {
                    cleanText = cleanText.Substring("Toggle ".Length);
                }
                else if (cleanText.StartsWith("Select\n"))
                {
                    cleanText = cleanText.Substring("Select\n".Length);
                }
                else if (cleanText.StartsWith("Select "))
                {
                    cleanText = cleanText.Substring("Select ".Length);
                }

                var fontSize = BitmapBuilder.GetDefaultFontSize(imageSize) - 1;
                var lineHeight = BitmapBuilder.GetDefaultLineHeight(imageSize);
                var spaceHeight = BitmapBuilder.GetDefaultSpaceHeight(imageSize);

                int textHeight = (int)(h * 0.32f);
                int textY = h - textHeight - 2;

                builder.DrawText(cleanText, 0, textY, w, textHeight, tc, fontSize, lineHeight, spaceHeight, null);

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
    }
}
