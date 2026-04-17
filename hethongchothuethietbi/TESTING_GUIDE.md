# 🧪 HƯỚNG DẪN KIỂM TRA CÁC LỖI ĐÃ SỬA

## 1️⃣ Kiểm Tra Hiển Thị Giá VNĐ

**Bước 1:** Chạy ứng dụng
```bash
dotnet run
```

**Bước 2:** Vào trang danh sách thiết bị
- URL: `https://localhost:7281/Equipment/Index`
- Kiểm tra: Giá hiển thị dạng `500,000 VNĐ` (không phải `$500,000.00`)

**Bước 3:** Vào giỏ hàng
- URL: `https://localhost:7281/Cart/Index`
- Kiểm tra: Giá hiển thị `500,000 VNĐ` thay vì `C` format

**Bước 4:** Vào Admin Equipment
- URL: `https://localhost:7281/Admin/Equipment/Index`
- Kiểm tra: Cột "Số Lượng" hiển thị và giá là VNĐ

---

## 2️⃣ Kiểm Tra AJAX Thêm Giỏ

**Bước 1:** Đăng nhập với tài khoản Customer
- Email: `customer@example.com`
- Password: (nhập mật khẩu)

**Bước 2:** Vào Equipment/Index
- URL: `https://localhost:7281/Equipment/Index`

**Bước 3:** Click "Thêm Vào Giỏ"
- ✅ **Kỳ vọng:** 
  - Button đổi màu xanh
  - Hiển thị text "Đã Thêm!"
  - Quay lại bình thường sau 2 giây
  - **KHÔNG reload trang**

**Bước 4:** Kiểm tra Console
- F12 → Network tab
- Click "Thêm Vào Giỏ" → Kiểm tra request
- ✅ POST đến `/Cart/AddToCart` 
- ✅ Response là JSON: `{"success": true, "cartCount": 1}`

---

## 3️⃣ Kiểm Tra Checkout & Validation

**Bước 1:** Thêm 1-2 thiết bị vào giỏ

**Bước 2:** Vào Giỏ Hàng
- URL: `https://localhost:7281/Cart/Index`
- Click "Tiến Hành Thanh Toán"

**Bước 3:** Checkout View
- ✅ Hiển thị bảng danh sách thiết bị từ cart
- ✅ Input datetime-local cho ngày bắt đầu/kết thúc
- ✅ Bên phải hiển thị "Tóm Tắt Đơn Hàng" sticky

**Bước 4:** Test Validation
- Chọn ngày kết thúc = ngày bắt đầu
- Click "Xác Nhận Đơn Hàng"
- ✅ Kỳ vọng: Lỗi "Thời gian thuê phải tối thiểu 24 giờ!"

**Bước 5:** Test JavaScript Tính Toán
- Chọn ngày bắt đầu: ngày hôm nay 10:00
- Chọn ngày kết thúc: ngày hôm sau 14:00 (28 giờ)
- ✅ Kỳ vọng:
  - "Số Ngày Thuê": 2 ngày
  - "Tổng Tiền" cập nhật theo công thức: `GiáNgày × SốNgày × SốLượng`

---

## 4️⃣ Kiểm Tra Giảm Quantity Khi Đặt Đơn

**Chuẩn bị:**
- Thiết bị A: Quantity = 5

**Bước 1:** Thêm 3 cái thiết bị A vào giỏ

**Bước 2:** Checkout → Xác nhận

**Bước 3:** Vào Admin Equipment
- URL: `https://localhost:7281/Admin/Equipment/Index`
- Kiểm tra thiết bị A:
- ✅ Kỳ vọng: Quantity = 2 (5 - 3 = 2)

**Bước 4:** Thêm 2 cái thiết bị A vào giỏ (đặt đơn lần 2)

**Bước 5:** Kiểm tra lại
- ✅ Kỳ vọng: Quantity = 0, Status = Rented

---

## 5️⃣ Kiểm Tra Admin Phê Duyệt & Update Kho

**Bước 1:** Đăng nhập Admin

**Bước 2:** Vào Admin Order
- URL: `https://localhost:7281/Admin/Order/Index`
- Kiểm tra đơn hàng "PendingPayment"

**Bước 3:** Click vào chi tiết đơn
- Hiển thị: Danh sách thiết bị + Tổng tiền

**Bước 4:** Click "Phê Duyệt Cọc"
- ✅ Kỳ vọng:
  - Status thay đổi: `PendingPayment` → `Deposited`
  - Vào Equipment/Index kiểm tra:
    - Quantity giảm theo số lượng trong order
    - Status = Rented nếu Quantity = 0

**Bước 5:** Click "Bàn Giao Thiết Bị"
- Nhập CCCD + xác nhận
- ✅ Status: `Deposited` → `Rented`

**Bước 6:** Click "Nhận Lại Thiết Bị"
- Tính phạt (nếu quá hạn)
- ✅ Status: `Rented` → `Completed`
- ✅ Equipment.Status = Available
- ✅ Equipment.Quantity quay lại số lượng ban đầu

---

## 6️⃣ Kiểm Tra Khả Năng Đặt Đơn

**Bước 1:** Đăng nhập Customer

**Bước 2:** Equipment → Thêm vào giỏ (ít nhất 1 cái)

**Bước 3:** Giỏ Hàng → Tiến Hành Thanh Toán

**Bước 4:** Checkout
- Chọn ngày >= 24 giờ
- Tích "Đồng ý điều khoản"
- Click "Xác Nhận Đơn Hàng"

**Bước 5:** Kỳ vọng
- ✅ Chuyển đến trang OrderDetail
- ✅ Hiển thị chi tiết đơn hàng + mã QR
- ✅ Cart được xóa (trống)

---

## 📋 CHECKLIST KIỂM TRA

```
[ ] Giá hiển thị VNĐ (không $)
[ ] Admin Equipment hiển thị Quantity
[ ] Thêm giỏ AJAX (không reload)
[ ] Checkout form có model & JavaScript
[ ] Validation 24 giờ
[ ] Quantity giảm khi đặt order
[ ] Admin phê duyệt → Update Equipment
[ ] Bàn giao → Status = Rented
[ ] Nhận lại → Status = Available, Quantity quay lại
[ ] Toàn bộ flow đặt đơn hoạt động
```

---

## 🐛 NẾUÇÓ LỖI

### Lỗi: AJAX không hoạt động
- Kiểm tra Console (F12 → Console)
- Kiểm tra Network → Response status
- Đảm bảo `ValidateAntiForgeryToken` trong CartController

### Lỗi: Giỏ không cập nhật
- Kiểm tra Session Configuration trong Program.cs
- Đảm bảo `AddSession()` và `UseSession()` được gọi

### Lỗi: Validation 24 giờ không hoạt động
- Kiểm tra: `(model.ExpectedReturnTime - model.ExpectedPickUpTime).TotalHours < 24`

### Lỗi: Database không cập nhật
- Chạy: `dotnet ef database update`
- Kiểm tra migration: `Migrations/20260417160832_AddQuantityToRentalOrderDetail.cs`

---

## 📞 LIÊN HỆ HỖ TRỢ

Nếu có vấn đề, kiểm tra:
1. Build log: `dotnet build`
2. Migration log: `dotnet ef migrations list`
3. Database schema: SQL Server Management Studio
4. Browser Console: F12 → Console tab
