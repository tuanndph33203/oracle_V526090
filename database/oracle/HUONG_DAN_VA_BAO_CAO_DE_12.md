# BÁO CÁO BÀI TẬP LỚN MÔN CÔNG NGHỆ ORACLE
## ĐỀ TÀI SỐ 12: XÂY DỰNG CƠ SỞ DỮ LIỆU HỆ THỐNG QUẢN LÝ QUÁN CAFÉ

---

## MỤC LỤC
1. [CHƯƠNG 1: GIỚI THIỆU ĐỀ TÀI VÀ THIẾT KẾ CƠ SỞ DỮ LIỆU](#chương-1)
   - 1.1 Mục tiêu và phạm vi đề tài
   - 1.2 Danh sách các thực thể (Entities) và thuộc tính
   - 1.3 Mô hình quan hệ thực thể (ERD)
2. [CHƯƠNG 2: CÀI ĐẶT CƠ SỞ DỮ LIỆU VẬT LÝ TRÊN ORACLE](#chương-2)
   - 2.1 Tạo Tablespace mới (`TBS_QUAN_CAFE`) và User mới (`CAFE_ADMIN`)
   - 2.2 Tạo cấu trúc bảng và các ràng buộc (PK, FK, Check, Unique)
   - 2.3 Nhập dữ liệu mẫu nghiệp vụ (Tối thiểu 5 bản ghi/bảng)
3. [CHƯƠNG 3: THIẾT KẾ CÁC TRUY VẤN SQL NGHIỆP VỤ (10+ CÂU)](#chương-3)
4. [CHƯƠNG 4: THỰC HÀNH QUẢN TRỊ CSDL ORACLE (5.0 ĐIỂM)](#chương-4)
   - 4.1 Quản lý Oracle Instance (Các chế độ Startup, Shutdown, Read-only, Restricted session)
   - 4.2 Quản trị Tablespace & Datafile (Mở rộng, di chuyển file, kịch bản cứu hộ phục hồi Datafile bị mất)
   - 4.3 Quản trị người dùng, Profile và Phân quyền (Roles & Privileges)
   - 4.4 Sao lưu và phục hồi Schema bằng Oracle Data Pump (`expdp` / `impdp`)
5. [CHƯƠNG 5: ỨNG DỤNG WEB KẾT NỐI CSDL ORACLE](#chương-5)
   - 5.1 Kiến trúc ứng dụng (Oracle APEX / Web Application)
   - 5.2 Các chức năng bán hàng (Order tại bàn, Menu món, Hóa đơn thanh toán)
6. [KẾT LUẬN VÀ TÀI LIỆU THAM KHẢO](#chương-6)

---

<a name="chương-1"></a>
## CHƯƠNG 1: GIỚI THIỆU ĐỀ TÀI VÀ THIẾT KẾ CƠ SỞ DỮ LIỆU

### 1.1 Mục tiêu và phạm vi đề tài
- **Mục tiêu:** Xây dựng một hệ thống cơ sở dữ liệu hoàn chỉnh trên nền tảng Oracle Database phục vụ cho công tác quản trị và vận hành một quán café hiện đại.
- **Phạm vi quản lý:**
  - Quản lý sơ đồ bàn và trạng thái phục vụ (Trống, Đang ngồi, Đã đặt).
  - Quản lý thực đơn đồ uống phân loại theo nhóm món (Cà phê phin, Cà phê máy, Trà, Sinh tố, Bánh ngọt).
  - Quản lý nhân sự quán (Chức vụ, nhân viên, ca làm việc).
  - Quản lý khách hàng thành viên và điểm tích lũy.
  - Quản lý quy trình gọi món, lập hóa đơn và thanh toán.
  - Quản lý kho nguyên vật liệu và nhập hàng.

### 1.2 Danh mục 12 bảng thực thể cốt lõi

| STT | Tên bảng | Ý nghĩa nghiệp vụ | Khóa chính (PK) | Khóa ngoại (FK) |
|---|---|---|---|---|
| 1 | `KHU_VUC` | Khu vực bàn (Tầng 1, Sân vườn, VIP) | `MaKhuVuc` | Không |
| 2 | `BAN` | Danh sách bàn phục vụ | `MaBan` | `MaKhuVuc` -> `KHU_VUC` |
| 3 | `LOAI_MON` | Nhóm danh mục đồ uống | `MaLoai` | Không |
| 4 | `MON` | Đồ uống và món ăn kèm | `MaMon` | `MaLoai` -> `LOAI_MON` |
| 5 | `CHUC_VU` | Chức vụ nhân sự | `MaCV` | Không |
| 6 | `NHAN_VIEN` | Danh sách nhân viên quán | `MaNV` | `MaCV` -> `CHUC_VU` |
| 7 | `KHACH_HANG`| Khách hàng thân thiết | `MaKH` | Không |
| 8 | `HOA_DON` | Hóa đơn bán hàng | `MaHD` | `MaBan`, `MaNV`, `MaKH` |
| 9 | `CHI_TIET_HD`| Chi tiết món trong hóa đơn | `(MaHD, MaMon)` | `MaHD`, `MaMon` |
| 10| `NGUYEN_LIEU`| Tồn kho hạt cà phê, sữa, siro | `MaNL` | Không |
| 11| `PHIEU_NHAP` | Phiếu nhập kho | `MaPN` | `MaNV` -> `NHAN_VIEN` |
| 12| `CHI_TIET_PN`| Chi tiết nguyên liệu nhập kho | `(MaPN, MaNL)` | `MaPN`, `MaNL` |

---

<a name="chương-2"></a>
## CHƯƠNG 2: CÀI ĐẶT CƠ SỞ DỮ LIỆU VẬT LÝ TRÊN ORACLE

> **Lưu ý đặc biệt từ Đề thi:** Toàn bộ bảng phải nằm trong một User mới và được lưu trong một Tablespace mới hoàn toàn.

- **Tập lệnh tạo Tablespace & User:** Xem tệp `database/oracle/01_create_tablespace_and_user.sql`
  - Tablespace: `TBS_QUAN_CAFE` với Datafile `tbs_quan_cafe_01.dbf` (Size 100M, Autoextend On).
  - User: `CAFE_ADMIN` / `CafeAdmin@123` được cấp quota không giới hạn trên tablespace này.
- **Tập lệnh tạo Bảng & Constraints:** Xem tệp `database/oracle/02_create_tables_and_constraints.sql`
  - Đầy đủ ràng buộc: Khóa chính (PK), Khóa ngoại (FK), Ràng buộc kiểm tra `CHECK` (đơn giá $\ge 0$, số lượng $> 0$, trạng thái hợp lệ).
- **Tập lệnh chèn dữ liệu mẫu:** Xem tệp `database/oracle/03_insert_sample_data.sql`
  - Mỗi bảng có từ 5 đến 12 bản ghi chuẩn tiếng Việt có dấu.

---

<a name="chương-3"></a>
## CHƯƠNG 3: CÁC TRUY VẤN SQL NGHIỆP VỤ (10+ CÂU)

Chi tiết cú pháp SQL và giải thích nghiệp vụ có trong tệp `database/oracle/04_business_queries_10_samples.sql`:
1. **Truy vấn cơ bản:** Lọc các món có giá từ 30.000 đến 50.000 VNĐ.
2. **Nối bảng (JOIN):** Hiển thị danh sách bàn kèm tên khu vực và trạng thái.
3. **Thống kê (GROUP BY + COUNT/AVG):** Đếm số món và đơn giá trung bình của từng loại đồ uống.
4. **Nhóm có điều kiện (HAVING):** Lọc nhóm món có từ 2 món trở lên và giá trung bình > 35.000 VNĐ.
5. **Nối nhiều bảng:** Xem chi tiết hóa đơn thanh toán kèm tên thu ngân, tên khách và số bàn.
6. **Subquery với IN:** Danh sách các món đã từng được gọi trong lịch sử bán hàng.
7. **Subquery với NOT EXISTS:** Tìm những món đồ uống chưa từng được khách mua lần nào (món mới/món ế).
8. **Subquery Max:** Tìm khách hàng có điểm tích lũy cao nhất quán.
9. **Tổng hợp N-N:** Doanh thu và số ly bán ra của từng món đồ uống.
10. **Hàm ngày tháng Oracle (`MONTHS_BETWEEN`):** Tính thâm niên và tiền thưởng cho nhân viên.
11. **Kiểm tra kho:** Cảnh báo nguyên liệu sắp hết (tồn kho $\le$ định mức tối thiểu).
12. **Window Function (`DENSE_RANK`):** Xếp hạng món bán chạy nhất quán.

---

<a name="chương-4"></a>
## CHƯƠNG 4: THỰC HÀNH QUẢN TRỊ CSDL ORACLE (5.0 ĐIỂM)

Tập lệnh thực hành chi tiết có trong tệp `database/oracle/05_oracle_dba_administration_scripts.sql`.

### 4.1 Quản trị Oracle Instance
- **Shutdown:** `SHUTDOWN IMMEDIATE` (đóng an toàn) vs `SHUTDOWN ABORT` (đóng khẩn cấp).
- **Startup 3 giai đoạn:**
  1. `STARTUP NOMOUNT`: Cấp phát vùng nhớ SGA và khởi động tiến trình nền.
  2. `ALTER DATABASE MOUNT`: Đọc Control file, xác định vị trí file dữ liệu.
  3. `ALTER DATABASE OPEN`: Mở datafile và redo log cho người dùng làm việc.
- **Trạng thái đặc biệt:**
  - `ALTER DATABASE OPEN READ ONLY`: Chế độ chỉ đọc dữ liệu.
  - `ALTER SYSTEM ENABLE RESTRICTED SESSION`: Chế độ bảo trì hệ thống.

### 4.2 Quản trị Tablespace & Datafile
- Thêm datafile: `ALTER TABLESPACE TBS_QUAN_CAFE ADD DATAFILE ... SIZE 50M AUTOEXTEND ON;`
- Bật/tắt Autoextend: `ALTER DATABASE DATAFILE ... AUTOEXTEND OFF;`
- Di chuyển Datafile: Offline file -> di chuyển trên HĐH -> `RENAME FILE` -> Online file.
- **Kịch bản cứu hộ phục hồi datafile:** Giả lập mất file `tbs_quan_cafe_01.dbf`, khởi động ở chế độ MOUNT, offline datafile lỗi, mở DB, chép lại file backup và thực thi `RECOVER DATAFILE`.

### 4.3 Quản trị User, Role và Profile
- Tạo `PROFILE CAFE_SECURITY_PROFILE`: Giới hạn 5 lần đăng nhập sai, khóa tài khoản nếu vượt quá.
- Tạo các Role nghiệp vụ: `ROLE_QUAN_LY`, `ROLE_THU_NGAN`, `ROLE_PHA_CHE`.
- Gán quyền đối tượng (`SELECT`, `INSERT`, `UPDATE`) chính xác theo vai trò cho các User `thungan01`, `phache01`.

### 4.4 Data Pump Export & Import Schema
- Tạo thư mục Oracle Directory: `CREATE OR REPLACE DIRECTORY CAFE_DUMP_DIR AS '...';`
- Xuất toàn bộ schema: `expdp CAFE_ADMIN/CafeAdmin@123 DIRECTORY=CAFE_DUMP_DIR DUMPFILE=cafe_backup.dmp SCHEMAS=CAFE_ADMIN`
- Phục hồi lại schema: `impdp CAFE_ADMIN/CafeAdmin@123 DIRECTORY=CAFE_DUMP_DIR DUMPFILE=cafe_backup.dmp SCHEMAS=CAFE_ADMIN TABLE_EXISTS_ACTION=REPLACE`

---

<a name="chương-5"></a>
## CHƯƠNG 5: ỨNG DỤNG WEB KẾT NỐI ORACLE (ORACLE APEX)

Để lấy trọn vẹn điểm cộng (2–3 điểm ưu tiên):
1. Truy cập [apex.oracle.com](https://apex.oracle.com) tạo Workspace miễn phí.
2. Trong menu **SQL Workshop** -> **SQL Scripts**: Upload và chạy 2 tệp `02_create_tables_and_constraints.sql` và `03_insert_sample_data.sql`.
3. Vào **App Builder** -> **Create New App**:
   - Trang 1: **Dashboard** (Biểu đồ doanh thu theo nhóm món đồ uống, biểu đồ trạng thái bàn).
   - Trang 2: **Quản lý Bàn** (Interactive Grid cho phép thêm, sửa, đổi trạng thái bàn).
   - Trang 3: **Menu Đồ uống** (Interactive Report cho phép tìm kiếm, thêm món mới, cập nhật giá).
   - Trang 4: **Bán hàng / Hóa đơn** (Form tạo đơn, chọn bàn, chọn món và thanh toán).

