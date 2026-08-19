using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SmartBoardingHouse.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }

        public async Task SendTenantAccountAsync(string toEmail, string tenantName, string password)
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderName = _config["Brevo:SenderName"] ?? "Quản lý phòng trọ";
            var senderEmail = _config["Brevo:SenderEmail"];


            var payload = new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = toEmail, name = tenantName } },
                subject = "Thông tin tài khoản đăng nhập phòng trọ",
                htmlContent = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                        <h2 style='color: #2196F3;'>Tài khoản phòng trọ đã được tạo</h2>
                        <p>Kính gửi anh/chị <strong>{tenantName}</strong>,</p>
                        <p>Tài khoản của anh/chị trên hệ thống <strong>SmartBoardingHouse</strong> đã được tạo thành công bởi chủ nhà.</p>
                        <p>Dưới đây là thông tin đăng nhập của anh/chị:</p>
                        <table style='background:#f5f5f5; padding:16px; border-radius:6px; width:100%;'>
                            <tr><td><strong>Email đăng nhập:</strong></td><td>{toEmail}</td></tr>
                            <tr><td><strong>Mật khẩu:</strong></td><td style='font-size:18px; color:#2196F3; letter-spacing:2px;'>{password}</td></tr>
                        </table>
                        <p style='margin-top:16px;'>Vui lòng đăng nhập và thay đổi mật khẩu của mình để bảo mật thông tin.</p>
                        <hr/>
                        <small style='color:#888;'>Email được gửi tự động từ hệ thống SmartBoardingHouse.</small>
                    </div>
                "
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Headers.Add("accept", "application/json");

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Brevo API error: {response.StatusCode} - {errorContent}");
            }
        }

        public async Task SendContractEmailAsync(
            string toEmail, string tenantName, string contractNumber,
            string roomNumber, string startDate, string endDate,
            string price, string deposit, int paymentDate)
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderName = _config["Brevo:SenderName"] ?? "Quản lý phòng trọ";
            var senderEmail = _config["Brevo:SenderEmail"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(senderEmail))
                return;

            var payload = new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = toEmail, name = tenantName } },
                subject = $"Hợp đồng thuê phòng {roomNumber} - {contractNumber}",
                htmlContent = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 650px; margin: auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                        <div style='background: #1565C0; color: white; padding: 24px; text-align: center;'>
                            <h1 style='margin:0; font-size:20px;'>CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</h1>
                            <p style='margin:4px 0; font-size:13px;'>Độc lập – Tự do – Hạnh phúc</p>
                            <h2 style='margin: 16px 0 0; font-size: 22px;'>HỢP ĐỒNG THUÊ PHÒNG TRỌ</h2>
                            <p style='margin: 4px 0; font-size: 13px;'>Mã hợp đồng: <strong>{contractNumber}</strong></p>
                        </div>
                        <div style='padding: 24px;'>
                            <p>Kính gửi anh/chị <strong>{tenantName}</strong>,</p>
                            <p>Hợp đồng thuê phòng của anh/chị đã được tạo thành công. Chi tiết hợp đồng như sau:</p>
                            <table style='width:100%; border-collapse: collapse; margin-top: 16px;'>
                                <tr style='background:#f5f5f5;'><td style='padding:10px; border:1px solid #ddd; font-weight:bold;'>Số phòng</td><td style='padding:10px; border:1px solid #ddd;'>Phòng {roomNumber}</td></tr>
                                <tr><td style='padding:10px; border:1px solid #ddd; font-weight:bold;'>Người thuê</td><td style='padding:10px; border:1px solid #ddd;'>{tenantName}</td></tr>
                                <tr style='background:#f5f5f5;'><td style='padding:10px; border:1px solid #ddd; font-weight:bold;'>Ngày bắt đầu</td><td style='padding:10px; border:1px solid #ddd;'>{startDate}</td></tr>
                                <tr><td style='padding:10px; border:1px solid #ddd; font-weight:bold;'>Ngày kết thúc</td><td style='padding:10px; border:1px solid #ddd;'>{endDate}</td></tr>
                                <tr style='background:#f5f5f5;'><td style='padding:10px; border:1px solid #ddd; font-weight:bold;'>Tiền thuê/tháng</td><td style='padding:10px; border:1px solid #ddd; color:#1565C0; font-weight:bold;'>{price} ₫</td></tr>
                                <tr><td style='padding:10px; border:1px solid #ddd; font-weight:bold;'>Tiền đặt cọc</td><td style='padding:10px; border:1px solid #ddd;'>{deposit} ₫</td></tr>
                                <tr style='background:#f5f5f5;'><td style='padding:10px; border:1px solid #ddd; font-weight:bold;'>Ngày thu tiền cố định</td><td style='padding:10px; border:1px solid #ddd;'>Ngày {paymentDate} hàng tháng</td></tr>
                            </table>
                            <p style='margin-top: 20px;'>Vui lòng đăng nhập vào ứng dụng để xem chi tiết và ký xác nhận hợp đồng.</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                            <small style='color:#888;'>Email được gửi tự động từ hệ thống SmartBoardingHouse.</small>
                        </div>
                    </div>
                "
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Headers.Add("accept", "application/json");
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Brevo API error: {response.StatusCode} - {errorContent}");
            }
        }
    }
}
