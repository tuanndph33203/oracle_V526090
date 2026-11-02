-- ============================================================================
-- ĐỀ TÀI SỐ 12: XÂY DỰNG CƠ SỞ DỮ LIỆU HỆ THỐNG QUẢN LÝ QUÁN CAFÉ
-- BƯỚC 3: NHẬP DỮ LIỆU MẪU (TỐI THIỂU MỖI BẢNG 5 BẢN GHI)
-- (Thực thi bằng User CAFE_ADMIN)
-- ============================================================================

-- 1. DỮ LIỆU BẢNG KHU_VUC (5 bản ghi)
INSERT INTO KHU_VUC (MaKhuVuc, TenKhuVuc, GhiChu) VALUES ('KV01', N'Tầng 1 - Trong nhà', N'Khu vực máy lạnh, gần quầy bar');
INSERT INTO KHU_VUC (MaKhuVuc, TenKhuVuc, GhiChu) VALUES ('KV02', N'Tầng 2 - Yên tĩnh', N'Thích hợp làm việc, học tập');
INSERT INTO KHU_VUC (MaKhuVuc, TenKhuVuc, GhiChu) VALUES ('KV03', N'Sân vườn', N'Không gian thoáng mát ngoài trời');
INSERT INTO KHU_VUC (MaKhuVuc, TenKhuVuc, GhiChu) VALUES ('KV04', N'Ban công tầng 2', N'View ngắm đường phố');
INSERT INTO KHU_VUC (MaKhuVuc, TenKhuVuc, GhiChu) VALUES ('KV05', N'Phòng VIP', N'Phòng họp riêng biệt có máy chiếu');

-- 2. DỮ LIỆU BẢNG BAN (8 bản ghi)
INSERT INTO BAN (MaBan, TenBan, SoGhe, TrangThai, MaKhuVuc) VALUES ('B01', N'Bàn T1-01', 4, 'TRONG', 'KV01');
INSERT INTO BAN (MaBan, TenBan, SoGhe, TrangThai, MaKhuVuc) VALUES ('B02', N'Bàn T1-02', 2, 'DANG_NGOI', 'KV01');
INSERT INTO BAN (MaBan, TenBan, SoGhe, TrangThai, MaKhuVuc) VALUES ('B03', N'Bàn T1-03', 6, 'DA_DAT', 'KV01');
INSERT INTO BAN (MaBan, TenBan, SoGhe, TrangThai, MaKhuVuc) VALUES ('B04', N'Bàn T2-01', 4, 'TRONG', 'KV02');
INSERT INTO BAN (MaBan, TenBan, SoGhe, TrangThai, MaKhuVuc) VALUES ('B05', N'Bàn T2-02', 4, 'DANG_NGOI', 'KV02');
INSERT INTO BAN (MaBan, TenBan, SoGhe, TrangThai, MaKhuVuc) VALUES ('B06', N'Bàn SV-01', 8, 'TRONG', 'KV03');
INSERT INTO BAN (MaBan, TenBan, SoGhe, TrangThai, MaKhuVuc) VALUES ('B07', N'Bàn BC-01', 2, 'TRONG', 'KV04');
INSERT INTO BAN (MaBan, TenBan, SoGhe, TrangThai, MaKhuVuc) VALUES ('B08', N'Bàn VIP-01', 12, 'DA_DAT', 'KV05');

-- 3. DỮ LIỆU BẢNG LOAI_MON (5 bản ghi)
INSERT INTO LOAI_MON (MaLoai, TenLoai, MoTa) VALUES ('LM01', N'Cà phê truyền thống', N'Cà phê đen, cà phê sữa pha phin & máy');
INSERT INTO LOAI_MON (MaLoai, TenLoai, MoTa) VALUES ('LM02', N'Cà phê máy phong cách Ý', N'Espresso, Latte, Cappuccino, Americano');
INSERT INTO LOAI_MON (MaLoai, TenLoai, MoTa) VALUES ('LM03', N'Trà trái cây & Trà sữa', N'Trà đào, trà vải, trà lài, trà sen vàng');
INSERT INTO LOAI_MON (MaLoai, TenLoai, MoTa) VALUES ('LM04', N'Sinh tố & Nước ép', N'Nước ép cam, dưa hấu, sinh tố bơ, mãng cầu');
INSERT INTO LOAI_MON (MaLoai, TenLoai, MoTa) VALUES ('LM05', N'Bánh ngọt & Đồ ăn vặt', N'Bánh Tiramisu, Croissant, Mousse chanh leo');

-- 4. DỮ LIỆU BẢNG MON (10 bản ghi)
INSERT INTO MON (MaMon, TenMon, DonViTinh, DonGia, TrangThai, MaLoai) VALUES ('M01', N'Cà phê đen đá', N'Ly', 25000, 'CON_HANG', 'LM01');
INSERT INTO MON (MaMon, TenMon, DonViTinh, DonGia, TrangThai, MaLoai) VALUES ('M02', N'Cà phê sữa đá', N'Ly', 29000, 'CON_HANG', 'LM01');
INSERT INTO MON (MaMon, TenMon, DonViTinh, DonGia, TrangThai, MaLoai) VALUES ('M03', N'Bạc xỉu ba tầng', N'Ly', 35000, 'CON_HANG', 'LM01');
INSERT INTO MON (MaMon, TenMon, DonViTinh, DonGia, TrangThai, MaLoai) VALUES ('M04', N'Cà phê Muối Huế', N'Ly', 35000, 'CON_HANG', 'LM01');
INSERT INTO MON (MaMon, TenMon, DonViTinh, DonGia, TrangThai, MaLoai) VALUES ('M05', N'Caramel Macchiato', N'Ly', 55000, 'CON_HANG', 'LM02');
INSERT INTO MON (MaMon, TenMon, DonViTinh, DonGia, TrangThai, MaLoai) VALUES ('M06', N'Trà đào cam sả', N'Ly', 45000, 'CON_HANG', 'LM03');
INSERT INTO MON (MaMon, TenMon, DonViTinh, DonGia, TrangThai, MaLoai) VALUES ('M07', N'Trà sen vàng hạt dẻ', N'Ly', 49000, 'CON_HANG', 'LM03');
INSERT INTO MON (MaMon, TenMon, DonViTinh, DonGia, TrangThai, MaLoai) VALUES ('M08', N'Sinh tố bơ sầu riêng', N'Ly', 52000, 'CON_HANG', 'LM04');
INSERT INTO MON (MaMon, TenMon, DonViTinh, DonGia, TrangThai, MaLoai) VALUES ('M09', N'Nước ép cam tươi', N'Ly', 38000, 'CON_HANG', 'LM04');
INSERT INTO MON (MaMon, TenMon, DonViTinh, DonGia, TrangThai, MaLoai) VALUES ('M10', N'Bánh Tiramisu Ý', N'Phần', 40000, 'CON_HANG', 'LM05');

-- 5. DỮ LIỆU BẢNG CHUC_VU (5 bản ghi)
INSERT INTO CHUC_VU (MaCV, TenCV, LuongCoBan) VALUES ('CV01', N'Quản lý quán', 12000000);
INSERT INTO CHUC_VU (MaCV, TenCV, LuongCoBan) VALUES ('CV02', N'Trưởng quầy pha chế (Head Barista)', 9000000);
INSERT INTO CHUC_VU (MaCV, TenCV, LuongCoBan) VALUES ('CV03', N'Nhân viên pha chế (Barista)', 6500000);
INSERT INTO CHUC_VU (MaCV, TenCV, LuongCoBan) VALUES ('CV04', N'Thu ngân (Cashier)', 6000000);
INSERT INTO CHUC_VU (MaCV, TenCV, LuongCoBan) VALUES ('CV05', N'Nhân viên phục vụ (Waiter)', 5000000);

-- 6. DỮ LIỆU BẢNG NHAN_VIEN (6 bản ghi)
INSERT INTO NHAN_VIEN (MaNV, HoTen, GioiTinh, NgaySinh, SoDienThoai, DiaChi, NgayVaoLam, TrangThai, MaCV)
VALUES ('NV01', N'Nguyễn Đình Tuấn', 'Nam', TO_DATE('2001-08-15', 'YYYY-MM-DD'), '0981112233', N'Cầu Giấy, Hà Nội', TO_DATE('2023-01-10', 'YYYY-MM-DD'), 'DANG_LAM', 'CV01');

INSERT INTO NHAN_VIEN (MaNV, HoTen, GioiTinh, NgaySinh, SoDienThoai, DiaChi, NgayVaoLam, TrangThai, MaCV)
VALUES ('NV02', N'Trần Thị Mai', 'Nu', TO_DATE('2002-05-20', 'YYYY-MM-DD'), '0972223344', N'Đống Đa, Hà Nội', TO_DATE('2023-03-01', 'YYYY-MM-DD'), 'DANG_LAM', 'CV04');

INSERT INTO NHAN_VIEN (MaNV, HoTen, GioiTinh, NgaySinh, SoDienThoai, DiaChi, NgayVaoLam, TrangThai, MaCV)
VALUES ('NV03', N'Lê Hoàng Long', 'Nam', TO_DATE('2000-11-12', 'YYYY-MM-DD'), '0963334455', N'Thanh Xuân, Hà Nội', TO_DATE('2023-02-15', 'YYYY-MM-DD'), 'DANG_LAM', 'CV02');

INSERT INTO NHAN_VIEN (MaNV, HoTen, GioiTinh, NgaySinh, SoDienThoai, DiaChi, NgayVaoLam, TrangThai, MaCV)
VALUES ('NV04', N'Phạm Minh Quân', 'Nam', TO_DATE('2003-09-08', 'YYYY-MM-DD'), '0914445566', N'Hà Đông, Hà Nội', TO_DATE('2023-06-01', 'YYYY-MM-DD'), 'DANG_LAM', 'CV03');

INSERT INTO NHAN_VIEN (MaNV, HoTen, GioiTinh, NgaySinh, SoDienThoai, DiaChi, NgayVaoLam, TrangThai, MaCV)
VALUES ('NV05', N'Vũ Phương Thảo', 'Nu', TO_DATE('2004-03-25', 'YYYY-MM-DD'), '0905556677', N'Ba Đình, Hà Nội', TO_DATE('2023-08-10', 'YYYY-MM-DD'), 'DANG_LAM', 'CV05');

INSERT INTO NHAN_VIEN (MaNV, HoTen, GioiTinh, NgaySinh, SoDienThoai, DiaChi, NgayVaoLam, TrangThai, MaCV)
VALUES ('NV06', N'Hoàng Văn Bách', 'Nam', TO_DATE('1999-12-01', 'YYYY-MM-DD'), '0936667788', N'Tây Hồ, Hà Nội', TO_DATE('2022-10-01', 'YYYY-MM-DD'), 'DA_NGHI', 'CV05');

-- 7. DỮ LIỆU BẢNG KHACH_HANG (6 bản ghi)
INSERT INTO KHACH_HANG (MaKH, HoTen, SoDienThoai, Email, DiemTichLuy, HangThanhVien, NgayDangKy)
VALUES ('KH01', N'Ngô Bảo Châu', '0912001002', 'chaungo@gmail.com', 250, 'BAC', TO_DATE('2023-02-10', 'YYYY-MM-DD'));

INSERT INTO KHACH_HANG (MaKH, HoTen, SoDienThoai, Email, DiemTichLuy, HangThanhVien, NgayDangKy)
VALUES ('KH02', N'Đặng Thu Thảo', '0983002003', 'thaodang@gmail.com', 520, 'VANG', TO_DATE('2023-01-15', 'YYYY-MM-DD'));

INSERT INTO KHACH_HANG (MaKH, HoTen, SoDienThoai, Email, DiemTichLuy, HangThanhVien, NgayDangKy)
VALUES ('KH03', N'Trần Tuấn Kiệt', '0974003004', 'kiettran@yahoo.com', 80, 'DONG', TO_DATE('2023-05-20', 'YYYY-MM-DD'));

INSERT INTO KHACH_HANG (MaKH, HoTen, SoDienThoai, Email, DiemTichLuy, HangThanhVien, NgayDangKy)
VALUES ('KH04', N'Phan Hoàng Nam', '0965004005', 'namphan@outlook.com', 1250, 'KIM_CUONG', TO_DATE('2022-11-05', 'YYYY-MM-DD'));

INSERT INTO KHACH_HANG (MaKH, HoTen, SoDienThoai, Email, DiemTichLuy, HangThanhVien, NgayDangKy)
VALUES ('KH05', N'Bùi Quỳnh Nga', '0906005006', 'ngabui@gmail.com', 30, 'DONG', TO_DATE('2023-09-12', 'YYYY-MM-DD'));

INSERT INTO KHACH_HANG (MaKH, HoTen, SoDienThoai, Email, DiemTichLuy, HangThanhVien, NgayDangKy)
VALUES ('KH06', N'Lê Minh Trí', '0947006007', 'trile@gmail.com', 0, 'DONG', TO_DATE('2023-10-01', 'YYYY-MM-DD'));

-- 8. DỮ LIỆU BẢNG HOA_DON (6 bản ghi)
INSERT INTO HOA_DON (MaHD, NgayTao, TongTien, GiamGia, ThanhTien, PhuongThucTT, TrangThai, MaBan, MaNV, MaKH)
VALUES ('HD001', TO_TIMESTAMP('2026-10-25 08:30:00', 'YYYY-MM-DD HH24:MI:SS'), 93000, 0, 93000, 'TIEN_MAT', 'DA_THANH_TOAN', 'B01', 'NV02', 'KH01');

INSERT INTO HOA_DON (MaHD, NgayTao, TongTien, GiamGia, ThanhTien, PhuongThucTT, TrangThai, MaBan, MaNV, MaKH)
VALUES ('HD002', TO_TIMESTAMP('2026-10-25 09:15:00', 'YYYY-MM-DD HH24:MI:SS'), 144000, 14000, 130000, 'CHUYEN_KHOAN', 'DA_THANH_TOAN', 'B02', 'NV02', 'KH02');

INSERT INTO HOA_DON (MaHD, NgayTao, TongTien, GiamGia, ThanhTien, PhuongThucTT, TrangThai, MaBan, MaNV, MaKH)
VALUES ('HD003', TO_TIMESTAMP('2026-10-25 10:00:00', 'YYYY-MM-DD HH24:MI:SS'), 70000, 0, 70000, 'TIEN_MAT', 'DA_THANH_TOAN', 'B04', 'NV02', NULL);

INSERT INTO HOA_DON (MaHD, NgayTao, TongTien, GiamGia, ThanhTien, PhuongThucTT, TrangThai, MaBan, MaNV, MaKH)
VALUES ('HD004', TO_TIMESTAMP('2026-10-26 14:20:00', 'YYYY-MM-DD HH24:MI:SS'), 180000, 30000, 150000, 'THE', 'DA_THANH_TOAN', 'B05', 'NV02', 'KH04');

INSERT INTO HOA_DON (MaHD, NgayTao, TongTien, GiamGia, ThanhTien, PhuongThucTT, TrangThai, MaBan, MaNV, MaKH)
VALUES ('HD005', TO_TIMESTAMP('2026-10-26 15:45:00', 'YYYY-MM-DD HH24:MI:SS'), 58000, 0, 58000, 'TIEN_MAT', 'DA_THANH_TOAN', NULL, 'NV02', 'KH03'); -- Khách mang về

INSERT INTO HOA_DON (MaHD, NgayTao, TongTien, GiamGia, ThanhTien, PhuongThucTT, TrangThai, MaBan, MaNV, MaKH)
VALUES ('HD006', TO_TIMESTAMP('2026-10-27 19:30:00', 'YYYY-MM-DD HH24:MI:SS'), 104000, 0, 104000, 'CHUYEN_KHOAN', 'CHO_THANH_TOAN', 'B02', 'NV02', 'KH05');

-- 9. DỮ LIỆU BẢNG CHI_TIET_HD (12 bản ghi)
INSERT INTO CHI_TIET_HD (MaHD, MaMon, SoLuong, DonGia, ThanhTien, GhiChu) VALUES ('HD001', 'M01', 1, 25000, 25000, N'Ít đường');
INSERT INTO CHI_TIET_HD (MaHD, MaMon, SoLuong, DonGia, ThanhTien, GhiChu) VALUES ('HD001', 'M02', 1, 29000, 29000, N'Nhiều đá');
INSERT INTO CHI_TIET_HD (MaHD, MaMon, SoLuong, DonGia, ThanhTien, GhiChu) VALUES ('HD001', 'M10', 1, 40000, 40000, NULL);

INSERT INTO CHI_TIET_HD (MaHD, MaMon, SoLuong, DonGia, ThanhTien, GhiChu) VALUES ('HD002', 'M06', 2, 45000, 90000, N'70% đường');
INSERT INTO CHI_TIET_HD (MaHD, MaMon, SoLuong, DonGia, ThanhTien, GhiChu) VALUES ('HD002', 'M05', 1, 55000, 55000, N'Uống nóng');

INSERT INTO CHI_TIET_HD (MaHD, MaMon, SoLuong, DonGia, ThanhTien, GhiChu) VALUES ('HD003', 'M03', 2, 35000, 70000, N'Nhiều sữa');

INSERT INTO CHI_TIET_HD (MaHD, MaMon, SoLuong, DonGia, ThanhTien, GhiChu) VALUES ('HD004', 'M07', 2, 49000, 98000, N'Thêm hạt dẻ');
INSERT INTO CHI_TIET_HD (MaHD, MaMon, SoLuong, DonGia, ThanhTien, GhiChu) VALUES ('HD004', 'M08', 1, 52000, 52000, NULL);
INSERT INTO CHI_TIET_HD (MaHD, MaMon, SoLuong, DonGia, ThanhTien, GhiChu) VALUES ('HD004', 'M04', 1, 35000, 35000, N'Ít đá');

INSERT INTO CHI_TIET_HD (MaHD, MaMon, SoLuong, DonGia, ThanhTien, GhiChu) VALUES ('HD005', 'M02', 2, 29000, 58000, N'Mang về để túi riêng');

INSERT INTO CHI_TIET_HD (MaHD, MaMon, SoLuong, DonGia, ThanhTien, GhiChu) VALUES ('HD006', 'M08', 2, 52000, 104000, N'Ít ngọt');

-- 10. DỮ LIỆU BẢNG NGUYEN_LIEU (6 bản ghi)
INSERT INTO NGUYEN_LIEU (MaNL, TenNL, DonViTinh, SoLuongTon, DinhMucToiThieu) VALUES ('NL01', N'Hạt cà phê Robusta Buôn Ma Thuột', N'Kg', 45.5, 10.0);
INSERT INTO NGUYEN_LIEU (MaNL, TenNL, DonViTinh, SoLuongTon, DinhMucToiThieu) VALUES ('NL02', N'Hạt cà phê Arabica Cầu Đất', N'Kg', 22.0, 5.0);
INSERT INTO NGUYEN_LIEU (MaNL, TenNL, DonViTinh, SoLuongTon, DinhMucToiThieu) VALUES ('NL03', N'Sữa đặc Ngôi Sao Phương Nam', N'Hộp', 60.0, 15.0);
INSERT INTO NGUYEN_LIEU (MaNL, TenNL, DonViTinh, SoLuongTon, DinhMucToiThieu) VALUES ('NL04', N'Sữa tươi thanh trùng Dalat Milk', N'Chai', 18.0, 10.0);
INSERT INTO NGUYEN_LIEU (MaNL, TenNL, DonViTinh, SoLuongTon, DinhMucToiThieu) VALUES ('NL05', N'Siro Đào Monin', N'Chai', 4.5, 3.0);
INSERT INTO NGUYEN_LIEU (MaNL, TenNL, DonViTinh, SoLuongTon, DinhMucToiThieu) VALUES ('NL06', N'Trà ô long túi lọc cao cấp', N'Gói', 12.0, 5.0);

-- 11. DỮ LIỆU BẢNG PHIEU_NHAP (5 bản ghi)
INSERT INTO PHIEU_NHAP (MaPN, NgayNhap, NhaCungCap, TongTien, MaNV)
VALUES ('PN001', TO_DATE('2026-10-01', 'YYYY-MM-DD'), N'Công ty Cổ phần Cà phê Trung Nguyên', 6500000, 'NV01');

INSERT INTO PHIEU_NHAP (MaPN, NgayNhap, NhaCungCap, TongTien, MaNV)
VALUES ('PN002', TO_DATE('2026-10-05', 'YYYY-MM-DD'), N'Đại lý Phân phối Dalat Milk Hà Nội', 2400000, 'NV01');

INSERT INTO PHIEU_NHAP (MaPN, NgayNhap, NhaCungCap, TongTien, MaNV)
VALUES ('PN003', TO_DATE('2026-10-10', 'YYYY-MM-DD'), N'Công ty Nguyên liệu Pha chế BaristaMart', 3800000, 'NV03');

INSERT INTO PHIEU_NHAP (MaPN, NgayNhap, NhaCungCap, TongTien, MaNV)
VALUES ('PN004', TO_DATE('2026-10-18', 'YYYY-MM-DD'), N'Công ty Cổ phần Vinamilk', 1950000, 'NV01');

INSERT INTO PHIEU_NHAP (MaPN, NgayNhap, NhaCungCap, TongTien, MaNV)
VALUES ('PN005', TO_DATE('2026-10-25', 'YYYY-MM-DD'), N'Nông trại Cầu Đất Farm Đà Lạt', 4200000, 'NV03');

-- 12. DỮ LIỆU BẢNG CHI_TIET_PN (8 bản ghi)
INSERT INTO CHI_TIET_PN (MaPN, MaNL, SoLuong, DonGiaNhap, ThanhTien) VALUES ('PN001', 'NL01', 30, 150000, 4500000);
INSERT INTO CHI_TIET_PN (MaPN, MaNL, SoLuong, DonGiaNhap, ThanhTien) VALUES ('PN001', 'NL02', 10, 200000, 2000000);
INSERT INTO CHI_TIET_PN (MaPN, MaNL, SoLuong, DonGiaNhap, ThanhTien) VALUES ('PN002', 'NL04', 60, 40000, 2400000);
INSERT INTO CHI_TIET_PN (MaPN, MaNL, SoLuong, DonGiaNhap, ThanhTien) VALUES ('PN003', 'NL05', 10, 220000, 2200000);
INSERT INTO CHI_TIET_PN (MaPN, MaNL, SoLuong, DonGiaNhap, ThanhTien) VALUES ('PN003', 'NL06', 16, 100000, 1600000);
INSERT INTO CHI_TIET_PN (MaPN, MaNL, SoLuong, DonGiaNhap, ThanhTien) VALUES ('PN004', 'NL03', 75, 26000, 1950000);
INSERT INTO CHI_TIET_PN (MaPN, MaNL, SoLuong, DonGiaNhap, ThanhTien) VALUES ('PN005', 'NL02', 20, 210000, 4200000);

COMMIT;

