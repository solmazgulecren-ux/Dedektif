from PIL import Image, ImageDraw
import numpy as np

def create_perfect_fingerprint():
    size = 512
    # Create 100% transparent RGBA image
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    center_x, center_y = size // 2, size // 2
    
    # Draw realistic forensic fingerprint ridges (whorls and loops)
    # Ridge lines in silver-cyan / neon purple (RGB: 160, 220, 255)
    ridge_color = (180, 230, 255, 240)
    
    # Concentric fingerprint ellipses with organic wave modulation
    for r in range(12, 210, 9):
        num_points = 360
        points = []
        for a in range(num_points):
            angle_rad = np.radians(a)
            # Elliptical shape (fingerprint loop shape: elongated vertically)
            rx = r * 0.72
            ry = r * 1.15
            
            # Add organic ridge noise/wave
            wave = np.sin(angle_rad * 6) * 1.5 + np.cos(angle_rad * 3) * 1.2
            
            # Cut off bottom ridge for loop pattern realism
            if 60 <= a <= 120 and r > 70:
                continue
                
            px = center_x + (rx + wave) * np.cos(angle_rad)
            py = center_y + (ry + wave) * np.sin(angle_rad)
            points.append((px, py))
            
        # Draw ridge line segments with 3px width
        for i in range(len(points) - 1):
            draw.line([points[i], points[i+1]], fill=ridge_color, width=3)
            
    # Add core fingerprint whorl center loops
    for r in range(4, 25, 4):
        draw.ellipse([center_x - r*0.7, center_y - r*1.1, center_x + r*0.7, center_y + r*1.1], outline=ridge_color, width=2)
        
    img.save('wwwroot/images/real_fingerprint.png')
    print('Perfect transparent fingerprint PNG generated!')

def create_perfect_blood_stain():
    size = 512
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    center_x, center_y = size // 2, size // 2
    blood_color = (170, 15, 15, 230)  # Deep Crimson Blood
    dark_blood = (110, 5, 5, 245)
    
    # Draw organic central blood pool
    num_points = 180
    np.random.seed(42)
    angles = np.linspace(0, 2 * np.pi, num_points)
    
    # Organic irregular blood drop contour
    radii = 75 + np.sin(angles * 5) * 20 + np.cos(angles * 8) * 15 + np.random.normal(0, 5, num_points)
    
    points = []
    for a, r in zip(angles, radii):
        px = center_x + r * np.cos(a)
        py = center_y + r * np.sin(a)
        points.append((px, py))
        
    draw.polygon(points, fill=blood_color)
    
    # Inner dark thick blood core
    inner_radii = radii * 0.55
    inner_points = [(center_x + r * np.cos(a), center_y + r * np.sin(a)) for a, r in zip(angles, inner_radii)]
    draw.polygon(inner_points, fill=dark_blood)
    
    # Add blood droplets / splatters around main stain
    splatters = [
        (center_x + 95, center_y - 40, 12),
        (center_x - 105, center_y + 35, 14),
        (center_x + 40, center_y + 100, 10),
        (center_x - 60, center_y - 90, 8),
        (center_x + 110, center_y + 70, 6),
        (center_x - 110, center_y - 65, 9),
        (center_x + 20, center_y - 115, 7),
        (center_x - 85, center_y + 95, 11),
    ]
    for sx, sy, sr in splatters:
        draw.ellipse([sx - sr, sy - sr, sx + sr, sy + sr], fill=blood_color)
        
    img.save('wwwroot/images/real_blood_stain.png')
    print('Perfect transparent blood stain PNG generated!')

create_perfect_fingerprint()
create_perfect_blood_stain()
