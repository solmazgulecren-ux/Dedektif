from PIL import Image

img_path = r'c:\Users\solma\OneDrive\Desktop\Dedektiflik\wwwroot\images\dedektif_helper.png'
img = Image.open(img_path)
img = img.convert('RGBA')
datas = img.getdata()

new_data = []
for item in datas:
    # Change all white (also shades of whites)
    # to transparent
    if item[0] > 220 and item[1] > 220 and item[2] > 220:
        new_data.append((255, 255, 255, 0))
    else:
        new_data.append(item)

img.putdata(new_data)
img.save(img_path, 'PNG')
