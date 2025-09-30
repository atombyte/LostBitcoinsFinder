# LostBitcoinFinder (For educational purposes only)

**Find the private keys of lost Bitcoin — *for educational purposes only***  
> This repository contains a Windows command-line executable for a brute-force-style Bitcoin key finder. It is intended for educational, testing and research purposes **only**, and should only be used on wallets/keys that you personally control. **Do not** run this tool against addresses you do not own or against any third-party wallet. Misuse may be illegal.

---

## What this program does
`LostBitcoinFinder` searching keyspaces by trying many key/address combinations. It is intended to demonstrate multi-core brute-force behaviour, logging, progress visualization, and QR-code generation — **for educational purposes only**.

When a match is found:
- A QR code image (screenshot) is generated showing the matched private key and displayed.
- The private key is written to `found.txt` (screenshot of file write behavior).

> **Important:** Under no circumstances should you use it to target real wallets you do not control.

---

## Safe usage notes
- Use this project for **education, testing, and defensive research only**.

---

## Build / Run (example)
The executable is `LostBitcoinFinder.exe` in these examples.

### Normal mode
`LostBitcoinFinder.exe` — uses **all CPU cores**.  
During normal operation the program prints the character `°` each time **10,000,000** combinations have been tried (this is the normal progress marker).

### Parameters
- `LostBitcoinFinder.exe 16`  
  Use 16 CPU cores for the search (if available).

- `LostBitcoinFinder.exe v`  
  Visualize the process: prints private keys currently being tested (visualization mode).

- `LostBitcoinFinder.exe 32 v`  
  Use 32 cores and show visualization output.

> You may pass either the numeric core count, the `v` flag, or both (order-insensitive).

---

## Output behavior
- During normal operation the program prints progress markers (`°`) every **10,000,000** combinations tried.
- In visualization mode (`v`) the program prints private keys/addresses it is testing.
- **On a "find":**
  - A QR code image (screenshot) is created and displayed.
  - The found private key is appended to `found.txt`.
  - The console also prints the private key beneath the QR preview.


---

## Example output
