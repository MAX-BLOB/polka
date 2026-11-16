from PIL import Image, ImageDraw, ImageFont
import os

S = 256
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

# gradient background
grad = Image.new("RGBA", (S, S))
gd = ImageDraw.Draw(grad)
c1 = (139, 150, 255)
c2 = (192, 132, 252)
for y in range(S):
    for_x = y / (S - 1)
    # diagonal-ish gradient
    r = int(c1[0] + (c2[0] - c1[0]) * for_x)
    g = int(c1[1] + (c2[1] - c1[1]) * for_x)
    b = int(c1[2] + (c2[2] - c1[2]) * for_x)
    gd.line([(0, y), (S, y)], fill=(r, g, b, 255))

mask = Image.new("L", (S, S), 0)
md = ImageDraw.Draw(mask)
md.rounded_rectangle([0, 0, S - 1, S - 1], radius=56, fill=255)
img.paste(grad, (0, 0), mask)

# letter
d = ImageDraw.Draw(img)
font_path = r"C:\Windows\Fonts\segoeuib.ttf"
font = ImageFont.truetype(font_path, 158)
text = "П"
bbox = d.textbbox((0, 0), text, font=font)
tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
x = (S - tw) / 2 - bbox[0]
y = (S - th) / 2 - bbox[1] - 6
d.text((x, y), text, font=font, fill=(12, 14, 20, 255))

out = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "assets", "polka.ico")
os.makedirs(os.path.dirname(out), exist_ok=True)
img.save(out, format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
print("saved", out, os.path.getsize(out))
