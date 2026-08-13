from PIL import Image, ImageDraw
import numpy as np

def make_clean_blood_png():
    # 512x512 PNG with 100% transparent RGBA background (0,0,0,0)
    size = 512
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    cx, cy = size // 2, size // 2
    
    # Blood color
    crimson = (175, 12, 12, 235)
    dark_blood = (115, 4, 4, 250)
    bright_blood = (210, 25, 25, 220)
    
    # Organic main blood pool
    np.random.seed(101)
    angles = np.linspace(0, 2 * np.pi, 200)
    r_main = 70 + np.sin(angles * 4) * 22 + np.cos(angles * 7) * 14 + np.random.normal(0, 4, 200)
    
    pts_main = [(cx + r * np.cos(a), cy + r * np.sin(a)) for a, r in zip(angles, r_main)]
    draw.polygon(pts_main, fill=crimson)
    
    # Dark central pool
    r_dark = r_main * 0.55
    pts_dark = [(cx + r * np.cos(a), cy + r * np.sin(a)) for a, r in zip(angles, r_dark)]
    draw.polygon(pts_dark, fill=dark_blood)
    
    # Bright highlights
    r_bright = r_main * 0.3
    pts_bright = [(cx + 10 + r * np.cos(a), cy - 10 + r * np.sin(a)) for a, r in zip(angles, r_bright)]
    draw.polygon(pts_bright, fill=bright_blood)
    
    # Splatters and spray droplets
    drops = [
        (cx + 90, cy - 45, 11), (cx - 100, cy + 40, 13), (cx + 45, cy + 95, 9),
        (cx - 55, cy - 85, 8), (cx + 105, cy + 65, 6), (cx - 105, cy - 60, 9),
        (cx + 15, cy - 110, 7), (cx - 80, cy + 90, 10), (cx + 120, cy - 15, 5),
        (cx - 115, cy - 10, 6), (cx + 65, cy - 95, 8), (cx - 45, cy + 110, 7),
        (cx + 80, cy + 105, 5), (cx - 95, cy - 90, 6)
    ]
    for dx, dy, dr in drops:
        draw.ellipse([dx - dr, dy - dr, dx + dr, dy + dr], fill=crimson)
        
    img.save('wwwroot/images/real_blood_stain.png')
    print('Clean blood stain PNG generated successfully!')

def make_clean_fingerprint_png():
    size = 512
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    cx, cy = size // 2, size // 2
    # Silver cyan forensic ridge color
    ridge_color = (200, 240, 255, 245)
    
    # Concentric fingerprint loops & whorls
    for r in range(10, 205, 8):
        num_pts = 360
        pts = []
        for a in range(num_pts):
            rad = np.radians(a)
            rx = r * 0.70
            ry = r * 1.12
            wave = np.sin(rad * 5) * 1.4 + np.cos(rad * 3) * 1.1
            
            # Loop gap at bottom
            if 65 <= a <= 115 and r > 65:
                continue
                
            px = cx + (rx + wave) * np.cos(rad)
            py = cy + (ry + wave) * np.sin(rad)
            pts.append((px, py))
            
        for i in range(len(pts) - 1):
            draw.line([pts[i], pts[i+1]], fill=ridge_color, width=3)
            
    # Center whorls
    for r in range(3, 22, 4):
        draw.ellipse([cx - r*0.7, cy - r*1.1, cx + r*0.7, cy + r*1.1], outline=ridge_color, width=2)
        
    img.save('wwwroot/images/real_fingerprint.png')
    print('Clean fingerprint PNG generated successfully!')

make_clean_blood_png()
make_clean_fingerprint_png()
