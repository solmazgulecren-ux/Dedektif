import os
from PIL import Image, ImageChops

def screen_paste(bg_image, item_image, position):
    # Crop the background area where the item will be placed
    box = (position[0], position[1], position[0] + item_image.width, position[1] + item_image.height)
    bg_patch = bg_image.crop(box)
    
    # Ensure item_image is RGB
    if item_image.mode != "RGB":
        item_image = item_image.convert("RGB")
    if bg_patch.mode != "RGB":
        bg_patch = bg_patch.convert("RGB")
        
    # Apply screen blend mode
    blended_patch = ImageChops.screen(bg_patch, item_image)
    
    # Paste back into background
    bg_image.paste(blended_patch, box)
    return bg_image

def composite_evidence():
    base_dir = "C:/Users/solma/OneDrive/Desktop/Dedektiflik/wwwroot/images"
    
    buildings = {
        1: { "bg": "kasap_final.png", "items": [
            {"img": "bloody_cleaver.png", "top": 0.58, "left": 0.64},
            {"img": "black_notebook.png", "top": 0.66, "left": 0.38},
            {"img": "torn_apron.png", "top": 0.40, "left": 0.16}
        ]},
        2: { "bg": "eczane_final.png", "items": [
            {"img": "empty_medicine_bottle.png", "top": 0.36, "left": 0.18},
            {"img": "prescription_notebook.png", "top": 0.60, "left": 0.68},
            {"img": "poison_ivy.png", "top": 0.68, "left": 0.44}
        ]},
        3: { "bg": "muhtarlik_final.png", "items": [
            {"img": "threat_letter.png", "top": 0.62, "left": 0.46},
            {"img": "broken_glasses.png", "top": 0.56, "left": 0.58},
            {"img": "hidden_safe.png", "top": 0.32, "left": 0.20}
        ]},
        4: { "bg": "karakol_final.png", "items": [
            {"img": "police_badge.png", "top": 0.62, "left": 0.34},
            {"img": "evidence_file.png", "top": 0.38, "left": 0.74},
            {"img": "missing_button.png", "top": 0.74, "left": 0.52}
        ]},
        5: { "bg": "terzi_final.png", "items": [
            {"img": "thread_spool.png", "top": 0.58, "left": 0.66},
            {"img": "torn_fabric.png", "top": 0.64, "left": 0.28},
            {"img": "hidden_pocket.png", "top": 0.42, "left": 0.48}
        ]}
    }
    
    for b_id, b_data in buildings.items():
        bg_path = os.path.join(base_dir, b_data["bg"])
        if not os.path.exists(bg_path):
            continue
            
        bg = Image.open(bg_path).convert("RGBA")
        width, height = bg.size
        
        for item in b_data["items"]:
            item_path = os.path.join(base_dir, item["img"])
            if not os.path.exists(item_path):
                continue
                
            item_img = Image.open(item_path).convert("RGB")
            
            # The CSS scales images to clamp(60px, 5.5vw, 100px). 
            # We'll use 8.5% of the background width to approximate.
            target_size = int(width * 0.085)
            item_img = item_img.resize((target_size, target_size), Image.Resampling.LANCZOS)
            
            x = int(width * item["left"])
            y = int(height * item["top"])
            
            # Prevent going out of bounds
            if x + target_size > width: x = width - target_size
            if y + target_size > height: y = height - target_size
            
            bg = screen_paste(bg, item_img, (x, y))
            
        bg.save(bg_path)
        print(f"Processed {b_data['bg']}")

def fix_wide_images():
    base_dir = "C:/Users/solma/OneDrive/Desktop/Dedektiflik/wwwroot/images"
    fixes = [
        {"img": "terzi_wide.png", "crop_percent": 0.15}, # Remove right 15%
        {"img": "muhtarlik_wide.png", "crop_percent": 0.10} # Remove right 10%
    ]
    
    for fix in fixes:
        img_path = os.path.join(base_dir, fix["img"])
        if not os.path.exists(img_path):
            continue
            
        img = Image.open(img_path)
        w, h = img.size
        
        # Crop right side
        new_w = int(w * (1 - fix["crop_percent"]))
        cropped = img.crop((0, 0, new_w, h))
        
        # Resize back to original
        final_img = cropped.resize((w, h), Image.Resampling.LANCZOS)
        final_img.save(img_path)
        print(f"Fixed {fix['img']}")

if __name__ == "__main__":
    composite_evidence()
    fix_wide_images()
