namespace SmartBoardingHouse.Common
{
    public class Message
    {
        public static string Created(string entity) => $"{entity} đã được tạo thành công.";
        public static string NotFound(string entity) => $"{entity} không tìm thấy";
        public static string Updated(string entity) => $"{entity} đã được cập nhật thành công.";
        public static string Deleted(string entity) => $"{entity} đã được xóa thành công.";
        public static string IdExists(string entity, int id) => $"{entity} với Id {id} đã tồn tại.";

        #region User
        public static string UserIdIsRequired() => "Id là bắt buộc.";
        public static string UserNameIsRequired() => "Tên là bắt buộc.";
        public static string UserPasswordIsRequired() => "Mật khẩu là bắt buộc.";
        public static string UserRoleIsRequired() => "Vai trò là bắt buộc.";
        public static string UserRoleIsInvalid() => "Giá trị vai trò không hợp lệ.";
        public static string UserIDCardNumberIsRequired() => "Số CMND/CCCD là bắt buộc.";
        public static string UserIDCardNumberIsTooShort() => "Số CMND/CCCD phải có ít nhất 10 ký tự.";
        public static string UserIDCardNumberExists(string idCardNumber) => $"Người dùng với số CMND/CCCD '{idCardNumber}' đã tồn tại.";
        public static string UserHasActiveContract() => "Người dùng này đang có hợp đồng hoạt động.";

        #endregion

        #region Room
        public static string RoomNumberIsRequired() => "Số phòng là bắt buộc.";
        public static string RoomPriceMustBeGreaterThanZero() => "Giá thuê phải lớn hơn 0.";
        public static string RoomAreaMustBeGreaterThanZero() => "Diện tích phải lớn hơn 0.";
        public static string RoomDepositMustBeNonNegative() => "Tiền đặt cọc phải lớn hơn hoặc bằng 0.";
        public static string RoomFloorIdIsRequired() => "Tầng là bắt buộc.";
        public static string RoomStatusIsInvalid() => "Trạng thái phòng không hợp lệ.";
        public static string RoomNumberExists() => $"Phòng đã tồn tại.";
        public static string RoomHasActiveContract() => $"Phòng đang có hợp đồng hoạt động.";
        #endregion

        #region Floor
        public static string FloorNumberIsRequired() => "Số tầng là bắt buộc.";
        public static string FloorRoomCountMustBeNonNegative() => "Số lượng phòng phải lớn hơn hoặc bằng 0.";
        public static string FloorNumberExists(int floorNumber) => $"Tầng số '{floorNumber}' đã tồn tại.";
        public static string FloorHasRooms() => "Tầng này vẫn còn phòng.";
        #endregion

        #region Contract
        public static string ContractNumberIsRequired() => "Mã hợp đồng là bắt buộc.";
        public static string ContractRoomNumberIsRequired() => "Số phòng là bắt buộc.";
        public static string ContractRoomIsExists() => "Phòng này đã có hợp đồng.";
        public static string ContractTenantNameIsRequired() => "Tên người thuê là bắt buộc.";
        public static string ContractTenantIsExists() => "Người thuê này đã có hợp đồng.";
        public static string ContractStartDateIsRequired() => "Ngày bắt đầu là bắt buộc.";
        public static string ContractEndDateIsRequired() => "Ngày kết thúc là bắt buộc.";
        public static string ContractStartDateMustBeBeforeEndDate() => "Ngày bắt đầu phải trước ngày kết thúc.";
        public static string ContractEndDateMustBeAfterStartDate() => "Ngày kết thúc phải sau ngày bắt đầu.";
        public static string ContractPaymentDateIsInvalid() => "Ngày thanh toán phải nằm trong khoảng từ 1 đến 31.";
        public static string ContractStatusIsInvalid() => "Trạng thái hợp đồng không hợp lệ.";
        public static string ContractNumberExists(string contractNumber) => $"Hợp đồng số '{contractNumber}' đã tồn tại.";
        #endregion

        #region Invoice
        public static string InvoiceNumberExists() => "Số hóa đơn đã tồn tại.";
        public static string InvoiceIsExists() => "Hóa đơn đã tồn tại.";
        public static string InvoiceNumberIsRequired() => "Số hóa đơn là bắt buộc.";
        public static string InvoiceRoomNumberIsRequired() => "Số phòng là bắt buộc.";
        public static string InvoiceDueDateIsRequired() => "Ngày đến hạn là bắt buộc.";
        public static string InvoiceStatusIsInvalid() => "Trạng thái hóa đơn không hợp lệ.";
        public static string InvoiceBillingMonthIsInvalid() => "Tháng tính tiền phải nằm trong khoảng từ 1 đến 12.";
        public static string InvoiceBillingYearIsInvalid() => "Năm tính tiền phải lớn hơn 2000.";
        public static string ElectricUsageIsInvalid() => "Chỉ số điện phải lớn hơn hoặc bằng 0.";
        public static string ElectricPriceMustBeGreaterThanZero() => "Giá điện phải lớn hơn 0.";
        public static string WaterUsageIsInvalid() => "Chỉ số nước phải lớn hơn hoặc bằng 0.";
        public static string WaterPriceMustBeGreaterThanZero() => "Giá nước phải lớn hơn 0.";
        public static string ServiceFeeIsInvalid() => "Phí dịch vụ phải lớn hơn hoặc bằng 0.";
        public static string InvoiceAlreadyPaid() => "Hóa đơn đã được thanh toán trước đó.";
        #endregion

        #region MaintenanceRequest
        public static string MaintenanceRoomNumberIsRequired() => "Số phòng là bắt buộc.";
        public static string MaintenanceTenantNameIsRequired() => "Tên người thuê là bắt buộc.";
        public static string MaintenanceRequestNumberIsRequired() => "Mã yêu cầu bảo trì là bắt buộc.";
        public static string MaintenanceTitleIsRequired() => "Tiêu đề là bắt buộc.";
        public static string MaintenanceTitleIsTooLong() => "Tiêu đề không được vượt quá 200 ký tự.";
        public static string MaintenanceDescriptionIsRequired() => "Mô tả là bắt buộc.";
        public static string MaintenanceDescriptionIsTooLong() => "Mô tả không được vượt quá 1000 ký tự.";
        public static string MaintenanceStatusIsInvalid() => "Trạng thái yêu cầu bảo trì không hợp lệ.";
        public static string MaintenancePriorityIsInvalid() => "Mức độ ưu tiên không hợp lệ.";
        public static string MaintenanceRequestExists(string roomNumber, string title) => $"Yêu cầu bảo trì cho phòng '{roomNumber}' với tiêu đề '{title}' đã tồn tại.";
        #endregion

        #region MeterReading
        public static string MeterReadingRoomNumberIsRequired() => "Số phòng là bắt buộc.";
        public static string MeterReadingMonthIsInvalid() => "Tháng phải nằm trong khoảng từ 1 đến 12.";
        public static string MeterReadingYearIsInvalid() => "Năm phải từ 2020 trở đi.";
        public static string MeterReadingElectricityIndexMustBeNonNegative() => "Chỉ số điện phải lớn hơn hoặc bằng 0.";
        public static string MeterReadingWaterIndexMustBeNonNegative() => "Chỉ số nước phải lớn hơn hoặc bằng 0.";
        public static string MeterReadingAlreadyExists() => "Số công tơ này đã có";
        public static string MeterReadingThisMonthMuchHighterLastMonth() => "Số tháng này phải lớn hơn số trước";
        public static string MeterReadingRoomNotOccupied() => "Phòng này không có người thuê";
        #endregion
    }
}