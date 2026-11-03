# UTC Coffee AI Agent Rules

Tài liệu này là bộ quy tắc bắt buộc cho mọi AI Agent khi làm việc trong repository `UTC Coffee`.
Mục tiêu: thay đổi nhỏ, đúng kiến trúc, an toàn dữ liệu, dễ review và dễ rollback.
## 0) 🔴 BẮT BUỘC: Đọc tài liệu kiến trúc trước khi làm

Trước khi thực hiện BẤT KỲ task nào, AI Agent PHẢI đọc và hiểu các file sau:

1. `Program.cs` 
2. `appsettings.json`

Yêu cầu:
- Không được phép sửa code nếu chưa đọc các file trên
- Phải dựa trên tài liệu + source code để hiểu hệ thống

Nếu tài liệu và source code mâu thuẫn:
→ Ưu tiên source code  
→ Ghi nhận và đề xuất cập nhật lại tài liệu


## 1) Nguyên tắc ưu tiên

1. Không làm vỡ hành vi nghiệp vụ hiện tại.
2. Chỉ sửa đúng phạm vi yêu cầu, tránh refactor rộng không cần thiết.
3. Luôn bám kiến trúc phân lớp hiện có.
4. Mọi thay đổi phải kiểm chứng được bằng build/test.
5. Không tự ý thay đổi môi trường, secrets, hoặc git history.

## 2) READ FIRST DOCUMENT (Bắt buộc đọc trước)

Trước khi làm bất kỳ hành động nào, AI Agent phải đọc lần lượt:

1. `AGENTS.md` (file này).
2. Các rule đang áp dụng trong workspace (nếu có).
3. Yêu cầu hiện tại của user.
4. Nhóm file liên quan trực tiếp tới task (entrypoint + call chain chính).

Nếu chưa đọc đủ ngữ cảnh, **không được sửa code**.

## 3) NO ASSUMPTION RULE (Không suy đoán)

- Không suy đoán API contract, schema, business rule, hoặc side effect khi chưa có bằng chứng trong source.
- Nếu thiếu thông tin trong code:
  - Ghi rõ: `Không tìm thấy trong source`.
  - Đề xuất cách xác minh, thay vì tự kết luận.
- Không tự ý tạo behavior mới chỉ dựa trên phỏng đoán nghiệp vụ.

## 4) READ ONLY MODE

- Khi user yêu cầu review/analysis hoặc chưa yêu cầu sửa, AI Agent làm việc ở chế độ chỉ đọc:
  - Được phép đọc, phân tích, tóm tắt.
  - Không chỉnh sửa file.
  - Không chạy thao tác có side effect lên dữ liệu/hệ thống.
- Chỉ chuyển sang chế độ chỉnh sửa khi user yêu cầu rõ ràng.

## 5) Checklist bắt buộc trước khi làm bất kỳ việc gì

AI Agent phải thực hiện đủ các bước sau trước khi chỉnh sửa code:

1. Đọc yêu cầu và xác định rõ loại thay đổi:
   - API contract?
   - Business rule?
   - Data/migration?
   - Config/runtime?
2. Đọc call chain tối thiểu:
   - `Program.cs`/`Extensions` của project liên quan.
   - Controller liên quan.
   - Handler/Service liên quan trong `AnFood.Application`.
   - Entity/DbContext/Repository trong `AnFood.Persistence` và `AnFood.Domain`.
3. Xác định impact scope:
   - `AnFood.API`, `AnFood.Application`, `AnFood.Persistence`, `AnFood.Domain`.
4. Kiểm tra rủi ro:
   - Auth/JWT/Authorize.
   - Payment/Card flow.
   - SQL query hiệu năng cao hoặc tác động dữ liệu lớn.
5. Chỉ khi hoàn thành các bước trên mới bắt đầu sửa code.

## 6) Kiến trúc dự án và ranh giới trách nhiệm

- `AnFood.API/`
  - Chỉ xử lý HTTP, binding, validation đầu vào cơ bản, trả response.
  - Không nhét business logic phức tạp vào controller.
- `AnFood.Application/`
  - Chứa toàn bộ nghiệp vụ chính (`*Handler`, `*Logic`).
  - Điều phối transaction/use-case, gọi repository, gọi external service.
- `AnFood.Persistence/`
  - Chứa `DataContext`, repositories, persistence concerns.
  - Không đặt nghiệp vụ domain tại đây.
- `AnFood.Domain/`
  - Chứa entity/model domain.
- `src/BuildingBlocks/`
  - Dùng cho utility/infrastructure dùng chung (DataUtils, ApiUtils, EventBus...).

## 7) Quy tắc chỉnh sửa C#/.NET

- Tuân thủ convention C#:
  - Indent 4 spaces.
  - PascalCase cho type/member public.
  - camelCase cho local/parameter.
- Giữ nullable safety cho `net8.0`.
- Ưu tiên `async/await`, không dùng `.Result`/`.Wait()` trên request path.
- Ưu tiên constructor injection.
- Nếu sửa `Program.cs`, kiểm tra thứ tự middleware:
  - `UseCors` -> `UseAuthentication` -> `UseAuthorization` -> `MapControllers`/`MapHub`.
- Không thêm dependency mới nếu chưa thật sự cần.

## 8) Quy tắc API contract

- Không tự ý breaking change request/response contract.
- Nếu bắt buộc đổi contract:
  - Nêu rõ impact tới Web/Mobile client.
  - Đề xuất chiến lược backward compatibility/versioning.
- Endpoint danh sách lớn phải cân nhắc filter/pagination.

## 9) Quy tắc database và migration

- Không sửa lại migration cũ đã apply (trừ khi có yêu cầu rõ ràng).
- Khi đổi schema: tạo migration mới trong `AnFood.Persistence/Migrations`.
- Không hard-code secrets/connection string.
- Với flow nhạy cảm (payment, card, point): ưu tiên tính nhất quán, auditability, và idempotency.

## 10) Quy tắc SQL (Bắt buộc)

- Tránh query N+1:
  - Ưu tiên join/include/batch query phù hợp.
  - Tránh loop gọi query lặp trên từng bản ghi khi có thể gom truy vấn.
- Kiểm tra index khi query lớn:
  - Với truy vấn filter/sort thường xuyên, cần xác nhận cột có index phù hợp.
- Với bảng lớn:
  - Tránh full scan nếu không cần thiết.
  - Ưu tiên filter theo điều kiện chọn lọc và phân trang.
- Không thay đổi query critical nếu chưa hiểu rõ:
  - Bao gồm query liên quan payment, sale bill, point/card transaction, báo cáo tài chính.
  - Nếu chưa hiểu rõ logic/hiệu năng hiện tại, dừng và phân tích thêm trước khi sửa.

## 11) Authentication, security, logging

- Không làm lỏng auth một cách vô ý:
  - Không remove `[Authorize]` trừ khi có yêu cầu rõ.
  - Nếu endpoint public, phải nêu lý do và cơ chế bảo vệ tương ứng.
- Không log token/password/secret dữ liệu nhạy cảm.
- Ưu tiên structured logging (`LogInformation("...", args)`).
- Không commit file chứa secret thực.

## 12) Quy trình làm việc chuẩn của AI Agent

### Trước khi sửa

- Đọc file liên quan trực tiếp + call chain.
- Tóm tắt nhanh approach nội bộ (không cần refactor ngoài scope).

### Trong khi sửa

- Sửa tối thiểu, đúng mục tiêu.
- Giữ nguyên coding style và naming hiện có của module.
- Không thay đổi file không liên quan.

### Sau khi sửa

- Rà soát lại phạm vi thay đổi để đảm bảo không vượt quá yêu cầu.
- Xác nhận không tạo breaking change ngoài chủ đích.

## 13) Quy tắc báo cáo kết quả

AI Agent phải báo cáo rõ:

- Danh sách file đã đổi.
- Lý do đổi cho từng nhóm thay đổi.
- Kết quả build/test.
- Rủi ro còn lại + cách verify thủ công.

## 14) Những điều AI Agent không được làm

- Không chạy lệnh git phá hủy (`reset --hard`, force checkout, force push) khi chưa được yêu cầu rõ.
- Không commit/push nếu user chưa yêu cầu.
- Không tự ý sửa config môi trường máy local/global.
- Không đổi kiến trúc tổng thể nếu user không yêu cầu.
- Không ghi đè thay đổi chưa commit của người dùng.

## 15) Thứ tự ưu tiên khi có nhiều phương án

Nếu có nhiều phương án hợp lệ, AI Agent ưu tiên theo thứ tự:
1) An toàn nghiệp vụ hiện tại
2) Thay đổi nhỏ, dễ review
3) Đúng layering hiện có
4) Dễ kiểm chứng bằng build/test
