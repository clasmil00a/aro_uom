# Installer Module UOM + ReceiptPrinter Patch

Installer ini menghasilkan satu EXE mandiri yang:

1. Memasang `Aronium.Pos.MultiUom`, file bahasa, migration, dan README.
2. Memasang DLL `Aronium.Pos.ReceiptPrinter` yang telah dipatch untuk
   menambahkan toggle `PrintPaymentTypes` pada receipt.
3. Membuat backup database dan mempertahankan backup DLL ReceiptPrinter asli.
4. Memulihkan file yang berubah jika instalasi gagal di tengah proses.

## Build

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Program Files\Aronium\Modules\Aronium.Pos.MultiUom\build-installer.ps1"
```

## Payload ReceiptPrinter

- Payload: `Payloads/Aronium.Pos.ReceiptPrinter.patched.payload`
- SHA-256 payload patched terbaru:
  `EE06C72209FCF759695321E31026CA75C5E7804AA2ACEB1CEFF5C4D4BFF59E58`
- SHA-256 patch sebelumnya yang dapat di-upgrade:
  `2AC1C0458802AFB297FADDC156CFB92FCAADFF7AB033F592222C3DF9F22DDFF9`
- SHA-256 build asli yang didukung:
  `74556BE1B965D1679619FD060D29F5ACD40C24B66935C77A1C223483971621A0`

Build dengan hash lain ditolak sebelum installer mengubah file.

## Perilaku idempotent

- Module UOM dengan payload yang sama tidak ditulis ulang.
- ReceiptPrinter versi terbaru yang sudah dipatch tidak ditulis ulang.
- ReceiptPrinter versi patch sebelumnya di-upgrade ke layout separator terbaru.
- Nilai `PrintPaymentTypes` yang sudah ada dipertahankan.
- Jika belum ada, nilai default adalah `false`.
- Separator `=` tetap berada di atas dan bawah `TOTAL`, baik ketika tipe
  pembayaran ditampilkan maupun disembunyikan.

## Restore

```powershell
Install-Aronium-UOM.exe --restore-receipt-patch --path "C:\Program Files\Aronium"
```

Restore mengembalikan DLL ReceiptPrinter asli dan menghapus setting yang
ditambahkan patch, tetapi tidak menghapus Module UOM.
