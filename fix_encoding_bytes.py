import shutil

file_path = 'wwwroot/index.html'
backup_path = 'wwwroot/index.html.bak'

shutil.copy(backup_path, file_path)

with open(file_path, 'rb') as f:
    content = f.read()

replacements = {
    b'\xc3\x83\xe2\x80\xa1': b'\xc3\x87', # Ç
    b'\xc3\x83\xc2\xa7': b'\xc3\xa7', # ç
    b'\xc3\x84\xc5\xb8': b'\xc4\x9f', # ğ
    b'\xc3\x84\xc5\xbe': b'\xc4\x9e', # Ğ
    b'\xc3\x84\xc2\xb1': b'\xc4\xb1', # ı
    b'\xc3\x84\xc2\xb0': b'\xc4\xb0', # İ
    b'\xc3\x83\xc2\xb6': b'\xc3\xb6', # ö
    b'\xc3\x83\xe2\x80\x93': b'\xc3\x96', # Ö
    b'\xc3\x85\xc5\xb8': b'\xc5\x9f', # ş
    b'\xc3\x85\xc2\x9f': b'\xc5\x9f', # ş (variant)
    b'\xc3\x85\xc5\xbe': b'\xc5\x9e', # Ş
    b'\xc3\x85\xc2\x9e': b'\xc5\x9e', # Ş (variant)
    b'\xc3\x83\xc2\xbc': b'\xc3\xbc', # ü
    b'\xc3\x83\xc5\x93': b'\xc3\x9c', # Ü
    b'\xc3\xa2\xe2\x82\xac\xe2\x80\x9d': b'\xe2\x80\x94', # —
}

for bad, good in replacements.items():
    content = content.replace(bad, good)

# Fix the title line specifically
bad_title = b'<title>\xc4\x9f\xc5\xb8\xe2\x80\x9d\xc2\x8e Dedektif \xe2\x80\x94 Karanl\xc4\xb1k Kasaban\xc4\xb1n S\xc4\xb1rr\xc4\xb1</title>'
bad_title2 = b'<title>\xc4\x9f\xc5\xb8\xe2\x80\x9d  Dedektif'
good_title = b'<title>\xf0\x9f\x94\x8e Dedektif \xe2\x80\x94 Karanl\xc4\xb1k Kasaban\xc4\xb1n S\xc4\xb1rr\xc4\xb1</title>'
# Let's just do a string replacement on the decoded content for the title if needed
with open(file_path, 'wb') as f:
    f.write(content)

with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('ğŸ” ', '🔍 ')

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(text)

print("Replaced characters successfully on byte level!")
