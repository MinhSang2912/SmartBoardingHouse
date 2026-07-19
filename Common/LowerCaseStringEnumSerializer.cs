using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace SmartBoardingHouse.Common
{
    /// <summary>
    /// Serializer dùng chung cho tất cả enum trong hệ thống.
    /// Lý do cần cái này: DB được dùng chung với 1 backend Node.js/Mongoose khác
    /// (xem comment trong BaseModel.cs), và Mongoose lưu enum dưới dạng chuỗi
    /// thường ("paid", "active", "occupied",...) chứ không phải số như mặc định
    /// của MongoDB .NET Driver (Paid = 1 -> lưu int 1).
    ///
    /// Nếu không có serializer này, mọi query so sánh enum từ C# (vd:
    /// Status == InvoiceStatus.Paid) sẽ sinh ra filter { status: 1 }, không khớp
    /// với dữ liệu thực tế đang lưu { status: "paid" } -> luôn trả về 0 kết quả.
    ///
    /// - Ghi (Serialize): luôn ghi ra chuỗi thường (vd: InvoiceStatus.Paid -> "paid")
    ///   để khớp với convention của Mongoose.
    /// - Đọc (Deserialize): hỗ trợ cả 2 kiểu để không phá dữ liệu cũ:
    ///     + String (không phân biệt hoa/thường): "paid", "Paid", "PAID" đều ra InvoiceStatus.Paid
    ///     + Int32: dữ liệu enum số cũ (nếu có) vẫn đọc được bình thường
    /// </summary>
    public class LowerCaseStringEnumSerializer<TEnum> : SerializerBase<TEnum> where TEnum : struct, Enum
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnum value)
        {
            context.Writer.WriteString(value.ToString().ToLowerInvariant());
        }

        public override TEnum Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();

            switch (bsonType)
            {
                case BsonType.String:
                    var strValue = context.Reader.ReadString();
                    if (Enum.TryParse<TEnum>(strValue, ignoreCase: true, out var parsed))
                        return parsed;
                    throw new FormatException(
                        $"Không thể chuyển giá trị '{strValue}' sang enum {typeof(TEnum).Name}");

                case BsonType.Int32:
                    var intValue = context.Reader.ReadInt32();
                    return (TEnum)Enum.ToObject(typeof(TEnum), intValue);

                case BsonType.Int64:
                    var longValue = context.Reader.ReadInt64();
                    return (TEnum)Enum.ToObject(typeof(TEnum), longValue);

                case BsonType.Null:
                    context.Reader.ReadNull();
                    return default;

                default:
                    throw new FormatException(
                        $"Kiểu BSON '{bsonType}' không được hỗ trợ khi đọc enum {typeof(TEnum).Name}");
            }
        }
    }
}
