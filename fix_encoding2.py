import shutil

file_path = 'wwwroot/index.html'
backup_path = 'wwwroot/index.html.bak'

shutil.copy(backup_path, file_path)

with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

# Define the correct mapping from the corrupted string to the correct Turkish char
replacements = {
    "A\xc3\x83\xe2\x80\xa1": "AÇ", # AÃ‡ -> AÇ
    "\xc3\x83\xe2\x80\xa1": "Ç", # Ã‡ -> Ç
    "\xc3\x83\xc2\xa7": "ç", # Ã§ -> ç
    "\xc3\x84\xc5\xb8": "ğ", # ÄŸ -> ğ
    "\xc3\x84\xc5\xbe": "Ğ", # Äž -> Ğ
    "\xc3\x84\xc2\xb1": "ı", # Ä± -> ı
    "\xc3\x84\xc2\xb0": "İ", # Ä° -> İ
    "\xc3\x83\xc2\xb6": "ö", # Ã¶ -> ö
    "\xc3\x83\xe2\x80\x93": "Ö", # Ã– -> Ö
    "\xc3\x85\xc5\xb8": "ş", # ÅŸ -> ş
    "\xc3\x85\xc2\x9f": "ş", # variant for ş
    "\xc3\x85\xc5\xbe": "Ş", # Åž -> Ş
    "\xc3\x85\xc2\x9e": "Ş", # variant for Ş with U+009E
    "\xc3\x83\xc2\xbc": "ü", # Ã¼ -> ü
    "\xc3\x83\xc5\x93": "Ü", # Ãœ -> Ü
    "g\xc3\x85\xb8": "gş",
    "\xc4\x9f\xc5\xb8\xe2\x80\x9d\x00": "🔍", # Maybe emoji?
    "ğŸ” ": "🔍 ", # emoji
    "â€”": "—" # em-dash
}

for bad, good in replacements.items():
    text = text.replace(bad, good)

# Also let's try a byte-level replace just to be absolutely sure.
# Wait, the above is using python strings, where `\xc3\x83` means U+00C3 in the string, which matches the text!

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(text)

print("Replaced characters successfully!")
