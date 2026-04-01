import os
from PIL import Image, ImageChops

def trim(im):
    bg = Image.new(im.mode, im.size, im.getpixel((0,0)))
    diff = ImageChops.difference(im, bg)
    diff = ImageChops.add(diff, diff, 2.0, -100)
    bbox = diff.getbbox()
    if bbox:
        return im.crop(bbox)
    return im

def create_full_icon(input_path, output_path):
    print(f"Loading {input_path}...")
    img = Image.open(input_path)
    
    # 1. Convert to RGBA (to handle transparency if needed)
    img = img.convert("RGBA")
    
    # 2. Trim padding (Auto-crop)
    print("Trimming margins...")
    img = trim(img)
    
    # 3. Create a square version (if not square)
    width, height = img.size
    max_dim = max(width, height)
    square_img = Image.new("RGBA", (max_dim, max_dim), (0, 0, 0, 0))
    # Paste centered
    square_img.paste(img, ((max_dim - width) // 2, (max_dim - height) // 2))
    
    # 4. Icon sizes (Standard Windows Multi-res)
    sizes = [(256, 256), (128, 128), (96, 96), (64, 64), (48, 48), (32, 32), (16, 16)]
    
    # 5. Save as ICO
    print(f"Saving multi-resolution icon to {output_path}...")
    square_img.save(output_path, format="ICO", sizes=sizes)
    print("✅ Full-Scale Icon Generated Successfully!")

if __name__ == "__main__":
    pwd = os.getcwd()
    # Use the high-res PNG as source
    src = os.path.join(pwd, "Assets", "enjaz_3d_icon_transparent.png")
    dest = os.path.join(pwd, "Assets", "enjaz_3d_icon_transparent.ico")
    
    if os.path.exists(src):
        create_full_icon(src, dest)
    else:
        # Fallback to designer logo if PNG is missing
        src_alt = os.path.join(pwd, "Assets", "designer_logo.png")
        if os.path.exists(src_alt):
            create_full_icon(src_alt, dest)
        else:
            print(f"❌ Source logo not found at {src}")
