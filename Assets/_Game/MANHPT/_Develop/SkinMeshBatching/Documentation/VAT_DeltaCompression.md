# Tối ưu dung lượng VAT bằng phương pháp nén Delta

## Tổng quan

Vertex Animation Texture (VAT) là một kỹ thuật hiệu quả để render nhiều nhân vật có animation trên thiết bị di động. Tuy nhiên, texture VAT có thể chiếm nhiều dung lượng (hiện tại khoảng 400KB cho 1 bot với 5 animation). Tài liệu này mô tả phương pháp nén Delta để giảm dung lượng VAT xuống dưới 100KB mà vẫn duy trì chất lượng animation.

## Nguyên lý nén Delta

Phương pháp nén Delta dựa trên quan sát rằng trong animation, các vertex thường di chuyển với biên độ nhỏ giữa các frame liên tiếp. Thay vì lưu trữ vị trí tuyệt đối của mỗi vertex tại mỗi frame, chúng ta chỉ lưu trữ:

1. **Keyframe (frame đầu tiên)**: Lưu trữ vị trí tuyệt đối của tất cả vertex
2. **Delta frame (các frame còn lại)**: Lưu trữ sự thay đổi (delta) vị trí so với frame trước đó

## Cấu trúc dữ liệu

Cấu trúc texture VAT vẫn giữ nguyên với:
- Trục X: Các vertex của mesh
- Trục Y: Các frame của animation

Tuy nhiên, cách lưu trữ dữ liệu sẽ thay đổi:
- **Hàng đầu tiên (Y=0)**: Keyframe với định dạng RGB24 (24 bit/pixel)
- **Các hàng còn lại**: Delta frame với định dạng RGB565 (16 bit/pixel)

## Quy trình nén

### 1. Chuẩn bị dữ liệu
- Tính toán vị trí của tất cả vertex tại tất cả frame
- Xác định bounding box cho animation
- Xác định giá trị maxDelta (giá trị cố định, ví dụ: 0.1)

### 2. Tạo keyframe (RGB24)
- Lấy frame đầu tiên làm keyframe
- Chuẩn hóa vị trí vertex vào khoảng [0,1] dựa trên bounding box
- Lưu trữ với định dạng RGB24 (8 bit cho mỗi kênh R, G, B)

### 3. Tính toán và nén delta frame (RGB565)
- Với mỗi frame tiếp theo, tính delta so với frame trước đó
- Chuẩn hóa delta vào khoảng [0,1] dựa trên maxDelta:
  - Delta chuẩn hóa = Delta / (2 * maxDelta) + 0.5
- Lưu trữ với định dạng RGB565:
  - 5 bit cho delta X (kênh R)
  - 6 bit cho delta Y (kênh G)
  - 5 bit cho delta Z (kênh B)

### 4. Áp dụng Run-Length Encoding (RLE) (tùy chọn)
- Xác định ngưỡng delta nhỏ (ví dụ: 0.001)
- Phân loại vertex thành "delta nhỏ" và "delta lớn"
- Áp dụng RLE để nén thêm các đoạn vertex có delta nhỏ

### 5. Đóng gói thành mảng byte
- Đóng gói keyframe (RGB24) và delta frame (RGB565) thành một mảng byte liên tục
- Cấu trúc mảng byte:
  - Header (thông tin về số frame, số vertex)
  - Dữ liệu keyframe (3 byte/vertex)
  - Dữ liệu delta frame (2 byte/vertex hoặc đã nén RLE)

### 6. Lưu trữ
- Lưu mảng byte vào ScriptableObject như hiện tại

## Quy trình giải nén

### 1. Đọc dữ liệu
- Đọc mảng byte từ ScriptableObject

### 2. Giải nén keyframe
- Đọc phần dữ liệu keyframe (3 byte/vertex)
- Chuyển đổi thành vị trí vertex chuẩn hóa [0,1]

### 3. Giải nén delta frame
- Đọc phần dữ liệu delta frame (2 byte/vertex hoặc đã nén RLE)
- Nếu sử dụng RLE, giải nén RLE trước
- Chuyển đổi từ định dạng RGB565 về giá trị delta chuẩn hóa [0,1]
- Dequantize delta: Delta thực = (Delta chuẩn hóa - 0.5) * 2 * maxDelta

### 4. Tính toán vị trí vertex cho mỗi frame
- Frame 0 (keyframe): Sử dụng vị trí đã giải nén trực tiếp
- Frame 1+: Vị trí = Vị trí frame trước + Delta

### 5. Tạo texture VAT
- Tạo texture với định dạng RGB24
- Điền dữ liệu vị trí vertex đã tính toán vào texture
- Texture này có cấu trúc giống như VAT truyền thống (không phải delta)

### 6. Sử dụng trong runtime
- Shader hoạt động như bình thường, không cần thay đổi
- Shader chỉ cần đọc texture và áp dụng bounding box

## Định dạng RGB565

RGB565 là một định dạng màu sử dụng tổng cộng 16 bit để biểu diễn một pixel, phân bổ như sau:
- 5 bit cho kênh Red (đỏ) - 32 mức
- 6 bit cho kênh Green (xanh lá) - 64 mức
- 5 bit cho kênh Blue (xanh dương) - 32 mức

So với định dạng RGB24 (8 bit cho mỗi kênh, tổng 24 bit), RGB565 sử dụng ít hơn 8 bit cho mỗi pixel, giúp giảm dung lượng xuống còn 2/3 so với ban đầu.

**Ưu điểm:**
- Giảm dung lượng đáng kể (16 bit thay vì 24 bit)
- Vẫn giữ được độ chính xác tương đối tốt, đặc biệt là cho kênh Green

**Nhược điểm:**
- Giảm độ chính xác: Thay vì 256 mức (8 bit), chỉ còn 32 mức (5 bit) cho Red và Blue, 64 mức (6 bit) cho Green

Trong bối cảnh VAT, việc giảm độ chính xác này có thể chấp nhận được cho delta frame vì delta thường có giá trị nhỏ.

## Run-Length Encoding (RLE)

Run-Length Encoding (RLE) là một kỹ thuật nén dữ liệu đơn giản nhưng hiệu quả, đặc biệt với dữ liệu có nhiều giá trị lặp lại liên tiếp. Nguyên lý cơ bản của RLE là thay vì lưu trữ mỗi giá trị riêng lẻ, chúng ta lưu trữ cặp (số lượng, giá trị).

### Nguyên lý hoạt động của RLE

1. **Xác định các đoạn lặp lại (run)**: Tìm các giá trị giống nhau xuất hiện liên tiếp
2. **Mã hóa**: Thay thế mỗi đoạn lặp lại bằng cặp (số lượng, giá trị)

### Áp dụng RLE cho nén delta trong VAT

Trong animation, nhiều vertex có thể không di chuyển hoặc di chuyển rất ít giữa các frame liên tiếp. Điều này tạo ra nhiều delta bằng hoặc gần bằng 0. RLE có thể tận dụng đặc điểm này để nén thêm dữ liệu.

#### Cách áp dụng RLE cho delta frame:

1. **Xác định ngưỡng delta nhỏ**: Ví dụ, delta có giá trị tuyệt đối nhỏ hơn 0.001 được coi là "delta nhỏ"

2. **Phân loại vertex**:
   - Vertex có delta nhỏ: Coi như delta = 0
   - Vertex có delta lớn: Lưu trữ delta đầy đủ

3. **Mã hóa RLE**:
   - Thay vì lưu trữ delta cho từng vertex, lưu trữ:
     - Số lượng vertex liên tiếp có delta nhỏ
     - Sau đó là số lượng vertex liên tiếp có delta lớn
     - Tiếp theo là giá trị delta cho các vertex có delta lớn

#### Ưu điểm của RLE trong nén delta:

1. **Giảm dung lượng đáng kể**: Đặc biệt hiệu quả với animation có nhiều vertex ít di chuyển
2. **Đơn giản để triển khai**: Thuật toán RLE rất đơn giản
3. **Nén không mất dữ liệu**: Không làm giảm độ chính xác của animation

## Ước tính mức giảm dung lượng

Giả sử chúng ta có một model với 1000 vertex và animation 30 frame:

### Không sử dụng nén:
- Định dạng RGB24: 1000 vertex × 30 frame × 3 byte = 90,000 byte

### Sử dụng nén delta với keyframe RGB24 và delta frame RGB565:
- Keyframe (RGB24): 1000 vertex × 1 frame × 3 byte = 3,000 byte
- Delta frame (RGB565): 1000 vertex × 29 frame × 2 byte = 58,000 byte
- Tổng: 61,000 byte
- Mức giảm: 32%

### Sử dụng nén delta + RLE:
- Giả sử 50% vertex có delta nhỏ:
  - Keyframe (RGB24): 3,000 byte
  - Delta frame với RLE: ~29,000 byte
  - Tổng: ~32,000 byte
  - Mức giảm: ~64%

## Tích hợp với hệ thống hiện tại

Phương pháp nén delta có thể tích hợp với hệ thống VAT hiện tại mà không cần thay đổi shader. Chỉ cần thay đổi:

1. **Hàm nén (khi tạo mảng byte)**:
   - Thay đổi cách nén dữ liệu từ vị trí tuyệt đối sang keyframe + delta
   - Áp dụng định dạng RGB565 cho delta frame
   - Tùy chọn áp dụng RLE

2. **Hàm giải nén (khi chuyển từ mảng byte về texture)**:
   - Giải nén keyframe và delta frame
   - Tính toán vị trí vertex cho mỗi frame
   - Tạo texture VAT truyền thống

Phần còn lại của hệ thống (shader, animation controller, v.v.) có thể giữ nguyên.

## Kết luận

Phương pháp nén delta với keyframe RGB24 và delta frame RGB565, kết hợp với RLE, là một giải pháp hiệu quả để giảm dung lượng VAT mà vẫn duy trì chất lượng animation tốt. Phương pháp này tương thích với hệ thống hiện tại, chỉ cần thay đổi cách nén và giải nén dữ liệu, không cần thay đổi shader.

Với mức giảm dung lượng từ 32% đến 64% (tùy thuộc vào việc có sử dụng RLE hay không), phương pháp này có thể giúp đạt được mục tiêu giảm dung lượng xuống dưới 100KB cho mỗi bot với 5 animation. 