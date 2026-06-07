Ameko được thiết kế xoay quanh các Dự án (Projects). Mặc dù việc sử dụng Dự án là không bắt buộc và bạn hoàn toàn có thể chỉnh sửa phụ đề cũng như sử dụng Ameko mà không cần đến chúng, nhưng các Dự án được thiết kế để giúp việc thao tác với nhiều tệp phụ đề trở nên dễ dàng hơn, đặc biệt là khi cộng tác với người khác.

## Trình duyệt Dự án

![](../assets/project-explorer-empty.png)

Khi bạn mở Ameko lần đầu, Trình duyệt Dự án sẽ nằm ở bên trái, liệt kê các tài liệu đang mở. Khi chưa tải tệp dự án nào, Dự án Mặc định (Default Project) sẽ đóng vai trò là nơi chứa tạm thời cho các tệp bạn mở trong phiên làm việc. Bạn có thể lưu dự án thành một tệp nếu muốn tận dụng các lợi ích của việc sử dụng tệp dự án.

## Mở một Thư mục dưới dạng Dự án

Nếu bạn đã có một thư mục dự án được thiết lập sẵn trên máy, bạn có thể đưa cấu trúc đó vào Ameko bằng cách mở thư mục đó dưới dạng một dự án.
Việc này sẽ tải tất cả các thư mục con và tệp phụ đề phù hợp vào Trình duyệt Dự án, nơi bạn có thể tuỳ chỉnh nội dung và lưu dự án kết quả thành một tệp.

## Key Names and Phrases

Chances are, your project has names and phrases you want to keep consistent throughout the show. Projects can have a Key
Names & Phrases (KNP) bible to help keep everyone on track:

![](../assets/knp-window.png)

Terms that appear in the script or reference files will appear in the Editing Area. See the User Interface tab for more
details.

## Tên hiển thị và Cách sử dụng

Mặc dù cấu trúc và tên trong Dự án _có thể_ phản ánh chính xác các tệp trên ổ đĩa, nhưng bạn hoàn toàn có thể sắp xếp và đổi tên chúng bên trong dự án theo ý muốn mà không làm ảnh hưởng đến các tệp gốc. Ví dụ, hãy xem xét cấu trúc phân cấp phẳng (flat hierarchy) sau đây với các tên tệp rất dài dòng:

```
Kono Bijutsubu ni wa Mondai ga Aru/
  [AMK] Konobi - 01 - Dialogue.ass
  [AMK] Konobi - 01 - Typesetting1.ass
  [AMK] Konobi - 01 - Typesetting2.ass
  Konobi - 01 - Captions.ja.srt
  [AMK] Konobi - 02 - Dialogue.ass
  [AMK] Konobi - 02 - Typesetting1.ass
  [AMK] Konobi - 02 - Typesetting2.ass
  Konobi - 02 - Captions.ja.srt
```

Cấu trúc này có thể được sắp xếp lại và làm gọn bên trong dự án bằng cách sử dụng tên hiển thị và thư mục ảo mà không làm lộn xộn các tệp hiện có:

```
01/
  Dialogue.ass
  TS1.ass
  TS2.ass
  Captions.srt
02/
  Dialogue.ass
  TS1.ass
  TS2.ass
  Captions.srt
```

## Cấu hình Dự án

![](../assets/project-config.png)

Một trong những lợi ích chính của việc sử dụng Dự án khi làm việc nhóm là cấu hình được đồng bộ hoá. Các tuỳ chọn được thiết lập trong Cấu hình Dự án sẽ ghi đè lên các tùy chọn cá nhân của người dùng khi dự án được tải. Điều này rất tuyệt vời để giữ cho ngưỡng cảnh báo CPS của mọi người giống nhau, và quan trọng hơn là duy trì một từ điển kiểm tra chính tả chung cũng như đảm bảo mọi người đang sử dụng cùng một ngôn ngữ kiểm tra chính tả. Ví dụ: nếu dự án được thiết lập để dùng tiếng Anh (Anh/UK), thì mọi người đều sẽ dùng tiếng Anh (Anh), và chữ "u" trong từ "colour" sẽ không bị bỏ sót hay báo lỗi nhầm.

![](../assets/project-install-dictionary.png)

Người dùng sẽ được nhắc tải xuống từ điển phù hợp nếu họ chưa có sẵn.

![](../assets/spellcheck.png)

Các từ mới cũng có thể được thêm trực tiếp vào từ điển của dự án ngay từ trình kiểm tra chính tả.

## Tích hợp Git

![](../assets/git-toolbox.png)

Khi được lưu ở thư mục gốc của dự án (ngay cạnh thư mục `.git`), các tệp Dự án cho phép truy cập dễ dàng vào các chức năng Git cơ bản, như commit, push, pull và xem danh sách các commit gần đây.
