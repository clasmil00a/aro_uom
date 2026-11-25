# Aronium Module UOM

Module UOM (Unit of Measure) untuk Aronium POS 1.51.1.0. Modul ini menambahkan penjualan multi-satuan, konversi stok, harga per satuan, dan harga bertingkat tanpa mengubah model data inti Aronium.

## Fitur

- Pemilih satuan dan kuantitas langsung di kasir.
- Konversi ke satuan dasar untuk total, pajak, dan stok.
- Harga per satuan dan tier harga berdasarkan kuantitas.
- Audit detail UOM pada `PosOrderItemMultiUom` dan `DocumentItemMultiUom`.
- Editor native pada tab **Satuan & Harga** di Management → Products.
- Installer mandiri untuk PC dengan Aronium POS 1.51.1.0.

## Rilis terbaru

Rilis `v1.0.2` memperbaiki penyimpanan satuan produk yang sebelumnya nonaktif. Editor native kini memuat seluruh baris satuan (aktif dan nonaktif), menyimpan dengan aman tanpa memicu unique constraint, dan tetap menyembunyikan satuan nonaktif dari picker kasir.

Unduhan:

- `Install-Aronium-UOM.exe`
- `Install-Aronium-UOM-1.51.1.0.zip`

Rilis tersedia di [GitHub Releases](https://github.com/clasmil00a/aro_uom/releases/tag/v1.0.2).

## Kompatibilitas

- Aronium POS: **1.51.1.0**
- .NET Framework: **4.7.1 atau lebih baru**
- Arsitektur: x64/AnyCPU
- Windows 10/11

## Instalasi paling mudah

1. Unduh `Tools/Install-Aronium-UOM.exe` dari repository ini.
2. Salin ke PC tujuan.
3. Jalankan file tersebut.
4. Setujui prompt administrator.
5. Installer akan memeriksa versi, membuat backup database, memasang modul dan migration, serta memverifikasi checksum DLL.

Log installer:

```text
%LOCALAPPDATA%\Aronium\Log\ModuleUomInstaller.log
```

Backup database:

```text
%LOCALAPPDATA%\Aronium\Data\pos.db.bak_multiuom_install_*
```

## Instalasi manual

Salin struktur berikut ke instalasi Aronium:

```text
Modules\Aronium.Pos.MultiUom\Aronium.Pos.MultiUom.dll
Modules\Aronium.Pos.MultiUom\Lang\id.lang
Modules\Aronium.Pos.MultiUom\Lang\en.lang
Migrations\Lite\Aronium.Pos.MultiUom\1_0__Add_MultiUom_Transaction_Details.sql
```

Tutup Aronium POS sebelum menyalin DLL. Mulai ulang setelah selesai.

## Cara memakai

1. Buka **Management → Products**.
2. Pilih produk dan tekan **Edit product**.
3. Buka tab **Satuan & Harga**.
4. Tambahkan satuan, faktor konversi, harga, status Default/Aktif, dan tier harga.
5. Tekan **Save** utama Aronium.
6. Saat cashier menambahkan produk, pilih satuan pada dialog **Pilih satuan dan harga**.

Dalam dialog kasir:

- `↑` / `↓` memilih satuan.
- Mengetik angka langsung mengisi Kuantitas.
- `Enter` mengonfirmasi.
- `Esc` membatalkan.

## Konvensi faktor

`ConversionFactor` adalah jumlah satuan dasar untuk satu unit alternatif.

Contoh stok dasar kilogram:

| Satuan | Faktor | Harga |
|---|---:|---:|
| 1 Kg | 1 | 19.000 |
| 500 gr | 0,5 | 10.000 |
| 250 gr | 0,25 | 5.000 |

Tepat satu satuan harus memiliki faktor `1`; satuan tersebut menjadi satuan dasar.

## Skema database

- `Unit`
- `ProductUnit`
- `ProductPriceTier`
- `ProductUnitPriceView`
- `PosOrderItemMultiUom`
- `DocumentItemMultiUom`

## Build dari source

Prasyarat:

- Aronium POS 1.51.1.0 terpasang di `C:\Program Files\Aronium`.
- .NET Framework compiler/SDK.

Build module:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\module\build.ps1"
```

Build installer:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\module\build-installer.ps1"
```

Installer membutuhkan payload modul, language files, migration, dan README dari instalasi Aronium lokal.

## Struktur repository

```text
module/       Source, DLL, installer source, language files
Migrations/   SQL migration modul
Tools/        Installer, ZIP, dan standalone manager
docs/         Log pengembangan
```

## Catatan keamanan

- Backup database selalu dilakukan installer sebelum mengubah file.
- Installer menolak Aronium yang sedang berjalan.
- Installer menolak versi Aronium atau .NET Framework yang tidak kompatibel.
- Uninstall modul tidak menghapus data konfigurasi/audit secara otomatis.
