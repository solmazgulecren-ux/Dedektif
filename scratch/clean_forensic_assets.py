from PIL import Image
import numpy as np

def clean_fingerprint():
    img = Image.open('wwwroot/images/real_fingerprint.png').convert('RGBA')
    data = np.array(img, dtype=np.float32)
    
    r = data[:, :, 0]
    g = data[:, :, 1]
    b = data[:, :, 2]
    a = data[:, :, 3]

    # Calculate brightness / intensity of ridge details
    # The fingerprint ridges are bright purple/cyan/white, background is dark/black or white box
    brightness = (r + g + b) / 3.0
    
    # Create new RGBA array
    out = np.zeros_like(data, dtype=np.uint8)
    
    # Mask for fingerprint ridge pixels (bright ridges on dark bg OR dark ridges on light bg)
    # If image has black bg, ridges are bright
    # Convert ridges to glowing silver-white adli tıp parmak izi: R=220, G=230, B=255
    # Alpha proportional to brightness intensity
    mask = brightness > 50
    
    # Normalize brightness mask between 0 and 1
    norm_b = np.clip((brightness - 50) / 180.0, 0, 1)
    
    out[:, :, 0] = 235  # Silver/white R
    out[:, :, 1] = 245  # Silver/white G
    out[:, :, 2] = 255  # Silver/white B
    out[:, :, 3] = (norm_b * 220).astype(np.uint8)  # Transparent alpha channel!
    
    clean_img = Image.fromarray(out, 'RGBA')
    clean_img.save('wwwroot/images/real_fingerprint.png')
    print('Fingerprint cleaned successfully!')

def clean_blood():
    img = Image.open('wwwroot/images/real_blood_stain.png').convert('RGBA')
    data = np.array(img, dtype=np.float32)
    
    r = data[:, :, 0]
    g = data[:, :, 1]
    b = data[:, :, 2]
    a = data[:, :, 3]
    
    # Blood is characterized by strong Red channel relative to Green and Blue
    # Pure white or light background has high R, G, B (e.g. R>170, G>170, B>170)
    # Dark background has low R, G, B
    
    is_white_bg = (r > 160) & (g > 160) & (b > 160)
    is_dark_bg = (r < 40) & (g < 40) & (b < 40)
    
    # Blood pixels have Red dominant over Green & Blue (r - g > 20 and r - b > 20)
    is_blood = (r > 60) & ((r - g) > 15) & ((r - b) > 15) & ~is_white_bg
    
    out = np.zeros_like(data, dtype=np.uint8)
    
    # Calculate blood redness intensity
    intensity = np.clip((r - (g + b) / 2.0) / 120.0, 0, 1)
    intensity[~is_blood] = 0
    
    out[:, :, 0] = np.clip(r * 1.1, 140, 220).astype(np.uint8)  # Deep Crimson Red
    out[:, :, 1] = np.clip(g * 0.4, 0, 40).astype(np.uint8)
    out[:, :, 2] = np.clip(b * 0.4, 0, 40).astype(np.uint8)
    out[:, :, 3] = (intensity * 230).astype(np.uint8)  # Smooth transparent alpha!
    
    clean_img = Image.fromarray(out, 'RGBA')
    clean_img.save('wwwroot/images/real_blood_stain.png')
    print('Blood stain cleaned successfully!')

clean_fingerprint()
clean_blood()
