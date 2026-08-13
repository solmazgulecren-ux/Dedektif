from PIL import Image
import numpy as np

def clean_splatter():
    # If there is a backup or we can process blood stain
    img = Image.open('wwwroot/images/real_blood_stain.png').convert('RGBA')
    arr = np.array(img, dtype=np.float32)
    
    r = arr[:, :, 0]
    g = arr[:, :, 1]
    b = arr[:, :, 2]
    a = arr[:, :, 3]

    # Mask for blood: Red channel significantly higher than Green and Blue
    # White background has high R, G, B (R>140, G>140, B>140)
    # Dark/gray background has low difference between R, G, B
    is_bg = (r > 150) & (g > 150) & (b > 150)
    is_blood = (r > 60) & ((r - g) > 18) & ((r - b) > 18) & ~is_bg
    
    out = np.zeros_like(arr, dtype=np.uint8)
    
    # Blood pixels get rich crimson color and opacity based on redness
    redness = np.clip((r - (g + b) / 2.0) / 100.0, 0, 1)
    
    out[:, :, 0] = np.clip(r * 1.1, 140, 220).astype(np.uint8)  # Deep Red
    out[:, :, 1] = np.clip(g * 0.3, 0, 30).astype(np.uint8)
    out[:, :, 2] = np.clip(b * 0.3, 0, 30).astype(np.uint8)
    out[:, :, 3] = (is_blood * 240 * redness).astype(np.uint8)  # 100% transparent background!
    
    clean = Image.fromarray(out, 'RGBA')
    clean.save('wwwroot/images/real_blood_stain.png')
    print('Cleaned blood stain saved!')

clean_splatter()
