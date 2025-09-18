﻿namespace RMS.Models.DTOs.Users
{
    public class UserDto
    {
        public string Id { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
