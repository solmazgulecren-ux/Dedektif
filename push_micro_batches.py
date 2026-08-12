import subprocess
import os
import time

def run_cmd(cmd):
    print(f"Executing: {cmd}")
    res = subprocess.run(cmd, shell=True, capture_output=True, text=True)
    if res.returncode != 0:
        print(f"Error: {res.stderr}")
    else:
        print(f"Output: {res.stdout}")
    return res.returncode == 0

# Get untracked and modified files
status_output = subprocess.check_output(["git", "status", "--porcelain"], text=True)
lines = [l.strip() for l in status_output.splitlines() if l.strip()]

files_to_push = []
for l in lines:
    parts = l.split(maxsplit=1)
    if len(parts) == 2:
        files_to_push.append(parts[1])

print(f"Total files to process: {len(files_to_push)}")

# Process in chunks of 2 files
chunk_size = 2
for i in range(0, len(files_to_push), chunk_size):
    chunk = files_to_push[i:i+chunk_size]
    print(f"\n--- Processing Chunk {i//chunk_size + 1}: {chunk} ---")
    
    # Stage files
    for f in chunk:
        run_cmd(f'git add "{f}"')
    
    # Commit
    msg_title = "Adli İnceleme Görselleri ve Vaka Verileri Güncellemesi"
    msg_body = "3D delil masası için yüksek çözünürlüklü yönsel nesne kaplamaları ve vaka ilerleme kayıtları."
    commit_ok = run_cmd(f'git commit -m "{msg_title}" -m "{msg_body}"')
    
    if commit_ok:
        # Push
        push_ok = run_cmd("git push origin main")
        if not push_ok:
            print("Push failed, retrying after prune...")
            run_cmd("git gc --prune=now")
            run_cmd("git push origin main")
    time.sleep(1)

print("\nAll micro-batches completed successfully!")
