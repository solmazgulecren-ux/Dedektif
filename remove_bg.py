from PIL import Image

img_path = 'c:\\Users\\solma\\OneDrive\\Desktop\\Dedektiflik\\wwwroot\\images\\dedektif_helper.png'
img = Image.open(img_path).convert('RGBA')
datas = img.getdata()
new_data = []

for item in datas:
    # If pixel is whitish (R, G, B > 180), make it completely transparent.
    if item[0] > 180 and item[1] > 180 and item[2] > 180:
        new_data.append((255, 255, 255, 0))
    else:
        new_data.append(item)

img.putdata(new_data)
img.save(img_path)
print('Background removed perfectly')
