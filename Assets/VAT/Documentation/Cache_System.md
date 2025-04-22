# Hệ thống Cache trong VAT

Hệ thống Vertex Animation Texture (VAT) sử dụng nhiều loại cache khác nhau để tối ưu hóa hiệu năng và quản lý dữ liệu animation. Tài liệu này mô tả chi tiết về các loại cache được sử dụng trong hệ thống.

## Các loại Cache

Hệ thống VAT sử dụng các loại cache sau:

1. **VertexCacheData**: Lưu trữ dữ liệu vertex cho mỗi frame của animation
2. **ColliderCacheData**: Lưu trữ dữ liệu collider cho mỗi frame của animation
3. **StateInforsCache**: Lưu trữ thông tin về các trạng thái animation
4. **CylinderCacheData**: Lưu trữ dữ liệu hình trụ cho collider đơn giản hóa
5. **AnimEventCache**: Lưu trữ thông tin về các animation event

## VertexCacheData

VertexCacheData là loại cache chính trong hệ thống VAT, lưu trữ dữ liệu vertex cho mỗi frame của animation.

### Cấu trúc dữ liệu

```csharp
public class VertexCacheData : ScriptableObject
{
    public List<StateInfo> StateInfors;
    
    [Serializable]
    public class StateInfo
    {
        public List<FrameInfo> frameInfos;
        public Texture2D texture;
        public byte[] bytes;
        public VAT_Utilities.AnimationInfo animationInfo;
        public ModelBoundingBox boundingBox;
        public string stateName;
        public int frameCount;
    }
    
    [Serializable]
    public class FrameInfo
    {
        public Vector3[] vertices;
    }
}
```

### Quy trình cache

1. Bake animation: Chạy animation và lấy dữ liệu vertex cho mỗi frame
2. Tính toán bounding box cho mỗi animation
3. Chuyển đổi dữ liệu vertex thành texture
4. Lưu texture dưới dạng byte array để giảm kích thước

### Sử dụng trong runtime

1. Khởi tạo texture từ byte array
2. Sử dụng texture trong shader để lấy vị trí vertex cho mỗi frame
3. Áp dụng bounding box để khôi phục vị trí vertex chính xác

## ColliderCacheData

ColliderCacheData lưu trữ dữ liệu collider cho mỗi frame của animation, cho phép hệ thống vật lý hoạt động chính xác với các mesh được animation.

### Cấu trúc dữ liệu

```csharp
public class ColliderCacheData : ScriptableObject
{
    public List<StateInfo> StateInfors;
    
    [Serializable]
    public class StateInfo
    {
        public string stateName;
        public VAT_Utilities.AnimationInfo animationInfo;
        public List<FrameInfo> frameInfos;
    }
    
    [Serializable]
    public class FrameInfo
    {
        public List<ColliderInfo> colliderInfos;
    }
    
    [Serializable]
    public class ColliderInfo
    {
        public Vector3 center;
        public Vector3 size;
        public Quaternion rotation;
    }
}
```

### Quy trình cache

1. Bake animation: Chạy animation và lấy dữ liệu collider cho mỗi frame
2. Lưu trữ vị trí, kích thước và góc quay của mỗi collider
3. Tối ưu hóa dữ liệu để giảm kích thước

### Sử dụng trong runtime

1. Lấy dữ liệu collider cho frame hiện tại
2. Cập nhật vị trí, kích thước và góc quay của các collider
3. Sử dụng cho việc phát hiện va chạm

## StateInforsCache

StateInforsCache lưu trữ thông tin về các trạng thái animation, bao gồm tên, thời lượng và frame rate.

### Cấu trúc dữ liệu

```csharp
public class StateInforsData : ScriptableObject
{
    public List<StateInfo> StateInfors;
    
    [Serializable]
    public struct StateInfo
    {
        public string stateName;
        public VAT_Utilities.AnimationInfo animationInfo;
    }
}
```

### Quy trình cache

1. Lấy thông tin về các trạng thái animation từ Animator
2. Lưu trữ tên, thời lượng và frame rate cho mỗi trạng thái

### Sử dụng trong runtime

1. Lấy thông tin về trạng thái animation hiện tại
2. Sử dụng thông tin này để tính toán frame hiện tại và cập nhật animation

## CylinderCacheData

CylinderCacheData lưu trữ dữ liệu hình trụ đơn giản hóa cho collider, giúp tối ưu hóa việc phát hiện va chạm.

### Cấu trúc dữ liệu

```csharp
public class CylinderCacheData : ScriptableObject
{
    public List<StateInfo> StateInfors;
    
    [Serializable]
    public class StateInfo
    {
        public string stateName;
        public VAT_Utilities.AnimationInfo animationInfo;
        public List<FrameInfo> frameInfos;
    }
    
    [Serializable]
    public class FrameInfo
    {
        public List<CylinderInfo> cylinderInfos;
    }
    
    [Serializable]
    public class CylinderInfo
    {
        public Vector3 start;
        public Vector3 end;
        public float radius;
    }
}
```

### Quy trình cache

1. Bake animation: Chạy animation và tính toán hình trụ đơn giản hóa cho mỗi phần của mesh
2. Lưu trữ điểm đầu, điểm cuối và bán kính của mỗi hình trụ
3. Tối ưu hóa dữ liệu để giảm kích thước

### Sử dụng trong runtime

1. Lấy dữ liệu hình trụ cho frame hiện tại
2. Sử dụng cho việc phát hiện va chạm đơn giản hóa
3. Kết hợp với ColliderCacheData để có hệ thống vật lý chính xác và hiệu quả

## AnimEventCache

AnimEventCache lưu trữ thông tin về các animation event, cho phép kích hoạt các hành động tại các thời điểm cụ thể trong animation.

### Cấu trúc dữ liệu

```csharp
public class AnimationEventCacheData : ScriptableObject
{
    public List<StateInfor> StateInfors;
    
    [Serializable]
    public struct StateInfor
    {
        public string StateName;
        public VAT_Utilities.AnimationInfo AnimationInfo;
        public List<AnimationEventInfor> AnimationEvents;
    }
    
    [Serializable]
    public struct AnimationEventInfor
    {
        public string FunctionName;
        public float Time;
        public UnityEvent OnEvent;
    }
}
```

### Quy trình cache

1. Lấy thông tin về các animation event từ animation clip gốc
2. Lưu trữ tên hàm, thời điểm và các tham số cho mỗi event
3. Tối ưu hóa dữ liệu để giảm kích thước

### Phương án Event Queue cho Animation Event

Để tối ưu hiệu năng khi xử lý animation event, hệ thống VAT sử dụng phương án Event Queue. Phương án này sắp xếp các event theo thứ tự thời gian và chỉ xử lý khi đến thời điểm thích hợp.

#### Cấu trúc dữ liệu

1. **Event Queue**: Một danh sách các event đã được sắp xếp theo thứ tự frame
2. **ScheduledEvent**: Cấu trúc dữ liệu lưu trữ thông tin về một event đã lên lịch
3. **Next Event Index**: Chỉ số của event tiếp theo cần kích hoạt

#### Quy trình chi tiết

1. **Khởi tạo khi chuyển trạng thái animation**:
   - Lấy thông tin về trạng thái mới từ AnimationEventCacheData
   - Xóa hàng đợi event hiện tại
   - Chuyển đổi tất cả các event từ định dạng thời gian (giây) sang định dạng frame
   - Thêm các event vào hàng đợi
   - Sắp xếp hàng đợi theo thứ tự tăng dần của frame
   - Đặt lại chỉ số event tiếp theo về 0

2. **Cập nhật trong mỗi frame**:
   - Kiểm tra xem có event nào cần kích hoạt không
   - Nếu có, kích hoạt tất cả các event đến hạn và cập nhật chỉ số

3. **Kích hoạt event**:
   - Gọi UnityEvent đã đăng ký (nếu có)
   - Gọi hàm tương ứng thông qua SendMessage (nếu có tên hàm)

#### Ưu điểm của phương án

1. **Hiệu quả**: Chỉ kiểm tra các event cần thiết, không duyệt qua tất cả các event trong mỗi frame
2. **Đơn giản**: Dễ triển khai và hiểu
3. **Tiết kiệm tài nguyên**: Giảm số lượng phép kiểm tra trong mỗi frame
4. **Chính xác**: Đảm bảo tất cả các event được kích hoạt đúng thời điểm

#### Xử lý các trường hợp đặc biệt

1. **Animation Loop**: Khi animation lặp lại, hệ thống đặt lại chỉ số event
2. **Chuyển trạng thái giữa chừng**: Khi chuyển trạng thái, hệ thống xây dựng lại hàng đợi event
3. **Xử lý độ chính xác**: Để đảm bảo không bỏ sót event, hệ thống xử lý trường hợp frame bị nhảy

### Sử dụng trong runtime

1. Khi chuyển trạng thái animation, xây dựng hàng đợi event
2. Trong mỗi frame, kiểm tra và kích hoạt các event đến hạn
3. Sử dụng các event để kích hoạt các hành động như phát âm thanh, tạo hiệu ứng, v.v.

## Tối ưu hóa Cache

Hệ thống VAT sử dụng nhiều kỹ thuật để tối ưu hóa cache:

1. **Nén dữ liệu**: Sử dụng các định dạng nén để giảm kích thước dữ liệu
2. **Lazy loading**: Chỉ tải dữ liệu khi cần thiết
3. **Pooling**: Tái sử dụng các đối tượng để giảm việc tạo/hủy
4. **LOD (Level of Detail)**: Sử dụng các phiên bản đơn giản hóa cho các đối tượng ở xa

## Kết luận

Hệ thống cache trong VAT đóng vai trò quan trọng trong việc tối ưu hóa hiệu năng và quản lý dữ liệu animation. Bằng cách sử dụng các loại cache khác nhau và áp dụng các kỹ thuật tối ưu hóa, hệ thống có thể xử lý số lượng lớn các đối tượng được animation mà vẫn duy trì hiệu năng tốt.

## Quy trình sử dụng Cache

### 1. Khởi tạo Cache

- **Editor Time**:
  - Các cache dạng ScriptableObject (VertexCacheData, ColliderCacheData, StateInforsCache) được tạo và lưu trữ trong Editor
  - Dữ liệu được bake từ animation gốc và lưu vào các cache này

- **Runtime (Awake/Start)**:
  - CylinderCacheData được khởi tạo và đăng ký với VAT_ColliderManager
  - Các texture được chuyển từ mảng byte về dạng texture để sử dụng trong shader

### 2. Sử dụng Cache

- **VertexUpdate**:
  - Đọc dữ liệu từ VertexCacheData
  - Cập nhật vị trí vertex dựa trên frame hiện tại

- **ColliderUpdate**:
  - Đọc dữ liệu từ ColliderCacheData
  - Cập nhật vị trí và kích thước collider dựa trên frame hiện tại

- **AnimationController**:
  - Đọc dữ liệu từ StateInforsCache
  - Quản lý việc chuyển đổi giữa các trạng thái animation

- **VAT_Physics**:
  - Sử dụng CylinderCacheData để thực hiện các phép kiểm tra va chạm
  - Xử lý các sự kiện va chạm và damage

## Mở rộng hệ thống Cache

Để hỗ trợ các tính năng mới, cần mở rộng hệ thống cache:

1. **BlendTree**:
   - Cần cache thông tin về các animation trong blend tree
   - Lưu trữ các tham số blend và trọng số

2. **RootMotion**:
   - Cache thông tin về chuyển động gốc của nhân vật
   - Tính toán và lưu trữ vị trí và hướng của nhân vật theo từng frame

3. **Ragdoll**:
   - Cache thông tin về các khớp và constraint cho ragdoll
   - Lưu trữ các tham số vật lý cho Verlet Integration 