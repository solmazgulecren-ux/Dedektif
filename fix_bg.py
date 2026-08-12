from PIL import Image
import glob
import os

def make_transparent(img_path):
    try:
        img = Image.open(img_path).convert('RGBA')
        datas = img.getdata()
        newData = []
        for item in datas:
            # If pixel is close to white (e.g., R>200, G>200, B>200)
            if item[0] > 200 and item[1] > 200 and item[2] > 200:
                newData.append((255, 255, 255, 0)) # transparent
            else:
                newData.append(item)
        img.putdata(newData)
        img.save(img_path, 'PNG')
    except Exception as e:
        pass

for file in glob.glob('wwwroot/images/*.png'):
    if 'wide' not in file and 'final' not in file and 'hasan' not in file and 'kemal' not in file and 'selma' not in file and 'yahya' not in file and 'gunes' not in file and 'interior' not in file and 'map' not in file:
        # Just run on the 15 items
        # To be safe, specify them:
        make_transparent(file)

