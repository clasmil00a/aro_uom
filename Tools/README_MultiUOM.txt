Aronium Multi-UOM & Price Manager
=================================

STATUS SAAT INI
---------------
Dukungan multi-satuan sudah terintegrasi ke Aronium POS melalui modul resmi:

  C:\Program Files\Aronium\Modules\Aronium.Pos.MultiUom\Aronium.Pos.MultiUom.dll

Modul ini:
- menampilkan pemilih satuan ketika produk ditambahkan ke kasir;
- menerapkan harga per satuan dan harga bertingkat;
- mengonversi kuantitas ke satuan dasar untuk perhitungan total dan stok;
- menyimpan detail UOM pada PosOrderItem dan DocumentItem;
- menyediakan tab "Satuan & Harga" pada pengelolaan produk.

Versi paket: 1.0.2

INSTALLER OTOMATIS
------------------
Di PC lain dengan Aronium POS 1.51.1.0, jalankan:

  C:\Program Files\Aronium\Tools\Install-Aronium-UOM.exe

Installer meminta persetujuan administrator, mengecek versi Aronium dan .NET
Framework, membackup database, memasang modul serta migration, dan memverifikasi
checksum DLL. Salin hanya file EXE tersebut ke PC tujuan.

CARA MENGELOLA DATA PRODUK
---------------------------
Pengelolaan utama dilakukan langsung di Aronium:

1. Buka Management > Products.
2. Pilih produk lalu Edit product.
3. Buka tab "Satuan & Harga".
4. Pada tabel "Satuan produk", klik "Tambah baris" lalu edit sel:
   - Satuan: ketik nama satuan; satuan baru otomatis dibuat di kamus.
   - Faktor: jumlah satuan dasar untuk 1 satuan ini.
   - Harga: harga jual per satuan.
   - Default: satu satuan yang otomatis dipilih di kasir.
   - Aktif: mengaktifkan/menonaktifkan satuan.
5. Tepat satu satuan harus memiliki Faktor 1; satuan itu otomatis menjadi dasar.
   Satuan nonaktif tetap terlihat di editor agar dapat diaktifkan kembali, tetapi
   tidak muncul di pemilih kasir.
6. Pada tabel "Harga bertingkat", klik "Tambah tier" lalu isi Satuan,
   Min. Qty, dan Harga/unit.
7. Klik tombol **Save** utama Aronium; tab Multi-UOM ikut tersimpan pada proses yang sama.

GUI Python tetap tersedia sebagai alat standalone/fallback:
  "Aronium Multi-UOM Manager.bat"

Tab produk Aronium
------------------
Editor native WPF "Satuan & Harga" sekarang memakai tabel yang langsung editable.
Kolomnya: Satuan, Faktor, Harga, Default, dan Aktif. Kolom Dasar tidak lagi
ditampilkan karena satuan dasar ditentukan otomatis dari Faktor = 1.
Harga memakai format mata uang aplikasi (contoh: 19.000 menjadi 19,000).
Mouse wheel dapat menggulir halaman meskipun pointer berada di dalam tabel.
Baris terpilih menggunakan biru muda, sedangkan baris lain tetap hitam;
warna tulisan tetap memakai foreground asli tema Aronium. Klik area kosong melepas
pilihan baris dan mengembalikan baris ke warna normal.

PILIH SATUAN DI KASIR
---------------------
- Dialog satuan langsung membuka baris pertama.
- Panah atas/bawah memilih satuan tanpa klik.
- Mengetik angka langsung memperbarui Kuantitas tanpa harus klik fieldnya.
- Enter mengonfirmasi pilihan seperti tombol OK.
- Esc menutup dialog seperti tombol Batal.
- Kolom Simbol dihapus; kolom Harga / satuan dan baris selection dibuat lebih besar.

DATA HASIL TRANSAKSI
---------------------
Kolom native Aronium tetap menyimpan nilai dalam satuan dasar agar perhitungan
stok, pajak, total, laporan, dan receipt vendor tetap konsisten.

Detail yang dipilih kasir disimpan pada tabel audit:
- PosOrderItemMultiUom
- DocumentItemMultiUom

Setiap baris menyimpan:
- ProductId, ProductUnitId, UnitId;
- ConversionFactor;
- SelectedQuantity dan SelectedUnitPrice;
- BaseQuantity dan BaseUnitPrice;
- AppliedTierId, AppliedTierMinQuantity, AppliedTierPrice.

Skema inti
----------
- Unit
- ProductUnit
- ProductPriceTier
- ProductUnitPriceView
- PosOrderItemMultiUom
- DocumentItemMultiUom

CATATAN PENTING
---------------
- Backup database sebelum perubahan besar ada di:
  %LOCALAPPDATA%\Aronium\Data\pos.db.bak_*
- Fil migrasi modul:
  C:\Program Files\Aronium\Migrations\Lite\Aronium.Pos.MultiUom\1_0__Add_MultiUom_Transaction_Details.sql
- Build ulang modul:
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Program Files\Aronium\Modules\Aronium.Pos.MultiUom\build.ps1"
- Restart Aronium POS setelah mengganti DLL modul.
