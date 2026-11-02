-- ============================================================================
-- ĐỀ TÀI SỐ 12: XÂY DỰNG CƠ SỞ DỮ LIỆU HỆ THỐNG QUẢN LÝ QUÁN CAFÉ
-- BƯỚC 5: TOÀN BỘ KỊCH BẢN QUẢN TRỊ CSDL ORACLE (MỤC 4: 5.0 ĐIỂM)
-- (Thực hiện chủ yếu bằng quyền SYSDBA trong SQL*Plus hoặc SQL Developer)
-- ============================================================================

-- ============================================================================
-- PHẦN A: QUẢN LÝ INSTANCE (KHỞI ĐỘNG, TẮT VÀ CÁC CHẾ ĐỘ HOẠT ĐỘNG)
-- ============================================================================

-- 1. Xem trạng thái hiện tại của Instance
SELECT instance_name, status, database_status FROM v$instance;

-- 2. Minh họa các chế độ tắt Database (Shutdown)
-- Chế độ 1: Shutdown thông thường (Đợi người dùng ngắt kết nối)
-- SHUTDOWN NORMAL;

-- Chế độ 2: Shutdown an toàn và phổ biến nhất (Rollback transaction dở dang và ngắt phiên)
SHUTDOWN IMMEDIATE;

-- Chế độ 3: Shutdown khẩn cấp (Không giải phóng bộ nhớ SGA ngay, phục hồi tự động khi mở lại)
-- SHUTDOWN ABORT;

-- 3. Minh họa quy trình khởi động 3 giai đoạn của Oracle Instance
-- Giai đoạn 1: Chỉ đọc tham số SPFILE/PFILE và cấp phát bộ nhớ SGA, khởi động các Background Process
STARTUP NOMOUNT;
SELECT status FROM v$instance; -- Trạng thái: STARTED

-- Giai đoạn 2: Tìm và mở file điều khiển (Control Files), xác định vị trí Datafile & Redo Log
ALTER DATABASE MOUNT;
SELECT status FROM v$instance; -- Trạng thái: MOUNTED

-- Giai đoạn 3: Mở toàn bộ Datafile và Redo Log cho phép User truy cập dữ liệu bình thường
ALTER DATABASE OPEN;
SELECT status FROM v$instance; -- Trạng thái: OPEN

-- 4. Minh họa chế độ READ ONLY (Chỉ đọc, không cho sửa/thêm dữ liệu)
SHUTDOWN IMMEDIATE;
STARTUP MOUNT;
ALTER DATABASE OPEN READ ONLY;
-- Kiểm tra chế độ:
SELECT open_mode FROM v$database; -- Kết quả: READ ONLY

-- Mở lại chế độ đọc ghi thông thường (READ WRITE):
SHUTDOWN IMMEDIATE;
STARTUP;
SELECT open_mode FROM v$database; -- Kết quả: READ WRITE

-- 5. Minh họa chế độ RESTRICTED SESSION (Bảo trì hệ thống, chỉ DBA mới được vào)
-- Cách 1: Bật khi Database đang OPEN
ALTER SYSTEM ENABLE RESTRICTED SESSION;

-- Kiểm tra:
SELECT logins FROM v$instance; -- Kết quả: RESTRICTED

-- Tắt chế độ bảo trì để người dùng đăng nhập bình thường:
ALTER SYSTEM DISABLE RESTRICTED SESSION;
SELECT logins FROM v$instance; -- Kết quả: ALLOWED


-- ============================================================================
-- PHẦN B: QUẢN LÝ TABLESPACE & DATAFILE
-- ============================================================================

-- 1. Truy vấn thông tin các Tablespace và Datafile hiện có trong hệ thống
SELECT tablespace_name, status, contents, extent_management 
FROM dba_tablespaces;

SELECT file_id, file_name, tablespace_name, bytes / 1024 / 1024 AS MB, autoextensible 
FROM dba_data_files;

-- 2. Thêm một Datafile mới vào Tablespace TBS_QUAN_CAFE
ALTER TABLESPACE TBS_QUAN_CAFE 
ADD DATAFILE 'tbs_quan_cafe_02.dbf' 
SIZE 50M 
AUTOEXTEND ON NEXT 5M MAXSIZE 500M;

-- 3. Bật/Tắt tính năng tự động mở rộng (Autoextend) của 1 Datafile
-- Tắt tự động mở rộng:
ALTER DATABASE DATAFILE 'tbs_quan_cafe_02.dbf' AUTOEXTEND OFF;

-- Bật lại tự động mở rộng:
ALTER DATABASE DATAFILE 'tbs_quan_cafe_02.dbf' AUTOEXTEND ON NEXT 10M MAXSIZE 1G;

-- 4. Thay đổi kích thước (Resize) trực tiếp một Datafile
ALTER DATABASE DATAFILE 'tbs_quan_cafe_02.dbf' RESIZE 80M;

-- 5. Di chuyển vị trí Datafile (Offline -> Copy/Move file OS -> Rename trong Oracle -> Online)
-- Bước a: Đưa Datafile về trạng thái Offline
ALTER DATABASE DATAFILE 'tbs_quan_cafe_02.dbf' OFFLINE;

-- Bước b: Sử dụng lệnh hệ điều hành copy/move file sang thư mục mới
-- Ví dụ trên Linux: !mv /old_path/tbs_quan_cafe_02.dbf /new_path/tbs_quan_cafe_02.dbf
-- Ví dụ trên Windows: Chuyển file bằng Explorer hoặc lệnh copy

-- Bước c: Cập nhật đường dẫn mới cho Oracle
-- ALTER DATABASE RENAME FILE 'tbs_quan_cafe_02.dbf' TO '/new_path/tbs_quan_cafe_02.dbf';

-- Bước d: Đưa Datafile trở lại Online (kèm recover nếu cần)
-- RECOVER DATAFILE 'tbs_quan_cafe_02.dbf';
-- ALTER DATABASE DATAFILE 'tbs_quan_cafe_02.dbf' ONLINE;

-- 6. Xóa một Datafile hoặc Xóa Tablespace
-- Xóa Datafile phụ:
ALTER TABLESPACE TBS_QUAN_CAFE DROP DATAFILE 'tbs_quan_cafe_02.dbf';

-- Xóa Tablespace và toàn bộ dữ liệu vật lý (minh họa, KHÔNG xóa TBS_QUAN_CAFE chính):
-- CREATE TABLESPACE TBS_TEST DATAFILE 'tbs_test.dbf' SIZE 10M;
-- DROP TABLESPACE TBS_TEST INCLUDING CONTENTS AND DATAFILES;

-- 7. KỊCH BẢN CỨU HỘ: KHÔI PHỤC (RECOVER) DATAFILE BỊ MẤT
-- Giả sử file tbs_quan_cafe_01.dbf bị xóa hoặc bị lỗi hệ thống:
-- Khi STARTUP OPEN báo lỗi ORA-01157 / ORA-01110 (Cannot identify/lock data file)
/*
   Bước 1: SQL> STARTUP MOUNT;
   Bước 2: SQL> ALTER DATABASE DATAFILE 'tbs_quan_cafe_01.dbf' OFFLINE;
   Bước 3: SQL> ALTER DATABASE OPEN;
   Bước 4: Copy file backup hợp lệ vào vị trí cũ.
   Bước 5: SQL> RECOVER DATAFILE 'tbs_quan_cafe_01.dbf';
   Bước 6: SQL> ALTER DATABASE DATAFILE 'tbs_quan_cafe_01.dbf' ONLINE;
*/


-- ============================================================================
-- PHẦN C: QUẢN TRỊ NGƯỜI DÙNG, PROFILE VÀ PHÂN QUYỀN (USER & ROLE)
-- ============================================================================

-- 1. Tạo Profile để giới hạn tài nguyên và bảo mật mật khẩu
CREATE PROFILE CAFE_SECURITY_PROFILE LIMIT
    SESSIONS_PER_USER 3            -- Tối đa 3 phiên đăng nhập cùng lúc
    FAILED_LOGIN_ATTEMPTS 5         -- Khóa tài khoản nếu nhập sai pass 5 lần
    PASSWORD_LIFE_TIME 90           -- Hạn đổi mật khẩu sau 90 ngày
    PASSWORD_LOCK_TIME 1;           -- Khóa tài khoản 1 ngày nếu quá số lần thử

-- 2. Tạo các vai trò nghiệp vụ (Roles) của quán café
CREATE ROLE ROLE_QUAN_LY;
CREATE ROLE ROLE_THU_NGAN;
CREATE ROLE ROLE_PHA_CHE;

-- 3. Phân quyền đối tượng (Object Privileges) cho từng Role
-- Vai trò Quản lý: Toàn quyền thao tác trên CSDL quán café
GRANT SELECT, INSERT, UPDATE, DELETE ON CAFE_ADMIN.MON TO ROLE_QUAN_LY;
GRANT SELECT, INSERT, UPDATE, DELETE ON CAFE_ADMIN.BAN TO ROLE_QUAN_LY;
GRANT SELECT, INSERT, UPDATE, DELETE ON CAFE_ADMIN.HOA_DON TO ROLE_QUAN_LY;
GRANT SELECT, INSERT, UPDATE, DELETE ON CAFE_ADMIN.CHI_TIET_HD TO ROLE_QUAN_LY;
GRANT SELECT, INSERT, UPDATE, DELETE ON CAFE_ADMIN.NHAN_VIEN TO ROLE_QUAN_LY;
GRANT SELECT, INSERT, UPDATE, DELETE ON CAFE_ADMIN.NGUYEN_LIEU TO ROLE_QUAN_LY;

-- Vai trò Thu ngân: Bán hàng, xem bàn, tạo hóa đơn, xem menu
GRANT SELECT ON CAFE_ADMIN.MON TO ROLE_THU_NGAN;
GRANT SELECT, UPDATE ON CAFE_ADMIN.BAN TO ROLE_THU_NGAN;
GRANT SELECT, INSERT, UPDATE ON CAFE_ADMIN.HOA_DON TO ROLE_THU_NGAN;
GRANT SELECT, INSERT, UPDATE ON CAFE_ADMIN.CHI_TIET_HD TO ROLE_THU_NGAN;
GRANT SELECT, INSERT ON CAFE_ADMIN.KHACH_HANG TO ROLE_THU_NGAN;

-- Vai trò Pha chế: Chỉ xem danh mục món và xem chi tiết đồ uống cần làm
GRANT SELECT ON CAFE_ADMIN.MON TO ROLE_PHA_CHE;
GRANT SELECT ON CAFE_ADMIN.CHI_TIET_HD TO ROLE_PHA_CHE;
GRANT SELECT, UPDATE(SoLuongTon) ON CAFE_ADMIN.NGUYEN_LIEU TO ROLE_PHA_CHE;

-- 4. Tạo User nhân viên và gán Role + Profile
CREATE USER thungan01 IDENTIFIED BY ThuNgan@123
PROFILE CAFE_SECURITY_PROFILE
DEFAULT TABLESPACE TBS_QUAN_CAFE;

CREATE USER phache01 IDENTIFIED BY PhaChe@123
PROFILE CAFE_SECURITY_PROFILE
DEFAULT TABLESPACE TBS_QUAN_CAFE;

-- Cấp quyền đăng nhập cho các User
GRANT CREATE SESSION TO thungan01, phache01;

-- Gán Role tương ứng cho User
GRANT ROLE_THU_NGAN TO thungan01;
GRANT ROLE_PHA_CHE TO phache01;

-- Thiết lập Role mặc định khi User login
ALTER USER thungan01 DEFAULT ROLE ROLE_THU_NGAN;
ALTER USER phache01 DEFAULT ROLE ROLE_PHA_CHE;


-- ============================================================================
-- PHẦN D: MINH HỌA IMPORT VÀ EXPORT SCHEMA (ORACLE DATA PUMP)
-- (Thực hiện qua Terminal / Command Prompt bằng tiện ích expdp và impdp)
-- ============================================================================

-- Bước 1: Tạo thư mục Directory trên Oracle và cấp quyền (Chạy trong SQL*Plus bằng SYS)
CREATE OR REPLACE DIRECTORY CAFE_DUMP_DIR AS '/opt/oracle/admin/dump';
-- (Trên Windows ví dụ: 'C:\oracle_backup')
GRANT READ, WRITE ON DIRECTORY CAFE_DUMP_DIR TO CAFE_ADMIN;

-- Bước 2: Lệnh EXPORT toàn bộ Schema CAFE_ADMIN ra file .DMP
-- Chạy trên CMD/Terminal (Không chạy trong SQL):
/*
expdp CAFE_ADMIN/CafeAdmin@123 \
    DIRECTORY=CAFE_DUMP_DIR \
    DUMPFILE=cafe_admin_full.dmp \
    LOGFILE=cafe_export.log \
    SCHEMAS=CAFE_ADMIN
*/

-- Bước 3: Lệnh IMPORT phục hồi Schema vào Database (chẳng hạn sang máy khác hoặc sau khi xóa dữ liệu)
-- Chạy trên CMD/Terminal:
/*
impdp CAFE_ADMIN/CafeAdmin@123 \
    DIRECTORY=CAFE_DUMP_DIR \
    DUMPFILE=cafe_admin_full.dmp \
    LOGFILE=cafe_import.log \
    SCHEMAS=CAFE_ADMIN \
    TABLE_EXISTS_ACTION=REPLACE
*/

