using System;
using System.ComponentModel.DataAnnotations;

namespace VEPS_Plus.ViewModels
{
    public class UsersVPN
    {
        public int UserId { get; set; }
        public string? PrivateKey { get; set; }
        public string? PublicKey { get; set; }
        public string? VPN_IP { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
