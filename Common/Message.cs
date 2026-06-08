namespace SmartBoardingHouse.Common
{
    public class Message
    {
        public static string Created(string entity) => $"{entity} created successfully.";
        public static string NotFound(string entity) => $"{entity} not found.";
        public static string Deleted(string entity) => $"{entity} deleted successfully.";
        public static string IdExists(string entity, int id) => $"{entity} with Id {id} already exists.";

        #region User
        public static string UserIdIsRequired() => $"Id is required.";
        public static string UserNameIsRequired() => $"Name is required.";
        public static string UserPasswordIsRequired() => $"Password is required.";
        public static string UserRoleIsRequired() => $"Role is required.";
        public static string UserRoleIsInvalid() => $"Invalid role value.";
        public static string UserIDCardNumberIsRequired() => $"ID card number is required.";
        public static string UserIDCardNumberIsTooShort() => $"ID card number must be at least 10 characters long.";
        #endregion

        #region Room
        public static string RoomNumberIsRequired() => "Room number is required.";
        public static string RoomPriceMustBeGreaterThanZero() => "Price must be greater than 0.";
        public static string RoomAreaMustBeGreaterThanZero() => "Area must be greater than 0.";
        public static string RoomDepositMustBeNonNegative() => "Room deposit must be greater than or equal to 0.";
        public static string RoomFloorIdIsRequired() => "Floor is required.";
        public static string RoomStatusIsInvalid() => "Invalid room status.";
        public static string RoomNumberExists(string roomNumber) => $"Room with number '{roomNumber}' already exists.";
        #endregion

        #region Floor
        public static string FloorNumberIsRequired() => "Floor number is required.";
        public static string FloorRoomCountMustBeNonNegative() => "Room count must be greater than or equal to 0.";
        public static string FloorNumberExists(string floorNumber) => $"Floor with number '{floorNumber}' already exists.";
        #endregion

        #region Contract
        public static string ContractNumberIsRequired() => "Contract number is required.";
        public static string ContractRoomNumberIsRequired() => "Room number is required.";
        public static string ContractTenantNameIsRequired() => "Tenant name is required.";
        public static string ContractStartDateIsRequired() => "Start date is required.";
        public static string ContractEndDateIsRequired() => "End date is required.";
        public static string ContractStartDateMustBeBeforeEndDate() => "Start date must be before end date.";
        public static string ContractEndDateMustBeAfterStartDate() => "End date must be after start date.";
        public static string ContractPaymentDateIsInvalid() => "Payment date must be between 1 and 31.";
        public static string ContractStatusIsInvalid() => "Invalid contract status.";
        public static string ContractNumberExists(string contractNumber) => $"Contract with number '{contractNumber}' already exists.";
        #endregion

        #region Invoice
        public static string InvoiceRoomNumberIsRequired() => "Room number is required.";
        public static string InvoiceAmountMustBeGreaterThanZero() => "Invoice amount must be greater than 0.";
        public static string InvoiceDueDateIsRequired() => "Due date is required.";
        public static string InvoiceDueDateMustBeInFuture() => "Due date must be today or in the future.";
        public static string InvoiceStatusIsInvalid() => "Invalid invoice status.";
        #endregion

        #region MaintenanceRequest
        public static string MaintenanceRoomNumberIsRequired() => "Room number is required.";
        public static string MaintenanceTenantNameIsRequired() => "Tenant name is required.";
        public static string MaintenanceTitleIsRequired() => "Title is required.";
        public static string MaintenanceTitleIsTooLong() => "Title must not exceed 200 characters.";
        public static string MaintenanceDescriptionIsRequired() => "Description is required.";
        public static string MaintenanceDescriptionIsTooLong() => "Description must not exceed 1000 characters.";
        public static string MaintenanceStatusIsInvalid() => "Invalid maintenance status.";
        public static string MaintenanceRequestExists(string roomNumber, string title) => $"Maintenance request for room '{roomNumber}' with title '{title}' already exists.";
        #endregion

        #region MeterReading
        public static string MeterReadingRoomNumberIsRequired() => "Room number is required.";
        public static string MeterReadingMonthIsInvalid() => "Month must be between 1 and 12.";
        public static string MeterReadingYearIsInvalid() => "Year must be 2020 or later.";
        public static string MeterReadingElectricityIndexMustBeNonNegative() => "Electricity index must be greater than or equal to 0.";
        public static string MeterReadingWaterIndexMustBeNonNegative() => "Water index must be greater than or equal to 0.";
        public static string MeterReadingAlreadyExists(string roomNumber, int month, int year) =>
            $"Meter reading for room '{roomNumber}' in {month}/{year} already exists.";
        #endregion
    }
}
