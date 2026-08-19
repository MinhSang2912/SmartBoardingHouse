using System.Collections.Generic;

namespace SmartBoardingHouse.Models.Response
{
    public class PagedResult<T>
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public List<T> Items { get; set; } = new();
    }
}
