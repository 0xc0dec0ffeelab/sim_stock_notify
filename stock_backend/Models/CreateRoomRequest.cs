using System.ComponentModel.DataAnnotations;

namespace stock_backend.Models
{
    public class CreateRoomRequest
    {
        public string RoomName { get; set; } = string.Empty;
        public string ChatType { get; set; } = string.Empty;
    }
}
