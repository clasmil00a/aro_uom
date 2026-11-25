#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Aronium Multi-UOM & Price Manager
---------------------------------
 GUI mandiri untuk mengelola fitur Multi Unit of Measure (satuan) dan
 variasi harga langsung pada database Aronium (pos.db).

 Aman: hanya menyentuh tabel Unit / ProductUnit / ProductPriceTier
 (membuatnya bila belum ada). Tidak mengubah tabel inti Aronium.

 Aronium POS versi produksi menghitung penjualan dengan satu satuan; tool ini
 menyiapkan & mengelola data multi-satuan + harga per satuan/volume yang siap
 dipakai oleh laporan/SQL, maupun oleh integrasi plugin/API berikutnya.
"""

import os
import sqlite3
import tkinter as tk
from tkinter import ttk, messagebox, simpledialog

APP_DIR = r"C:\Program Files\Aronium"
DEFAULT_DB = os.path.join(os.environ.get("LOCALAPPDATA", ""), "Aronium", "Data", "pos.db")

# Skema idempotent (sama dengan migrasi 75_0__Add_MultiUom.sql)
SCHEMA_SQL = """
CREATE TABLE IF NOT EXISTS Unit (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    Name      TEXT NOT NULL,
    Symbol    TEXT,
    IsEnabled INTEGER (1) NOT NULL DEFAULT (1)
);
CREATE TABLE IF NOT EXISTS ProductUnit (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId        INTEGER NOT NULL
                     CONSTRAINT FK_ProductUnit_Product REFERENCES Product (Id) ON DELETE CASCADE,
    UnitId           INTEGER NOT NULL
                     CONSTRAINT FK_ProductUnit_Unit REFERENCES Unit (Id) ON DELETE CASCADE,
    ConversionFactor NUMERIC NOT NULL DEFAULT (1),
    Price            NUMERIC NOT NULL DEFAULT (0),
    IsBaseUnit       INTEGER (1) NOT NULL DEFAULT (0),
    IsDefault        INTEGER (1) NOT NULL DEFAULT (0),
    IsEnabled        INTEGER (1) NOT NULL DEFAULT (1),
    UNIQUE (ProductId, UnitId)
);
CREATE INDEX IF NOT EXISTS IX_ProductUnit_Product ON ProductUnit (ProductId);
CREATE TABLE IF NOT EXISTS ProductPriceTier (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId     INTEGER NOT NULL
                  CONSTRAINT FK_ProductPriceTier_Product REFERENCES Product (Id) ON DELETE CASCADE,
    ProductUnitId INTEGER
                  CONSTRAINT FK_ProductPriceTier_ProductUnit REFERENCES ProductUnit (Id) ON DELETE CASCADE,
    MinQuantity   NUMERIC NOT NULL DEFAULT (0),
    Price         NUMERIC NOT NULL,
    CurrencyId    INTEGER REFERENCES Currency (Id)
);
CREATE INDEX IF NOT EXISTS IX_ProductPriceTier_Product ON ProductPriceTier (ProductId);
CREATE VIEW IF NOT EXISTS ProductUnitPriceView AS
SELECT P.Id AS ProductId, P.Name AS ProductName, PU.Id AS ProductUnitId,
       U.Name AS UnitName, U.Symbol AS UnitSymbol, PU.ConversionFactor AS ConversionFactor,
       CASE WHEN PU.Price > 0 THEN PU.Price ELSE P.Price END AS Price,
       PU.IsBaseUnit AS IsBaseUnit, PU.IsDefault AS IsDefault
FROM ProductUnit PU
    INNER JOIN Product P ON P.Id = PU.ProductId
    INNER JOIN Unit    U ON U.Id = PU.UnitId;
"""

SEED_UNITS = [("Piece", "pcs"), ("Kilogram", "kg"), ("Gram", "g"),
              ("Litre", "l"), ("Millilitre", "ml"), ("Box", "box")]


class DataAccess:
    """Semua logika DB (mudah diuji terpisah dari GUI)."""

    def __init__(self, db_path):
        self.db_path = db_path
        self.conn = sqlite3.connect(db_path, timeout=15)
        self.conn.row_factory = sqlite3.Row
        self.conn.execute("PRAGMA busy_timeout = 15000;")
        self.conn.execute("PRAGMA foreign_keys = ON;")
        self._ensure_schema()

    def _ensure_schema(self):
        cur = self.conn.cursor()
        cur.executescript(SCHEMA_SQL)
        for name, sym in SEED_UNITS:
            cur.execute("INSERT INTO Unit (Name, Symbol) SELECT ?, ? WHERE NOT EXISTS (SELECT 1 FROM Unit WHERE Symbol = ?)",
                        (name, sym, sym))
        self.conn.commit()

    # ---- produk ----
    def search_products(self, term=""):
        q = ("SELECT Id, Name, Code, MeasurementUnit, Price FROM Product "
             "WHERE IsService = 0 ")
        params = []
        if term:
            q += " AND (Name LIKE ? OR IFNULL(Code,'') LIKE ?) "
            params += [f"%{term}%", f"%{term}%"]
        q += " ORDER BY Name COLLATE NOCASE LIMIT 1000"
        return self.conn.execute(q, params).fetchall()

    def get_product(self, pid):
        return self.conn.execute(
            "SELECT Id, Name, Code, MeasurementUnit, Price FROM Product WHERE Id = ?", (pid,)
        ).fetchone()

    # ---- unit dictionary ----
    def list_units(self):
        return self.conn.execute("SELECT Id, Name, Symbol, IsEnabled FROM Unit ORDER BY Name COLLATE NOCASE").fetchall()

    def add_unit(self, name, symbol):
        cur = self.conn.cursor()
        cur.execute("INSERT INTO Unit (Name, Symbol, IsEnabled) VALUES (?,?,1)", (name, symbol))
        self.conn.commit()
        return cur.lastrowid

    # ---- ProductUnit ----
    def list_product_units(self, pid):
        return self.conn.execute(
            "SELECT pu.Id, pu.UnitId, u.Name AS UnitName, u.Symbol AS UnitSymbol, "
            "pu.ConversionFactor, pu.Price, pu.IsBaseUnit, pu.IsDefault, pu.IsEnabled "
            "FROM ProductUnit pu INNER JOIN Unit u ON u.Id = pu.UnitId "
            "WHERE pu.ProductId = ? ORDER BY pu.IsBaseUnit DESC, u.Name COLLATE NOCASE", (pid,)
        ).fetchall()

    def add_product_unit(self, pid, unit_id, factor, price, is_base, is_default, is_enabled):
        cur = self.conn.cursor()
        cur.execute("SELECT COUNT(*) c FROM ProductUnit WHERE ProductId=?", (pid,))
        if cur.fetchone()["c"] == 0:
            is_base = 1  # satuan pertama otomatis jadi satuan dasar
        if is_base:
            cur.execute("UPDATE ProductUnit SET IsBaseUnit=0 WHERE ProductId=?", (pid,))
        if is_default:
            cur.execute("UPDATE ProductUnit SET IsDefault=0 WHERE ProductId=?", (pid,))
        cur.execute(
            "INSERT INTO ProductUnit (ProductId, UnitId, ConversionFactor, Price, IsBaseUnit, IsDefault, IsEnabled) "
            "VALUES (?,?,?,?,?,?,?)",
            (pid, unit_id, factor, price, is_base, is_default, is_enabled))
        self.conn.commit()
        return cur.lastrowid

    def update_product_unit(self, id_, pid, unit_id, factor, price, is_base, is_default, is_enabled):
        cur = self.conn.cursor()
        if is_base:
            cur.execute("UPDATE ProductUnit SET IsBaseUnit=0 WHERE ProductId=?", (pid,))
        if is_default:
            cur.execute("UPDATE ProductUnit SET IsDefault=0 WHERE ProductId=?", (pid,))
        cur.execute(
            "UPDATE ProductUnit SET UnitId=?, ConversionFactor=?, Price=?, IsBaseUnit=?, IsDefault=?, IsEnabled=? WHERE Id=? AND ProductId=?",
            (unit_id, factor, price, is_base, is_default, is_enabled, id_, pid))
        self.conn.commit()

    def delete_product_unit(self, id_, pid):
        self.conn.execute("DELETE FROM ProductUnit WHERE Id=? AND ProductId=?", (id_, pid))
        self.conn.commit()

    # ---- ProductPriceTier ----
    def list_tiers(self, pid):
        return self.conn.execute(
            "SELECT t.Id, t.ProductUnitId, t.MinQuantity, t.Price, "
            "COALESCE(u.Name, '(semua satuan)') AS UnitName, COALESCE(u.Symbol, '') AS UnitSymbol "
            "FROM ProductPriceTier t "
            "LEFT JOIN ProductUnit pu ON pu.Id=t.ProductUnitId "
            "LEFT JOIN Unit u ON u.Id=pu.UnitId "
            "WHERE t.ProductId=? ORDER BY t.ProductUnitId, t.MinQuantity", (pid,)
        ).fetchall()

    def add_tier(self, pid, product_unit_id, min_qty, price):
        self.conn.execute(
            "INSERT INTO ProductPriceTier (ProductId, ProductUnitId, MinQuantity, Price) VALUES (?,?,?,?)",
            (pid, product_unit_id, min_qty, price))
        self.conn.commit()

    def delete_tier(self, id_, pid):
        self.conn.execute("DELETE FROM ProductPriceTier WHERE Id=? AND ProductId=?", (id_, pid))
        self.conn.commit()

    def close(self):
        try:
            self.conn.close()
        except Exception:
            pass


def _num(entry_val, default=0.0):
    s = (entry_val or "").strip().replace(",", ".")
    if s == "":
        return default
    return float(s)


class ManagerApp:
    def __init__(self, root, db_path):
        self.root = root
        self.db_path = db_path
        self.dba = DataAccess(db_path)
        self.current_pid = None
        self._build_ui()
        self.refresh_products()

    # ---------- UI ----------
    def _build_ui(self):
        self.root.title("Aronium Multi-UOM & Price Manager")
        self.root.geometry("1000x640")

        top = ttk.Frame(self.root, padding=8)
        top.pack(fill="x")
        ttk.Label(top, text=f"Database: {self.db_path}").pack(side="left")
        ttk.Button(top, text="Muat Ulang", command=self.refresh_products).pack(side="right")

        search = ttk.Frame(self.root, padding=(8, 0, 8, 4))
        search.pack(fill="x")
        ttk.Label(search, text="Cari produk:").pack(side="left")
        self.search_var = tk.StringVar()
        e = ttk.Entry(search, textvariable=self.search_var)
        e.pack(side="left", fill="x", expand=True, padx=6)
        e.bind("<Return>", lambda ev: self.refresh_products())
        ttk.Button(search, text="Cari", command=self.refresh_products).pack(side="left")

        body = ttk.Frame(self.root, padding=8)
        body.pack(fill="both", expand=True)

        left = ttk.LabelFrame(body, text="Produk", width=320)
        left.pack(side="left", fill="both", expand=False)
        self.prod_list = tk.Listbox(left, activestyle="dotbox")
        self.prod_list.pack(side="left", fill="both", expand=True)
        sb = ttk.Scrollbar(left, orient="vertical", command=self.prod_list.yview)
        sb.pack(side="right", fill="y")
        self.prod_list.config(yscrollcommand=sb.set)
        self.prod_list.bind("<<ListboxSelect>>", self.on_product_select)
        self._prod_ids = []

        right = ttk.Frame(body)
        right.pack(side="left", fill="both", expand=True, padx=(8, 0))

        self.prod_title = ttk.Label(right, text="(pilih produk)", font=("Segoe UI", 11, "bold"))
        self.prod_title.pack(anchor="w", pady=(0, 6))

        # Units section
        uf = ttk.LabelFrame(right, text="Satuan produk (multi-UOM)")
        uf.pack(fill="both", expand=True)
        cols = ("unit", "factor", "price", "base", "default", "enabled")
        self.unit_tree = ttk.Treeview(uf, columns=cols, show="headings", height=7, selectmode="browse")
        for c, t, w in [("unit", "Satuan", 140), ("factor", "Faktor konversi", 100),
                        ("price", "Harga", 100), ("base", "Dasar", 55),
                        ("default", "Default", 60), ("enabled", "Aktif", 55)]:
            self.unit_tree.heading(c, text=t)
            self.unit_tree.column(c, width=w, anchor="center")
        self.unit_tree.pack(side="left", fill="both", expand=True)
        usb = ttk.Scrollbar(uf, orient="vertical", command=self.unit_tree.yview)
        usb.pack(side="right", fill="y")
        self.unit_tree.config(yscrollcommand=usb.set)
        self.unit_tree.bind("<<TreeviewSelect>>", self.on_unit_select)

        # Unit entry form
        form = ttk.Frame(right)
        form.pack(fill="x", pady=6)
        ttk.Label(form, text="Satuan:").grid(row=0, column=0, sticky="w")
        self.unit_combo = ttk.Combobox(form, width=18, state="readonly")
        self.unit_combo.grid(row=0, column=1, padx=4)
        ttk.Button(form, text="+ Satuan baru", command=self.add_new_unit).grid(row=0, column=2, padx=2)
        ttk.Label(form, text="Faktor:").grid(row=0, column=3, sticky="e")
        self.factor_var = tk.StringVar(value="1")
        ttk.Entry(form, textvariable=self.factor_var, width=8).grid(row=0, column=4, padx=4)
        ttk.Label(form, text="Harga:").grid(row=0, column=5, sticky="e")
        self.price_var = tk.StringVar(value="0")
        ttk.Entry(form, textvariable=self.price_var, width=10).grid(row=0, column=6, padx=4)
        self.base_var = tk.BooleanVar(value=False)
        self.default_var = tk.BooleanVar(value=False)
        self.enabled_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(form, text="Dasar", variable=self.base_var).grid(row=1, column=1, sticky="w")
        ttk.Checkbutton(form, text="Default", variable=self.default_var).grid(row=1, column=2, sticky="w")
        ttk.Checkbutton(form, text="Aktif", variable=self.enabled_var).grid(row=1, column=3, sticky="w")
        btns = ttk.Frame(form)
        btns.grid(row=1, column=4, columnspan=3, sticky="e")
        self.add_btn = ttk.Button(btns, text="Tambah", command=self.save_unit_new)
        self.add_btn.pack(side="left", padx=2)
        self.update_btn = ttk.Button(btns, text="Simpan Perubahan", command=self.save_unit_update, state="disabled")
        self.update_btn.pack(side="left", padx=2)
        self.del_btn = ttk.Button(btns, text="Hapus", command=self.delete_unit, state="disabled")
        self.del_btn.pack(side="left", padx=2)

        # Tiers section
        tf = ttk.LabelFrame(right, text="Variasi harga per kuantitas (harga grosir/volume)")
        tf.pack(fill="both", expand=True, pady=(8, 0))
        self.tier_tree = ttk.Treeview(tf, columns=("unit", "min", "price"), show="headings", height=4, selectmode="browse")
        self.tier_tree.heading("unit", text="Satuan")
        self.tier_tree.heading("min", text="Min. Qty")
        self.tier_tree.heading("price", text="Harga / unit")
        self.tier_tree.column("unit", width=170, anchor="center")
        self.tier_tree.column("min", width=100, anchor="center")
        self.tier_tree.column("price", width=120, anchor="center")
        self.tier_tree.pack(side="left", fill="both", expand=True)
        tsb = ttk.Scrollbar(tf, orient="vertical", command=self.tier_tree.yview)
        tsb.pack(side="right", fill="y")
        self.tier_tree.config(yscrollcommand=tsb.set)

        tform = ttk.Frame(right)
        tform.pack(fill="x", pady=6)
        ttk.Label(tform, text="Satuan:").pack(side="left")
        self.tier_unit_combo = ttk.Combobox(tform, width=16, state="readonly")
        self.tier_unit_combo.pack(side="left", padx=4)
        self._tier_unit_ids = []
        ttk.Label(tform, text="Min Qty:").pack(side="left")
        self.tmin_var = tk.StringVar(value="0")
        ttk.Entry(tform, textvariable=self.tmin_var, width=8).pack(side="left", padx=4)
        ttk.Label(tform, text="Harga/unit:").pack(side="left")
        self.tprice_var = tk.StringVar(value="0")
        ttk.Entry(tform, textvariable=self.tprice_var, width=10).pack(side="left", padx=4)
        ttk.Button(tform, text="Tambah tier", command=self.add_tier).pack(side="left", padx=2)
        ttk.Button(tform, text="Hapus tier", command=self.delete_tier).pack(side="left", padx=2)

        self.status = ttk.Label(self.root, text="Siap.", relief="sunken", anchor="w", padding=4)
        self.status.pack(fill="x", side="bottom")

    def set_status(self, msg):
        self.status.config(text=msg)

    # ---------- events ----------
    def refresh_products(self):
        self.prod_list.delete(0, "end")
        self._prod_ids = []
        for r in self.dba.search_products(self.search_var.get().strip()):
            label = r["Name"] + (f"  [{r['Code']}]" if r["Code"] else "")
            self.prod_list.insert("end", label)
            self._prod_ids.append(r["Id"])
        self.set_status(f"{len(self._prod_ids)} produk dimuat.")

    def select_product(self, product_id):
        for index, pid in enumerate(self._prod_ids):
            if str(pid) == str(product_id):
                self.prod_list.selection_set(index)
                self.prod_list.focus(index)
                self.prod_list.event_generate("<<TreeviewSelect>>")
                self.prod_list.see(index)
                return True
        return False

    def on_product_select(self, ev):
        sel = self.prod_list.curselection()
        if not sel:
            return
        self.current_pid = self._prod_ids[sel[0]]
        p = self.dba.get_product(self.current_pid)
        self.prod_title.config(text=f"{p['Name']}  |  Satuan saat ini: {p['MeasurementUnit'] or '-'}  |  Harga: {p['Price']}")
        self.reload_units()
        self.reload_tiers()
        self.clear_unit_form()

    def _fill_unit_combo(self):
        vals = [f"{u['Name']} ({u['Symbol']})" if u["Symbol"] else u["Name"] for u in self.dba.list_units()]
        self.unit_combo["values"] = vals

    def reload_units(self):
        self._fill_unit_combo()
        self.unit_tree.delete(*self.unit_tree.get_children())
        for r in self.dba.list_product_units(self.current_pid):
            self.unit_tree.insert("", "end", iid=str(r["Id"]), values=(
                f"{r['UnitName']} ({r['UnitSymbol']})" if r["UnitSymbol"] else r["UnitName"],
                r["ConversionFactor"], r["Price"],
                "Yes" if r["IsBaseUnit"] else "", "Yes" if r["IsDefault"] else "",
                "Yes" if r["IsEnabled"] else ""))

    def reload_tiers(self):
        self.tier_tree.delete(*self.tier_tree.get_children())
        self._tier_unit_ids = []
        self.tier_unit_combo["values"] = ()
        units = self.dba.list_product_units(self.current_pid)
        for r in units:
            label = f"{r['UnitName']} ({r['UnitSymbol']})" if r["UnitSymbol"] else r["UnitName"]
            self.tier_unit_combo["values"] = self.tier_unit_combo["values"] + (label,) if self.tier_unit_combo["values"] else (label,)
            self._tier_unit_ids.append(r["Id"])
        if self._tier_unit_ids and not self.tier_unit_combo.get():
            self.tier_unit_combo.current(0)
        for r in self.dba.list_tiers(self.current_pid):
            unit = f"{r['UnitName']} ({r['UnitSymbol']})" if r["UnitSymbol"] else r["UnitName"]
            self.tier_tree.insert("", "end", iid=str(r["Id"]), values=(unit, r["MinQuantity"], r["Price"]))

    def clear_unit_form(self):
        self.unit_combo.set("")
        self.factor_var.set("1")
        self.price_var.set("0")
        self.base_var.set(False)
        self.default_var.set(False)
        self.enabled_var.set(True)
        self.add_btn.config(state="normal")
        self.update_btn.config(state="disabled")
        self.del_btn.config(state="disabled")

    def on_unit_select(self, ev):
        sel = self.unit_tree.selection()
        if not sel:
            return
        uid = int(sel[0])
        row = next((r for r in self.dba.list_product_units(self.current_pid) if r["Id"] == uid), None)
        if not row:
            return
        self.unit_combo.set(f"{row['UnitName']} ({row['UnitSymbol']})" if row["UnitSymbol"] else row["UnitName"])
        self.factor_var.set(str(row["ConversionFactor"]))
        self.price_var.set(str(row["Price"]))
        self.base_var.set(bool(row["IsBaseUnit"]))
        self.default_var.set(bool(row["IsDefault"]))
        self.enabled_var.set(bool(row["IsEnabled"]) if row["IsEnabled"] is not None else True)
        self.add_btn.config(state="disabled")
        self.update_btn.config(state="normal")
        self.del_btn.config(state="normal")
        self._editing_id = uid

    def _selected_unit_id(self):
        txt = self.unit_combo.get()
        for u in self.dba.list_units():
            label = f"{u['Name']} ({u['Symbol']})" if u["Symbol"] else u["Name"]
            if label == txt:
                return u["Id"]
        return None

    def save_unit_new(self):
        if self.current_pid is None:
            messagebox.showinfo("Info", "Pilih produk dulu.")
            return
        uid = self._selected_unit_id()
        if not uid:
            messagebox.showwarning("Validasi", "Pilih satuan dari daftar.")
            return
        try:
            factor = _num(self.factor_var.get(), 1) or 1
            price = _num(self.price_var.get(), 0)
        except ValueError:
            messagebox.showerror("Validasi", "Faktor/Harga harus angka.")
            return
        try:
            self.dba.add_product_unit(self.current_pid, uid, factor, price,
                                      self.base_var.get(), self.default_var.get(), self.enabled_var.get())
        except sqlite3.IntegrityError:
            messagebox.showerror("Validasi", "Satuan ini sudah ada untuk produk tersebut.")
            return
        self.reload_units()
        self.clear_unit_form()
        self.set_status("Satuan ditambahkan.")

    def save_unit_update(self):
        uid = getattr(self, "_editing_id", None)
        if not uid:
            return
        try:
            factor = _num(self.factor_var.get(), 1) or 1
            price = _num(self.price_var.get(), 0)
        except ValueError:
            messagebox.showerror("Validasi", "Faktor/Harga harus angka.")
            return
        self.dba.update_product_unit(uid, self.current_pid, self._selected_unit_id(), factor, price,
                                     self.base_var.get(), self.default_var.get(), self.enabled_var.get())
        self.reload_units()
        self.clear_unit_form()
        self.set_status("Perubahan disimpan.")

    def delete_unit(self):
        uid = getattr(self, "_editing_id", None)
        if not uid:
            return
        if not messagebox.askyesno("Konfirmasi", "Hapus satuan ini dari produk?"):
            return
        self.dba.delete_product_unit(uid, self.current_pid)
        self.reload_units()
        self.clear_unit_form()
        self.set_status("Satuan dihapus.")

    def add_new_unit(self):
        name = simpledialog.askstring("Satuan baru", "Nama satuan (mis. Karung):", parent=self.root)
        if not name:
            return
        sym = simpledialog.askstring("Satuan baru", "Simbol (mis. krg):", parent=self.root)
        self.dba.add_unit(name.strip(), (sym or "").strip())
        self.reload_units()
        self.set_status(f"Satuan '{name}' ditambahkan ke kamus.")

    def add_tier(self):
        if self.current_pid is None:
            messagebox.showinfo("Info", "Pilih produk dulu.")
            return
        try:
            mq = _num(self.tmin_var.get(), 0)
            pr = _num(self.tprice_var.get(), 0)
        except ValueError:
            messagebox.showerror("Validasi", "Min Qty / Harga harus angka.")
            return
        selected = self.tier_unit_combo.current()
        if selected < 0 or selected >= len(self._tier_unit_ids):
            messagebox.showwarning("Validasi", "Pilih satuan untuk tier harga.")
            return
        self.dba.add_tier(self.current_pid, self._tier_unit_ids[selected], mq, pr)
        self.reload_tiers()
        self.set_status("Tier harga ditambahkan.")

    def delete_tier(self):
        sel = self.tier_tree.selection()
        if not sel:
            return
        if not messagebox.askyesno("Konfirmasi", "Hapus tier harga ini?"):
            return
        self.dba.delete_tier(int(sel[0]), self.current_pid)
        self.reload_tiers()
        self.set_status("Tier dihapus.")


def main():
    import argparse
    parser = argparse.ArgumentParser(description="Aronium Multi-UOM Manager")
    parser.add_argument("--product-id", type=int, help="Pilih produk awal")
    args = parser.parse_args()

    db = DEFAULT_DB
    if not os.path.exists(db):
        # izinkan memilih file db lain
        root0 = tk.Tk(); root0.withdraw()
        from tkinter import filedialog
        db = filedialog.askopenfilename(title="Pilih pos.db", filetypes=[("SQLite DB", "*.db"), ("All", "*.*")])
        root0.destroy()
        if not db:
            return
    root = tk.Tk()
    try:
        app = ManagerApp(root, db)
        if args.product_id is not None:
            root.after(150, lambda: app.select_product(args.product_id))
    except sqlite3.OperationalError as e:
        messagebox.showerror("Gagal membuka database", str(e))
        return
    root.mainloop()


if __name__ == "__main__":
    main()