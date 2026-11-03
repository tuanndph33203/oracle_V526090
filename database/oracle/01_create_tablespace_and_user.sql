-- ============================================================================
-- ĐỀ TÀI SỐ 12: XÂY DỰNG CƠ SỞ DỮ LIỆU HỆ THỐNG QUẢN LÝ QUÁN CAFÉ
-- HỌC PHẦN: CÔNG NGHỆ ORACLE - ĐẠI HỌC GIAO THÔNG VẬN TẢI
-- BƯỚC 1: KHỞI TẠO TABLESPACE MỚI VÀ USER MỚI
-- (Thực thi bằng quyền SYSDBA hoặc SYSTEM)
-- ============================================================================

-- 1. Chuyển sang container pluggable database nếu dùng Oracle 12c/19c/21c (vd: ORCLPDB)
-- ALTER SESSION SET CONTAINER = ORCLPDB;

-- 2. Tạo Tablespace mới dành riêng cho đề tài Quản lý Quán Café
-- (Lưu ý: Thay đổi đường dẫn datafile phù hợp với ổ đĩa máy tính cài đặt)
-- Ví dụ trên Windows: 'C:\APP\ORACLE\ORADATA\ORCL\TBS_QUAN_CAFE_01.DBF'
-- Ví dụ trên Linux:   '/opt/oracle/oradata/ORCLCDB/tbs_quan_cafe_01.dbf'
CREATE TABLESPACE TBS_QUAN_CAFE
DATAFILE 'tbs_quan_cafe_01.dbf' 
SIZE 100M 
AUTOEXTEND ON NEXT 10M 
MAXSIZE 1G
EXTENT MANAGEMENT LOCAL 
SEGMENT SPACE MANAGEMENT AUTO;

-- 3. Tạo Tablespace tạm thời nếu cần (tuỳ chọn)
-- CREATE TEMPORARY TABLESPACE TBS_TEMP_CAFE TEMPFILE 'tbs_temp_cafe_01.dbf' SIZE 50M AUTOEXTEND ON;

-- 4. Tạo User mới quản trị CSDL Quán Café (User Schema)
-- Trong Oracle 12c+ nếu tạo ở CDB root cần c##, còn ở PDB hoặc Oracle 11g thì tạo bình thường
CREATE USER CAFE_ADMIN IDENTIFIED BY CafeAdmin123#
DEFAULT TABLESPACE TBS_QUAN_CAFE
TEMPORARY TABLESPACE TEMP
QUOTA UNLIMITED ON TBS_QUAN_CAFE
ACCOUNT UNLOCK;

-- 5. Cấp các quyền cơ bản và quyền tạo đối tượng cho CAFE_ADMIN
GRANT CREATE SESSION TO CAFE_ADMIN;
GRANT CREATE TABLE TO CAFE_ADMIN;
GRANT CREATE VIEW TO CAFE_ADMIN;
GRANT CREATE SEQUENCE TO CAFE_ADMIN;
GRANT CREATE PROCEDURE TO CAFE_ADMIN;
GRANT CREATE TRIGGER TO CAFE_ADMIN;
GRANT ALTER SESSION TO CAFE_ADMIN;

-- Hoặc cấp quyền qua các role chuẩn của Oracle
GRANT CONNECT, RESOURCE TO CAFE_ADMIN;

-- 6. Kiểm tra lại thông tin Tablespace và User vừa tạo
SELECT tablespace_name, status, contents, extent_management 
FROM dba_tablespaces 
WHERE tablespace_name = 'TBS_QUAN_CAFE';

SELECT username, default_tablespace, temporary_tablespace, account_status 
FROM dba_users 
WHERE username = 'CAFE_ADMIN';

-- HOÀN TẤT BƯỚC 1:
-- Đăng xuất khỏi SYS/SYSTEM và kết nối vào CSDL bằng User: CAFE_ADMIN / CafeAdmin123#

