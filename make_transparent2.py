from PIL import Image

img_path = r'c:\Users\solma\OneDrive\Desktop\Dedektiflik\wwwroot\images\dedektif_helper.png'
img = Image.open(img_path).convert('RGBA')

datas = img.getdata()
new_data = []

# Tolerance for "white"
threshold = 200

for item in datas:
    r, g, b, a = item
    
    # If the pixel is close to white, make it transparent
    if r > threshold and g > threshold and b > threshold:
        # Calculate how close to white it is (255 is purely white)
        avg = (r + g + b) / 3
        
        if avg > 240:
            new_data.append((255, 255, 255, 0)) # Completely transparent
        else:
            # Partially transparent for anti-aliasing (smooth edges)
            alpha = int(255 - ((avg - threshold) / (255 - threshold) * 255))
            new_data.append((r, g, b, alpha))
    else:
        new_data.append(item)

img.putdata(new_data)
img.save(r'c:\Users\solma\OneDrive\Desktop\Dedektiflik\wwwroot\images\dedektif_helper_clean.png', 'PNG')
