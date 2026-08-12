import sys
import shutil

file_path = 'wwwroot/index.html'

with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

replacements = {
    "Ã‡": "Ç",
    "Ã§": "ç",
    "ÄŸ": "ğ",
    "Äž": "Ğ",
    "Ä±": "ı",
    "Ä°": "İ",
    "Ã¶": "ö",
    "Ã–": "Ö",
    "ÅŸ": "ş",
    "Åž": "Ş",
    "Ã¼": "ü",
    "Ãœ": "Ü",
    "ğŸ” ": "🔍 ", # Also fixing the emoji
    "â€”": "—" # fixing em-dash
}

for bad, good in replacements.items():
    text = text.replace(bad, good)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(text)

print("Replaced characters successfully!")
