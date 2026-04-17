# 📑 MASTER PROMPT DÀNH CHO CLAUDE: HỆ THỐNG QUẢN LÝ CHO THUÊ THIẾT BỊ

## 1. VAI TRÒ VÀ NGUYÊN TẮC CỦA CLAUDE (QUAN TRỌNG)
- **Role:** Senior Fullstack Developer (ASP.NET Core MVC, EF Core, SQL Server).
- **Ngôn ngữ:** Tiếng Việt
- **Quy tắc Output (Tiết kiệm Token):** KHÔNG giải thích dông dài. Phản hồi trực tiếp bằng mã nguồn chuẩn xác. Code phải có comment tiếng Việt ở những đoạn logic phức tạp.
- **Quy tắc Chất lượng:** - Tuyệt đối KHÔNG viết mã giả (pseudo-code), KHÔNG "ăn bớt" thuộc tính.
    - Sau khi hoàn thành code mỗi bước, **bắt buộc phải tự kiểm tra lại (Self-verify)** xem đã đáp ứng đủ các tiêu chuẩn kỹ thuật bên dưới chưa (đặc biệt là Layout, Navigation và Ràng buộc dữ liệu) trước khi xuất kết quả.

## 2. TIÊU CHUẨN KỸ THUẬT & GIAO DIỆN BẮT BUỘC (STRICT RULES)

### A. Kiến trúc & Quy tắc kinh doanh (Business Rules):
- **Kiến trúc:** Web-based, Client/Server, Database tập trung.
- **Vai trò (Roles):** Admin (Toàn quyền), Staff (Vận hành, Read-only báo cáo), Customer (Khách thuê).
- **Quy tắc:**
  - Thời gian thuê tối thiểu 24h.
  - Price Persistence (Tính bất biến của giá): Giá chốt lúc đặt đơn không đổi dù Admin đổi giá sau đó.
  - Dual-Inventory Sync: Quản lý kho khép kín (Sẵn sàng <-> Đang thuê <-> Hư hỏng).
  - Không có cổng thanh toán tự động. Chuyển khoản QR thủ công + Staff xác nhận.

### B. Trải nghiệm người dùng & Layout (Chống lỗi 404 & Giao diện mặc định):
- **Đồng bộ ViewStart:** BẮT BUỘC phải cấu hình `_ViewStart.cshtml` ở cả thư mục `Views` (Client) và `Areas/Admin/Views` (Admin) bằng **đường dẫn tuyệt đối** (VD: `Layout = "~/Views/Shared/_Layout.cshtml";` và `Layout = "~/Areas/Admin/Views/Shared/_AdminLayout.cshtml";`) để ghi đè hoàn toàn giao diện mặc định của MVC.
- **Thanh Điều hướng (Navigation Trực quan):** Cả 2 file Layout (Client và Admin) BẮT BUỘC phải có đầy đủ Menu dẫn đến các chức năng đã làm. Phải tích hợp `_LoginPartial` hoặc code hiển thị nút **Đăng nhập / Đăng ký / Đăng xuất** và Tên người dùng. Tuyệt đối không để người dùng phải gõ URL thủ công.
- **Area Admin:** Mọi Controller trong Area Admin PHẢI có thuộc tính `[Area("Admin")]`.
- **Đường dẫn Tuyệt đối:** Khai báo Layout trong View và `_ViewStart` PHẢI dùng đường dẫn bắt đầu bằng dấu `~` (VD: `Layout = "~/Areas/Admin/Views/Shared/_AdminLayout.cshtml";`).
- **Cấu trúc Thư mục:** Đảm bảo tạo thư mục `Shared` bên trong `Areas/Admin/Views` để chứa `_AdminLayout.cshtml`.
- **Chống lỗi 404:** BẮT BUỘC phải có một trang lỗi tùy chỉnh (VD: `Views/Shared/Error.cshtml`) để hiển thị khi người dùng truy cập URL không tồn tại hoặc gặp lỗi.
- **Menu Điều hướng:** Cả 2 Layout (Client và Admin) phải có menu điều hướng rõ ràng, dễ hiểu, và dẫn đến tất cả các chức năng đã triển khai. Menu phải hiển thị khác nhau dựa trên vai trò người dùng (VD: Admin mới thấy menu quản lý kho).
- **Đăng nhập/Đăng xuất:** Cả 2 Layout phải tích hợp `_LoginPartial` hoặc code hiển thị nút Đăng nhập / Đăng ký / Đăng xuất và Tên người dùng khi đã đăng nhập. Điều này giúp người dùng dễ dàng quản lý tài khoản của mình và đảm bảo trải nghiệm nhất quán trên toàn bộ ứng dụng.
- **Responsive Design:** BẮT BUỘC phải sử dụng Bootstrap để đảm bảo giao diện hiển thị tốt trên cả desktop và mobile.
- **Accessibility:** BẮT BUỘC phải tuân thủ các nguyên tắc về khả năng truy cập (Accessibility) để đảm bảo mọi người dùng đều có thể sử dụng ứng dụng một cách dễ dàng.

### C. Kiến trúc & Logic (Business Rules):
- **Quy tắc:** Thuê tối thiểu 24h; Tính bất biến của giá (Price Persistence); Quản lý kho khép kín (Dual-Inventory Sync); Chuyển khoản QR thủ công + Staff xác nhận.
- **ViewModel:** BẮT BUỘC tạo các lớp ViewModel để hiển thị/nhận dữ liệu kết hợp, không truyền trực tiếp Entity Model ra View.
- **Tính năng Danh sách (Pro Index):** Mọi danh sách quản trị phải có: (1) Tìm kiếm, (2) Phân trang, (3) Sắp xếp (Sort Tăng dần ASC / Giảm dần DESC).
- **Background Service:** BẮT BUỘC phải có một dịch vụ nền (Hosted Service) chạy định kỳ để tự động hủy đơn quá hạn và báo mất thiết bị.
- **PDF Generation:** BẮT BUỘC phải tích hợp thư viện tạo PDF (VD: iTextSharp) để xuất biên lai bàn giao thiết bị.
- **Security:** BẮT BUỘC phải sử dụng ASP.NET Core Identity để quản lý người dùng và phân quyền. Tất cả các trang Admin phải được bảo vệ bằng Authorize Attribute.
- **Error Handling:** BẮT BUỘC phải có cơ chế xử lý lỗi toàn cục (Global Error Handling) để tránh lỗi 404 và hiển thị trang lỗi tùy chỉnh.
- **Logging:** BẮT BUỘC phải tích hợp logging (VD: Serilog) để ghi lại các sự kiện quan trọng như đăng nhập, tạo đơn, cập nhật kho.
- **Unit Testing:** BẮT BUỘC phải viết Unit Test cho các lớp Service và Controller quan trọng để đảm bảo tính ổn định của ứng dụng.
- **Responsive Design:** BẮT BUỘC phải sử dụng Bootstrap để đảm bảo giao diện hiển thị tốt trên cả desktop và mobile.
- **Accessibility:** BẮT BUỘC phải tuân thủ các nguyên tắc về khả năng truy cập (Accessibility) để đảm bảo mọi người dùng đều có thể sử dụng ứng dụng một cách dễ dàng.
- **Performance Optimization:** BẮT BUỘC phải tối ưu hóa hiệu suất bằng cách sử dụng kỹ thuật như Lazy Loading, Caching, và tối ưu hóa truy vấn cơ sở dữ liệu.
- **Code Quality:** BẮT BUỘC phải tuân thủ các nguyên tắc về chất lượng mã nguồn như SOLID, DRY, và KISS để đảm bảo mã dễ đọc, dễ bảo trì và mở rộng trong tương lai.
- **Documentation:** BẮT BUỘC phải có tài liệu hướng dẫn sử dụng và triển khai ứng dụng để hỗ trợ người dùng và các nhà phát triển khác trong việc hiểu và sử dụng hệ thống một cách hiệu quả.
- **Deployment:** BẮT BUỘC phải có hướng dẫn triển khai ứng dụng lên môi trường sản xuất, bao gồm cấu hình máy chủ, cơ sở dữ liệu và các bước cần thiết để đảm bảo ứng dụng hoạt động ổn định sau khi triển khai.
- **Security Best Practices:** BẮT BUỘC phải tuân thủ các best practices về bảo mật như mã hóa dữ liệu nhạy cảm, sử dụng HTTPS, và bảo vệ chống lại các cuộc tấn công phổ biến như SQL Injection, Cross-Site Scripting (XSS), và Cross-Site Request Forgery (CSRF).
- **User Experience (UX):** BẮT BUỘC phải tập trung vào trải nghiệm người dùng bằng cách thiết kế giao diện thân thiện, dễ sử dụng và cung cấp phản hồi rõ ràng cho các hành động của người dùng.
- **Scalability:** BẮT BUỘC phải thiết kế hệ thống với khả năng mở rộng để có thể xử lý lượng người dùng và dữ liệu tăng lên trong tương lai mà không ảnh hưởng đến hiệu suất của ứng dụng.
- **Maintainability:** BẮT BUỘC phải đảm bảo mã nguồn dễ bảo trì bằng cách sử dụng cấu trúc rõ ràng, phân tách các thành phần một cách hợp lý và cung cấp tài liệu đầy đủ cho các nhà phát triển khác.
- **ViewModel:** PHẢI tạo các lớp ViewModel để hiển thị và nhận dữ liệu kết hợp, không truyền trực tiếp Entity Model ra View.
- **Tính năng Danh sách (Pro Index):** Triển khai cho tất cả các bảng quản trị:
    1. **Tìm kiếm (Search):** Theo tên hoặc thuộc tính liên quan.
    2. **Phân trang (Pagination):** Chia nhỏ kết quả tìm kiếm cho danh sách.
    3. **Sắp xếp (Sorting):** Cho phép sắp xếp theo một thuộc tính cụ thể (tên, giá, ngày...), hỗ trợ cả **Tăng dần (ASC)** và **Giảm dần (DESC)**

### D. Mô hình Dữ liệu (EF Core Code First):
- **Quan hệ 1-N:** Thiết lập khóa ngoại (Foreign Key) và bắt buộc có thuộc tính điều hướng (`virtual`) ở các lớp.
- **Data Annotations:** Dùng đầy đủ `[Required]`, `[StringLength]`, `[Range]`, `[Display]`.
- **Navigation Properties:** Cấu hình thuộc tính điều hướng (`virtual`) trong mỗi lớp để hỗ trợ truy xuất quan hệ thuận tiện
- **Code First:** Tạo Cơ sở dữ liệu từ các lớp thực thể đã xây dựng.

## 3. CẤU TRÚC DATABASE CHI TIẾT
1. **AppUser:** IdentityUser + FullName, Address, CccdNumber. (Navigation: `ICollection<RentalOrder>`).
2. **Category (1):** Id, Name, Description. (Navigation: `ICollection<Equipment>`).
3. **Equipment (N):** Id, Name, CategoryId (FK), PricePerDay, Status (Available, Rented, Broken, Maintenance), ConditionNotes. (Navigation: `Category`).
4. **RentalOrder:** Id, CustomerId (FK), TotalAmount, DepositAmount, PenaltyAmount, OrderStatus (PendingPayment, Deposited, Rented, Completed, Cancelled, Lost), ExpectedPickUpTime, ExpectedReturnTime, ActualReturnTime. (Navigation: `Customer`, `Details`, `Comments`).
5. **RentalOrderDetail:** Id, OrderId (FK), EquipmentId (FK), UnitPriceAtBooking (Quy tắc Price Persistence).
6. **OrderComment:** Id, OrderId (FK), AuthorId, Content, ImageUrl (UNC/Biên lai), CreatedDate.
7. **MaintenanceLog:** Id, EquipmentId (FK), Description, StartDate, EndDate.

## 4. LỘ TRÌNH TRIỂN KHAI (ROADMAP)
*Chỉ thực hiện MỘT BƯỚC DUY NHẤT khi được yêu cầu. Làm xong phải tự rà soát lỗi rồi mới dừng lại chờ lệnh tiếp theo.*

**Phase 1: Khởi tạo & Cơ sở dữ liệu**
- **[Bước 1]:** Tạo các lớp Model (Entities) dựa trên phần 3 với đầy đủ Data Annotations và quan hệ 1-N (Navigation Properties).
- **[Bước 2]:** Cấu hình `ApplicationDbContext` (EF Core) và Fluent API. Seed data tài khoản Admin mặc định và 3 Roles ("Admin", "Staff", "Customer").
- **[Bước 3]:** Cấu hình Logic Đăng ký & Phân quyền (ASP.NET Core Identity): Mặc định đăng ký ngoài là "Customer". Tạo `UserController` trong Area Admin để tạo tài khoản/cấp quyền Staff/Admin.

**Phase 2: Giao diện Khách hàng (Customer Journey)**
- **[Bước 4]:** Tạo chức năng hiển thị danh sách thiết bị, lọc và xem chi tiết (kèm hiển thị lịch trống). (Ghi nhớ: Cập nhật menu Layout).
- **[Bước 5]:** Xây dựng Giỏ hàng (Cart) lưu bằng Session.
- **[Bước 6]:** Xây dựng chức năng Checkout: Chọn ngày giờ (>24h), tạo `RentalOrder` (PendingPayment) và tạo mã QR.
- **[Bước 7]:** Trang "Đơn thuê của tôi" nổi bật khi đăng nhập. Có chức năng `OrderComment` để khách upload ảnh biên lai.

**Phase 3: Giao diện & Vận hành của Staff (Staff Journey - Thuộc Area Admin)**
- **[Bước 8]:** Khởi tạo Area "Admin". Xây dựng `_AdminLayout.cshtml` (Bootstrap, Sidebar menu chuẩn, có nút Đăng xuất) và `_ViewStart.cshtml` (Đường dẫn tuyệt đối).
- **[Bước 9]:** Trang Quản lý đơn hàng (Staff): Bảng có bộ lọc trạng thái; Trang chi tiết duyệt ảnh biên lai -> Nút "Xác nhận cọc".
- **[Bước 10]:** Giao diện Bàn giao & Trả máy: Trang Handover (Nhập tiền cọc/CCCD, xuất PDF); Trang Check-in (Tự động tính Phạt trễ hạn, nút "Hoàn cọc").

**Phase 4: Giao diện Quản trị của Admin (Admin Journey - Thuộc Area Admin)**
- **[Bước 11]:** Dashboard tổng quan (Admin only): Thống kê doanh thu, số đơn, máy hỏng.
- **[Bước 12]:** Quản lý Kho (Inventory): CRUD Category và Equipment. Dùng ViewModel, tích hợp Tìm kiếm, Phân trang, Sắp xếp (ASC/DESC).
- **[Bước 13]:** Tự động hóa: Background Service (Lazy Cleanup) hủy đơn quá hạn, báo mất thiết bị.