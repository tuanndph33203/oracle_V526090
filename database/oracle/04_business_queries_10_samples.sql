-- ============================================================================
-- ĐỀ TÀI SỐ 12: XÂY DỰNG CƠ SỞ DỮ LIỆU HỆ THỐNG QUẢN LÝ QUÁN CAFÉ
-- BƯỚC 4: THIẾT KẾ CÁC TRUY VẤN SQL NGHIỆP VỤ (ĐÁP ỨNG MỤC 3 CỦA ĐỀ THI)
-- Bao gồm: Truy vấn cơ bản, Nối nhiều bảng, Group By, Having, Lồng nhau, Thống kê
-- ============================================================================

-- ----------------------------------------------------------------------------
-- TRUY VẤN 1 (CƠ BẢN): 
-- Liệt kê danh sách đồ uống có giá bán từ 30.000 VNĐ đến 50.000 VNĐ,
-- sắp xếp giảm dần theo đơn giá.
-- ----------------------------------------------------------------------------
SELECT MaMon, TenMon, DonViTinh, DonGia, TrangThai
FROM MON
WHERE DonGia BETWEEN 30000 AND 50000
ORDER BY DonGia DESC;

-- ----------------------------------------------------------------------------
-- TRUY VẤN 2 (NỐI BẢNG INNER JOIN): 
-- Hiển thị danh sách các bàn cùng với tên khu vực tương ứng và số ghế ngồi.
-- ----------------------------------------------------------------------------
SELECT b.MaBan, b.TenBan, b.SoGhe, b.TrangThai, kv.TenKhuVuc
FROM BAN b
JOIN KHU_VUC kv ON b.MaKhuVuc = kv.MaKhuVuc
ORDER BY kv.TenKhuVuc, b.TenBan;

-- ----------------------------------------------------------------------------
-- TRUY VẤN 3 (HÀM THỐNG KÊ + GROUP BY): 
-- Thống kê số lượng món và đơn giá trung bình của từng nhóm đồ uống (LOAI_MON).
-- ----------------------------------------------------------------------------
SELECT lm.MaLoai, lm.TenLoai, 
       COUNT(m.MaMon) AS TongSoMon, 
       ROUND(AVG(m.DonGia), 0) AS GiaTrungBinh,
       MIN(m.DonGia) AS GiaThapNhat,
       MAX(m.DonGia) AS GiaCaoNhat
FROM LOAI_MON lm
LEFT JOIN MON m ON lm.MaLoai = m.MaLoai
GROUP BY lm.MaLoai, lm.TenLoai
ORDER BY TongSoMon DESC;

-- ----------------------------------------------------------------------------
-- TRUY VẤN 4 (GROUP BY + HAVING): 
-- Tìm những nhóm đồ uống có tổng số lượng món lớn hơn hoặc bằng 2 món 
-- và có giá bán trung bình trên 35.000 VNĐ.
-- ----------------------------------------------------------------------------
SELECT lm.TenLoai, 
       COUNT(m.MaMon) AS SoLuongMon,
       ROUND(AVG(m.DonGia), 0) AS GiaTB
FROM LOAI_MON lm
JOIN MON m ON lm.MaLoai = m.MaLoai
GROUP BY lm.TenLoai
HAVING COUNT(m.MaMon) >= 2 AND AVG(m.DonGia) > 35000;

-- ----------------------------------------------------------------------------
-- TRUY VẤN 5 (NỐI NHIỀU BẢNG): 
-- Xem chi tiết các hóa đơn đã thanh toán kèm tên nhân viên lập đơn, 
-- tên khách hàng và tên bàn phục vụ.
-- ----------------------------------------------------------------------------
SELECT hd.MaHD, 
       TO_CHAR(hd.NgayTao, 'DD/MM/YYYY HH24:MI:SS') AS ThoiGianLap,
       NVL(b.TenBan, N'Mang về') AS ViTri,
       nv.HoTen AS NhanVienLap,
       NVL(kh.HoTen, N'Khách vãng lai') AS KhachHang,
       hd.ThanhTien,
       hd.PhuongThucTT
FROM HOA_DON hd
LEFT JOIN BAN b ON hd.MaBan = b.MaBan
JOIN NHAN_VIEN nv ON hd.MaNV = nv.MaNV
LEFT JOIN KHACH_HANG kh ON hd.MaKH = kh.MaKH
WHERE hd.TrangThai = 'DA_THANH_TOAN'
ORDER BY hd.NgayTao DESC;

-- ----------------------------------------------------------------------------
-- TRUY VẤN 6 (TRUY VẤN LỒNG NHAU VỚI SUBQUERY IN): 
-- Liệt kê danh sách các món đồ uống đã từng được khách gọi trong hóa đơn.
-- ----------------------------------------------------------------------------
SELECT MaMon, TenMon, DonGia 
FROM MON
WHERE MaMon IN (
    SELECT DISTINCT MaMon 
    FROM CHI_TIET_HD
);

-- ----------------------------------------------------------------------------
-- TRUY VẤN 7 (TRUY VẤN LỒNG NHAU VỚI NOT IN / NOT EXISTS): 
-- Tìm những món trong thực đơn CHƯA TỪNG được bán lần nào (món ế/món mới).
-- ----------------------------------------------------------------------------
SELECT m.MaMon, m.TenMon, m.DonGia, lm.TenLoai
FROM MON m
JOIN LOAI_MON lm ON m.MaLoai = lm.MaLoai
WHERE NOT EXISTS (
    SELECT 1 
    FROM CHI_TIET_HD ct 
    WHERE ct.MaMon = m.MaMon
);

-- ----------------------------------------------------------------------------
-- TRUY VẤN 8 (TRUY VẤN LỒNG NHAU SO SÁNH GIÁ TRỊ CỰC ĐẠI): 
-- Tìm thông tin khách hàng có số điểm tích lũy cao nhất quán café.
-- ----------------------------------------------------------------------------
SELECT MaKH, HoTen, SoDienThoai, Email, DiemTichLuy, HangThanhVien
FROM KHACH_HANG
WHERE DiemTichLuy = (
    SELECT MAX(DiemTichLuy) 
    FROM KHACH_HANG
);

-- ----------------------------------------------------------------------------
-- TRUY VẤN 9 (THỐNG KÊ DOANH THU THEO MÓN - TỔNG HỢP N-N): 
-- Thống kê tổng số lượng đã bán và tổng doanh thu thu về từ mỗi món đồ uống.
-- ----------------------------------------------------------------------------
SELECT m.MaMon, m.TenMon, 
       SUM(ct.SoLuong) AS TongLyDaBan, 
       SUM(ct.ThanhTien) AS TongDoanhThu
FROM MON m
JOIN CHI_TIET_HD ct ON m.MaMon = ct.MaMon
JOIN HOA_DON hd ON ct.MaHD = hd.MaHD
WHERE hd.TrangThai = 'DA_THANH_TOAN'
GROUP BY m.MaMon, m.TenMon
ORDER BY TongDoanhThu DESC;

-- ----------------------------------------------------------------------------
-- TRUY VẤN 10 (HÀM NGÀY THÁNG ORACLE: MONTHS_BETWEEN, SYSDATE): 
-- Tính thâm niên làm việc (số tháng làm việc) và ước tính tiền thưởng thâm niên
-- cho nhân viên quán (cứ 6 tháng thưởng 500.000 VNĐ).
-- ----------------------------------------------------------------------------
SELECT MaNV, HoTen, 
       TO_CHAR(NgayVaoLam, 'DD/MM/YYYY') AS NgayVaoLam,
       ROUND(MONTHS_BETWEEN(SYSDATE, NgayVaoLam), 1) AS SoThangLamViec,
       FLOOR(MONTHS_BETWEEN(SYSDATE, NgayVaoLam) / 6) * 500000 AS TienThuongThamNien
FROM NHAN_VIEN
WHERE TrangThai = 'DANG_LAM'
ORDER BY SoThangLamViec DESC;

-- ----------------------------------------------------------------------------
-- TRUY VẤN 11 (CẢNH BÁO KHO NGUYÊN LIỆU): 
-- Lọc danh sách nguyên liệu trong kho đang ở mức báo động (Số lượng tồn < Định mức).
-- ----------------------------------------------------------------------------
SELECT MaNL, TenNL, DonViTinh, SoLuongTon, DinhMucToiThieu,
       (DinhMucToiThieu - SoLuongTon) AS SoLuongCanNhapThem
FROM NGUYEN_LIEU
WHERE SoLuongTon <= DinhMucToiThieu;

-- ----------------------------------------------------------------------------
-- TRUY VẤN 12 (WINDOW FUNCTION RANK / DENSE_RANK TRÊN ORACLE): 
-- Xếp hạng Top các món đồ uống đem lại doanh thu cao nhất cho quán.
-- ----------------------------------------------------------------------------
SELECT m.TenMon,
       SUM(ct.ThanhTien) AS DoanhThu,
       DENSE_RANK() OVER (ORDER BY SUM(ct.ThanhTien) DESC) AS HangDoanhThu
FROM MON m
JOIN CHI_TIET_HD ct ON m.MaMon = ct.MaMon
JOIN HOA_DON hd ON ct.MaHD = hd.MaHD
WHERE hd.TrangThai = 'DA_THANH_TOAN'
GROUP BY m.TenMon;
