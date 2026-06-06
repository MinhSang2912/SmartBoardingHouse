namespace SmartBoardingHouse.Common
{
    public class Enums
    {
        public enum ContractStatus
        {
            Active = 0,
            Expired = 1,
            Canceled = 2
        }
        public enum RoomStatus
        {
            Available = 0,
            Occupied = 1,
            Maintenance = 2
        }
        public enum InvoiceStatus
        {
            Unpaid = 0,
            Paid = 1,
            Overdue = 2
        }
        public enum MaintenanceStatus
        {
            Pending = 0,
            InProgress = 1,
            Completed = 2,
            Canceled = 3
        }
    }
}
