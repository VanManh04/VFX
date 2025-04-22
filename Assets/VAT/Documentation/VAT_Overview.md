# Tổng quan về Vertex Animation Texture (VAT)

## Vấn đề với animation truyền thống

Trong game 3D, đặc biệt là game FPS với nhiều nhân vật, animation truyền thống sử dụng SkinnedMeshRenderer gặp phải một số vấn đề:

1. **Hiệu năng thấp trên thiết bị di động**:
   - SkinnedMeshRenderer tạo nhiều draw call
   - Animator và GPU processing tốn nhiều tài nguyên
   - Việc tính toán skinning (biến dạng mesh theo skeleton) tốn nhiều CPU

2. **Giới hạn số lượng nhân vật**:
   - Thiết bị di động cấu hình thấp (2-3GB RAM) không thể xử lý nhiều nhân vật cùng lúc
   - Mỗi SkinnedMeshRenderer yêu cầu một draw call riêng, gây áp lực lên CPU và GPU

## Giải pháp: Vertex Animation Texture (VAT)

Vertex Animation Texture (VAT) là một kỹ thuật lưu trữ animation dưới dạng texture, cho phép GPU xử lý animation thay vì CPU. Thay vì tính toán vị trí của mỗi vertex dựa trên skeleton và weight, VAT lưu trữ sẵn vị trí của mỗi vertex tại mỗi frame của animation.

### Nguyên lý hoạt động

1. **Baking Animation vào Texture**:
   - Mỗi animation được "bake" thành một texture 2D
   - Hàng ngang (trục X) của texture đại diện cho các vertex của mesh
   - Hàng dọc (trục Y) của texture đại diện cho các frame của animation
   - Mỗi pixel lưu trữ thông tin vị trí (RGB) của một vertex tại một frame cụ thể

2. **UV đặc biệt**:
   - Mỗi vertex được gán một UV đặc biệt để sample đúng vị trí trên texture VAT
   - UV.x: vị trí của vertex trên texture VAT (hàng ngang)
   - UV.y: được cập nhật động để đại diện cho frame hiện tại (hàng dọc)

3. **Shader xử lý**:
   - Shader đọc vị trí của vertex từ texture VAT dựa trên UV đặc biệt
   - Shader tính toán vị trí cuối cùng của vertex dựa trên bounding box và các thông số khác
   - Không cần CPU tính toán skinning, giảm tải cho CPU

### Quy trình hoạt động

1. **Preprocessing (Editor time)**:
   - Bake animation từ SkinnedMeshRenderer thành texture VAT
   - Tạo UV đặc biệt cho mesh
   - Tính toán bounding box cho mỗi animation

2. **Runtime**:
   - Animation Controller cập nhật frame hiện tại
   - Shader sample texture VAT để lấy vị trí vertex
   - Mesh được render với vị trí vertex đã được cập nhật
   - Collider được cập nhật đồng bộ với vị trí vertex

## Lợi ích của VAT

1. **Hiệu năng cao hơn**:
   - Giảm đáng kể số lượng draw call
   - Chuyển gánh nặng tính toán từ CPU sang GPU
   - Có thể render nhiều nhân vật hơn với cùng tài nguyên

2. **Hỗ trợ thiết bị cấu hình thấp**:
   - Phù hợp với thiết bị di động cấu hình thấp (2-3GB RAM)
   - Có thể render lên đến 1000 bot với 30 FPS

3. **Tính linh hoạt**:
   - Hỗ trợ cross fade giữa các animation
   - Có thể mở rộng để hỗ trợ BlendTree, AnimationEvent, RootMotion
   - Tích hợp được với hệ thống vật lý và ragdoll tùy chỉnh

## Thách thức

1. **Dung lượng texture**:
   - Texture VAT có thể chiếm nhiều dung lượng (hiện tại khoảng 400KB cho 1 bot với 5 animation)
   - Cần các phương pháp tối ưu để giảm dung lượng (mục tiêu dưới 100KB)

2. **Độ chính xác**:
   - Nén texture có thể làm sai lệch vị trí vertex, ảnh hưởng đến chất lượng animation
   - Cần cân bằng giữa dung lượng và chất lượng

3. **Tính năng phức tạp**:
   - Việc triển khai BlendTree, AnimationEvent, RootMotion với VAT đòi hỏi nhiều công sức
   - Cần thiết kế cẩn thận để đảm bảo hiệu năng và tính mở rộng 