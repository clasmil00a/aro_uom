# Standalone ReceiptPrinter Payment Types Patcher

Tool ini dipertahankan sebagai installer patch terpisah dan sumber payload
ReceiptPrinter. Untuk instalasi normal, gunakan installer gabungan:

```text
Install-Aronium-UOM.exe
```

Installer gabungan memasang Module UOM dan patch ReceiptPrinter dalam satu
proses, mempertahankan setting yang sudah ada, serta menyediakan rollback.

## Fungsi

Menambahkan toggle **Print payment types** pada:

`Settings > Print options > Customize receipt`

Ketika dinonaktifkan, baris `Cash:`, `Debit:`, `QRIS:`, dan tipe pembayaran
lain disembunyikan. `Total`, `Paid amount`, dan change tetap ditampilkan.

## Build

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\build.ps1"
```

## Perintah

```powershell
Aronium-ReceiptPrinter-PaymentTypes-Patcher.exe --path "C:\Program Files\Aronium"
Aronium-ReceiptPrinter-PaymentTypes-Patcher.exe --restore --path "C:\Program Files\Aronium"
```

Build yang diuji: Aronium `1.51.1.0`, hash DLL asli
`74556BE1B965D1679619FD060D29F5ACD40C24B66935C77A1C223483971621A0`,
hash DLL patched
`2AC1C0458802AFB297FADDC156CFB92FCAADFF7AB033F592222C3DF9F22DDFF9`.
