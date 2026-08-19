namespace SmartBoardingHouse.Common
{
    public class Enums
    {
        public enum Role
        {
            Admin = 0,
            Tenant = 1
        }
        public enum ContractStatus
        {
            Active = 0,
            Expired = 1,
            Terminated = 2
        }
        public enum RoomStatus
        {
            Available = 0,
            Occupied = 1,
            InActive = 2
        }
        public enum InvoiceStatus
        {
            Pending = 0,
            Paid = 1,
            Cancelled = 2,
            Unpaid = 3,
            Overdue = 4
        }
        public enum MaintenanceStatus
        {
            Pending = 0,
            Processing = 1,
            Completed = 2,
            Canceled = 3
        }

        public enum PriotyRequest
        {
            Low = 0,
            Medium = 1,
            High = 2,
            Immediate = 3
        }

        public enum ActivityType
        {
            Payment,    
            CheckOut,   
            CheckIn,     
            Maintenance 
        }

        public enum MeterType
        {
            Electric = 0,
            Water = 1
        }

        public enum MessageType
        {
            Text = 0,
            Image = 1,
        }

        public enum MaintenanceCategory
        {
            Electrical = 0,
            Plumbing = 1,
            Furniture = 2,
            Other = 3
        }

        public enum NotificationType
        {
            General = 0,
            Invoice = 1,
            Debt = 2,
            Maintenance = 3,
            Message = 4,
            Contract = 5
        }

        public enum InvoiceType
        {
            Rent = 0,
            Deposit = 1
        }
    }
}
