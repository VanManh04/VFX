# Vertex Animation Texture (VAT) System Documentation

## Giới thiệu

Đây là tài liệu chi tiết về hệ thống Vertex Animation Texture (VAT) được phát triển cho game FPS trên thiết bị di động cấu hình thấp. Hệ thống này nhằm thay thế SkinnedMeshRenderer truyền thống để tối ưu hiệu năng khi render nhiều nhân vật có animation cùng lúc.

## Cấu trúc tài liệu

Tài liệu được chia thành các phần sau:

1. [Tổng quan về VAT](./VAT_Overview.md) - Giới thiệu về Vertex Animation Texture, nguyên lý hoạt động và lợi ích
2. [Kiến trúc hệ thống](./System_Architecture.md) - Mô tả chi tiết về cấu trúc và các thành phần của hệ thống
3. [Hệ thống Cache](./Cache_System.md) - Giải thích về các loại cache dữ liệu được sử dụng trong hệ thống
4. [Shader và Rendering](./Shader_Rendering.md) - Chi tiết về các shader và quy trình rendering
5. [Hệ thống Collider](./Collider_System.md) - Mô tả về hệ thống collider tùy chỉnh
6. [Ragdoll với Verlet Integration](./Ragdoll_System.md) - Kế hoạch tích hợp ragdoll dựa trên Verlet Integration
7. [Tối ưu hóa hiệu năng](./Performance_Optimization.md) - Các phương pháp tối ưu hiệu năng đã và sẽ áp dụng
8. [Roadmap phát triển](./Development_Roadmap.md) - Kế hoạch phát triển ngắn hạn, trung hạn và dài hạn

## Mục tiêu của hệ thống

- Tối ưu hiệu năng render trên thiết bị di động cấu hình thấp (2-3GB RAM)
- Hỗ trợ số lượng lớn nhân vật có animation (lên đến 1000 bot với 30 FPS)
- Giảm thiểu dung lượng dữ liệu animation (mục tiêu dưới 100KB hoặc 50KB cho mỗi bot với 5 animation)
- Hỗ trợ các tính năng animation phức tạp như BlendTree, AnimationEvent, RootMotion
- Tích hợp hệ thống vật lý và ragdoll hiệu quả

## Hiện trạng

Hiện tại, hệ thống đã có thể:
- Render lên đến 1000 bot với 30 FPS trên thiết bị di động
- Hỗ trợ chuyển đổi animation với cross fade
- Tích hợp hệ thống collider tùy chỉnh để phát hiện va chạm
- Tối ưu hóa dung lượng từ 2MB xuống còn 400KB cho mỗi bot với 5 animation

## Thách thức và kế hoạch

- Tiếp tục tối ưu dung lượng texture VAT (mục tiêu dưới 100KB)
- Phát triển các tính năng BlendTree, AnimationEvent, RootMotion
- Tích hợp ragdoll với Verlet Integration
- Tái cấu trúc code để cải thiện hiệu năng và khả năng mở rộng 