import { ISettingGroupDefinition } from '../../models/system-setting';

export const SYSTEM_SETTING_GROUPS: ISettingGroupDefinition[] = [
    {
        module: 'SaleBill',
        label: 'Bán hàng',
        icon: 'pi pi-shopping-cart',
        description: 'Cấu hình các tính năng sử dụng trong màn hình bán hàng và hóa đơn.',
        items: [
            // {
            //     Code: 'sale-bill-manual-tax',
            //     Name: 'Thuế thủ công',
            //     Value: 'true',
            //     DataType: 'Boolean',
            //     Description: '',
            //     IsActive: true
            // },
            // {
            //     Code: 'sale-bill-gold-hour',
            //     Name: 'Bán giờ vàng',
            //     Value: 'true',
            //     DataType: 'Boolean',
            //     Description: '',
            //     IsActive: true
            // },
            // {
            //     Code: 'sale-bill-sell-gift-card',
            //     Name: 'Bán, nạp thẻ tặng',
            //     Value: 'true',
            //     DataType: 'Boolean',
            //     Description: '',
            //     IsActive: true
            // },
            // {
            //     Code: 'sale-bill-approve-sale',
            //     Name: 'Duyệt bán hàng',
            //     Value: 'true',
            //     DataType: 'Boolean',
            //     Description: '',
            //     IsActive: true
            // },
            // {
            //     Code: 'sale-bill-require-case-code',
            //     Name: 'Bán thẻ buộc mã vụ việc',
            //     Value: 'false',
            //     DataType: 'Boolean',
            //     Description: '',
            //     IsActive: false
            // },
            // {
            //     Code: 'sale-bill-manual-discount',
            //     Name: 'Chiết khấu thủ công',
            //     Value: 'true',
            //     DataType: 'Boolean',
            //     Description: '',
            //     IsActive: true
            // },
            // {
            //     Code: 'sale-bill-edit-price',
            //     Name: 'Sửa giá bán',
            //     Value: 'true',
            //     DataType: 'Boolean',
            //     Description: '',
            //     IsActive: true
            // },
            // {
            //     Code: 'sale-bill-send-balance-sms',
            //     Name: 'Gửi SMS số dư',
            //     Value: 'true',
            //     DataType: 'Boolean',
            //     Description: '',
            //     IsActive: true
            // },
            // {
            //     Code: 'sale-bill-require-customer-card',
            //     Name: 'Bán thẻ buộc thẻ hội viên',
            //     Value: 'true',
            //     DataType: 'Boolean',
            //     Description: '',
            //     IsActive: true
            // },
            // {
            //     Code: 'sale-bill-recharge-service-card',
            //     Name: 'Nạp thẻ dịch vụ',
            //     Value: 'true',
            //     DataType: 'Boolean',
            //     Description: '',
            //     IsActive: true
            // },
            // {
            //     Code: 'sale-service-used-otp',
            //     Name: 'Dùng package buộc OTP',
            //     Value: 'true',
            //     DataType: 'Boolean',
            //     Description: '',
            //     IsActive: true
            // },
            {
                Code: 'sale-require-kitchen',
                Name: 'Bán hàng bắt buộc gửi bếp/bar',
                Value: 'true',
                DataType: 'Boolean',
                Description: 'Bật tính năng này nếu quán muốn nhân viên phục vụ bắt buộc phải gửi bếp/bar thì mới được in phiếu tính tiền và thanh toán.',
                IsActive: true
            },
            {
                Code: 'sale-enable-kitchen-print',
                Name: 'Bật/Tắt in Bếp/Bar',
                Value: 'true',
                DataType: 'Boolean',
                Description: 'Bật: Tự động in phiếu Báo Bếp/Bar khi gửi món hoặc hủy món vào Bếp/Bar. Tắt: Chỉ gửi dữ liệu không phát lệnh in.',
                IsActive: true
            },
            {
                Code: 'kitchen-print-by-area',
                Name: 'In theo khu vực chế biến (Bếp, Bar...)',
                Value: 'true',
                DataType: 'Boolean',
                Description: 'Khi bật: tách riêng phiếu in theo từng khu vực chế biến đã cấu hình. Khi tắt: in chung tất cả món vào 1 phiếu.',
                IsActive: true
            },
            {
                Code: 'kitchen-default-area-id',
                Name: 'Máy in / Khu vực chế biến mặc định',
                Value: '',
                DataType: 'String',
                Description: 'Máy in / Khu vực được sử dụng mặc định khi món ăn chưa được gán khu vực chế biến riêng.',
                IsActive: true
            },
            {
                Code: 'sale-enable-invoice-date',
                Name: 'Cho phép chọn Ngày hoá đơn',
                Value: 'true',
                DataType: 'Boolean',
                Description: 'Khi bật: hiển thị ô chọn Ngày hoá đơn khi tạo/sửa đơn (luồng mới). Khi tắt: ẩn ô này và tự động lấy ngày giờ hiện tại (luồng cũ).',
                IsActive: true
            },
            {
                Code: 'invoice-lock-enabled',
                Name: 'Thiết lập sửa hoá đơn',
                Value: 'false',
                DataType: 'Boolean',
                Description: 'Bật để thiết lập khóa ngày sửa hoá đơn theo ngày cố định hoặc tự động lùi số ngày.',
                IsActive: false
            },
            {
                Code: 'invoice-lock-date',
                Name: 'Ngày khóa hoá đơn',
                Value: '',
                DataType: 'String',
                Description: 'Ngày khóa sửa/hủy hóa đơn. Hoá đơn có ngày lập nhỏ hơn hoặc bằng ngày này sẽ không được sửa.',
                IsActive: true
            },
            {
                Code: 'invoice-lock-auto',
                Name: 'Tự động khóa hoá đơn',
                Value: 'true',
                DataType: 'Boolean',
                Description: 'Tự động tính ngày khóa theo số ngày cấu hình.',
                IsActive: true
            },
            {
                Code: 'invoice-lock-auto-days',
                Name: 'Ngày tự động khóa hoá đơn',
                Value: '0',
                DataType: 'Number',
                Description: 'Số ngày tự động lùi so với ngày hiện tại để xác định ngày khóa hoá đơn.',
                IsActive: true
            }
        ]
    },
    {
        module: 'customer',
        label: 'Khách hàng',
        icon: 'pi pi-users',
        description: 'Cấu hình các thao tác xử lý liên quan đến dữ liệu khách hàng.',
        items: [
            {
                Code: 'customer_group3_manual',
                Name: 'Sửa nhóm KH 3',
                Value: 'false',
                DataType: 'Boolean',
                Description: '',
                IsActive: false
            }
        ]
    },
    {
        module: 'accounting',
        label: 'Kế toán & Hóa đơn',
        icon: 'pi pi-file-edit',
        description: 'Cấu hình các đường dẫn và tính năng liên quan đến Hóa đơn điện tử.',
        items: [
            {
                Code: 'EINVOICE_LANDING_URL',
                Name: 'URL Trang đăng ký hóa đơn của khách hàng',
                Value: 'https://einvoice.ansolution.vn',
                DataType: 'string',
                Description: 'URL dùng để sinh QR Code in trên bill thanh toán cho khách hàng quét tự đăng ký xuất HĐ',
                IsActive: true
            },
            {
                Code: 'SearchCancelledInvoiceDays',
                Name: 'Số ngày tìm kiếm hoá đơn huỷ',
                Value: '30',
                DataType: 'Number',
                Description: 'Số ngày cho phép tìm kiếm hoá đơn/đơn hàng huỷ. Hệ thống chỉ hỗ trợ tra cứu các đơn hàng có ngày tạo trong vòng x ngày gần nhất.',
                IsActive: true
            }
        ]
    },
    {
        module: 'system',
        label: 'Hệ thống & QR Order',
        icon: 'pi pi-qrcode',
        description: 'Cấu hình các đường dẫn và cài đặt hệ thống QR Order.',
        items: [
            {
                Code: 'QrOrderBaseUrl',
                Name: 'Đường dẫn QR Order',
                Value: 'http://order.ansolution.vn',
                DataType: 'string',
                Description: 'URL đường dẫn gốc cho trang QR Order của khách hàng',
                IsActive: true
            }
        ]
    }
];
