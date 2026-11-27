# Log Pengembangan Aronium Multi-UOM

_Status terakhir: 2026-11-27_

## Tujuan

Menambahkan dukungan multi-satuan (UOM), konversi ke satuan stok, harga per satuan, dan harga bertingkat ke Aronium POS tanpa mengubah model data inti secara destruktif.

## Status saat checkpoint

### Sudah selesai

- Database aktif `C:\Users\MRX\AppData\Local\Aronium\Data\pos.db` sudah memiliki:
  - `Unit`
  - `ProductUnit`
  - `ProductPriceTier`
  - `ProductUnitPriceView`
- Migrasi `C:\Program Files\Aronium\Migrations\Lite\75_0__Add_MultiUom.sql` sudah pernah dijalankan.
- GUI pengelola UOM tersedia di `C:\Program Files\Aronium\Tools\aronium_multiuom_manager.py`.
- Kontrak extension penting sudah terpetakan:
  - `IAroniumModule`, `LocalizedModule.Initialize`
  - `IPosActionPlugin`, `IFormExtension<T>`, `IViewPlugin<T>`
  - `IDatabaseService`
  - `IOrderItemPreprocessor.Process(Order, OrderItem)`
  - `IProductPlugin`
  - `IDocumentItemExtraDataHandler.Save(Dictionary<DocumentItem,object>)`
- Contoh resmi plugin produk berbasis WPF sudah ditemukan di `Aronium.Pos.StockControl.dll`.

### Kemajuan implementasi 2026-11-24

- Modul produksi `Aronium.Pos.MultiUom` sudah dibuat dan berhasil di-build.
- Modul resmi berhasil dimuat oleh Aronium 1.51.1.0 (terbukti di log aplikasi).
- Picker satuan POS dan input kuantitas sudah tersedia melalui `IOrderItemPreprocessor`.
- Harga satuan dan tier sudah diterapkan; kolom native menyimpan kuantitas/harga satuan dasar agar seluruh logika stok vendor tetap benar.
- Tabel audit `PosOrderItemMultiUom` dan `DocumentItemMultiUom` sudah menyimpan ProductUnitId, UnitId, ConversionFactor, kuantitas/harga terpilih, serta detail tier.
- GUITK sekarang mengikat tier ke `ProductUnitId` dan dapat dibuka dari tab produk Aronium.
- Migrasi modul: `Migrations\Lite\Aronium.Pos.MultiUom\1_0__Add_MultiUom_Transaction_Details.sql`.

### Editor native dan keyboard (2026-11-25)

- Tab `Satuan & Harga` memakai DataGrid yang langsung editable.
- Tabel satuan: Satuan, Faktor, Harga, Default, Aktif. Kolom Dasar dihapus; Faktor 1 otomatis menjadi satuan dasar.
- Tabel tier: Satuan, Min. Qty, Harga/unit. Baris baru dapat ditambahkan dan diedit langsung.
- Tombol `Simpan perubahan` dihapus; penyimpanan memakai tombol `Save` utama Aronium melalui `IProductPlugin.Save()`.
- Kolom Default dilebarkan, baris dinaikkan, teks dipusatkan, dan checkbox memakai area klik 28x28.
- Mouse wheel di dalam tabel meneruskan scroll ke halaman tab saat tabel mencapai batas.
- UI Automation smoke test: edit satuan `Satuan Uji` faktor 1 harga 123, checkbox Default, serta tier min 2 harga 100 berhasil disimpan.
- Dialog kasir terverifikasi: kolom Simbol dihapus, Harga/satuan tampil penuh, baris lebih besar, mengetik angka langsung mengubah Kuantitas, panah bawah memilih baris berikutnya, dan Enter mengonfirmasi.
- Produk/satuan uji kemudian dihapus; data pengguna yang sudah ada tidak dihapus.

### Hasil smoke test end-to-end

- Modul tercatat `INFO Aronium.Pos.MultiUom.MultiUomModule - Initializing Aronium.Pos.MultiUom module` pada log Aronium 1.51.1.0.
- Picker menampilkan `Kilogram (kg, faktor 1)` dan `Box (box, faktor 6)`.
- Tier `Box`, min qty 2, harga 55.000 menghasilkan total 110.000.
- Baris native menyimpan `Quantity=12` dan `Price=9166.6667` (satuan dasar).
- `PosOrderItemMultiUom` menyimpan qty 2, harga 55.000, faktor 6, base qty 12, dan detail tier.
- Setelah pembayaran, `DocumentItemMultiUom` menyimpan detail yang sama.
- Stok berkurang `12` satuan dasar, sesuai faktor konversi.
- Produk yang sama dengan satuan kg dan box tetap menjadi dua baris (tidak digabung).
- Seluruh produk/order/document/stok uji kemudian dihapus; DB kembali ke jumlah 0.
- REA evidence modul final: `ev_993a2faf6bcdb099493ba438c054434083c6325267458cd8256e19ebf4700ede`.

### Format harga dan tampilan grid (2026-11-25)

- Harga pada tabel produk, tier, dan dialog satuan memakai formatter mata uang aplikasi; `19000` tampil sebagai `19,000`.
- Kolom `Default` dan `Aktif` dipLebarkan tanpa memotong header.
- Checkbox memakai template glyph khusus yang benar-benar terpusat, dengan area klik 28x28.
- Warna foreground teks tidak lagi dipaksa hitam; teks memakai warna tema Aronium.
- Gaya baris: baris terpilih biru muda `(64,132,180)`, baris tidak terpilih hitam `(37,37,37)`; foreground teks tetap putih seperti tema Aronium.
- `GridLinesVisibility=None`, border sel `0`, dan border fokus dihapus sehingga tidak ada garis ganda.
- Klik area kosong melepas selection; UI Automation terverifikasi `Before=True`, `After=False`, aplikasi tetap hidup.
- Kolom `Aktif` menjadi terlihat penuh dan tabel dapat digulir horizontal.
- UI Automation/screenshot mengonfirmasi harga `19,000`, posisi checkbox terpusat, dan perilaku selection setelah klik area kosong.

### Installer Module UOM (2026-11-25)

- Installer mandiri tersedia di `C:\Program Files\Aronium\Tools\Install-Aronium-UOM.exe`.
- Payload DLL, language files, migration, dan README di-embed sebagai resource.
- Installer memeriksa admin, .NET Framework release >= 461808, Aronium 1.51.1.0, serta aplikasi yang sedang berjalan.
- Database dibackup sebelum instalasi; checksum DLL terverifikasi dan log installer disimpan di `%LOCALAPPDATA%\Aronium\Log\ModuleUomInstaller.log`.
- Installer berhasil dijalankan pada PC ini dan module tetap berhasil dimuat setelah instalasi.
- Paket ZIP tersedia di `Tools\Install-Aronium-UOM-1.51.1.0.zip`.

### Installer gabungan UOM + ReceiptPrinter Patch (2026-11-27)

- Installer `Install-Aronium-UOM.exe` versi `1.0.4` sekarang memasang Module UOM dan patch ReceiptPrinter dalam satu proses.
- Payload patch ReceiptPrinter baked-in sebagai resource; installer tidak bergantung pada file patcher terpisah.
- Hash ReceiptPrinter asli yang diizinkan: `74556BE1B965D1679619FD060D29F5ACD40C24B66935C77A1C223483971621A0`.
- Hash ReceiptPrinter patched: `2AC1C0458802AFB297FADDC156CFB92FCAADFF7AB033F592222C3DF9F22DDFF9`.
- Build di luar kedua hash ditolak sebelum file aplikasi diubah.
- Installer mencadangkan database, mempertahankan backup DLL ReceiptPrinter asli, memverifikasi checksum, dan mengembalikan snapshot file bila instalasi gagal di tengah proses.
- Perilaku idempotent: payload UOM yang sama tidak ditulis ulang, ReceiptPrinter yang sudah patched tidak diganti, dan nilai `PrintPaymentTypes` yang sudah ada dipertahankan. Jika belum ada, default `false`.
- Mode restore: `--restore-receipt-patch --path "C:\Program Files\Aronium"` mengembalikan DLL asli dan menghapus setting patch tanpa menghapus Module UOM.
- Staging test lulus untuk fresh install, instalasi ulang, restore, penolakan hash unsupported, dan rollback setelah kegagalan konfigurasi.
- Pada instalasi aktif, UOM dan ReceiptPrinter sudah terpasang; setting `PrintPaymentTypes=false` dipertahankan.
- Smoke test menemukan bahwa payload `.dll` di dalam tree `Modules` menyebabkan Aronium memuat ReceiptPrinter dua kali dan menghasilkan `Duplicated module name`. Akar masalah payload source ikut terindeks bukan logika patch. Payload dipindahkan menjadi `.payload` sehingga tidak dipindai sebagai assembly.
- Setelah perbaikan, Aronium POS 1.51.1.0 start tanpa error baru dan log mengonfirmasi `Initializing Aronium.Pos.MultiUom module` serta inisialisasi `Aronium.Pos.ReceiptPrinter.DecimalHandler`.
- Installer SHA-256: `0E34F65D83F36FA14AE4136A9FC2F1E262534FD2C9EAEEEA9157A82CBBE57D14`.
- Paket ZIP SHA-256: `208816F511DBB225BC6982E47FCB5757287434A8C01B2A1CEA028CAFD7641111`.
- Source installer, payload, build script, dan tool patcher standalone disertakan di source repository.

### Savepoint 1 — dibuat ulang (2026-11-25)

- Snapshot sebelumnya dibatalkan dan diganti dengan kondisi terbaru setelah perbaikan hapus lalu buat ulang satuan.
- Snapshot lengkap tersimpan di `C:\Users\MRX\Desktop\Aronium-UOM-Savepoints\savepoint-1` dan `savepoint-1.zip`.
- Snapshot mencakup source/build modul v1.0.3, installer EXE/ZIP, migration, tools, database SQLite konsisten terbaru, manifest SHA-256, dan script restore.
- Commit GitHub terbaru dicatat pada manifest `savepoint-1.json`; checksum ZIP final dicatat pada README indeks savepoint.
- Branch: `savepoint/1`; tag: `savepoint-1`; release: `v1.0.3`.
- Database snapshot bersifat lokal dan tidak diunggah ke repository publik.
- Restore dilakukan melalui `RESTORE-SAVEPOINT-1.ps1 -RestoreDatabase`.

### Perbaikan baris satuan nonaktif / UNIQUE constraint (2026-11-25)

- Akar masalah: editor hanya memuat `ProductUnit` aktif, sedangkan baris nonaktif tetap memenuhi unique constraint `ProductId + UnitId`. Penambahan satuan yang sudah pernah nonaktif karena itu mencoba `INSERT` dan gagal dengan `UNIQUE constraint failed`.
- Editor kini memakai query terpisah yang memuat seluruh baris satuan produk, termasuk nonaktif, sehingga baris lama dapat diaktifkan kembali.
- Query khusus kasir tetap hanya mengambil satuan aktif; satuan nonaktif tidak lagi muncul pada dialog picker.
- Penyimpanan `ProductUnit` sekarang memakai upsert berdasarkan pasangan `ProductId + UnitId`, mendeteksi duplikasi secara eksplisit, serta menangani baris lama yang sudah dihapus.
- Verifikasi pada salinan database aktif untuk produk `Ladaku` (ProductId 908): 2 baris nonaktif terbaca di editor, `SaveAll` plugin berhasil tanpa exception, jumlah baris tetap 2, dan 0 satuan nonaktif tampil di picker kasir.
- Modul berhasil dimuat ulang oleh Aronium POS 1.51.1.0 tanpa error baru.

### Perbaikan hapus lalu buat ulang satuan (2026-11-25)

- Gejala: setelah semua `ProductUnit` produk dihapus dan berhasil disimpan, penambahan satuan baru gagal dengan `Expected a non-null scalar value of type System.Int32, but the query returned no rows`.
- Akar masalah: `FindProductUnitId` memakai `ExecuteScalar<int>` atas query `SELECT Id ...`. Implementasi `Aronium.Data.SQLite` melempar exception bila hasil query tidak memiliki baris, sehingga kondisi "belum ada ProductUnit" tidak dapat direpresentasikan sebagai `0`.
- Perbaikan: pencarian ID memakai `COALESCE((SELECT Id ... LIMIT 1),0)`, sehingga selalu menghasilkan satu baris scalar dan kondisi tidak ditemukan menghasilkan `0` dengan aman.
- Regression test memakai salinan database aktif dan semantik `ExecuteScalar` yang sama dengan vendor: tambah satuan pertama berhasil, hapus satuan tersebut, lalu buat satuan lain juga berhasil. Jumlah baris akhir tetap satu dan tidak ada exception.
- Versi distribusi berikutnya: `v1.0.3`.

### Belum selesai

- Receipt/print native masih menampilkan nilai satuan dasar; detail satuan terpilih tersedia di tabel audit.
- Void perlu diuji ulang setelah perubahan marker internal anti-penggabungan.

## Keputusan desain

- `ConversionFactor` = jumlah unit dasar untuk 1 unit alternatif; satuan dasar bernilai `1`.
- Modul harus memakai extension point/plugin resmi Aronium. IL patch hanya fallback jika tidak ada extension point yang memadai.
- Nama modul: `Aronium.Pos.MultiUom`.
- Data tier harus terikat ke `ProductUnitId`, bukan hanya `ProductId`.
- Baris transaksi harus menyimpan UOM terpilih, faktor, harga satuan, dan (jika memungkinkan) tier terpilih agar audit/reprint stabil.

## Schema aktual yang terverifikasi

REA SQLite evidence: `ev_ed3cd063a54dedfe71dce518354c54700a1002f386d741d4010fe059ff175f3e`

- `Product.MeasurementUnit TEXT` — satuan tunggal bawaan vendor.
- `PosOrderItem`: `Id`, `PosOrderId`, `ProductId`, `Quantity`, `Price`, `Discount`, `Comment`, dan kolom retail lain; belum ada UOM.
- `DocumentItem`: `Id`, `DocumentId`, `ProductId`, `Quantity`, `ExpectedQuantity`, `PriceBeforeTax`, `Price`, `Discount`, `Total`, dan kolom pajak/diskon; belum ada UOM.
- `Stock`: `Id`, `ProductId`, `WarehouseId`, `Quantity`; kuantitas disimpan dalam satuan stok produk.
- `Migration` utama kembali ke versi vendor `74.0`; baris manual `75_0__Add_MultiUom.sql` dihapus dari tabel `Migration` karena validator Aronium 1.51.1 menolak versi database `75.0` (schema tabel tetap dipertahankan).
- File migrasi manual `75_0__Add_MultiUom.sql` dipindahkan ke `Tools\75_0__Add_MultiUom.sql.disabled` agar tidak ter-scan lagi; pembuatan schema inti kini dilakukan idempotent oleh modul.
- Modul multi-UOM memakai migrasi modul versi `1.0` dan tercatat pada DB aktif.
- Jumlah data awal: 6 `Unit`, 0 `ProductUnit`, 0 `ProductPriceTier`.

## Evidence REA sebelumnya yang masih relevan

- SDK/contracts: `ev_d6c8400168fa4074ce77b7021706073950c7cf9449a1b9dc8810c205e458e5c1`
- Core contracts/models: `ev_29fe32cf0b2b7196f53ccb834c8a3daf78d905edef9def55217f09445f7a3a7a`
- Modularity: `ev_cdb7616229ee77acc1b06169b15480cec0ebc70511ca1ca20272e83e315f9aad`
- StockControl: `ev_c3704f4c49c8c74c149570bb77d778ee8f9130094056ece7bfd9701cbf67de4a`
- MovingAveragePrice: `ev_cf94d65c1ae306b9210a0c1d3800f3d37c18c5f440bb63e11658e316ea6d84a4`
- Common assembly retained overview: `ev_dd46ca5e3872aaef58c9a9e5550d2c1cd67ca0c832923cee99c9ead241afe39e`

## Temuan awal

- `IPosActionPlugin` hanya menyediakan metadata (`Id`, `Name`, `SecurityKey`, `IsVisible`, `Icon`, `HasNotifications`), bukan method `Execute`; trigger belum terpetakan.
- `ProductStockControlViewPlugin` adalah `IProductPlugin` + `IVisualPlugin` + `IPlugin` + `IPluggable`; pola ini cocok untuk tab pengelolaan produk.
- `OrderItem` memiliki `Quantity` dan `Price`; `DocumentItem` memiliki `UOMQuantity`, `UOMPrice`, dan `UOMPriceBeforeTax` pada model in-memory, tetapi schema SQLite tabel transaksi belum punya kolom persistence yang setara.
- `IOrderItemPreprocessor.Process(Order, OrderItem)` adalah kandidat hook untuk menetapkan harga setelah item dipilih, tetapi detail urutan pemanggilannya harus diverifikasi.

## Fase implementasi

### Fase 1 — Pemetaan alur (selesai)

- [x] Alur pilih produk → tambah item → harga → order.
- [x] Pemanggilan `IOrderItemPreprocessor`.
- [x] Pola dialog/WPF dari modul resmi.

### Fase 2 — Persistence (selesai)

- [x] Skema detail transaksi.
- [x] Migrasi modul UOM/tier.
- [x] Strategi sidecar tanpa mengubah model core.

### Fase 3 — Implementasi modul

- [x] Source project dan build script.
- [x] `MultiUomModule` dan registrasi plugin.
- [x] Repository/price resolver UOM + tier.
- [x] Picker satuan POS.
- [x] Persistence detail baris.
- [x] Penyesuaian stok melalui kuantitas satuan dasar.
- [ ] Receipt/print detail satuan alternatif.

### Fase 4 — Verifikasi

- [ ] Build dengan compiler .NET Framework yang tersedia.
- [ ] Smoke test load module.
- [ ] Uji transaksi: base unit, unit alternatif, tier harga, void, stok, receipt.
- [ ] Update `README_MultiUOM.txt`.

## Catatan operasional

- Auto compaction global dinonaktifkan pada `C:\Users\MRX\.config\opencode\opencode.json`:
  - `compaction.auto = false`
  - `compaction.background = false`
- Backup DB sebelum migrasi/ubah data produksi.
- Jangan menganggap output REA terpotong sebagai analisis lengkap; gunakan Evidence ID/page聚焦.
